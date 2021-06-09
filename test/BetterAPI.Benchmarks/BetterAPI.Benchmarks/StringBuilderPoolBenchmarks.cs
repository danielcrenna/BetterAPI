// Copyright (c) Daniel Crenna. All rights reserved.
// 
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, you can obtain one at http://mozilla.org/MPL/2.0/.

using BenchmarkDotNet.Attributes;
using BetterAPI.Reflection;

namespace BetterAPI.Benchmarks
{
    [SimpleJob]
    [MemoryDiagnoser]
    [SkewnessColumn]
    [KurtosisColumn]
    [MarkdownExporter]
    public class StringBuilderPoolBenchmarks
    {
        [Benchmark(Baseline = true)]
        public void StringBuilder_TryFinally()
        {
            var sb = Pooling.StringBuilderPool.Get();
            string value;
            try
            {
                sb.Append('0');
                value = sb.ToString();
            }
            finally
            {
                Pooling.StringBuilderPool.Return(sb);
            }
        }

        [Benchmark]
        public void StringBuilder_Scoped()
        {
            var value = Pooling.StringBuilderPool.Scoped(sb => sb.Append('0'));
        }
    }
}