// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace BetterAPI.Operations.Internal
{
    /// <summary>
    /// Source: https://gist.github.com/georgiosd/eeed0c3d36c6d7074eddec8daf922d7e
    /// </summary>
    internal static class BlockingCollectionExtensions
    {
        public static IObservable<IList<T>> AsBatchingObservable<T>(this BlockingCollection<T> sequence, int n,
            CancellationToken cancellationToken)
        {
            return sequence.AsConsumingObservable(cancellationToken).Buffer(n);
        }

        public static IObservable<IList<T>> AsBatchingObservable<T>(this BlockingCollection<T> sequence, TimeSpan w,
            CancellationToken cancellationToken)
        {
            return sequence.AsConsumingObservable(cancellationToken).Buffer(w);
        }

        public static IObservable<IList<T>> AsBatchingObservable<T>(this BlockingCollection<T> sequence, int n,
            TimeSpan w, CancellationToken cancellationToken)
        {
            return sequence.AsConsumingObservable(cancellationToken).Buffer(w, n);
        }

        public static IObservable<T> AsConsumingObservable<T>(this BlockingCollection<T> sequence,
            CancellationToken cancellationToken)
        {
            var subject = new Subject<T>();
            var token = new CancellationToken();
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, token);
            var consumingTask = new Task(() =>
            {
                while (!sequence.IsCompleted)
                    try
                    {
                        var item = sequence.Take(cancellationToken);
                        try
                        {
                            subject.OnNext(item);
                        }
                        catch (Exception ex)
                        {
                            subject.OnError(ex);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                subject.OnCompleted();
            }, TaskCreationOptions.LongRunning);

            return new TaskAwareObservable<T>(subject, consumingTask, tokenSource);
        }

        public static IObservable<T> AsRateLimitedObservable<T>(this BlockingCollection<T> sequence, int occurrences,
            TimeSpan timeUnit, CancellationToken cancellationToken)
        {
            var subject = new Subject<T>();
            var token = new CancellationToken();
            var tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, token);
            var consumingTask = new Task(() =>
            {
                using var throttle = new Throttle(occurrences, timeUnit);
                while (!sequence.IsCompleted)
                    try
                    {
                        var item = sequence.Take(cancellationToken);
                        throttle.WaitToProceed();
                        try
                        {
                            subject.OnNext(item);
                        }
                        catch (Exception ex)
                        {
                            subject.OnError(ex);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }

                subject.OnCompleted();
            }, TaskCreationOptions.LongRunning);

            return new TaskAwareObservable<T>(subject, consumingTask, tokenSource);
        }

        public static Partitioner<T> GetConsumingPartitioner<T>(this BlockingCollection<T> collection)
        {
            return new BlockingCollectionPartitioner<T>(collection);
        }

        private class TaskAwareObservable<T> : IObservable<T>, IDisposable
        {
            private readonly Subject<T> _subject;
            private readonly Task _task;
            private readonly CancellationTokenSource _taskCancellationTokenSource;

            public TaskAwareObservable(Subject<T> subject, Task task, CancellationTokenSource tokenSource)
            {
                _task = task;
                _subject = subject;
                _taskCancellationTokenSource = tokenSource;
            }

            public void Dispose()
            {
                _taskCancellationTokenSource.Cancel();
                _task.Wait();

                _taskCancellationTokenSource.Dispose();

                _subject.Dispose();
            }

            public IDisposable Subscribe(IObserver<T> observer)
            {
                var disposable = _subject.Subscribe(observer);
                if (_task.Status == TaskStatus.Created) _task.Start();

                return disposable;
            }
        }

        private class BlockingCollectionPartitioner<T> : Partitioner<T>
        {
            private readonly BlockingCollection<T> _collection;

            internal BlockingCollectionPartitioner(BlockingCollection<T> collection)
            {
                _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            }

            public override bool SupportsDynamicPartitions => true;

            public override IList<IEnumerator<T>> GetPartitions(int partitionCount)
            {
                var dynamicPartitioner = GetDynamicPartitions();

                return Enumerable.Range(0, partitionCount).Select(_ => dynamicPartitioner.GetEnumerator()).ToArray();
            }

            public override IEnumerable<T> GetDynamicPartitions()
            {
                return _collection.GetConsumingEnumerable();
            }
        }

        // Original source from: http://www.pennedobjects.com/2010/10/better-rate-limiting-with-dot-net/
        internal class Throttle : IDisposable
        {
            private readonly Timer _exitTimer;
            private readonly ConcurrentQueue<int> _exitTimes;
            private readonly SemaphoreSlim _semaphore;
            private bool _isDisposed;

            public Throttle(int occurrences, TimeSpan timeUnit)
            {
                Occurrences = occurrences;
                TimeUnitMilliseconds = (int) timeUnit.TotalMilliseconds;

                _semaphore = new SemaphoreSlim(Occurrences, Occurrences);
                _exitTimes = new ConcurrentQueue<int>();
                _exitTimer = new Timer(ExitTimerCallback, null, TimeUnitMilliseconds, -1);
            }

            public int Occurrences { get; }
            public int TimeUnitMilliseconds { get; }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void ExitTimerCallback(object? state)
            {
                int exitTime;
                while (_exitTimes.TryPeek(out exitTime) && unchecked(exitTime - Environment.TickCount) <= 0)
                {
                    _semaphore.Release();
                    _exitTimes.TryDequeue(out exitTime);
                }

                int timeUntilNextCheck;
                if (_exitTimes.TryPeek(out exitTime))
                    timeUntilNextCheck = unchecked(exitTime - Environment.TickCount);
                else
                    timeUntilNextCheck = TimeUnitMilliseconds;

                _exitTimer.Change(timeUntilNextCheck, -1);
            }

            public bool WaitToProceed(int millisecondsTimeout)
            {
                if (!_semaphore.Wait(millisecondsTimeout))
                    return false;
                var exitTime = unchecked(Environment.TickCount + TimeUnitMilliseconds);
                _exitTimes.Enqueue(exitTime);
                return true;
            }

            public bool WaitToProceed(TimeSpan timeout)
            {
                return WaitToProceed((int) timeout.TotalMilliseconds);
            }

            public void WaitToProceed()
            {
                WaitToProceed(Timeout.Infinite);
            }

            protected virtual void Dispose(bool isDisposing)
            {
                if (!isDisposing || _isDisposed) return;

                _semaphore.Dispose();
                _exitTimer.Dispose();
                _isDisposed = true;
            }
        }
    }
}