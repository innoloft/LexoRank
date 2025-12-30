using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("LexoRank.Tests")]
namespace LexoRank;

public class LexoRankBucket
{
    internal static readonly LexoRankBucket Bucket0 = new LexoRankBucket("0");
    internal static readonly LexoRankBucket Bucket1 = new LexoRankBucket("1");
    internal static readonly LexoRankBucket Bucket2 = new LexoRankBucket("2");

    private static readonly LexoRankBucket[] Values = { Bucket0, Bucket1, Bucket2 };

    private readonly LexoInteger _value;

    private LexoRankBucket(string val)
    {
        _value = LexoInteger.Parse(val, LexoRank.NumeralSystem);
    }


    public string Format()
    {
        return _value.Format();
    }

    public LexoRankBucket Next()
    {
        if (this == Bucket0) return Bucket1;
        if (this == Bucket1) return Bucket2;
        return Bucket0;
    }

    public bool Equals(LexoRankBucket other)
    {
        return _value.Equals(other._value);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        return obj is LexoRankBucket other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }


    public LexoRankBucket Prev()
    {
        if (this == Bucket0) return Bucket2;
        if (this == Bucket1) return Bucket0;
        return Bucket1;
    }

    public static LexoRankBucket From(string str)
    {
        var val = LexoInteger.Parse(str, LexoRank.NumeralSystem);

        foreach (var bucket in Values)
        {
            if (bucket._value.Equals(val)) return bucket;
        }

        throw new LexoException("Unknown bucket: " + str);
    }
}