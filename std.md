# C# Zero Allocation Standard

A practical guide for writing memory-efficient C# applications.

---

# Why This Standard Exists

## The Problem

Memory allocation in .NET applications directly impacts performance. Every heap allocation has costs:
- **Allocation time** - memory must be requested and initialized
- **GC pressure** - more allocations mean more frequent garbage collections
- **GC pauses** - garbage collection can cause latency spikes in your application

## Root Causes of Memory Problems

| Cause | Description |
|-------|-------------|
| **Incorrect allocation patterns** | Creating objects in loops, temporary objects for single operations |
| **Weak platform knowledge** | Not understanding that strings are immutable, structs are copied by value |
| **Weak language feature knowledge** | Missing modern C# features that enable zero-allocation code |

## The Principle

> **"Allocate as little memory as possible"**

In many cases, we can avoid allocating memory entirely using advanced C# features.

## When to Apply This Standard

| Apply | Don't Over-Apply |
|-------|------------------|
| Hot paths (frequently executed code) | Cold paths (startup, rare operations) |
| Tight loops processing large data | Simple CRUD operations |
| High-throughput services | Prototypes and MVPs |
| Latency-sensitive applications | Code where readability is paramount |

## Two Competing Goals

Every optimization balances:

1. **Minimize heap allocations**
   - Reference types require heap allocation
   - Each allocation needs eventual GC reclamation
   - GC takes time

2. **Minimize value copying**
   - Value types are copied when passed to methods
   - Large structs = expensive copies
   - Copy time depends on struct size

---

# Structures

Value types avoid heap allocations but are copied by value. This section covers techniques to get benefits of both worlds: no heap allocations AND minimal copying.

> **Requires C# 7.2+** for most features described here.

---

## Point 1: Use the `in` Parameter Modifier

### Theory

