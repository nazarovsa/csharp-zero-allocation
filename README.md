# C# Zero Allocation

## Memory problems in our algorithms

Reasons why we have problems with memory:
- Incorrect memory allocation. (Create objects in loops, etc.)
- Weak knowledge about how the .net platform operates. (String is immutable, etc.)

## Our instruments for optimization

Our main principle is "Allocate as little memory as possible". It may seem strange, but in some cases we can avoid allocating memory at all.  
Next instruments will help us with it.

### Span, ReadOnlySpan, stackalloc

#### Span

#### ReadOnlySpan

#### stackalloc

### Memory, ReadOnlyMemory

#### Memory

#### ReadOnlyMemory

#### IMemoryOwner

### Object & Array pooling

#### How pools helps us save memory.

#### How to clean up pools.

## Practice

- ValueStringBuilder.
- [Guider](https://github.com/nazarovsa/csharp-zero-allocation/tree/main/src/Guider) - Sample of optimization of guid to efficient string helper.
- Generating entities and write them to file.