using Microsoft.CodeAnalysis;

namespace PackScan.Analyzer.Core.Options;

internal readonly struct OptionValue<T> : IEquatable<OptionValue<T>>
{
    public static implicit operator T(OptionValue<T> value) => value.Value;

    private readonly T _value;

    public string PropertyName { get; }
    public Diagnostic? Error { get; }

    public T Value
    {
        get => Error is not null
            ? throw new InvalidOperationException(Error.GetMessage())
            : _value;
    }

    public OptionValue(string propertyName, T value)
    {
        _value = value;

        PropertyName = propertyName;
        Error = null;
    }
    public OptionValue(string propertyName, Diagnostic error)
    {
        _value = default!;

        PropertyName = propertyName;
        Error = error;
    }

    public void Validate(ICollection<Diagnostic> diagnostics)
    {
        if (Error is not null)
            diagnostics.Add(Error);
    }

    public override bool Equals(object obj)
        => obj is OptionValue<T> other && Equals(other);
    public bool Equals(OptionValue<T> other)
    {
        return other.PropertyName == PropertyName
            && Equals(other.Error, Error)
            && EqualityComparer<T>.Default.Equals(other._value, _value);
    }
    public override int GetHashCode()
        => HashCode.Combine(PropertyName, _value, Error);

    public override string? ToString()
        => Value?.ToString();
}
