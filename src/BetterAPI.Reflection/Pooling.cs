﻿// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.Extensions.ObjectPool;

namespace BetterAPI.Reflection
{
    public static class Pooling
    {
        public static ArgumentsPool Arguments = new ArgumentsPool();

        public class ArgumentsPool
        {
            private readonly ObjectWrapper[] _items;
            private object[] _firstItem;

            public ArgumentsPool() : this(Environment.ProcessorCount * 2)
            {
            }

            public ArgumentsPool(int maximumRetained)
            {
                _items = new ObjectWrapper[maximumRetained - 1];
            }

            public object[] Get(int length)
            {
                var comparator = _firstItem;
                if (comparator != null &&
                    Interlocked.CompareExchange(ref _firstItem, default, comparator) == comparator &&
                    comparator.Length == length)
                    return comparator;
                var items = _items;
                for (var index = 0; index < items.Length; ++index)
                {
                    var item = items[index].Element;
                    if (item?.Length != length)
                        continue;
                    if (item != null && Interlocked.CompareExchange(ref items[index].Element, default, item) == item)
                        return item;
                }

                return new object[length];
            }

            public void Return(object[] obj)
            {
                if (_firstItem == null && Interlocked.CompareExchange(ref _firstItem, obj, default) == null)
                    return;
                var items = _items;
                var index = 0;
                while (index < items.Length &&
                       Interlocked.CompareExchange(ref items[index].Element, obj, default) != null)
                    ++index;
            }

            [DebuggerDisplay("{" + nameof(Element) + "}")]
            private struct ObjectWrapper
            {
                public object[] Element;
            }
        }

        public static class StringBuilderPool
        {
            private static readonly ObjectPool<StringBuilder> Pool =
                new LeakTrackingObjectPool<StringBuilder>(new DefaultObjectPool<StringBuilder>(
                    new StringBuilderPooledObjectPolicy()
                ));

            public static StringBuilder Get()
            {
                return Pool.Get();
            }

            public static void Return(StringBuilder obj)
            {
                Pool.Return(obj);
            }

            public static string Scoped(Action<StringBuilder> closure)
            {
                var sb = Pool.Get();
                try
                {
                    closure(sb);
                    return sb.ToString();
                }
                finally
                {
                    Pool.Return(sb);
                }
            }

            public static string Scoped(Action<StringBuilder> closure, int startIndex, int length)
            {
                var sb = Pool.Get();
                try
                {
                    closure(sb);
                    return sb.ToString(startIndex, length);
                }
                finally
                {
                    Pool.Return(sb);
                }
            }
        }

        public static class ListPool<T>
        {
            private static readonly ObjectPool<List<T>> Pool =
                new LeakTrackingObjectPool<List<T>>(
                    new DefaultObjectPool<List<T>>(new CollectionObjectPolicy<List<T>, T>())
                );

            public static List<T> Get()
            {
                return Pool.Get();
            }

            public static void Return(List<T> obj)
            {
                Pool.Return(obj);
            }
        }

        public static class HashSetPool<T>
        {
            private static readonly ObjectPool<HashSet<T>> Pool =
                new LeakTrackingObjectPool<HashSet<T>>(
                    new DefaultObjectPool<HashSet<T>>(
                        new CollectionObjectPolicy<HashSet<T>, T>())
                );

            public static HashSet<T> Get()
            {
                return Pool.Get();
            }

            public static void Return(HashSet<T> obj)
            {
                Pool.Return(obj);
            }
        }

        public static class HashSetPool
        {
            private static readonly ObjectPool<HashSet<string>> Pool =
                new LeakTrackingObjectPool<HashSet<string>>(
                    new DefaultObjectPool<HashSet<string>>(
                        new StringSetPolicy<OrdinalIgnoreCaseComparer>())
                );

            public static HashSet<string> Get()
            {
                return Pool.Get();
            }

            public static void Return(HashSet<string> obj)
            {
                Pool.Return(obj);
            }
        }

        public static class StopwatchPool
        {
            public static ObjectPool<Stopwatch> Pool = new LeakTrackingObjectPool<Stopwatch>(
                new DefaultObjectPool<Stopwatch>(new StopwatchPoolPolicy()));

            public static TimeSpan Scoped(Action<Stopwatch> closure)
            {
                var sw = Pool.Get();
                closure(sw);
                var elapsed = sw.Elapsed;
                Pool.Return(sw);
                return elapsed;
            }

            private class StopwatchPoolPolicy : IPooledObjectPolicy<Stopwatch>
            {
                public Stopwatch Create()
                {
                    return new Stopwatch();
                }

                public bool Return(Stopwatch obj)
                {
                    obj.Reset();
                    return true;
                }
            }
        }

        #region Policies

        /// <summary>
        ///     The default policy provided by Microsoft uses new T() constraint,
        ///     which silently defers to Activator.CreateInstance.
        /// </summary>
        private class DefaultObjectPolicy<T> : IPooledObjectPolicy<T> where T : notnull
        {
            public T Create()
            {
                return CreateNew();
            }

            public bool Return(T obj)
            {
                return true;
            }

            private static T CreateNew()
            {
                return Instancing.CreateInstance<T>();
            }
        }

        private class CollectionObjectPolicy<TCollection, TElement> : IPooledObjectPolicy<TCollection>
            where TCollection : ICollection<TElement>
        {
            public TCollection Create()
            {
                return Instancing.CreateInstance<TCollection>();
            }

            public bool Return(TCollection collection)
            {
                collection.Clear();
                return collection.Count == 0;
            }
        }

        private interface IStringSetComparer : IEqualityComparer<string>
        {
            StringComparer Comparer { get; }
        }

        // ReSharper disable once ClassNeverInstantiated.Local
        private class OrdinalIgnoreCaseComparer : IStringSetComparer
        {
            public bool Equals(string x, string y)
            {
                return Comparer.Equals(x, y);
            }

            public int GetHashCode(string obj)
            {
                return Comparer.GetHashCode(obj);
            }

            public StringComparer Comparer => StringComparer.OrdinalIgnoreCase;
        }

        private class StringSetPolicy<T> : IPooledObjectPolicy<HashSet<string>> where T : IStringSetComparer
        {
            public HashSet<string> Create()
            {
                return new HashSet<string>(Instancing.CreateInstance<T>());
            }

            public bool Return(HashSet<string> set)
            {
                set.Clear();
                return set.Count == 0;
            }
        }

        #endregion
    }
}