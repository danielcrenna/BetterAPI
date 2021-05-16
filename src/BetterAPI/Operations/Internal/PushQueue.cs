// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using BetterAPI.Serialization;

namespace BetterAPI.Operations.Internal
{
    internal sealed class PushQueue<T> : IDisposable
    {
        private readonly IDictionary<ulong, int> _attempts = new ConcurrentDictionary<ulong, int>();
        private readonly SemaphoreSlim _empty;

        private readonly bool _internal;
        private readonly Stopwatch _uptime;

        private Task _background;
        private Func<T, bool> _backlogConsumer;
        private BlockingCollection<T> _buffer;
        private CancellationTokenSource _cancel;

        private Func<T, bool> _consumer;
        private Action<Exception> _errorConsumer;

        private int _sent;
        private Action<T> _undeliverableConsumer;
        private int _undelivered;

        public PushQueue(IObservable<T> source) : this()
        {
            Produce(source);

            OnStarted = () => { };
            OnStopped = () => { };
        }

        public PushQueue() : this(new BlockingCollection<T>(), true)
        {
        }

        public PushQueue(int capacity) : this(new BlockingCollection<T>(capacity), true)
        {
        }

        public PushQueue(IProducerConsumerCollection<T> source) : this(new BlockingCollection<T>(source),
            true)
        {
        }

        public PushQueue(IProducerConsumerCollection<T> source, int capacity) : this(
            new BlockingCollection<T>(source, capacity), true)
        {
        }

        public PushQueue(BlockingCollection<T> source) : this(source, false)
        {
        }

        private PushQueue(BlockingCollection<T> source, bool @internal = true)
        {
            _buffer = source;
            _internal = @internal;

            MaxDegreeOfParallelism = 1;

            _uptime = new Stopwatch();
            _cancel = new CancellationTokenSource();
            _empty = new SemaphoreSlim(1);

            _consumer = m => true;
            _backlogConsumer = m => true;
            _undeliverableConsumer = m => { };
        }

        public int MaxDegreeOfParallelism { get; set; }

        public TimeSpan Uptime => _uptime.Elapsed;

        public int Sent => _sent;

        public double Rate => _sent / _uptime.Elapsed.TotalSeconds;

        public int Queued => _buffer.Count;

        public int Undeliverable => _undelivered;

        public bool Running { get; private set; }

        public Action OnStarted { get; set; }

        public Action OnStopped { get; set; }

        public RetryPolicy RetryPolicy { get; set; }

        public RateLimitPolicy RateLimitPolicy { get; set; }

        public Func<T, ulong> HashFunction => x => ValueHash.ComputeHash(x);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Produce(T message)
        {
            if (_buffer.IsAddingCompleted)
                HandleBacklog(message);
            else
                _buffer.Add(message);
        }

        public void Produce(T message, int attempts)
        {
            if (_buffer.IsAddingCompleted)
            {
                HandleBacklog(message);
            }
            else
            {
                if (RetryPolicy != null) _attempts[HashMessage(message)] = attempts;

                _buffer.Add(message);
            }
        }

        public void Produce(IList<T> messages)
        {
            if (messages.Count == 0) return;

            foreach (var message in messages) Produce(message);
        }

        public void Produce(IEnumerable<T> stream, TimeSpan? interval = null)
        {
            var projection = new Func<IEnumerable<T>>(() => stream).AsContinuousObservable();

            if (interval.HasValue)
                Produce(projection.Buffer(interval.Value));
            else
                Produce(projection);
        }

        public void Produce(Func<T> func, TimeSpan? interval = null)
        {
            if (_buffer.IsAddingCompleted)
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");

            func.AsContinuousObservable(interval)
                .Subscribe(Produce, e => { _errorConsumer?.Invoke(e); }, () => { }, _cancel.Token);
        }

        public void Produce(IObservable<T> observable)
        {
            if (_buffer.IsAddingCompleted)
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");

            observable.Subscribe(Produce, e => { _errorConsumer?.Invoke(e); }, () => { }, _cancel.Token);
        }

        public void Produce(IObservable<IList<T>> observable)
        {
            if (_buffer.IsAddingCompleted)
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");

            observable.Subscribe(Produce, e => { _errorConsumer?.Invoke(e); }, () => { }, _cancel.Token);
        }

