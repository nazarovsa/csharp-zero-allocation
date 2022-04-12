# C# Zero Allocation

## Memory problems in our algorithms

Reasons why we have problems with memory:
- Incorrect memory allocation. (Create objects in loops, etc.)
- Weak knowledge about how the .net platform operates. (String is immutable, etc.)

## Our instruments for optimization

Our main principle is "Allocate as little memory as possible". It may seem strange, but in some cases we can avoid allocating memory at all.  
Next instruments will help us with it.

### `Span<T>`, `ReadOnlySpan<T>`, stackalloc

#### `Span<T>`

Provides a type- and memory-safe representation of a contiguous region of arbitrary memory.

**`Span<T>` is a ref struct that is allocated on the stack rather than on the managed heap.** Ref struct types have a number of **restrictions** to ensure that they cannot be promoted to the managed heap:  
- They can't be boxed
- They can't be assigned to variables of type `Object`, dynamic or to any interface type
- They can't be fields in a reference type
- They can't be used across await and yield boundaries. 

In addition, calls to two methods, `Equals(Object)` and GetHashCode, throw a `NotSupportedException`.

A `Span<T>` represents a contiguous region of arbitrary memory. A `Span<T>` instance is often used to hold the elements of an array or a portion of an array. Unlike an array, however, a `Span<T>` instance can point to managed memory, native memory, or memory managed on the stack.

##### `Span<T>` and arrays

When it wraps an array, `Span<T>` can wrap an entire array, as it did in the examples in the `Span<T>` and memory section. Because it supports slicing, `Span<T>` can also point to any contiguous range within the array.
The following example creates a slice of the middle five elements of a 10-element integer array. Note that the code doubles the values of each integer in the slice. As the output shows, the changes made by the span are reflected in the values of the array.

```csharp
using System;

var array = new int[] { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 };
var slice = new Span<int>(array, 2, 5);
for (int ctr = 0; ctr < slice.Length; ctr++)
    slice[ctr] *= 2;

// Examine the original array values.
foreach (var value in array)
    Console.Write($"{value}  ");
Console.WriteLine();

// The example displays the following output:
//      2  4  12  16  20  24  28  16  18  20
```

##### `Span<T>` and slices

`Span<T>` includes two overloads of the `Slice` method that form a slice out of the current span that starts at a specified index. This makes it possible to treat the data in a `Span<T>` as a set of logical chunks that can be processed as needed by portions of a data processing pipeline with minimal performance impact.  
For example, since modern server protocols are often text-based, manipulation of strings and substrings is particularly important. In the String class, the major method for extracting substrings is Substring. For data pipelines that rely on extensive string manipulation, its use offers some performance penalties, since it:
- Creates a new string to hold the substring.
- Copies a subset of the characters from the original string to the new string.
This allocation and copy operation can be eliminated by using either `Span<T>` or `ReadOnlySpan<T>`, as the following example shows:

```csharp
using System;

class Program
{
    static void Main()
    {
        string contentLength = "Content-Length: 132";
        var length = GetContentLength(contentLength.ToCharArray());
        Console.WriteLine($"Content length: <length>");
    }

    private static int GetContentLength(ReadOnlySpan<char> span)
    {
        var slice = span.Slice(16);
        return int.Parse(slice);
    }
}
// Output:
//      Content length: 132 
```

#### `ReadOnlySpan<T>`

Provides a type-safe and memory-safe **read-only** representation of a contiguous region of arbitrary memory.

**`ReadOnlySpan<T>` is a ref struct that is allocated on the stack and can never escape to the managed heap.** `ReadOnlySpan<T>` has the same restrictions as `Span<T>`.
A `ReadOnlySpan<T>` instance is often used to reference the elements of an array or a portion of an array. Unlike an array, however, a `ReadOnlySpan<T>` instance can point to managed memory, native memory, or memory managed on the stack.

#### stackalloc

A `stackalloc` expression allocates a block of memory on the stack. A stack allocated memory block created during the method execution is automatically discarded when that method returns. You cannot explicitly free the memory allocated with `stackalloc`. A stack allocated memory block is not subject to garbage collection and doesn't have to be pinned with a fixed statement.
You can assign the result of a stackalloc expression to a variable of one of the following types:
- Beginning with C# 7.2, System.Span<T> or System.ReadOnlySpan<T>
- A pointer type, as the following example shows

**The amount of memory available on the stack is limited.** If you allocate too much memory on the stack, a `StackOverflowException` is thrown. To avoid that, follow the rules below:
- Limit the amount of memory you allocate with stackalloc. For example, if the intended buffer size is below a certain limit, you allocate the memory on the stack; otherwise, use an array of the required length, as the following code shows:
```csharp
const int MaxStackLimit = 1024;
Span<byte> buffer = inputLength <= MaxStackLimit ? stackalloc byte[MaxStackLimit] : new byte[inputLength]; 
```
- Avoid using `stackalloc` inside loops. Allocate the memory block outside a loop and reuse it inside the loop.

**The content of the newly allocated memory is undefined.** You should initialize it before the use. For example, you can use the `Span<T>`. Clear method that sets all the items to the default value of type T.

### `Memory<T>`, `ReadOnlyMemory<T>`

#### `Memory<T>`

#### `ReadOnlyMemory<T>`

#### `IMemoryOwner<T>`

### Object & Array pooling

#### How pools helps us save memory.

#### How to clean up pools.

## Practice

- ValueStringBuilder.
- [Guider](https://github.com/nazarovsa/csharp-zero-allocation/tree/main/src/Guider) - Sample of optimization of guid to efficient string helper.
- Generating entities and write them to file.

## Sources

- [`Span<T>`: docs](https://docs.microsoft.com/en-us/dotnet/api/system.span-1?view=net-6.0)
- [`ReadOnlySpan<T>`: docs](https://docs.microsoft.com/en-us/dotnet/api/system.readonlyspan-1?view=net-6.0)
- [`stackalloc`: docs](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc)
- [.NET Platform Architecture. Stanislav Sidristij - Memory<T> and Span<T>](https://github.com/sidristij/dotnetbook/blob/master/book/en/MemorySpan.md)
- [What is Span in C# and why you should be using it](https://www.youtube.com/watch?v=FM5dpxJMULY)
- [Writing C# without allocating ANY memory](https://www.youtube.com/watch?v=B2yOjLyEZk0) - Guider sample taken from this video.
- [RU - Станислав Сидристый — Делаем zero-allocation код на примере оптимизации крупной библиотеки](https://www.youtube.com/watch?v=-FDfnUyYSyc)
- [ValueStringBuilder: a stack-based string-builder](https://andrewlock.net/a-deep-dive-on-stringbuilder-part-6-vaulestringbuilder-a-stack-based-string-builder/)
- [ValueStringBuilder: source code](https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/Text/ValueStringBuilder.cs)