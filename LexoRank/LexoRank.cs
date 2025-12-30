using System.Text;
using LexoRank.NumeralSystems;

namespace LexoRank;

/// <summary>
/// Represents a LexoRank value, which is a string-based ranking system.
/// This class is immutable and thread-safe.
/// </summary>
public class LexoRank: IComparable<LexoRank>, IComparable
{
    private const int MinIntegerDigits = 6;

    /// <summary>
    /// The numeral system used for Lexorank calculations (Base36).
    /// </summary>
    public static readonly ILexoNumeralSystem NumeralSystem = new LexoNumeralSystem36();
    private static readonly LexoDecimal ZeroDecimal = LexoDecimal.Parse("0", NumeralSystem);
    private static readonly LexoDecimal OneDecimal = LexoDecimal.Parse("1", NumeralSystem);
    private static readonly LexoDecimal EightDecimal = LexoDecimal.Parse("8", NumeralSystem);
    private static readonly LexoDecimal MinDecimal = ZeroDecimal;

    private static readonly LexoDecimal MaxDecimal =
        LexoDecimal.Parse("1000000", NumeralSystem).Subtract(OneDecimal);

    private static readonly LexoDecimal MidDecimal = Between(MinDecimal, MaxDecimal);
    private static readonly LexoDecimal InitialMinDecimal = LexoDecimal.Parse("100000", NumeralSystem);

    private static readonly LexoDecimal InitialMaxDecimal =
        LexoDecimal.Parse(Convert.ToString(NumeralSystem.ToChar(NumeralSystem.GetBase() - 2)) + "00000",
            NumeralSystem);

    private readonly string _value;

    private LexoRank(string value)
    {
        if (value == null) throw new ArgumentNullException(nameof(value));
        _value = value;
        var parts = _value.Split('|');
        Bucket = LexoRankBucket.From(parts[0]);
        Decimal = LexoDecimal.Parse(parts[1], NumeralSystem);
    }

    private LexoRank(LexoRankBucket bucket, LexoDecimal dec)
    {
        _value = bucket.Format() + "|" + FormatDecimal(dec);
        Bucket = bucket;
        Decimal = dec;
    }

    /// <summary>
    /// Gets the bucket component of the rank.
    /// </summary>
    public LexoRankBucket Bucket { get; }

    /// <summary>
    /// Gets the decimal component of the rank.
    /// </summary>
    public LexoDecimal Decimal { get; }