        public void Produce(IObservable<IObservable<T>> observable)
        {
            if (_buffer.IsAddingCompleted)
                throw new InvalidOperationException("You cannot subscribe the buffer while stopping");

            observable.Subscribe(Produce, e => { _errorConsumer?.Invoke(e); }, () => { }, _cancel.Token);
        }

        public void Attach(Func<T, bool> consumer)
        {
            _consumer = consumer;
        }

        public void AttachError(Action<Exception> onError)
        {
            _errorConsumer = onError;
        }

        public void Start(bool immediate = false)
        {
            if (Running) return;

            if (_background != null)
            {
                Stop(immediate);
                _background?.Dispose();
                _background = null;
            }

            RequisitionBackgroundTask();

            _uptime.Start();
            Running = true;
            OnStarted?.Invoke();
        }

        /// <summary>Stops accepting new messages for immediate delivery. </summary>
        /// <param name="immediate">
        ///     If <code>true</code>, the service immediately redirects all messages in the queue to the
        ///     backlog; emails that are queued after a stop call are always sent to the backlog. Otherwise, all queued messages
        ///     are sent before closing the producer to additional messages.
        /// </param>
        public void Stop(bool immediate = false)
        {
            if (!Running) return;

            _buffer.CompleteAdding();

            if (!immediate)
                WaitForEmptyBuffer();
            else
                FlushBacklog();

            Running = false;
            _uptime.Stop();
            if (_internal) _buffer = new BlockingCollection<T>();

            OnStopped?.Invoke();
        }

        private void WaitForEmptyBuffer()
        {
            _empty.Wait();
            while (!_buffer.IsCompleted) Task.Delay(10).GetAwaiter().GetResult();

            _empty.Release();

            _cancel.Cancel();
            _cancel.Token.WaitHandle.WaitOne();
        }

        private void HandleBacklog(T message)
        {
            if (_backlogConsumer == null) return;

            if (_errorConsumer != null)
            {
                try
                {
                    if (!_backlogConsumer.Invoke(message)) HandleUndeliverable(message);
                }
                catch (Exception e)
                {
                    _errorConsumer.Invoke(e);
                    HandleUndeliverable(message);
                }
            }
            else
            {
                if (!_backlogConsumer.Invoke(message)) HandleUndeliverable(message);
            }
        }

        private void HandleUndeliverable(T message)
        {
            if (_undeliverableConsumer == null) return;

            if (_errorConsumer != null)
                try
                {
                    _undeliverableConsumer.Invoke(message);
                }
                catch (Exception e)
                {
                    _errorConsumer.Invoke(e);
                }
            else
                _undeliverableConsumer.Invoke(message);

            Interlocked.Increment(ref _undelivered);
            if (RetryPolicy != null) _attempts.Remove(HashMessage(message));
        }

        protected void Dispose(bool disposing)
        {
            if (!disposing) return;

            if (Running) Stop();

            if (_background != null && _background.IsCompleted)
                _background?.Dispose();
            _background = null;
            _cancel?.Dispose();
            _cancel = null;
        }

        public void Restart(bool immediate = false)
        {
            Stop(immediate);

            Start(immediate);
        }

        private void RequisitionBackgroundTask()
        {
            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = MaxDegreeOfParallelism, CancellationToken = _cancel.Token
            };

            _background = Task.Run(() =>
            {
                try
                {
                    ProduceOn(GetProductionSource(), options);
                }
                catch (OperationCanceledException)
                {
                    FlushBacklog();
                }
            });
        }

        // ReSharper disable once InconsistentNaming
        private BlockingCollection<T> GetProductionSource()
        {
            BlockingCollection<T> source;
            if (RateLimitPolicy != null && RateLimitPolicy.Enabled)
            {
                // Convert the outgoing blocking collection into a rate limited observable, then feed a new blocking queue with it
                var sequence = _buffer.AsRateLimitedObservable(RateLimitPolicy.Occurrences, RateLimitPolicy.TimeUnit,
                    _cancel.Token);
                source = new BlockingCollection<T>();
                sequence.Subscribe(source.Add, exception => { }, () => { });
            }
            else
            {
                source = _buffer;
            }

            return source;
        }

