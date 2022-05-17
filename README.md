# C# Zero Allocation

Repository contains materials collected by me about memory allocation and optimization. 
At the end of README.md you can find links to original sources.  

**All rights for original materials belong to the authors.**

## Memory problems in our algorithms

Reasons why we have problems with memory:
- Incorrect memory allocation. (Create objects in loops, etc.)
- Weak knowledge about how the .net platform operates. (String is immutable, struct types are copied on pass, etc.)
- Weak knowledge about language features.

## Our instruments for optimization

Our main principle is "Allocate as little memory as possible". But sometimes it is not enough.
It may seem strange, but in some cases, we can avoid allocating memory at all if we know the advanced features of a language.

Let's start with some interesting facts about the `struct` type and then look closer to other instruments for optimization.

### Structures and their vagaries (~ 17 minutes to read)
_Much of the sample code in this section uses features added in **C# 7.2**. 
To use those features, make sure your project isn't configured to use an earlier version._ 

As we know, `struct` is a value type.
One advantage to using value types is that they often avoid heap allocations. 
The disadvantage is that they're copied by value. 
This trade-off makes it harder to optimize algorithms that operate on large amounts of data. 
The language features highlighted in this section provide mechanisms that enable safe efficient code using references to value types. 
Use these features wisely to minimize both allocations and copy operations.

The section also explains some low-level optimizations that are advisable when you've run a profiler and have identified bottlenecks:
- Use the `in` parameter modifier.
- Use `ref readonly return` statements.
- Use `ref struct` types.
- Use `nint` and `nuint` types.  

These techniques balance two competing goals:
- **Minimize allocations on the heap**: variables that are reference types hold a reference to a location in memory and are allocated on the managed heap. Only the reference is copied when a reference type is passed as an argument to a method or returned from a method. Each new object requires a new allocation, and later must be reclaimed. Garbage collection takes time.
- **Minimize the copying of values**: variables that are value types directly contain their value, and the value is typically copied when passed to a method or returned from a method. This behavior includes copying the value of this when calling iterators and async instance methods of structs. The copy operation takes time, depending on the size of the type.

This section uses the following example concept of the 3D-point structure to explain its recommendations:
```csharp
public struct Point3D
{
    public double X;
    public double Y;
    public double Z;
}
```
Different examples use different implementations of this concept.

#### Declare immutable structs as readonly

Declare a `readonly struct` to indicate that a type is **immutable**. 
The readonly modifier informs the compiler that your intent is to create an immutable type. 

The compiler enforces that design decision with the following rules:
- All field members must be read-only.
- All properties must be read-only, including auto-implemented properties.
 
These two rules are sufficient to ensure that no member of a `readonly struct` modifies the state of that struct. 
The struct is immutable. The Point3D structure could be defined as an immutable struct as shown in the following example:

```csharp
readonly public struct ReadonlyPoint3D
{
    public ReadonlyPoint3D(double x, double y, double z)
    {
        this.X = x;
        this.Y = y;
        this.Z = z;
    }

    public double X { get; }
    public double Y { get; }
    public double Z { get; }
}
```

Follow this recommendation whenever your design intent is to create an immutable value type. 
Any performance improvements are an added benefit. The `readonly struct` keywords clearly express your design intent.

#### Declare readonly members for mutable structs

In C# 8.0 and later, when a struct type is mutable, declare members that don't modify state as [readonly members](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/struct#readonly-instance-members).
Consider a different application that needs a 3D point structure, but must support mutability. 
The following version of the 3D point structure adds the readonly modifier only to those members that don't modify the structure. 
Follow this example when your design must support modifications to the struct by some members, but you still want the benefits of enforcing readonly on some members:

```csharp
public struct Point3D
{
    public Point3D(double x, double y, double z)
    {
        _x = x;
        _y = y;
        _z = z;
    }

    private double _x;
    public double X
    {
        readonly get => _x;
        set => _x = value;
    }

    private double _y;
    public double Y
    {
        readonly get => _y;
        set => _y = value;
    }

    private double _z;
    public double Z
    {
        readonly get => _z;
        set => _z = value;
    }

    public readonly double Distance => Math.Sqrt(X * X + Y * Y + Z * Z);

    public readonly override string ToString() => $"{X}, {Y}, {Z}";
}
```

