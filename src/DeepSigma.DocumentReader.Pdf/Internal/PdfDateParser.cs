using System.Globalization;

namespace DeepSigma.DocumentReader.Pdf.Internal;

/// <summary>
/// Parses PDF date strings of the form <c>D:YYYYMMDDHHmmSSOHH'mm'</c> (per the PDF spec,
/// where the offset is <c>Z</c>, <c>+HH'mm'</c>, or <c>-HH'mm'</c>). All fields after the
/// year are optional. Returns <see langword="null"/> for missing or malformed input.
/// </summary>
internal static class PdfDateParser
{
    public static DateTimeOffset? Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        string value = raw.Trim();
        if (value.StartsWith("D:", StringComparison.Ordinal))
        {
            value = value[2..];
        }

        int digitCount = 0;
        while (digitCount < value.Length && char.IsAsciiDigit(value[digitCount]))
        {
            digitCount++;
        }

        string dateDigits = value[..digitCount];
        if (dateDigits.Length < 4)
        {
            return null;
        }

        try
        {
            int year = Field(dateDigits, 0, 4, 1);
            int month = Field(dateDigits, 4, 2, 1);
            int day = Field(dateDigits, 6, 2, 1);
            int hour = Field(dateDigits, 8, 2, 0);
            int minute = Field(dateDigits, 10, 2, 0);
            int second = Field(dateDigits, 12, 2, 0);

            TimeSpan offset = ParseOffset(value[digitCount..]);
            return new DateTimeOffset(year, month, day, hour, minute, second, offset);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    private static int Field(string digits, int start, int length, int fallback)
        => start + length <= digits.Length
            ? int.Parse(digits.AsSpan(start, length), CultureInfo.InvariantCulture)
            : fallback;

    private static TimeSpan ParseOffset(string rest)
    {
        rest = rest.Replace("'", string.Empty, StringComparison.Ordinal);
        if (rest.Length == 0 || rest[0] == 'Z')
        {
            return TimeSpan.Zero;
        }

        if (rest[0] is not ('+' or '-'))
        {
            return TimeSpan.Zero;
        }

        int hours = rest.Length >= 3 ? int.Parse(rest.AsSpan(1, 2), CultureInfo.InvariantCulture) : 0;
        int minutes = rest.Length >= 5 ? int.Parse(rest.AsSpan(3, 2), CultureInfo.InvariantCulture) : 0;
        var offset = new TimeSpan(hours, minutes, 0);
        return rest[0] == '-' ? -offset : offset;
    }
}
