namespace ZeroAllocation.Core;

public ref partial struct ValueStringBuilder
{
    internal void AppendSpanFormattable<T>(T value, string? format = null, IFormatProvider? provider = null)
        where T : ISpanFormattable
    {
        if (value.TryFormat(_chars.Slice(_pos), out int charsWritten, format, provider))
        {
            _pos += charsWritten;
        }
        else
        {
            Append(value.ToString(format, provider));
        }
    }
}