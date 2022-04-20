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

### `Memory<T>`, `ReadOnlyMemory<T>`, `IMemoryOwner<T>`

#### `Memory<T>`
Represents a contiguous region of memory.

Like `Span<T>`, `Memory<T>` represents a contiguous region of memory. **Unlike `Span<T>`, however, `Memory<T>` is not a ref struct**. This means that **`Memory<T>` can be placed on the managed heap**, whereas `Span<T>` cannot. As a result, the **`Memory<T>` structure does not have the same restrictions as a `Span<T>`** instance. In particular:
- It can be used as a field in a class.
- It can be used across await and yield boundaries.

In addition to `Memory<T>`, you can use `System.ReadOnlyMemory<T>` to represent immutable or read-only memory.

#### `ReadOnlyMemory<T>`

Represents a contiguous region of memory, similar to ReadOnlySpan<T>. **Unlike `ReadOnlySpan<T>`, it is not a byref-like type**.

#### `IMemoryOwner<T>`
Identifies the owner of a block of memory who is responsible for disposing of the underlying memory appropriately.

`IMemoryOwner<T>` can be used to manage rented memory in a right manner. These cases are occuring when you need to pass a memory block through your calls.

The `IMemoryOwner<T>` interface is used to define the owner responsible for the lifetime management of a `Memory<T>` buffer. An instance of the `IMemoryOwner<T>` interface is returned by the `MemoryPool<T>.Rent` method.
While a buffer can have multiple consumers, it can only have a single owner at any given time. The owner can:
Create the buffer either directly or by calling a factory method.
Transfer ownership to another consumer. In this case, the previous owner should no longer use the buffer.
Destroy the buffer when it is no longer in use.
Because the `IMemoryOwner<T>` object implements the `IDisposable` interface, you should call its `Dispose` method only after the memory buffer is no longer needed and you have destroyed it. You should not dispose of the `IMemoryOwner<T>` object while a reference to its memory is available. This means that the type in which `IMemoryOwner<T>` is declared should not have a `Finalize` method.

### Object & Array pooling

The object pool pattern is a software creational design pattern that uses a set of initialized objects kept ready to use – a "pool" – rather than allocating and destroying them on demand. A client of the pool will request an object from the pool and perform operations on the returned object. When the client has finished, it returns the object to the pool rather than destroying it; this can be done manually or automatically.

Object pools are primarily used for performance: in some circumstances, object pools significantly improve performance. Object pools complicate object lifetime, as objects obtained from and returned to a pool are not actually created or destroyed at this time, and thus require care in implementation.

#### How pools helps us save memory.

Obviously, if we are reusing objects, it allow us to save memory and prevent GC collections. We don't need to create and destroy extra objects anymore, because we taking them from the pool.

## Practice

- [ValueStringBuilder](https://github.com/nazarovsa/csharp-zero-allocation/blob/main/src/ZeroAllocation.Core/ValueStringBuilder.cs)
- [GuidTransformer](https://github.com/nazarovsa/csharp-zero-allocation/tree/main/src/Guider) - Sample of optimization of guid to efficient string helper.
- [Abstract lottery ticket combination generator](https://github.com/nazarovsa/csharp-zero-allocation/tree/main/src/Generation)

## Sources

- [`Span<T>`: docs](https://docs.microsoft.com/en-us/dotnet/api/system.span-1?view=net-6.0)
- [`ReadOnlySpan<T>`: docs](https://docs.microsoft.com/en-us/dotnet/api/system.readonlyspan-1?view=net-6.0)
- [`stackalloc`: docs](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc)
- [`Memory<T>`: docs](https://docs.microsoft.com/en-us/dotnet/api/system.memory-1?view=net-6.0)
- [`IMemoryOwner<T>`: docs](https://docs.microsoft.com/en-us/dotnet/api/system.buffers.imemoryowner-1?view=net-6.0)
- [.NET Platform Architecture. Stanislav Sidristij - Memory<T> and Span<T>](https://github.com/sidristij/dotnetbook/blob/master/book/en/MemorySpan.md)
- [What is Span in C# and why you should be using it](https://www.youtube.com/watch?v=FM5dpxJMULY)
- [Writing C# without allocating ANY memory](https://www.youtube.com/watch?v=B2yOjLyEZk0) - GuidTransformer sample taken from this video.
- [RU - Станислав Сидристый — Делаем zero-allocation код на примере оптимизации крупной библиотеки](https://www.youtube.com/watch?v=-FDfnUyYSyc)
- [ValueStringBuilder: a stack-based string-builder](https://andrewlock.net/a-deep-dive-on-stringbuilder-part-6-vaulestringbuilder-a-stack-based-string-builder/)
- [ValueStringBuilder: source code](https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/Text/ValueStringBuilder.cs)