        private void FlushBacklog()
        {
            _empty.Wait();
            try
            {
                while (!_buffer.IsCompleted)
                    if (_buffer.TryTake(out var message, -1, _cancel.Token))
                        HandleBacklog(message);
            }
            finally
            {
                _empty.Release();
            }
        }

        private void ProduceOn(BlockingCollection<T> source, ParallelOptions options)
        {
            var partitioner = source.GetConsumingPartitioner();

            Parallel.ForEach(partitioner, options,
                async (@event, state) => await ProductionCycle(options, @event, state));
        }

        private async Task ProductionCycle(ParallelOptions options, T message, ParallelLoopState state)
        {
            if (state.ShouldExitCurrentIteration)
            {
                HandleBacklog(message);
                return;
            }

            if (_errorConsumer != null)
            {
                try
                {
                    if (!_consumer.Invoke(message))
                    {
                        await HandleUnsuccessfulDelivery(options, message, state);
                        return;
                    }
                }
                catch (Exception e)
                {
                    _errorConsumer.Invoke(e);
                    await HandleUnsuccessfulDelivery(options, message, state);
                    return;
                }
            }
            else
            {
                if (!_consumer.Invoke(message))
                {
                    await HandleUnsuccessfulDelivery(options, message, state);
                    return;
                }
            }

            if (RetryPolicy != null) _attempts.Remove(HashMessage(message));

            Interlocked.Increment(ref _sent);
            options.CancellationToken.ThrowIfCancellationRequested();
        }

        private async Task HandleUnsuccessfulDelivery(ParallelOptions options, T message, ParallelLoopState state)
        {
            if (RetryPolicy == null)
            {
                HandleUndeliverable(message);
                return;
            }

            var hash = HashMessage(message);
            var attempts = IncrementAttempts(hash);
            var decision = RetryPolicy.DecideOn(message, attempts);

            switch (decision)
            {
                case RetryDecision.RetryImmediately:
                    await ProductionCycle(options, message, state);
                    break;
                case RetryDecision.Requeue:
                    if (!_buffer.IsAddingCompleted && RetryPolicy?.RequeueInterval != null)
                        Produce(Observable.Return(message).Delay(RetryPolicy.RequeueInterval(attempts)));
                    else
                        Produce(message);

                    break;
                case RetryDecision.Backlog:
                    HandleBacklog(message);
                    break;
                case RetryDecision.Undeliverable:
                    HandleUndeliverable(message);
                    break;
                case RetryDecision.Destroy:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private ulong HashMessage(T message)
        {
            return HashFunction?.Invoke(message) ?? (ulong) message.GetHashCode();
        }

        private int IncrementAttempts(ulong hash)
        {
            if (!_attempts.TryGetValue(hash, out _))
                _attempts.Add(hash, 1);
            else
                _attempts[hash] = _attempts[hash] + 1;

            return _attempts[hash];
        }

        #region Backlog

        /// <summary>
        ///     This consumer is invoked when the producer is stopped immediately or otherwise interrupted, as such on disposal.
        ///     Any messages still waiting to be delivered are flushed to this consumer. If the consumer reports a failure, then
        ///     the messages are swept to the undeliverable consumer.
        /// </summary>
        public void AttachBacklog(Func<T, bool> handler)
        {
            _backlogConsumer = handler;
        }

        #endregion

        #region Undeliverable

        /// <summary>
        ///     This consumer is invoked when the producer has given up on trying to deliver this message iteration.
        ///     This is the last chance consumer before the message is scrubbed from transient state.
        ///     Keep in mind that nothing stops another process from sending the same message in once it has been
        ///     finalized (sent or undeliverable) at the producer, since the hash is cleared for that message.
        ///     Hence, this provides a best effort "at least once" delivery guarantee, though you are responsible
        ///     for recovering in the event of a non-delivery or failure, as the pipeline cannot make guarantees beyond
        ///     only clearing handlers that return true or reach a finalized state.
        /// </summary>
        /// <param name="consumer"></param>
        public void AttachUndeliverable(Action<T> consumer)
        {
            _undeliverableConsumer = consumer;
        }

        #endregion
    }
}