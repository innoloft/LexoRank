namespace LexoRank.NumeralSystems;

public class LexoNumeralSystem36 : ILexoNumeralSystem
{
    public string Name => "Base36";

    public int GetBase()
    {
        return 36;
    }

    public char GetPositiveChar()
    {
        return '+';
    }

    public char GetNegativeChar()
    {
        return '-';
    }

    public char GetRadixPointChar()
    {
        return ':';
    }

    public int ToDigit(char ch)
    {
        if (ch >= '0' && ch <= '9')
            return ch - 48;
        if (ch >= 'a' && ch <= 'z')
            return ch - 97 + 10;
        throw new LexoException("Not valid digit: " + ch);
    }

    public char ToChar(int digit)
    {
        if (digit < 10)
            return (char)(digit + 48);
        return (char)(digit - 10 + 97);
    }
}