The preceding sample shows many of the locations where you can apply the `readonly` modifier: methods, properties, and property accessors. 
If you use auto-implemented properties, the compiler adds the `readonly` modifier to the `get` accessor for read-write properties. 
The compiler adds the `readonly` modifier to the auto-implemented property declarations for properties with only a `get` accessor.

Adding the `readonly` modifier to members that don't mutate state provides two related benefits. First, the compiler enforces your intent. 
That member can't mutate the struct's state. Second, the compiler won't create [defensive copies](https://github.com/nazarovsa/csharp-zero-allocation#avoid-defensive-copies) of `in` parameters when accessing a `readonly` member. 
The compiler can make this optimization safely because it guarantees that the `struct` is not modified by a `readonly` member.

#### Use `ref readonly return` statements

Use a `ref readonly return` when both of the following conditions are true:
- The return value is a `struct` larger than [IntPtr.Size](https://docs.microsoft.com/en-us/dotnet/api/system.intptr.size?view=net-6.0#system-intptr-size).
- The storage lifetime is greater than the method returning the value.

You can return values by reference when the value being returned isn't local to the returning method. 
Returning by reference means that only the reference is copied, not the structure. 
In the following example, the Origin property can't use a ref return because the value being returned is a local variable:
```csharp
public Point3D Origin => new Point3D(0,0,0);
```

However, the following property definition can be returned by reference because the returned value is a static member:
```csharp
public struct Point3D
{
    private static Point3D origin = new Point3D(0,0,0);

    // Dangerous! returning a mutable reference to internal storage
    public ref Point3D Origin => ref origin;

    // other members removed for space
}
```

You don't want callers modifying the origin, so you should return the value by ref readonly:
```csharp
public struct Point3D
{
    private static Point3D origin = new Point3D(0,0,0);

    public static ref readonly Point3D Origin => ref origin;

    // other members removed for space
}
```

Returning `ref readonly enables` you to save copying larger structures and preserve the immutability of your internal data members.
At the call site, callers make the choice to use the `Origin` property as a `ref readonly` or as a value:

```csharp
var originValue = Point3D.Origin;
ref readonly var originReference = ref Point3D.Origin;
```

The first assignment in the preceding code makes a copy of the `Origin` constant and assigns that copy. 
The second assigns a reference. 
Notice that the `readonly` modifier must be part of the declaration of the variable. The reference to which it refers can't be modified. Attempts to do so result in a compile-time error.

The `readonly` modifier is required on the declaration of `originReference`.

The compiler enforces that the caller can't modify the reference. 
Attempts to assign the value directly generate a compile-time error. 
In other cases, the compiler allocates a defensive copy unless it can safely use the readonly reference. 
Static analysis rules determine if the struct could be modified. 
The compiler doesn't create a defensive copy when the struct is a `readonly struct` or the member is a `readonly` member of the struct. 
Defensive copies aren't needed to pass the struct as an `in` argument.

#### Use the in parameter modifier

The following sections explain what the in modifier does, how to use it, and when to use it for performance optimization.

##### The out, ref, and in keywords

The `in` keyword complements the `ref` and `out` keywords to pass arguments by reference. 
The `in` keyword specifies that the argument is passed by reference, but the called method doesn't modify the value. 
The `in` modifier can be applied to any member that takes parameters, such as methods, delegates, lambdas, local functions, indexers, and operators.

With the addition of the `in` keyword, C# provides a full vocabulary to express your design intent. Value types are copied when passed to a called method when you don't specify any of the following modifiers in the method signature. Each of these modifiers specifies that a variable is passed by reference, avoiding the copy. Each modifier expresses a different intent:
- `out`: This method sets the value of the argument used as this parameter.
- `ref`: This method may modify the value of the argument used as this parameter.
- `in`: This method doesn't modify the value of the argument used as this parameter.

Add the `in` modifier to pass an argument by reference and declare your design intent to pass arguments by reference to avoid unnecessary copying. 
You don't intend to modify the object used as that argument.

The `in` modifier complements `out` and `ref` in other ways as well. 
You can't create overloads of a method that differ only in the presence of `in`, `out`, or `ref`. 
These new rules extend the same behavior that had always been defined for `out` and `ref` parameters. 
Like the `out` and `ref` modifiers, value types aren't boxed because the `in` modifier is applied. 
Another feature of `in` parameters is that you can use literal values or constants for the argument to an `in` parameter.

The `in` modifier can also be used with reference types or numeric values. However, the benefits in those cases are minimal, if any.

There are several ways in which the compiler enforces the read-only nature of an `in` argument. 
First of all, the called method can't directly assign to an `in` parameter. 
It can't directly assign to any field of an `in` parameter when that value is a `struct` type. 
In addition, you can't pass an `in` parameter to any method using the `ref` or `out` modifier. 
These rules apply to any field of an `in` parameter, provided the field is a `struct` type and the parameter is also a `struct` type. 
In fact, these rules apply for multiple layers of member access provided the types at all levels of member access are `structs`. 
The compiler enforces that `struct` types passed as `in` arguments and their `struct` members are read-only variables when used as arguments to other methods.

##### Use in parameters for large structs

You can apply the `in` modifier to any `readonly struct` parameter, 
but this practice is likely to improve performance only for value types that are substantially larger than [IntPtr.Size](https://docs.microsoft.com/en-us/dotnet/api/system.intptr.size?view=net-6.0#system-intptr-size). 
For simple types (such as `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `char`, `float`, `double`, `decimal` and `bool`, and `enum` types), 
any potential performance gains are minimal. 
Some simple types, such as `decimal` at 16 bytes in size, 
are larger than either 4-byte or 8-byte references but not by enough to make a measurable difference in performance in most scenarios. 
And performance may degrade by using pass-by-reference for types smaller than [IntPtr.Size](https://docs.microsoft.com/en-us/dotnet/api/system.intptr.size?view=net-6.0#system-intptr-size).

The following code shows an example of a method that calculates the distance between two points in 3D space.

```csharp
private static double CalculateDistance(in Point3D point1, in Point3D point2)
{
    double xDifference = point1.X - point2.X;
    double yDifference = point1.Y - point2.Y;
    double zDifference = point1.Z - point2.Z;

    return Math.Sqrt(
        xDifference * xDifference + 
        yDifference * yDifference + 
        zDifference * zDifference);
}
```

The arguments are two structures that each contain three doubles. 
A double is 8 bytes, so each argument is 24 bytes. 
By specifying the in modifier, you pass a 4-byte or 8-byte reference to those arguments, depending on the architecture of the machine. 
The difference in size is small, but it can add up when your application calls this method in a tight loop using many different values.

However, the impact of any low-level optimizations like using the `in` modifier should be measured to validate a performance benefit. 
For example, you might think that using in on a `Guid` parameter would be beneficial. The `Guid` type is 16 bytes in size, twice the size of an 8-byte reference. 
But such a small difference isn't likely to result in a measurable performance benefit unless it's in a method that's in a time critical hot path for your application.

##### Optional use of in at call site

Unlike a `ref` or `out` parameter, you don't need to apply the `in` modifier at the call site. 
The following code shows two examples of calling the `CalculateDistance` method. 
The first uses two local variables passed by reference. 
The second includes a temporary variable created as part of the method call.
```csharp
var distance = CalculateDistance(pt1, pt2);
var fromOrigin = CalculateDistance(pt1, new Point3D());
```

Omitting the `in` modifier at the call site informs the compiler that it's allowed to make a copy of the argument for any of the following reasons:
- There exists an implicit conversion but not an identity conversion from the argument type to the parameter type.
- The argument is an expression but doesn't have a known storage variable.
- An overload exists that differs by the presence or absence of `in`. In that case, the by value overload is a better match.

These rules are useful as you update existing code to use read-only reference arguments. 
Inside the called method, you can call any instance method that uses by-value parameters. 
In those instances, a copy of the `in` parameter is created.

Because the compiler may create a temporary variable for any `in` parameter, 
you can also specify default values for any `in` parameter. 
The following code specifies the origin (point 0,0,0) as the default value for the second point:
```csharp
private static double CalculateDistance2(in Point3D point1, in Point3D point2 = default)
{
    double xDifference = point1.X - point2.X;
    double yDifference = point1.Y - point2.Y;
    double zDifference = point1.Z - point2.Z;

    return Math.Sqrt(xDifference * xDifference + 
        yDifference * yDifference + 
        zDifference * zDifference);
}
```

To force the compiler to pass read-only arguments by reference, 
specify the `in` modifier on the arguments at the call site, as shown in the following code:
```csharp
distance = CalculateDistance(in pt1, in pt2);
distance = CalculateDistance(in pt1, new Point3D());
distance = CalculateDistance(pt1, in Point3D.Origin);
```
This behavior makes it easier to adopt `in` parameters over time in large codebases where performance gains are possible. 
You add the `in` modifier to method signatures first. 
Then you can add the `in` modifier at call sites
and create `readonly struct` types to enable the compiler to avoid creating defensive copies of `in` parameters in more locations.

##### Avoid defensive copies

Pass a `struct` as the argument for an `in` parameter only if it's declared with the `readonly` modifier or the method accesses only `readonly` members of the struct. 
Otherwise, the compiler must create _defensive copies_ in many situations to ensure that arguments are not mutated. 
Consider the following example that calculates the distance of a 3D point from the origin:
```csharp
private static double CalculateDistance(in Point3D point1, in Point3D point2)
{
    double xDifference = point1.X - point2.X;
    double yDifference = point1.Y - point2.Y;
    double zDifference = point1.Z - point2.Z;

    return Math.Sqrt(xDifference * xDifference + 
        yDifference * yDifference + 
        zDifference * zDifference);
}
```

The `Point3D` structure is not a read-only struct. 
There are six different property access calls in the body of this method. 
On first examination, you may think these accesses are safe. 
After all, a `get` accessor shouldn't modify the state of the object. 
But there's no language rule that enforces that. 
It's only a common convention. 
Any type could implement a `get` accessor that modified the internal state.

Without some language guarantee, the compiler must create a temporary copy of the argument before calling any member not marked with the `readonly` modifier. 
The temporary storage is created on the stack, the values of the argument are copied to the temporary storage, 
and the value is copied to the stack for each member access as the `this` argument. 
In many situations, these copies harm performance enough that pass-by-value is faster 
than pass-by-read-only-reference when the argument type isn't a `readonly struct` and the method calls members that aren't marked `readonly`. 
If you mark all methods that don't modify the struct state as `readonly`, 
the compiler can safely determine that the struct state isn't modified, and a defensive copy is not needed.

If the distance calculation uses the immutable struct, `ReadonlyPoint3D`, temporary objects aren't needed:
```csharp
private static double CalculateDistance3(in ReadonlyPoint3D point1, in ReadonlyPoint3D point2 = default)
{
    double xDifference = point1.X - point2.X;
    double yDifference = point1.Y - point2.Y;
    double zDifference = point1.Z - point2.Z;

    return Math.Sqrt(xDifference * xDifference + 
        yDifference * yDifference + 
        zDifference * zDifference);
}
```

The compiler generates more efficient code when you call members of a `readonly struct`. 
The `this` reference, instead of a copy of the receiver, is always an `in` parameter passed by reference to the member method. 
This optimization saves copying when you use a `readonly struct` as an `in` argument.

Don't pass a nullable value type as an `in` argument. 
The `Nullable<T>` type isn't declared as a read-only struct. 
That means the compiler must generate defensive copies for any nullable value type argument passed to a method using the `in` modifier on the parameter declaration.

You can see an example program that demonstrates the performance differences using BenchmarkDotNet in dotnet [samples repository](https://github.com/dotnet/samples/tree/main/csharp/safe-efficient-code/benchmark) on GitHub. 
It compares passing a mutable struct by value and by reference with passing an immutable struct by value and by reference. The use of the immutable struct and pass by reference is fastest.

#### Use ref struct types

Use a ref struct or a `readonly ref struct`, such as `Span<T>` or `ReadOnlySpan<T>`, 
to work with blocks of memory as a sequence of bytes. 
The memory used by the span is constrained to a single stack frame. 
This restriction enables the compiler to make several optimizations. 
The primary motivation for this feature was `Span<T>` and related structures. 
You'll achieve performance improvements from these enhancements by using new and updated .NET APIs that make use of the Span<T> type.

Declaring a struct as `readonly ref` combines the benefits and restrictions of `ref struct` and `readonly struct` declarations. 
The memory used by the readonly span is restricted to a single stack frame, 
and the memory used by the readonly span can't be modified.

You may have similar requirements working with memory created using `stackalloc` or when using memory from interop APIs. 
You can define your own `ref struct` types for those needs.

#### Use nint and nuint types

[Native-sized integer types](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/nint-nuint) are 32-bit integers in a 32-bit process or 64-bit integers in a 64-bit process. 
Use them for interop scenarios, low-level libraries, and to optimize performance in scenarios where integer math is used extensively.

#### Conclusions

Using value types minimizes the number of allocation operations:
- Storage for value types is stack-allocated for local variables and method arguments.
- Storage for value types that are members of other objects is allocated as part of that object, not as a separate allocation.
- Storage for value type return values is stack allocated.

Contrast that with reference types in those same situations:
- Storage for reference types is heap allocated for local variables and method arguments. The reference is stored on the stack.
- Storage for reference types that are members of other objects are separately allocated on the heap. The containing object stores the reference.
- Storage for reference type return values is heap allocated. The reference to that storage is stored on the stack.

Minimizing allocations comes with tradeoffs. You copy more memory when the size of the struct is larger than the size of a reference. A reference is typically 64 bits or 32 bits, and depends on the target machine CPU.

These tradeoffs generally have minimal performance impact. However, for large structs or larger collections, the performance impact increases. The impact can be large in tight loops and hot paths for programs.

These enhancements to the C# language are designed for performance critical algorithms where minimizing memory allocations is a major factor in achieving the necessary performance. You may find that you don't often use these features in the code you write. However, these enhancements have been adopted throughout .NET. As more APIs make use of these features, you'll see the performance of your applications improve.

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
Span<byte> buffer = inputLength <= MaxStackLimit 
    ? stackalloc byte[MaxStackLimit] 
    : new byte[inputLength]; 
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
- [QueryBuilder](https://github.com/nazarovsa/csharp-zero-allocation/tree/main/src/QueryBuilders.Benchmark) - Benchmarks of building queries by different ways.
- [ObjectPools](https://github.com/nazarovsa/csharp-zero-allocation/tree/main/src/ObjectPools.Benchmark) - Benchmarks of object pool usage.
- [GuidTransformer](https://github.com/nazarovsa/csharp-zero-allocation/tree/main/src/GuidTransformer) - Optimization sample of guid to efficient string helper.
- [Abstract lottery ticket combination generator](https://github.com/nazarovsa/csharp-zero-allocation/tree/main/src/Generation) - Optimization sample of lottery ticket combination generator.

## Sources
### In English

- [`Span<T>`: docs](https://docs.microsoft.com/en-us/dotnet/api/system.span-1?view=net-6.0)
- [`ReadOnlySpan<T>`: docs](https://docs.microsoft.com/en-us/dotnet/api/system.readonlyspan-1?view=net-6.0)
- [`stackalloc`: docs](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/stackalloc)
- [`Memory<T>`: docs](https://docs.microsoft.com/en-us/dotnet/api/system.memory-1?view=net-6.0)
- [`IMemoryOwner<T>`: docs](https://docs.microsoft.com/en-us/dotnet/api/system.buffers.imemoryowner-1?view=net-6.0)
- [Write safe and efficient C# code](https://docs.microsoft.com/en-us/dotnet/csharp/write-safe-efficient-code)
- [.NET Platform Architecture. Stanislav Sidristij - Memory<T> and Span<T>](https://github.com/sidristij/dotnetbook/blob/master/book/en/MemorySpan.md)
- [What is Span in C# and why you should be using it](https://www.youtube.com/watch?v=FM5dpxJMULY)
- [Writing C# without allocating ANY memory](https://www.youtube.com/watch?v=B2yOjLyEZk0) - GuidTransformer sample taken from this video.
- [Memory pools: github repo](https://github.com/sidristij/memory-pools) - Sidristiy memory pools: including pools and traffic-free enumerable implementation.
- [ValueStringBuilder: a stack-based string-builder](https://andrewlock.net/a-deep-dive-on-stringbuilder-part-6-vaulestringbuilder-a-stack-based-string-builder/)
- [ValueStringBuilder: source code](https://github.com/dotnet/runtime/blob/main/src/libraries/Common/src/System/Text/ValueStringBuilder.cs)

### In Russian
- [Struct и readonly: как избежать падения производительности](https://habr.com/ru/company/microsoft/blog/423053/)
- [RU - Станислав Сидристый — Делаем zero-allocation код на примере оптимизации крупной библиотеки](https://www.youtube.com/watch?v=-FDfnUyYSyc)
