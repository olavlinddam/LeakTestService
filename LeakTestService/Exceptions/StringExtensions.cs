using System.Text;

namespace LeakTestService.Exceptions;

public static class StringExtensions
{
    /// <summary>
    /// Normalizes the input string by UpperCasing the first char of the string.
    /// </summary>
    public static string FirstCharToUpper(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }
        return $"{input[0].ToString().ToUpper()}{input.Substring(1)}";
    }
    
    /// <summary>
    /// Normalizes the input string by UpperCasing the first char of the string.
    /// If input = "sniffingpoing" the 8th letter is Upper Cased.
    /// if input = "leaktestid" the 4th and the 8th letter is Upper Cased.
    /// This is to match the column names in the database. 
    /// </summary>
    public static string NormalizeField(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        StringBuilder sb = new StringBuilder(input);
        sb[0] = char.ToUpper(sb[0]);

        switch (sb.ToString().ToLower())
        {
            case "sniffingpoint":
                sb[8] = char.ToUpper(sb[8]);
                break;
            case "leaktestid":
                sb[4] = char.ToUpper(sb[4]);
                sb[8] = char.ToUpper(sb[8]);
                break;
        }

        return sb.ToString();
    }

}