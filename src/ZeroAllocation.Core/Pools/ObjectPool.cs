using System.Collections.Concurrent;

namespace ZeroAllocation.Core.Pools;

public interface IPoolable
{
}

public class ObjectPool<T>
    where T : IPoolable, new()
{
    private readonly int _maxSize;

    private readonly Func<T>? _factory;
    
    private readonly ConcurrentStack<T> _pool = new ConcurrentStack<T>();

    public int Count => _pool.Count;

    public ObjectPool()
    {
        _maxSize = 128;
    }

    public ObjectPool(int maxSize)
    {
        if(maxSize < 1)
            throw new ArgumentOutOfRangeException(nameof(maxSize), "maxSize must be greater than 0");
        
        _maxSize = maxSize;
    }

    public ObjectPool(int maxSize, Func<T> factory) : this(maxSize)
    {
        _factory = factory;
    }

    public T Get()
    {
        if (_pool.TryPop(out var obj))
            return obj;

        if (_factory == null)
            return new T();

        return _factory.Invoke();
    }

    public void Return(T obj)
    {
        if(_pool.Count < _maxSize)
            _pool.Push(obj);
    }
}