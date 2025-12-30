using System.Text;
using LexoRank.NumeralSystems;

namespace LexoRank;

/// <summary>
/// Represents a decimal value in the LexoRank system.
/// </summary>
public class LexoDecimal : IComparable<LexoDecimal>, IComparable
{
    private readonly LexoInteger _mag;
    private readonly int _sig;

    private LexoDecimal(LexoInteger mag, int sig)
    {
        _mag = mag;
        _sig = sig;
    }

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is LexoDecimal other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(LexoDecimal)}");
    }

    /// <inheritdoc />
    public int CompareTo(LexoDecimal? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;

        var tMag = _mag;
        var oMag = other._mag;
        if (_sig > other._sig)
            oMag = oMag.ShiftLeft(_sig - other._sig);
        else if (_sig < other._sig) tMag = tMag.ShiftLeft(other._sig - _sig);

        return tMag.CompareTo(oMag);
    }

    /// <summary>
    /// Returns half of the base for the given numeral system.
    /// </summary>
    /// <param name="sys">The numeral system.</param>
    /// <returns>A LexoDecimal representing 0.5 in the given system.</returns>
    public static LexoDecimal Half(ILexoNumeralSystem sys)
    {
        var mid = sys.GetBase() / 2;
        return Make(LexoInteger.Make(sys, 1, new[] { mid }), 1);
    }

    /// <summary>
    /// Parses a string representation of a LexoDecimal.
    /// </summary>
    /// <param name="str">The string to parse.</param>
    /// <param name="system">The numeral system.</param>
    /// <returns>The parsed LexoDecimal.</returns>
    /// <exception cref="FormatException">Thrown if the format is invalid.</exception>
    public static LexoDecimal Parse(string str, ILexoNumeralSystem system)
    {
        var partialIndex = str.IndexOf(system.GetRadixPointChar());
        if (str.LastIndexOf(system.GetRadixPointChar()) != partialIndex)
            throw new FormatException("More than one " + system.GetRadixPointChar());

        if (partialIndex < 0) return Make(LexoInteger.Parse(str, system), 0);

        var intStr = str.Substring(0, partialIndex) + str.Substring(partialIndex + 1);
        return Make(LexoInteger.Parse(intStr, system), str.Length - 1 - partialIndex);
    }

    /// <summary>
    /// Creates a LexoDecimal from a LexoInteger.
    /// </summary>
    /// <param name="integer">The integer value.</param>
    /// <returns>The LexoDecimal.</returns>
    public static LexoDecimal From(LexoInteger integer) => Make(integer, 0);

    /// <summary>
    /// Creates a LexoDecimal from a LexoInteger and a scale (significance).
    /// </summary>
    /// <param name="integer">The integer value.</param>
    /// <param name="sig">The scale.</param>
    /// <returns>The LexoDecimal.</returns>
    public static LexoDecimal Make(LexoInteger integer, int sig)
    {
        if (integer.IsZero()) return new LexoDecimal(integer, 0);

        var zeroCount = 0;

        for (var i = 0; i < sig && integer.GetMag(i) == 0; ++i) ++zeroCount;

        var newInteger = integer.ShiftRight(zeroCount);
        var newSig = sig - zeroCount;
        return new LexoDecimal(newInteger, newSig);
    }

    /// <summary>
    /// Gets the numeral system used by this decimal.
    /// </summary>
    /// <returns>The numeral system.</returns>
    public ILexoNumeralSystem GetSystem() => _mag.GetSystem();

    /// <summary>
    /// Adds another LexoDecimal to this one.
    /// </summary>
    /// <param name="other">The other LexoDecimal.</param>
    /// <returns>The result of the addition.</returns>
    public LexoDecimal Add(LexoDecimal other)
    {
        var (thisMag, otherMag, scale) = AlignScales(other);
        return Make(thisMag.Add(otherMag), scale);
    }

    /// <summary>
    /// Subtracts another LexoDecimal from this one.
    /// </summary>
    /// <param name="other">The other LexoDecimal.</param>
    /// <returns>The result of the subtraction.</returns>
    public LexoDecimal Subtract(LexoDecimal other)
    {
        var (thisMag, otherMag, scale) = AlignScales(other);
        return Make(thisMag.Subtract(otherMag), scale);
    }

    /// <summary>
    /// Aligns the scales of this decimal and another by shifting magnitudes.
    /// </summary>
    /// <param name="other">The other LexoDecimal.</param>
    /// <returns>A tuple of aligned magnitudes and the common scale.</returns>
    private (LexoInteger thisMag, LexoInteger otherMag, int scale) AlignScales(LexoDecimal other)
    {
        var thisMag = _mag;
        var thisSig = _sig;
        var otherMag = other._mag;
        var otherSig = other._sig;

        while (thisSig < otherSig)
        {
            thisMag = thisMag.ShiftLeft();
            ++thisSig;
        }

        while (thisSig > otherSig)
        {
            otherMag = otherMag.ShiftLeft();
            ++otherSig;
        }

        return (thisMag, otherMag, thisSig);
    }

    /// <summary>
    /// Multiplies this LexoDecimal by another.
    /// </summary>
    /// <param name="other">The other LexoDecimal.</param>
    /// <returns>The result of the multiplication.</returns>
    public LexoDecimal Multiply(LexoDecimal other) => Make(_mag.Multiply(other._mag), _sig + other._sig);

    /// <summary>
    /// Returns the floor of this decimal.
    /// </summary>
    /// <returns>The floor as a LexoInteger.</returns>
    public LexoInteger Floor() => _mag.ShiftRight(_sig);

    /// <summary>
    /// Returns the ceiling of this decimal.
    /// </summary>
    /// <returns>The ceiling as a LexoInteger.</returns>
    public LexoInteger Ceil()
    {
        if (IsExact()) return _mag;

        var floor = Floor();
        return floor.Add(LexoInteger.One(floor.GetSystem()));
    }

    /// <summary>
    /// Checks if the decimal is exact (has no fractional part).
    /// </summary>
    /// <returns>True if exact, false otherwise.</returns>
    public bool IsExact()
    {
        if (_sig == 0) return true;

        for (var i = 0; i < _sig; ++i)
            if (_mag.GetMag(i) != 0)
                return false;

        return true;
    }

    /// <summary>
    /// Gets the scale of the decimal.
    /// </summary>
    /// <returns>The scale.</returns>
    public int GetScale() => _sig;

    /// <summary>
    /// Sets the scale of the decimal.
    /// </summary>
    /// <param name="nSig">The new scale.</param>
    /// <returns>The new LexoDecimal with the specified scale.</returns>
    public LexoDecimal SetScale(int nSig) => SetScale(nSig, false);

    /// <summary>
    /// Sets the scale of the decimal, optionally using ceiling rounding.
    /// </summary>
    /// <param name="nSig">The new scale.</param>
    /// <param name="ceiling">Whether to use ceiling rounding.</param>
    /// <returns>The new LexoDecimal.</returns>
    public LexoDecimal SetScale(int nSig, bool ceiling)
    {
        if (nSig >= _sig) return this;

        if (nSig < 0) nSig = 0;

        var diff = _sig - nSig;
        var nmag = _mag.ShiftRight(diff);
        if (ceiling) nmag = nmag.Add(LexoInteger.One(nmag.GetSystem()));

        return Make(nmag, nSig);
    }

    /// <summary>
    ///     Convert to string.
    /// </summary>
    /// <returns></returns>
    public string Format()
    {
        var intStr = _mag.Format();
        if (_sig == 0) return intStr;

        var sb = new StringBuilder(intStr);
        var head = sb[0];
        var specialHead = head == _mag.GetSystem().GetPositiveChar() ||
                          head == _mag.GetSystem().GetNegativeChar();
        if (specialHead) sb.Remove(0, 1);

        while (sb.Length < _sig + 1) sb.Insert(0, _mag.GetSystem().ToChar(0));

        sb.Insert(sb.Length - _sig, _mag.GetSystem().GetRadixPointChar());
        if (sb.Length - _sig == 0) sb.Insert(0, _mag.GetSystem().ToChar(0));

        if (specialHead) sb.Insert(0, head);

        return sb.ToString();
    }

    private bool Equals(LexoDecimal other) => Equals(_mag, other._mag) && _sig == other._sig;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((LexoDecimal)obj);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (_mag.GetHashCode() * 397) ^ _sig;
        }
    }

    public override string ToString() => Format();
}