    /// <inheritdoc />
    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(null, obj)) return 1;
        if (ReferenceEquals(this, obj)) return 0;
        return obj is LexoRank other
            ? CompareTo(other)
            : throw new ArgumentException($"Object must be of type {nameof(LexoRank)}");
    }

    /// <inheritdoc />
    public int CompareTo(LexoRank? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (ReferenceEquals(null, other)) return 1;
        return string.Compare(_value, other._value, StringComparison.Ordinal);
    }

    /// <summary>
    /// Returns the minimum possible rank.
    /// </summary>
    /// <returns>The minimum rank.</returns>
    public static LexoRank Min()
    {
        return From(LexoRankBucket.Bucket0, MinDecimal);
    }

    /// <summary>
    /// Returns the maximum possible rank.
    /// </summary>
    /// <returns>The maximum rank.</returns>
    public static LexoRank Max()
    {
        return Max(LexoRankBucket.Bucket0);
    }

    /// <summary>
    /// Returns the middle rank.
    /// </summary>
    /// <returns>The middle rank.</returns>
    public static LexoRank Middle()
    {
        var minLexoRank = Min();
        return minLexoRank.Between(Max(minLexoRank.Bucket));
    }

    /// <summary>
    /// Returns the maximum rank for a specific bucket.
    /// </summary>
    /// <param name="bucket">The bucket.</param>
    /// <returns>The maximum rank in the bucket.</returns>
    public static LexoRank Max(LexoRankBucket bucket)
    {
        return From(bucket, MaxDecimal);
    }

    /// <summary>
    /// Generates the previous rank.
    /// </summary>
    /// <returns>The previous rank.</returns>
    public LexoRank GenPrev()
    {
        if (IsMax()) return new LexoRank(Bucket, InitialMaxDecimal);

        var floorInteger = Decimal.Floor();
        var floorDecimal = LexoDecimal.From(floorInteger);
        var nextDecimal = floorDecimal.Subtract(EightDecimal);
        if (nextDecimal.CompareTo(MinDecimal) <= 0) nextDecimal = Between(MinDecimal, Decimal);

        return new LexoRank(Bucket, nextDecimal);
    }

    /// <summary>
    /// Returns a rank in the next bucket with the same decimal value.
    /// </summary>
    /// <returns>The rank in the next bucket.</returns>
    public LexoRank InNextBucket()
    {
        return From(Bucket.Next(), Decimal);
    }

    /// <summary>
    /// Returns a rank in the previous bucket with the same decimal value.
    /// </summary>
    /// <returns>The rank in the previous bucket.</returns>
    public LexoRank InPrevBucket()
    {
        return From(Bucket.Prev(), Decimal);
    }

    /// <summary>
    /// Checks if this is the minimum rank.
    /// </summary>
    /// <returns>True if minimum, false otherwise.</returns>
    public bool IsMin()
    {
        return Decimal.Equals(MinDecimal);
    }

    /// <summary>
    /// Checks if this is the maximum rank.
    /// </summary>
    /// <returns>True if maximum, false otherwise.</returns>
    public bool IsMax()
    {
        return Decimal.Equals(MaxDecimal);
    }

    /// <summary>
    /// Formats the rank as a string.
    /// </summary>
    /// <returns>The string representation of the rank.</returns>
    public string Format()
    {
        return _value;
    }

    /// <summary>
    /// Generates the next rank.
    /// </summary>
    /// <returns>The next rank.</returns>
    public LexoRank GenNext()
    {
        if (IsMin()) return new LexoRank(Bucket, InitialMinDecimal);

        var ceilInteger = Decimal.Ceil();
        var ceilDecimal = LexoDecimal.From(ceilInteger);
        var nextDecimal = ceilDecimal.Add(EightDecimal);
        if (nextDecimal.CompareTo(MaxDecimal) >= 0) nextDecimal = Between(Decimal, MaxDecimal);

        return new LexoRank(Bucket, nextDecimal);
    }

    /// <summary>
    /// Calculates a rank between this rank and another rank.
    /// </summary>
    /// <param name="other">The other rank.</param>
    /// <returns>A rank between this and the other rank.</returns>
    /// <exception cref="LexoException">Thrown if buckets are different or ranks are equal.</exception>
    public LexoRank Between(LexoRank other)
    {
        if (!Bucket.Equals(other.Bucket)) throw new LexoException("Between works only within the same bucket");

        var cmp = Decimal.CompareTo(other.Decimal);
        if (cmp > 0)
            return new LexoRank(Bucket, Between(other.Decimal, Decimal));
        if (cmp == 0)
            throw new LexoException("Try to rank between issues with same rank this=" + this +
                                    " other=" + other + " this.decimal=" + Decimal +
                                    " other.decimal=" + other.Decimal);
        return new LexoRank(Bucket, Between(Decimal, other.Decimal));
    }

    private bool Equals(LexoRank other)
    {
        return string.Equals(_value, other._value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((LexoRank)obj);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public override string ToString()
    {
        return _value;
    }

    /// <summary>
    /// Returns the initial rank for a specific bucket.
    /// </summary>
    /// <param name="bucket">The bucket.</param>
    /// <returns>The initial rank.</returns>
    public static LexoRank Initial(LexoRankBucket bucket)
    {
        return bucket == LexoRankBucket.Bucket0
            ? From(bucket, InitialMinDecimal)
            : From(bucket, InitialMaxDecimal);
    }

    private static LexoDecimal Between(LexoDecimal oLeft, LexoDecimal oRight)
    {
        if (oLeft.GetSystem() != oRight.GetSystem()) throw new LexoException("Expected same system");

        var left = oLeft;
        var right = oRight;
        LexoDecimal nLeft;
        if (oLeft.GetScale() < oRight.GetScale())
        {
            nLeft = oRight.SetScale(oLeft.GetScale(), false);
            if (oLeft.CompareTo(nLeft) >= 0) return Middle(oLeft, oRight);

            right = nLeft;
        }

        if (oLeft.GetScale() > right.GetScale())
        {
            nLeft = oLeft.SetScale(right.GetScale(), true);
            if (nLeft.CompareTo(right) >= 0) return Middle(oLeft, oRight);

            left = nLeft;
        }

        LexoDecimal nRight;
        for (var scale = left.GetScale(); scale > 0; right = nRight)
        {
            var nScale1 = scale - 1;
            var nLeft1 = left.SetScale(nScale1, true);
            nRight = right.SetScale(nScale1, false);
            var cmp = nLeft1.CompareTo(nRight);
            if (cmp == 0) return CheckMid(oLeft, oRight, nLeft1);

            if (nLeft1.CompareTo(nRight) > 0) break;

            scale = nScale1;
            left = nLeft1;
        }

        var mid = Middle(oLeft, oRight, left, right);

        int nScale;
        for (var mScale = mid.GetScale(); mScale > 0; mScale = nScale)
        {
            nScale = mScale - 1;
            var nMid = mid.SetScale(nScale);
            if (oLeft.CompareTo(nMid) >= 0 || nMid.CompareTo(oRight) >= 0) break;

            mid = nMid;
        }

        return mid;
    }

    private static LexoDecimal Middle(LexoDecimal lBound, LexoDecimal rBound, LexoDecimal left, LexoDecimal right)
    {
        var mid = Middle(left, right);
        return CheckMid(lBound, rBound, mid);
    }

    private static LexoDecimal CheckMid(LexoDecimal lBound, LexoDecimal rBound, LexoDecimal mid)
    {
        if (lBound.CompareTo(mid) >= 0) return Middle(lBound, rBound);

        return mid.CompareTo(rBound) >= 0 ? Middle(lBound, rBound) : mid;
    }

    private static LexoDecimal Middle(LexoDecimal left, LexoDecimal right)
    {
        var sum = left.Add(right);
        var mid = sum.Multiply(LexoDecimal.Half(left.GetSystem()));
        var scale = left.GetScale() > right.GetScale() ? left.GetScale() : right.GetScale();
        if (mid.GetScale() > scale)
        {
            var roundDown = mid.SetScale(scale, false);
            if (roundDown.CompareTo(left) > 0) return roundDown;

            var roundUp = mid.SetScale(scale, true);
            if (roundUp.CompareTo(right) < 0) return roundUp;
        }

        return mid;
    }

    private static string FormatDecimal(LexoDecimal dec)
    {
        var formatVal = dec.Format();
        var val = new StringBuilder(formatVal);
        var partialIndex = formatVal.IndexOf(NumeralSystem.GetRadixPointChar());
        var zero = NumeralSystem.ToChar(0);
        if (partialIndex < 0)
        {
            partialIndex = formatVal.Length;
            val.Append(NumeralSystem.GetRadixPointChar());
        }

        while (partialIndex < MinIntegerDigits)
        {
            val.Insert(0, zero);
            ++partialIndex;
        }

        while (val[val.Length - 1] == zero) val.Length = val.Length - 1;

        return val.ToString();
    }

    /// <summary>
    /// Parses a string representation of a LexoRank.
    /// </summary>
    /// <param name="str">The string to parse.</param>
    /// <returns>The parsed LexoRank.</returns>
    /// <exception cref="ArgumentException">Thrown if the string is null, empty, or invalid format.</exception>
    public static LexoRank Parse(string str)
    {
        if (string.IsNullOrWhiteSpace(str)) throw new ArgumentException("Rank string cannot be null or empty", nameof(str));

        var parts = str.Split('|');
        if (parts.Length != 2) throw new ArgumentException("Invalid LexoRank format. Expected 'bucket|decimal'.", nameof(str));

        // Validate bucket
        try
        {
            LexoRankBucket.From(parts[0]);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid bucket component", nameof(str), ex);
        }

        // Validate decimal
        try
        {
            LexoDecimal.Parse(parts[1], NumeralSystem);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid decimal component", nameof(str), ex);
        }

        return new LexoRank(str);
    }

    /// <summary>
    /// Creates a LexoRank from a bucket and a decimal value.
    /// </summary>
    /// <param name="bucket">The bucket.</param>
    /// <param name="dec">The decimal value.</param>
    /// <returns>The created LexoRank.</returns>
    /// <exception cref="LexoException">Thrown if the decimal system doesn't match.</exception>
    public static LexoRank From(LexoRankBucket bucket, LexoDecimal dec)
    {
        if (!dec.GetSystem().Name.Equals(NumeralSystem.Name)) throw new LexoException("Expected different system");

        return new LexoRank(bucket, dec);
    }

    /// <summary>
    /// Creates a LexoRank from a timestamp.
    /// </summary>
    /// <param name="timestamp">The timestamp to convert.</param>
    /// <param name="bucket">The bucket to use (default is Bucket0).</param>
    /// <returns>A LexoRank representing the timestamp.</returns>
    /// <remarks>
    /// <para>
    /// Due to the limited range of LexoRank values (approximately 2.1 billion positions),
    /// timestamps are normalized using modulo arithmetic to fit within the valid range.
    /// </para>
    /// <para>
    /// <strong>Important Limitations:</strong>
    /// </para>
    /// <list type="bullet">
    /// <item>Chronological order is only guaranteed for timestamps within approximately 69 years of each other</item>
    /// <item>Timestamps wrap around after exceeding the maximum value (36^6 ticks)</item>
    /// <item>This method is best suited for recent timestamps or relative ordering within a limited time window</item>
    /// </list>
    /// <para>
    /// For long-term timestamp ordering across decades or centuries, consider using a different
    /// approach such as storing timestamps separately and using LexoRank only for user-defined ordering.
    /// </para>
    /// </remarks>
    public static LexoRank FromTimestamp(DateTime timestamp, LexoRankBucket? bucket = null)
    {
        // Use Unix seconds to fit within the ~69 year range of 6 base36 digits
        // This fits into the "MaxDecimal" (36^6) until roughly the year 2038.
        long unixSeconds = new DateTimeOffset(timestamp).ToUnixTimeSeconds();

        // Safety: Modulo ensures we wrap around after 2038 instead of breaking,
        // similar to how the original code handled ticks.
        long maxVal = 2176782336; // 36^6
        long safeValue = unixSeconds % maxVal;

        string base36 = ConvertToBase36(safeValue);
        var dec = LexoDecimal.Parse(base36, NumeralSystem);

        // Ensure it's between Min and Max (though modulo logic above mostly handles this)
        if (dec.CompareTo(MinDecimal) < 0) dec = MinDecimal;
        if (dec.CompareTo(MaxDecimal) > 0) dec = MaxDecimal;

        return From(bucket ?? LexoRankBucket.Bucket0, dec);
    }

    /// <summary>
    /// Calculates the rank between two rank strings.
    /// Handles null or empty strings as Min/Max.
    /// </summary>
    /// <param name="prevStr">The previous rank string (or null/empty for Min).</param>
    /// <param name="nextStr">The next rank string (or null/empty for Max).</param>
    /// <returns>The calculated middle rank.</returns>
    /// <exception cref="LexoException">Thrown if the calculated rank is invalid or if inputs are invalid.</exception>
    public static LexoRank CalculateBetween(string? prevStr, string? nextStr)
    {
        LexoRank? prev = string.IsNullOrEmpty(prevStr) ? null : Parse(prevStr!);
        LexoRank? next = string.IsNullOrEmpty(nextStr) ? null : Parse(nextStr!);

        if (prev == null && next == null)
        {
            return Middle();
        }

        if (prev == null)
        {
            // No previous, so we want something before 'next'.
            if (next!.IsMin())
            {
                // Cannot go before Min
                throw new LexoException("Cannot calculate rank before Minimum rank");
            }
            return Min().Between(next);
        }

        if (next == null)
        {
            // No next, so we want something after 'prev'.
            if (prev.IsMax())
            {
                // Cannot go after Max
                throw new LexoException("Cannot calculate rank after Maximum rank");
            }
            return prev.Between(Max(prev.Bucket));
        }

        return prev.Between(next);
    }

    private static string ConvertToBase36(long value)
    {
        if (value == 0) return NumeralSystem.ToChar(0).ToString();

        var sb = new StringBuilder();
        bool isNegative = value < 0;
        if (isNegative) value = -value;

        while (value > 0)
        {
            sb.Insert(0, NumeralSystem.ToChar((int)(value % 36)));
            value /= 36;
        }

        if (isNegative) sb.Insert(0, NumeralSystem.GetNegativeChar());

        return sb.ToString();
    }
}