using NumberBase = HexControl.SharedControl.Documents.NumberBase;

namespace HexControl.SharedControl.Control.Helpers;

internal static class BaseConverter
{
    public static int Convert(long number, NumberBase numberBase, bool capitalization, IList<char> characters)
    {
        const int numericOffset = 48;

        if (number == 0)
        {
            characters[0] = '0';
            return 1;
        }

        var i = 0;
        var @base = (int)numberBase;
        while (number != 0)
        {
            var part = number % @base;
            var @char = part >= 10 ? NumberToHexChar(part - 10, capitalization) : (char)(part + numericOffset);

            characters[i] = @char;
            number /= @base;
            i++;
        }

        return i;
    }

    private static char NumberToHexChar(long number, bool capitalization)
    {
        const int alphabetOffset = 97;
        const int capsAlphabetOffset = 65;

        return (char)(capitalization ? number + capsAlphabetOffset : number + alphabetOffset);
    }
}