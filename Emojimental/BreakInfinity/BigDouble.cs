using System.Globalization;

namespace BreakInfinity;

/// <summary>
/// A lightweight base-10 floating type for incremental/idle style numbers.
/// Stores values as <c>Mantissa * 10^Exponent</c> with <c>1 &lt;= |Mantissa| &lt; 10</c> (unless zero).
/// </summary>
public readonly struct BigDouble :
    IEquatable<BigDouble>,
    IComparable<BigDouble>,
    IFormattable
{
    public double Mantissa { get; }
    public int Exponent { get; }

    public static BigDouble Zero => default;
    public static BigDouble One => new(1);

    public BigDouble(double value)
    {
        if (double.IsNaN(value) || value == 0d)
        {
            Mantissa = 0d;
            Exponent = 0;
            return;
        }

        if (double.IsInfinity(value))
            value = double.MaxValue * Math.Sign(value);

        var abs = Math.Abs(value);
        var exp = (int)Math.Floor(Math.Log10(abs));
        var man = value / Pow10(exp);
        Normalize(man, exp, out var mNorm, out var eNorm);
        Mantissa = mNorm;
        Exponent = eNorm;
    }

    public BigDouble(double mantissa, int exponent)
    {
        Normalize(mantissa, exponent, out var mNorm, out var eNorm);
        Mantissa = mNorm;
        Exponent = eNorm;
    }

    public static implicit operator BigDouble(int value) => new(value);
    public static implicit operator BigDouble(long value) => new(value);
    public static implicit operator BigDouble(float value) => new(value);
    public static implicit operator BigDouble(double value) => new(value);
    public static implicit operator BigDouble(decimal value) => new((double)value);

    public bool Equals(BigDouble other)
        => Mantissa.Equals(other.Mantissa) && Exponent == other.Exponent;

    public override bool Equals(object? obj)
        => obj is BigDouble other && Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(Mantissa, Exponent);

    public int CompareTo(BigDouble other)
    {
        if (Mantissa == 0d)
            return other.Mantissa == 0d ? 0 : -Math.Sign(other.Mantissa);
        if (other.Mantissa == 0d)
            return Math.Sign(Mantissa);

        var signA = Math.Sign(Mantissa);
        var signB = Math.Sign(other.Mantissa);
        if (signA != signB)
            return signA.CompareTo(signB);

        // Same sign: compare exponents first, then mantissas.
        if (Exponent != other.Exponent)
        {
            var expCompare = Exponent.CompareTo(other.Exponent);
            return signA > 0 ? expCompare : -expCompare;
        }

        var manCompare = Mantissa.CompareTo(other.Mantissa);
        return signA > 0 ? manCompare : -manCompare;
    }

    public static bool operator ==(BigDouble left, BigDouble right) => left.Equals(right);
    public static bool operator !=(BigDouble left, BigDouble right) => !left.Equals(right);
    public static bool operator <(BigDouble left, BigDouble right) => left.CompareTo(right) < 0;
    public static bool operator >(BigDouble left, BigDouble right) => left.CompareTo(right) > 0;
    public static bool operator <=(BigDouble left, BigDouble right) => left.CompareTo(right) <= 0;
    public static bool operator >=(BigDouble left, BigDouble right) => left.CompareTo(right) >= 0;

    public static BigDouble operator -(BigDouble value)
        => new(-value.Mantissa, value.Exponent);

    public static BigDouble operator +(BigDouble left, BigDouble right)
    {
        if (left.Mantissa == 0d) return right;
        if (right.Mantissa == 0d) return left;

        // Align to the larger exponent.
        if (left.Exponent < right.Exponent)
            (left, right) = (right, left);

        var diff = left.Exponent - right.Exponent;

        // Too far apart: smaller term doesn't matter at double precision.
        if (diff >= 20)
            return left;

        var scaled = right.Mantissa / Pow10(diff);
        return new BigDouble(left.Mantissa + scaled, left.Exponent);
    }

    public static BigDouble operator -(BigDouble left, BigDouble right)
        => left + (-right);

    public static BigDouble operator *(BigDouble left, BigDouble right)
    {
        if (left.Mantissa == 0d || right.Mantissa == 0d)
            return Zero;

        return new BigDouble(left.Mantissa * right.Mantissa, left.Exponent + right.Exponent);
    }

    public static BigDouble operator /(BigDouble left, BigDouble right)
    {
        if (right.Mantissa == 0d)
            throw new DivideByZeroException();
        if (left.Mantissa == 0d)
            return Zero;

        return new BigDouble(left.Mantissa / right.Mantissa, left.Exponent - right.Exponent);
    }

    public string Display()
    {
        if (Mantissa == 0d)
            return "0";

        if (Exponent < -324)
            return "0.00";

        if (Exponent < 3)
        {
            var d = Mantissa * Pow10(Exponent);
            return d.ToString("0.00", CultureInfo.InvariantCulture);
        }

        if (Exponent < 6)
        {
            var d = Mantissa * Pow10(Exponent);
            d = Math.Round(d, 9, MidpointRounding.AwayFromZero);
            d = Math.Truncate(d);
            return d.ToString("0", CultureInfo.InvariantCulture);
        }

        var m = Mantissa;
        var e = Exponent;

        var rounded = Math.Round(m, 3, MidpointRounding.AwayFromZero);
        if (Math.Abs(rounded) >= 10d)
        {
            rounded /= 10d;
            e += 1;
        }

        return rounded.ToString("0.000", CultureInfo.InvariantCulture) + "E" + e.ToString(CultureInfo.InvariantCulture);
    }

    public string TS() => Display();

    public double ToDouble()
    {
        if (Mantissa == 0d)
            return 0d;

        // Avoid overflow/underflow where possible.
        if (Exponent is > 308 or < -324)
            return Mantissa > 0 ? double.PositiveInfinity : double.NegativeInfinity;

        return Mantissa * Pow10(Exponent);
    }

    public override string ToString()
        => ToString(null, CultureInfo.InvariantCulture);

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        var provider = formatProvider ?? CultureInfo.InvariantCulture;

        if (Mantissa == 0d)
            return 0d.ToString(provider);

        // Default / General
        if (string.IsNullOrWhiteSpace(format))
            return FormatGeneral(significantDigits: 4, provider);

        // Support G / G{n} as "significant digits".
        if (format.StartsWith("G", StringComparison.OrdinalIgnoreCase))
        {
            var sig = 4;
            if (format.Length > 1 && int.TryParse(format[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                sig = Math.Clamp(parsed, 1, 17);

            return FormatGeneral(sig, provider);
        }

        // Support E / E{n} similarly to double: {n} = digits after decimal.
        if (format.StartsWith("E", StringComparison.OrdinalIgnoreCase))
        {
            var decimals = 3;
            if (format.Length > 1 && int.TryParse(format[1..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                decimals = Math.Clamp(parsed, 0, 15);

            return FormatScientific(decimals, provider);
        }

        // For other numeric formats (F2, N0, custom patterns...), fall back to double when representable.
        var asDouble = ToDouble();
        if (!double.IsInfinity(asDouble) && !double.IsNaN(asDouble))
            return asDouble.ToString(format, provider);

        // Otherwise, fall back to a stable scientific format.
        return FormatGeneral(significantDigits: 4, provider);
    }

    private string FormatGeneral(int significantDigits, IFormatProvider provider)
    {
        // If it reasonably fits into a double without losing magnitude, show the full number.
        // This mirrors typical idle-game UI expectations: small numbers as plain, large as scientific.
        if (Exponent >= -4 && Exponent < 6)
        {
            var d = ToDouble();
            if (!double.IsInfinity(d) && !double.IsNaN(d))
                return d.ToString("G", provider);
        }

        var decimals = Math.Max(0, significantDigits - 1);
        return FormatScientific(decimals, provider);
    }

    private string FormatScientific(int decimals, IFormatProvider provider)
    {
        var m = Mantissa;
        var e = Exponent;

        // Apply rounding at the mantissa level so we can fix 9.999 -> 10.0 carry.
        var rounded = Math.Round(m, decimals, MidpointRounding.AwayFromZero);
        if (Math.Abs(rounded) >= 10d)
        {
            rounded /= 10d;
            e += 1;
        }

        var mantissaFormat = decimals == 0
            ? "0"
            : "0." + new string('#', decimals);

        var mantissaText = rounded.ToString(mantissaFormat, provider);
        return mantissaText + "E" + e.ToString(CultureInfo.InvariantCulture);
    }

    public static BigDouble Floor(BigDouble value)
    {
        if (value.Exponent < 0)
        {
            return value.Mantissa < 0 ? new BigDouble(-1) : BigDouble.Zero;
        }

        if (value.Exponent >= 17) return value;

        var d = value.ToDouble();
        return new BigDouble(Math.Floor(d));
    }

    private static void Normalize(double mantissa, int exponent, out double mantissaOut, out int exponentOut)
    {
        if (double.IsNaN(mantissa) || mantissa == 0d)
        {
            mantissaOut = 0d;
            exponentOut = 0;
            return;
        }

        if (double.IsInfinity(mantissa))
            mantissa = double.MaxValue * Math.Sign(mantissa);

        var abs = Math.Abs(mantissa);
        var shift = (int)Math.Floor(Math.Log10(abs));

        // Make mantissa in [1,10) by shifting.
        mantissa /= Pow10(shift);
        exponent += shift;

        // Fix edge cases due to floating error.
        if (Math.Abs(mantissa) >= 10d)
        {
            mantissa /= 10d;
            exponent += 1;
        }
        else if (Math.Abs(mantissa) < 1d)
        {
            mantissa *= 10d;
            exponent -= 1;
        }

        mantissaOut = mantissa;
        exponentOut = exponent;
    }

    private static double Pow10(int exponent)
        => Math.Pow(10d, exponent);
}