The `in` keyword passes a value type by reference but prevents modification. It combines:
- Pass-by-reference (no copy on call)
- Read-only semantics (caller's value protected)

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **No copy on call** | Large structs passed by reference (4 or 8 bytes) |
| **Immutability guarantee** | Method cannot modify the original value |
| **Works with literals** | Can pass `new Point3D()` or constants directly |

### When to Use

Use `in` when struct size is **larger than `IntPtr.Size`** (8 bytes on x64, 4 bytes on x86).

| Struct Size | Recommendation |
|-------------|----------------|
| ≤ 8 bytes | Pass by value (copy is cheap) |
| > 8 bytes | Consider `in` parameter |
| > 16 bytes | Strongly prefer `in` parameter |

### Common Cases

- Methods receiving large structs (Point3D, Matrix4x4, Guid)
- Tight loops with struct parameters
- Mathematical operations on value types

### Examples

**Example 1: Distance calculation with `in` parameters**

```csharp
// BAD: Copies 24 bytes per parameter (Point3D = 3 doubles = 24 bytes)
public static double CalculateDistance(Point3D p1, Point3D p2)
{
    double dx = p1.X - p2.X;
    double dy = p1.Y - p2.Y;
    double dz = p1.Z - p2.Z;
    return Math.Sqrt(dx * dx + dy * dy + dz * dz);
}

// GOOD: Passes 8-byte reference instead of 24-byte struct
public static double CalculateDistance(in Point3D p1, in Point3D p2)
{
    double dx = p1.X - p2.X;
    double dy = p1.Y - p2.Y;
    double dz = p1.Z - p2.Z;
    return Math.Sqrt(dx * dx + dy * dy + dz * dz);
}
```

**Example 2: Matrix multiplication**

```csharp
public readonly struct Matrix4x4
{
    // 16 floats = 64 bytes
    private readonly float _m11, _m12, _m13, _m14;
    private readonly float _m21, _m22, _m23, _m24;
    private readonly float _m31, _m32, _m33, _m34;
    private readonly float _m41, _m42, _m43, _m44;

    // Passing 64-byte struct by reference saves significant copying
    public static Matrix4x4 Multiply(in Matrix4x4 a, in Matrix4x4 b)
    {
        // multiplication logic
    }
}
```

**Call site options:**

```csharp
var p1 = new Point3D(1, 2, 3);
var p2 = new Point3D(4, 5, 6);

// Both work - 'in' is optional at call site
var distance1 = CalculateDistance(p1, p2);
var distance2 = CalculateDistance(in p1, in p2);

// Can use with literals and default values
var fromOrigin = CalculateDistance(in p1, new Point3D());
```

---

## Point 2: Avoid Defensive Copies

### Theory

When a non-readonly struct is passed as `in` parameter, the compiler creates **defensive copies** before calling any member. This is because the compiler cannot guarantee the member won't mutate the struct.

### Why It Matters

| Scenario | What Happens |
|----------|--------------|
| `readonly struct` + `in` | No copy - compiler knows struct is immutable |
| Mutable struct + `in` | Defensive copy before each member access |
| Mutable struct + `readonly` member | No copy for that specific member |

Defensive copies can make `in` parameters **slower** than pass-by-value!

### Common Cases

- Using `in` with non-readonly structs (causes copies)
- Calling non-readonly methods on `in` parameters
- Using `Nullable<T>` with `in` (always causes copies)

### Examples

**Example 1: Defensive copy problem**

```csharp
// Non-readonly struct
public struct Point3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

public static double CalculateDistance(in Point3D p1, in Point3D p2)
{
    // PROBLEM: 6 property accesses = 6 defensive copies!
    // Compiler copies entire struct before each .X, .Y, .Z access
    double dx = p1.X - p2.X;
    double dy = p1.Y - p2.Y;
    double dz = p1.Z - p2.Z;
    return Math.Sqrt(dx * dx + dy * dy + dz * dz);
}
```

**Example 2: Solution - use readonly struct**

```csharp
// Readonly struct - no defensive copies
public readonly struct Point3D
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public Point3D(double x, double y, double z) => (X, Y, Z) = (x, y, z);
}

public static double CalculateDistance(in Point3D p1, in Point3D p2)
{
    // No defensive copies - struct is readonly
    double dx = p1.X - p2.X;
    double dy = p1.Y - p2.Y;
    double dz = p1.Z - p2.Z;
    return Math.Sqrt(dx * dx + dy * dy + dz * dz);
}
```

**Example 3: Nullable<T> always causes defensive copies**

```csharp
// BAD: Nullable<T> is not readonly - always causes defensive copies
public static void Process(in Guid? id)
{
    if (id.HasValue)  // Defensive copy here
    {
        Use(id.Value);  // Defensive copy here
    }
}

// GOOD: Pass Nullable<T> by value or use overloads
public static void Process(Guid id) { Use(id); }
public static void Process() { /* no id case */ }
```

---

## Point 3: Declare Immutable Structs as `readonly`

### Theory

The `readonly struct` modifier tells the compiler that a struct is immutable. The compiler enforces:
- All fields must be readonly
- All properties must be readonly (get-only)

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **Intent clarity** | Code explicitly communicates immutability |
| **Compiler optimization** | No defensive copies when passed as `in` parameter (see Point 2) |
| **Safety** | Compiler prevents accidental mutation |

### Common Cases

- Data transfer objects (DTOs)
- Coordinate/point structures
- Configuration values
- Any struct that should never change after creation

### Examples

**Example 1: 3D Point (immutable by design)**

```csharp
// BAD: Mutable struct - causes defensive copies with `in`
public struct Point3D
{
    public double X;
    public double Y;
    public double Z;
}

// GOOD: Readonly struct - no defensive copies
public readonly struct Point3D
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    public Point3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
```

**Example 2: Money value object**

```csharp
public readonly struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Currency mismatch");

        return new Money(Amount + other.Amount, Currency);
    }
}
```

---

## Point 4: Declare `readonly` Members for Mutable Structs

### Theory

When a struct must be mutable, mark individual members that don't modify state as `readonly`. Available in C# 8.0+.

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **Partial optimization** | Readonly members avoid defensive copies (see Point 2) |
| **Granular control** | Mutating members remain writable |
| **Documentation** | Shows which members are safe to call |

### Common Cases

- Structs with some mutable and some computed properties
- Structs where only specific setters need mutation
- `ToString()`, `GetHashCode()`, and similar methods on mutable structs

### Examples

**Example 1: Mutable point with readonly computed property**

```csharp
public struct Point3D
{
    private double _x;
    private double _y;
    private double _z;

    public double X
    {
        readonly get => _x;
        set => _x = value;
    }

    public double Y
    {
        readonly get => _y;
        set => _y = value;
    }

    public double Z
    {
        readonly get => _z;
        set => _z = value;
    }

    // Readonly method - no defensive copy when called
    public readonly double Distance => Math.Sqrt(X * X + Y * Y + Z * Z);

    public readonly override string ToString() => $"{X}, {Y}, {Z}";
}
```

**Example 2: Counter with readonly read access**

```csharp
public struct Counter
{
    private int _value;

    public int Value
    {
        readonly get => _value;
        set => _value = value;
    }

    public void Increment() => _value++;

    // Safe to mark as readonly - doesn't modify state
    public readonly bool IsZero => _value == 0;
    public readonly override string ToString() => _value.ToString();
}
```

---

## Point 5: Use `ref readonly return`

### Theory

Return a reference to a value instead of copying it. The `readonly` modifier prevents callers from modifying the returned reference.

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **No copy on return** | Large structs returned by reference |
| **Caller protection** | `readonly` prevents modification |
| **Caller choice** | Can receive as value or reference |

### Requirements

- Returned value must outlive the method (static field, class field, etc.)
- Cannot return local variables by reference

### Common Cases

- Returning static default values
- Exposing internal struct fields without copying
- Caching and returning large computed values

### Examples

**Example 1: Static origin point**

```csharp
public readonly struct Point3D
{
    public double X { get; }
    public double Y { get; }
    public double Z { get; }

    private static readonly Point3D _origin = new(0, 0, 0);

    // Returns reference to static field - no copy
    public static ref readonly Point3D Origin => ref _origin;

    public Point3D(double x, double y, double z) => (X, Y, Z) = (x, y, z);
}

// Usage:
var copy = Point3D.Origin;                    // Makes a copy
ref readonly var reference = ref Point3D.Origin;  // No copy, uses reference
```

**Example 2: Cache with ref readonly access**

```csharp
public class TransformCache
{
    private readonly Matrix4x4[] _transforms = new Matrix4x4[100];

    // Return reference to array element - no 64-byte copy
    public ref readonly Matrix4x4 GetTransform(int index)
    {
        return ref _transforms[index];
    }
}

// Usage:
ref readonly var transform = ref cache.GetTransform(5);
// Use transform without copying 64 bytes
```

---

## Point 6: Use `ref struct` Types

### Theory

A `ref struct` is a struct that can only exist on the stack. It cannot be boxed or stored on the heap. `Span<T>` and `ReadOnlySpan<T>` are ref structs.

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **Stack-only** | Guaranteed no heap allocation |
| **Performance** | Enables compiler optimizations |
| **Memory safety** | Can safely reference stack memory |

### Restrictions

| Cannot Do | Reason |
|-----------|--------|
| Box to `object` | Would move to heap |
| Use as class field | Class is on heap |
| Use in async methods | State machine is on heap |
| Use in iterators | Iterator state is on heap |
| Implement interfaces | Interface dispatch requires boxing |

### Important: Non-Standard Thinking Required

Using `Span<T>` and `ref struct` types often requires **advanced algorithms and creative solutions** because:

- **Limited API surface** - many standard library methods don't accept spans
- **No LINQ support** - cannot use standard LINQ operators
- **Restrictions cascade** - one `ref struct` in a call chain forces the entire chain to be synchronous and stack-bound
- **No collections** - cannot store in `List<T>`, `Dictionary<K,V>`, etc.

Be prepared to write custom implementations for operations that would be trivial with regular types.

### Common Cases

- Working with `Span<T>` and `ReadOnlySpan<T>`
- Creating stack-only wrappers for performance
- Building parsers and tokenizers

### Examples

**Example 1: Stack-only string tokenizer**

```csharp
public ref struct SpanTokenizer
{
    private ReadOnlySpan<char> _remaining;
    private readonly char _separator;

    public SpanTokenizer(ReadOnlySpan<char> text, char separator)
    {
        _remaining = text;
        _separator = separator;
    }

    public bool MoveNext(out ReadOnlySpan<char> token)
    {
        if (_remaining.IsEmpty)
        {
            token = default;
            return false;
        }

        int idx = _remaining.IndexOf(_separator);
        if (idx < 0)
        {
            token = _remaining;
            _remaining = default;
        }
        else
        {
            token = _remaining.Slice(0, idx);
            _remaining = _remaining.Slice(idx + 1);
        }
        return true;
    }
}

// Usage - zero allocations
var tokenizer = new SpanTokenizer("a,b,c".AsSpan(), ',');
while (tokenizer.MoveNext(out var token))
{
    Console.WriteLine(token.ToString());
}
```

**Example 2: readonly ref struct for immutable stack-only data**

```csharp
public readonly ref struct ParseResult
{
    public bool Success { get; }
    public ReadOnlySpan<char> Value { get; }
    public ReadOnlySpan<char> Remaining { get; }

    public ParseResult(bool success, ReadOnlySpan<char> value, ReadOnlySpan<char> remaining)
    {
        Success = success;
        Value = value;
        Remaining = remaining;
    }
}
```

---

## Point 7: Use `nint` and `nuint` Types

### Theory

Native-sized integers (`nint`, `nuint`) are 32-bit on 32-bit processes and 64-bit on 64-bit processes. They match the platform's pointer size.

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **Platform optimization** | Uses native word size for arithmetic |
| **Interop compatibility** | Matches native pointer sizes |
| **Index operations** | Optimal for array/span indexing |

### Common Cases

- Interop with native code
- Low-level memory operations
- Performance-critical indexing in tight loops
- Working with pointers

### Examples

**Example 1: Native-sized loop counter**

```csharp
// For very tight loops with large iterations
public static void ProcessBuffer(Span<byte> buffer)
{
    for (nint i = 0; i < buffer.Length; i++)
    {
        buffer[(int)i] = 0;
    }
}
```

**Example 2: Interop with native memory**

```csharp
public unsafe struct NativeBuffer
{
    public nint Pointer;
    public nint Length;

    public Span<byte> AsSpan()
    {
        return new Span<byte>((void*)Pointer, (int)Length);
    }
}
```

---

## Structures: Quick Reference

| # | Technique | When to Use | Benefit |
|---|-----------|-------------|---------|
| 1 | `in` parameter | Struct > 8 bytes | Pass by reference |
| 2 | Avoid defensive copies | Using `in` with structs | Prevent hidden copies |
| 3 | `readonly struct` | Immutable value types | No defensive copies |
| 4 | `readonly` members | Mutable structs with pure methods | Partial optimization |
| 5 | `ref readonly return` | Returning stored large structs | No copy on return |
| 6 | `ref struct` | Stack-only performance types | Guaranteed no heap |
| 7 | `nint`/`nuint` | Interop, low-level code | Native word size |

---

# Span, Memory, and Stack Allocation

This section covers types for working with contiguous memory regions without heap allocations.

---

## Point 1: Use `Span<T>` for Contiguous Memory

### Theory

`Span<T>` provides a type-safe and memory-safe representation of a contiguous region of memory. It's a `ref struct` allocated on the stack.

A `Span<T>` can point to:
- Managed memory (arrays)
- Native memory (via pointers)
- Stack memory (via `stackalloc`)

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **Unified API** | Same code works with arrays, stack, and native memory |
| **Zero-copy slicing** | Create subviews without allocating new arrays |
| **Type safety** | Bounds checking, no unsafe code needed |
| **Performance** | Stack-allocated, no GC pressure |

### Restrictions (ref struct)

- Cannot be boxed
- Cannot be a field in a class
- Cannot be used across `await`/`yield`
- Cannot be stored in collections

### Common Cases

- Parsing strings without substring allocations
- Processing byte buffers
- Working with array slices
- Interop with native code

### Examples

**Example 1: Zero-copy array slicing**

```csharp
// BAD: Creates new array allocation
public static int[] GetMiddleElements(int[] array, int start, int count)
{
    var result = new int[count];  // Heap allocation!
    Array.Copy(array, start, result, 0, count);
    return result;
}

// GOOD: Returns view into existing array - no allocation
public static Span<int> GetMiddleElements(int[] array, int start, int count)
{
    return new Span<int>(array, start, count);
}

// Usage
var array = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
Span<int> middle = GetMiddleElements(array, 3, 4);  // View of [4, 5, 6, 7]

// Modifications affect original array
middle[0] = 100;  // array[3] is now 100
```

**Example 2: Parsing without allocations**

```csharp
// BAD: String.Split allocates array + strings
public static (string key, string value) ParseHeader(string header)
{
    var parts = header.Split(':');  // Allocates array and strings!
    return (parts[0].Trim(), parts[1].Trim());
}

// GOOD: Span-based parsing - zero allocations
public static (ReadOnlySpan<char> key, ReadOnlySpan<char> value) ParseHeader(
    ReadOnlySpan<char> header)
{
    int colonIndex = header.IndexOf(':');
    var key = header.Slice(0, colonIndex).Trim();
    var value = header.Slice(colonIndex + 1).Trim();
    return (key, value);
}
```

---

## Point 2: Use `ReadOnlySpan<T>` for Read-Only Access

### Theory

`ReadOnlySpan<T>` is the immutable counterpart to `Span<T>`. It provides read-only access to contiguous memory and has the same restrictions as `Span<T>`.

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **Intent clarity** | Signals data won't be modified |
| **String compatibility** | `string.AsSpan()` returns `ReadOnlySpan<char>` |
| **Safety** | Compiler prevents accidental modification |

### Common Cases

- Parsing and reading string data
- Processing immutable buffers
- API design where mutation isn't needed

### Examples

**Example 1: String processing without allocation**

```csharp
// BAD: Substring allocates new string
public static int GetContentLength(string header)
{
    // "Content-Length: 132"
    string value = header.Substring(16);  // Allocates!
    return int.Parse(value);
}

// GOOD: Span-based - zero allocation
public static int GetContentLength(ReadOnlySpan<char> header)
{
    ReadOnlySpan<char> value = header.Slice(16);
    return int.Parse(value);
}

// Usage
string header = "Content-Length: 132";
int length = GetContentLength(header.AsSpan());  // No allocation
```

**Example 2: Searching in strings**

```csharp
public static bool ContainsIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<char> value)
{
    return text.Contains(value, StringComparison.OrdinalIgnoreCase);
}

// Usage - no allocations
bool found = ContainsIgnoreCase("Hello World".AsSpan(), "world".AsSpan());
```

---

## Point 3: Use `stackalloc` for Small Temporary Buffers

### Theory

`stackalloc` allocates memory on the stack instead of the heap. Stack memory is automatically freed when the method returns - no GC involved.

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **No GC pressure** | Memory freed automatically on method exit |
| **Fast allocation** | Stack allocation is just pointer adjustment |
| **Locality** | Stack memory has good cache locality |

### Rules and Limits

| Rule | Description |
|------|-------------|
| **Size limit** | Keep allocations small (< 1KB recommended) |
| **No loops** | Never `stackalloc` inside loops |
| **Fallback pattern** | Use heap for large/unknown sizes |

### Common Cases

- Small temporary buffers for formatting
- Parsing buffers with known max size
- Temporary storage for cryptographic operations

### Examples

**Example 1: Safe stackalloc with fallback**

```csharp
public static string FormatGuid(Guid guid)
{
    // 36 chars for guid format "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
    Span<char> buffer = stackalloc char[36];

    guid.TryFormat(buffer, out int charsWritten);
    return new string(buffer.Slice(0, charsWritten));
}
```

**Example 2: Conditional stack/heap allocation**

```csharp
public static void ProcessData(int size)
{
    const int MaxStackSize = 1024;

    // Stack for small, heap for large
    Span<byte> buffer = size <= MaxStackSize
        ? stackalloc byte[MaxStackSize]
        : new byte[size];

    // Use only the needed portion
    buffer = buffer.Slice(0, size);

    // Process buffer...
    ProcessBuffer(buffer);
}
```

**Example 3: Avoid stackalloc in loops**

```csharp
// BAD: stackalloc in loop - will overflow stack!
public static void BadExample(string[] items)
{
    foreach (var item in items)
    {
        Span<char> buffer = stackalloc char[256];  // DANGER!
        // ...
    }
}

// GOOD: stackalloc outside loop
public static void GoodExample(string[] items)
{
    Span<char> buffer = stackalloc char[256];  // Once, outside loop
    foreach (var item in items)
    {
        buffer.Clear();
        // Reuse buffer...
    }
}
```

---

## Point 4: Use `Memory<T>` When Span Cannot Be Used

### Theory

`Memory<T>` represents a contiguous memory region like `Span<T>`, but it's **not a ref struct**. This means it can be stored on the heap, used in async methods, and stored in collections.

### Why It Matters

| Span<T> | Memory<T> |
|---------|-----------|
| Stack only | Can be on heap |
| Cannot use in async | Works with async/await |
| Cannot store in class fields | Can be a field |
| Direct access to memory | Access via `.Span` property |

### When to Use Memory<T>

Use `Memory<T>` when you need span-like functionality but:
- Need to store in a class field
- Need to pass across `await` boundaries
- Need to store in collections

### Common Cases

- Async I/O operations with buffers
- Storing memory slices in objects
- Pipeline-style data processing

### Examples

**Example 1: Async buffer processing**

```csharp
// Cannot use Span<T> in async method
public async Task ProcessDataAsync(Memory<byte> buffer)
{
    // Memory<T> works across await
    await stream.ReadAsync(buffer);

    // Get Span for synchronous processing
    ProcessSynchronously(buffer.Span);
}

private void ProcessSynchronously(Span<byte> data)
{
    // Fast span-based processing
    for (int i = 0; i < data.Length; i++)
    {
        data[i] = (byte)(data[i] ^ 0xFF);
    }
}
```

**Example 2: Storing memory slice in a class**

```csharp
public class BufferSegment
{
    // Cannot store Span<T> as field - use Memory<T>
    private readonly Memory<byte> _data;

    public BufferSegment(Memory<byte> data)
    {
        _data = data;
    }

    public void Process()
    {
        // Get Span for actual work
        Span<byte> span = _data.Span;
        // Process span...
    }
}
```

---

## Point 5: Use `ReadOnlyMemory<T>` for Immutable Async Buffers

### Theory

`ReadOnlyMemory<T>` is to `Memory<T>` what `ReadOnlySpan<T>` is to `Span<T>` - an immutable version that can be stored on the heap.

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **Async-compatible** | Works across await boundaries |
| **Storable** | Can be field in class |
| **Immutable** | Prevents accidental modification |

### Common Cases

- Storing immutable data chunks
- Async string/text processing
- Read-only buffer pipelines

### Examples

**Example 1: Async text processing**

```csharp
public class TextProcessor
{
    private readonly ReadOnlyMemory<char> _text;

    public TextProcessor(string text)
    {
        _text = text.AsMemory();  // No allocation, just wraps string
    }

    public async Task<int> CountWordsAsync()
    {
        await Task.Yield();  // Simulate async work

        ReadOnlySpan<char> span = _text.Span;
        int count = 0;
        bool inWord = false;

        foreach (char c in span)
        {
            if (char.IsWhiteSpace(c))
            {
                inWord = false;
            }
            else if (!inWord)
            {
                inWord = true;
                count++;
            }
        }

        return count;
    }
}
```

**Example 2: Slicing without allocation**

```csharp
public static ReadOnlyMemory<char> GetFirstLine(ReadOnlyMemory<char> text)
{
    int newlineIndex = text.Span.IndexOf('\n');
    return newlineIndex < 0 ? text : text.Slice(0, newlineIndex);
}

// Usage in async context
public async Task ProcessFirstLineAsync(string content)
{
    ReadOnlyMemory<char> firstLine = GetFirstLine(content.AsMemory());
    await ProcessLineAsync(firstLine);
}
```

---

## Point 6: Use `IMemoryOwner<T>` for Pooled Memory Management

### Theory

`IMemoryOwner<T>` represents ownership of a `Memory<T>` block, typically rented from `MemoryPool<T>`. It implements `IDisposable` to return memory to the pool.

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **Pooled memory** | Reuses allocations, reduces GC |
| **Clear ownership** | Single owner responsible for disposal |
| **Dispose pattern** | Familiar cleanup mechanism |

### Rules

- Only one owner at a time
- Owner must dispose when done
- Don't access memory after disposal

### Common Cases

- High-throughput buffer processing
- Network I/O with pooled buffers
- Temporary large allocations

### Examples

**Example 1: Using MemoryPool with IMemoryOwner**

```csharp
public static async Task ProcessLargeDataAsync(Stream stream, int bufferSize)
{
    // Rent from pool instead of allocating
    using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(bufferSize);
    Memory<byte> buffer = owner.Memory.Slice(0, bufferSize);

    int bytesRead;
    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
    {
        ProcessChunk(buffer.Slice(0, bytesRead).Span);
    }
    // Memory returned to pool when owner is disposed
}
```

**Example 2: Transferring ownership**

```csharp
public class DataChunk : IDisposable
{
    private IMemoryOwner<byte>? _owner;
    private Memory<byte> _data;

    private DataChunk(IMemoryOwner<byte> owner, int length)
    {
        _owner = owner;
        _data = owner.Memory.Slice(0, length);
    }

    public static DataChunk Create(int size)
    {
        var owner = MemoryPool<byte>.Shared.Rent(size);
        return new DataChunk(owner, size);
    }

    public Memory<byte> Data => _data;

    public void Dispose()
    {
        _owner?.Dispose();
        _owner = null;
        _data = default;
    }
}
```

---

## Span/Memory: Quick Reference

| # | Type | Stack-Only | Async | Store in Class | Use Case |
|---|------|------------|-------|----------------|----------|
| 1 | `Span<T>` | Yes | No | No | Fast synchronous processing |
| 2 | `ReadOnlySpan<T>` | Yes | No | No | Read-only synchronous access |
| 3 | `stackalloc` | Yes | No | No | Small temporary buffers |
| 4 | `Memory<T>` | No | Yes | Yes | Async buffer operations |
| 5 | `ReadOnlyMemory<T>` | No | Yes | Yes | Async read-only buffers |
| 6 | `IMemoryOwner<T>` | No | Yes | Yes | Pooled memory management |

### Decision Tree

```
Need contiguous memory access?
├── Synchronous code only?
│   ├── Need to modify? → Span<T>
│   └── Read-only? → ReadOnlySpan<T>
│       └── Need temp buffer? → stackalloc + Span<T>
└── Async or need to store?
    ├── Need to modify? → Memory<T>
    └── Read-only? → ReadOnlyMemory<T>
        └── Need pooling? → IMemoryOwner<T>
```

---

# Pooling

Pooling is a technique to reuse objects instead of creating and destroying them repeatedly. This reduces GC pressure and allocation overhead.

---

## Point 1: Understand Object Pooling Pattern

### Theory

Object pooling is a creational design pattern that maintains a set of initialized objects ready for use. Instead of allocating and deallocating objects on demand, clients borrow objects from the pool and return them when done.

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **Reduced allocations** | Objects are reused, not created each time |
| **Less GC pressure** | Fewer objects to collect |
| **Predictable performance** | No allocation spikes during hot paths |
| **Amortized cost** | Initial allocation cost spread over many uses |

### When to Use

| Good Candidates | Poor Candidates |
|-----------------|-----------------|
| Expensive to create objects | Cheap, small objects |
| Frequently created/destroyed | Rarely used objects |
| Objects with consistent lifecycle | Objects with variable lifetimes |
| High-throughput scenarios | Low-frequency operations |

### Common Cases

- Database connections
- HTTP clients
- StringBuilder instances
- Buffer arrays
- Regex instances (in high-throughput parsing)

### Examples

**Example 1: Simple object pool with ObjectPool<T>**

```csharp
using Microsoft.Extensions.ObjectPool;

// Define a policy for creating and resetting objects
public class StringBuilderPooledObjectPolicy : PooledObjectPolicy<StringBuilder>
{
    public override StringBuilder Create() => new StringBuilder();

    public override bool Return(StringBuilder obj)
    {
        obj.Clear();
        return true;  // Object can be reused
    }
}

// Usage
public class ReportGenerator
{
    private readonly ObjectPool<StringBuilder> _pool;

    public ReportGenerator(ObjectPool<StringBuilder> pool)
    {
        _pool = pool;
    }

    public string GenerateReport(IEnumerable<string> items)
    {
        StringBuilder sb = _pool.Get();
        try
        {
            foreach (var item in items)
            {
                sb.AppendLine(item);
            }
            return sb.ToString();
        }
        finally
        {
            _pool.Return(sb);  // Return to pool for reuse
        }
    }
}

// Setup with DI
services.AddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
services.AddSingleton(sp =>
{
    var provider = sp.GetRequiredService<ObjectPoolProvider>();
    return provider.Create(new StringBuilderPooledObjectPolicy());
});
```

**Example 2: Custom simple pool**

```csharp
public class SimplePool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _objects = new();
    private readonly Func<T> _factory;
    private readonly Action<T>? _reset;
    private readonly int _maxSize;

    public SimplePool(int maxSize = 100, Func<T>? factory = null, Action<T>? reset = null)
    {
        _maxSize = maxSize;
        _factory = factory ?? (() => new T());
        _reset = reset;
    }

    public T Rent()
    {
        return _objects.TryTake(out var item) ? item : _factory();
    }

    public void Return(T item)
    {
        _reset?.Invoke(item);
        if (_objects.Count < _maxSize)
        {
            _objects.Add(item);
        }
    }
}

// Usage
var pool = new SimplePool<List<int>>(
    maxSize: 50,
    factory: () => new List<int>(capacity: 100),
    reset: list => list.Clear()
);

var list = pool.Rent();
try
{
    list.Add(1);
    list.Add(2);
    // Use list...
}
finally
{
    pool.Return(list);
}
```

---

## Point 2: Use `ArrayPool<T>` for Temporary Arrays

### Theory

`ArrayPool<T>` provides a pool of reusable arrays. Instead of allocating new arrays, you rent them from the pool and return them when done.

### Why It Matters

| Benefit | Description |
|---------|-------------|
| **Zero allocations** | Arrays are reused from pool |
| **Thread-safe** | Built-in synchronization |
| **Shared instance** | `ArrayPool<T>.Shared` available globally |
| **Configurable** | Can create custom pools with specific sizes |

### Important Rules

| Rule | Description |
|------|-------------|
| **Return what you rent** | Always return arrays to the pool |
| **Use try/finally** | Ensure return even on exceptions |
| **Don't hold references** | After return, array may be given to others |
| **Clear if sensitive** | Use `clearArray: true` for sensitive data |
| **Size may differ** | Rented array may be larger than requested |

### Common Cases

- Temporary buffers in I/O operations
- Intermediate arrays in algorithms
- Buffers for serialization/deserialization
- Any repeated array allocation in hot paths

### Examples

**Example 1: Basic ArrayPool usage**

```csharp
// BAD: Allocates new array each call
public void ProcessDataBad(Stream stream)
{
    var buffer = new byte[4096];  // Heap allocation every call!
    int bytesRead;
    while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
    {
        Process(buffer, bytesRead);
    }
}

// GOOD: Reuses array from pool
public void ProcessDataGood(Stream stream)
{
    byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
    try
    {
        int bytesRead;
        while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            Process(buffer, bytesRead);
        }
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
```

**Example 2: Handling variable-size arrays**

```csharp
public static string ProcessItems(IReadOnlyList<int> items)
{
    // Rent array - may be larger than requested!
    int[] buffer = ArrayPool<int>.Shared.Rent(items.Count);
    try
    {
        // Copy items to buffer
        for (int i = 0; i < items.Count; i++)
        {
            buffer[i] = items[i] * 2;
        }

        // IMPORTANT: Use items.Count, not buffer.Length!
        return string.Join(", ", buffer.Take(items.Count));
    }
    finally
    {
        ArrayPool<int>.Shared.Return(buffer);
    }
}
```

**Example 3: Clear sensitive data**

```csharp
public static void ProcessPassword(string password)
{
    char[] buffer = ArrayPool<char>.Shared.Rent(password.Length);
    try
    {
        password.CopyTo(0, buffer, 0, password.Length);
        // Process password...
        HashPassword(buffer.AsSpan(0, password.Length));
    }
    finally
    {
        // Clear sensitive data before returning to pool!
        ArrayPool<char>.Shared.Return(buffer, clearArray: true);
    }
}
```

---

## Point 3: Use `MemoryPool<T>` for Async Scenarios

### Theory

`MemoryPool<T>` is similar to `ArrayPool<T>` but returns `IMemoryOwner<T>` which wraps the rented memory. This provides better ownership semantics and works well with async code.

### Why It Matters

| ArrayPool<T> | MemoryPool<T> |
|--------------|---------------|
| Returns `T[]` | Returns `IMemoryOwner<T>` |
| Manual return | Dispose returns to pool |
| Raw array access | `Memory<T>` wrapper |
| More control | Cleaner async patterns |

### Common Cases

- Async I/O operations
- Pipeline-style processing
- When ownership transfer is needed
- Code using `Memory<T>` APIs

### Examples

**Example 1: Async stream processing with MemoryPool**

```csharp
public static async Task<int> CountBytesAsync(Stream stream)
{
    int totalBytes = 0;

    // using ensures memory is returned to pool
    using IMemoryOwner<byte> owner = MemoryPool<byte>.Shared.Rent(4096);
    Memory<byte> buffer = owner.Memory;

    int bytesRead;
    while ((bytesRead = await stream.ReadAsync(buffer)) > 0)
    {
        totalBytes += bytesRead;
    }

    return totalBytes;
}
```

**Example 2: Processing pipeline with ownership transfer**

```csharp
public class DataProcessor
{
    public async Task ProcessAsync(Stream input, Stream output)
    {
        using var owner = MemoryPool<byte>.Shared.Rent(8192);
        var buffer = owner.Memory;

        int bytesRead;
        while ((bytesRead = await input.ReadAsync(buffer)) > 0)
        {
            var chunk = buffer.Slice(0, bytesRead);

            // Transform in place
            Transform(chunk.Span);

            // Write transformed data
            await output.WriteAsync(chunk);
        }
    }

    private void Transform(Span<byte> data)
    {
        for (int i = 0; i < data.Length; i++)
        {
            data[i] = (byte)(data[i] ^ 0xFF);
        }
    }
}
```

---

## Point 4: Create Custom Pools for Specific Needs

### Theory

Sometimes built-in pools don't fit your requirements. Custom pools can optimize for specific object types, sizes, or usage patterns.

### When to Create Custom Pools

| Scenario | Reason |
|----------|--------|
| **Specific reset logic** | Objects need custom cleanup |
| **Size constraints** | Need exact buffer sizes |
| **Warm-up requirements** | Pre-allocate objects at startup |
| **Metrics/monitoring** | Track pool usage |

### Examples

**Example 1: Pre-warmed pool with size limits**

```csharp
public sealed class BoundedPool<T> : IDisposable where T : class
{
    private readonly ConcurrentQueue<T> _pool = new();
    private readonly Func<T> _factory;
    private readonly Action<T>? _reset;
    private readonly Action<T>? _destroy;
    private readonly int _maxSize;
    private int _currentCount;

    public BoundedPool(
        int maxSize,
        int warmupCount,
        Func<T> factory,
        Action<T>? reset = null,
        Action<T>? destroy = null)
    {
        _maxSize = maxSize;
        _factory = factory;
        _reset = reset;
        _destroy = destroy;

        // Pre-warm the pool
        for (int i = 0; i < Math.Min(warmupCount, maxSize); i++)
        {
            _pool.Enqueue(_factory());
            _currentCount++;
        }
    }

    public T Rent()
    {
        if (_pool.TryDequeue(out var item))
        {
            return item;
        }

        // Pool empty - create new if under limit
        if (Interlocked.Increment(ref _currentCount) <= _maxSize)
        {
            return _factory();
        }

        Interlocked.Decrement(ref _currentCount);
        throw new InvalidOperationException("Pool exhausted");
    }

    public void Return(T item)
    {
        _reset?.Invoke(item);
        _pool.Enqueue(item);
    }

    public void Dispose()
    {
        while (_pool.TryDequeue(out var item))
        {
            _destroy?.Invoke(item);
        }
    }
}

// Usage
var pool = new BoundedPool<MemoryStream>(
    maxSize: 100,
    warmupCount: 10,
    factory: () => new MemoryStream(capacity: 4096),
    reset: ms => { ms.Position = 0; ms.SetLength(0); },
    destroy: ms => ms.Dispose()
);
```

**Example 2: Pool with automatic return (lease pattern)**

```csharp
public readonly struct PooledLease<T> : IDisposable where T : class
{
    private readonly Action<T> _returnAction;

    public T Value { get; }

    public PooledLease(T value, Action<T> returnAction)
    {
        Value = value;
        _returnAction = returnAction;
    }

    public void Dispose() => _returnAction(Value);
}

public class LeasePool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _pool = new();
    private readonly Action<T>? _reset;

    public LeasePool(Action<T>? reset = null)
    {
        _reset = reset;
    }

    public PooledLease<T> Rent()
    {
        var item = _pool.TryTake(out var pooled) ? pooled : new T();
        return new PooledLease<T>(item, Return);
    }

    private void Return(T item)
    {
        _reset?.Invoke(item);
        _pool.Add(item);
    }
}

// Usage - automatic return with using
var pool = new LeasePool<StringBuilder>(sb => sb.Clear());

using (var lease = pool.Rent())
{
    lease.Value.Append("Hello");
    lease.Value.Append(" World");
    Console.WriteLine(lease.Value.ToString());
}  // Automatically returned to pool
```

---

## Pooling: Quick Reference

| # | Type | Use Case | Return Method |
|---|------|----------|---------------|
| 1 | `ObjectPool<T>` | General object reuse | `pool.Return(obj)` |
| 2 | `ArrayPool<T>` | Temporary arrays | `pool.Return(array)` |
| 3 | `MemoryPool<T>` | Async memory operations | `owner.Dispose()` |
| 4 | Custom Pool | Specific requirements | Custom |

### Pooling Checklist

- [ ] Always return rented objects/arrays
- [ ] Use try/finally or using to ensure return
- [ ] Clear sensitive data before returning
- [ ] Don't hold references after returning
- [ ] Remember: rented array size may be larger than requested
- [ ] Consider pre-warming pools for predictable latency
- [ ] Monitor pool hit/miss rates in production

---

# Checklists

## Quick Decision Checklist

### Should I optimize this code?

- [ ] Is this a hot path (frequently executed)?
- [ ] Have I measured and identified a bottleneck?
- [ ] Is memory allocation the actual problem?
- [ ] Will optimization significantly improve user experience?

If all yes → proceed with optimization.

### Struct vs Class?

- [ ] Is the data immutable or rarely mutated? → Consider struct
- [ ] Is the size ≤ 16 bytes? → Struct is efficient
- [ ] Will it be passed frequently to methods? → Consider struct + `in`
- [ ] Need inheritance or polymorphism? → Use class
- [ ] Need null as a valid state? → Use class (or `Nullable<T>`)

### Which Span/Memory type?

```
Is code synchronous only?
├── Yes → Use Span<T> or ReadOnlySpan<T>
└── No (async/stored) → Use Memory<T> or ReadOnlyMemory<T>

Need to modify data?
├── Yes → Use Span<T> or Memory<T>
└── No → Use ReadOnlySpan<T> or ReadOnlyMemory<T>
```

### Should I use pooling?

- [ ] Object is expensive to create?
- [ ] Object is created/destroyed frequently?
- [ ] Object lifecycle is predictable?
- [ ] I can ensure proper return to pool?

If all yes → use pooling.

---

## Code Review Checklist

### Structures

- [ ] Immutable structs marked as `readonly struct`
- [ ] Large structs (>8 bytes) passed with `in` modifier
- [ ] Methods on mutable structs that don't modify state marked `readonly`
- [ ] No `Nullable<T>` with `in` parameters (causes defensive copies)

### Span/Memory

- [ ] Using `Span<T>` instead of creating new arrays for slices
- [ ] Using `ReadOnlySpan<char>` for string parsing instead of `Substring`
- [ ] `stackalloc` only for small, fixed-size buffers (< 1KB)
- [ ] `stackalloc` never inside loops
- [ ] `Memory<T>` used for async scenarios, not `Span<T>`

### Pooling

- [ ] `ArrayPool<T>.Shared.Return()` called in finally block
- [ ] Sensitive data cleared before returning to pool
- [ ] Using actual data length, not rented array length
- [ ] No references held to returned pool objects

### General

- [ ] No allocations in tight loops
- [ ] String concatenation uses `StringBuilder` or interpolation (not `+` in loops)
- [ ] LINQ avoided in hot paths (use loops instead)
- [ ] No boxing of value types (watch for `object` parameters)

---

## Common Mistakes to Avoid

| Mistake | Problem | Solution |
|---------|---------|----------|
| `in` with mutable struct | Defensive copies | Use `readonly struct` |
| `in` with `Nullable<T>` | Always copies | Pass by value |
| `stackalloc` in loop | Stack overflow | Allocate once, reuse |
| Forgetting pool return | Memory leak | Use try/finally or using |
| Using rented array `.Length` | Processing garbage | Track actual data length |
| `Span<T>` in async method | Compile error | Use `Memory<T>` |
| Storing `Span<T>` in field | Compile error | Use `Memory<T>` |
| Substring in parsing | Allocations | Use `Span<char>.Slice` |

---

## Performance Testing Reminder

Before and after optimization:

1. **Measure first** - Use BenchmarkDotNet for micro-benchmarks
2. **Profile allocations** - Use dotMemory, PerfView, or VS Diagnostics
3. **Test realistic scenarios** - Micro-benchmarks can be misleading
4. **Consider maintenance cost** - Optimize only where it matters

```csharp
// Example BenchmarkDotNet setup
[MemoryDiagnoser]
public class MyBenchmarks
{
    [Benchmark(Baseline = true)]
    public string OriginalMethod() { /* ... */ }

    [Benchmark]
    public string OptimizedMethod() { /* ... */ }
}
```

---

# Summary

## The Three Pillars

| Pillar | Key Types | Main Benefit |
|--------|-----------|--------------|
| **Structures** | `readonly struct`, `in`, `ref` | Avoid heap allocations |
| **Span/Memory** | `Span<T>`, `Memory<T>`, `stackalloc` | Zero-copy operations |
| **Pooling** | `ArrayPool<T>`, `ObjectPool<T>` | Reuse allocations |

## Golden Rules

1. **Measure before optimizing** - Don't guess where problems are
2. **Prefer `readonly struct`** - Prevents defensive copies
3. **Use `in` for large structs** - Pass by reference, not value
4. **Prefer `Span<T>` over arrays** - Zero-copy slicing
5. **Pool frequently allocated objects** - Reduce GC pressure
6. **Always return pooled resources** - Use try/finally or using
7. **Keep stack allocations small** - < 1KB, never in loops
8. **Profile after optimizing** - Verify improvements

