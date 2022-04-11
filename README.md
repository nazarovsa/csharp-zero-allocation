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

## Sources

- [.NET Platform Architecture. Stanislav Sidristij - Memory{T} and Span{T}](https://github.com/sidristij/dotnetbook/blob/master/book/en/MemorySpan.md)
- [What is Span in C# and why you should be using it](https://www.youtube.com/watch?v=FM5dpxJMULY)
- [Writing C# without allocating ANY memory](https://www.youtube.com/watch?v=B2yOjLyEZk0) - Guider sample taken from this video.
- [RU - Станислав Сидристый — Делаем zero-allocation код на примере оптимизации крупной библиотеки](https://www.youtube.com/watch?v=-FDfnUyYSyc)
- [ValueStringBuilder: a stack-based string-builder](https://andrewlock.net/a-deep-dive-on-stringbuilder-part-6-vaulestringbuilder-a-stack-based-string-builder/)
- [ValueStringBuilder: source code](https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/Text/ValueStringBuilder.cs)