using System.Buffers;
using System.Runtime.CompilerServices;

namespace ZeroAllocation.Core;

public class CountdownMemoryOwner<T> : IMemoryOwner<T>
{
    private int _length;
    private int _offset;
    private int _owners;
    private T[] _array;
    private CountdownMemoryOwner<T>? _parent;
    private Memory<T> _memory;

    public Memory<T> Memory
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _memory;

        private set => _memory = value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void AddOwner() => _owners++;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CountdownMemoryOwner<T> Init(CountdownMemoryOwner<T>? parent, int offset, int length,
        bool defaultOwner = true)
    {
        _owners = defaultOwner ? 1 : 0;
        _offset = offset;
        _length = length;
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _parent.AddOwner();
        Memory = parent.Memory.Slice(_offset, _length);
        return this;
    }    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public CountdownMemoryOwner<T> Init(T[] array, int length)
    {
        _owners = 1;
        _offset = 0;
        _length = length;
        _parent = default;
        _array = array;
        Memory = array.AsMemory(0, _length);
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose()
    {
        _owners--;
        if(_owners > 0)
            return;

        if (_parent != default)
        {
            _parent.Dispose();
            _parent = null;
        }
        else
        {
            ArrayPool<T>.Shared.Return(_array);
        }
    }
}