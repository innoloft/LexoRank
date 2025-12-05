namespace LexoRank.NumeralSystems;

public interface ILexoNumeralSystem
{
    string Name { get; }

    int GetBase();

    char GetPositiveChar();

    char GetNegativeChar();

    char GetRadixPointChar();

    int ToDigit(char var1);

    char ToChar(int var1);
}