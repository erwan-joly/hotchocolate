using BenchmarkDotNet.Attributes;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;

#nullable enable

namespace HotChocolate.Execution.Benchmarks
{
    [RPlotExporter, CategoriesColumn, RankColumn, MeanColumn, MedianColumn, MemoryDiagnoser]
    public class ObjectPoolBenchmarks
    {
        private readonly int _poolSize = 8;

        [Params(200)]
        public int Size { get; set; }

        [Benchmark]
        public void BufferedObjectPoolFullLock_GET_AND_RETURN_MANY()
        {
            var pool = new TestPool<ObjectBuffer<PoolElement>>(_poolSize);
            var buffer = new BufferedObjectPoolFullLock<PoolElement>(pool);
            var stack = new Stack<PoolElement>();
            for (int i = 0; i < Size; i++)
            {
                stack.Push(buffer.Get());
                if (stack.Count > 30)
                {
                    while (stack.TryPop(out PoolElement element))
                    {
                        buffer.Return(element);
                    }
                }
            }
        }

        private class PoolElement : IObjectBuffer
        {
            public void Reset()
            {
            }
        }

        private class TestPool<T> : DefaultObjectPool<T> where T : class, IObjectBuffer, new()
        {
            public List<T> Rented =
                new List<T>();

            public List<T> Returned =
                new List<T>();

            public TestPool(int size)
                : base(new Policy(), size)
            {
            }

            public override T Get()
            {
                T buffer = base.Get();
                Rented.Add(buffer);
                Returned.Remove(buffer);
                return buffer;
            }
            public override void Return(T obj)
            {
                Returned.Add(obj);
                Rented.Remove(obj);
                base.Return(obj);
            }

            private class Policy : IPooledObjectPolicy<T>
            {
                public T Create() => new T();

                public bool Return(T obj)
                {
                    obj.Reset();
                    return true;
                }
            }
        }
    }
}