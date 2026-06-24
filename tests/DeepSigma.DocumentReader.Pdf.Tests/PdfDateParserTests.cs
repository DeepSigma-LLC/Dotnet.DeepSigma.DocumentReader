using DeepSigma.DocumentReader.Pdf.Internal;
using Xunit;

namespace DeepSigma.DocumentReader.Pdf.Tests;

public sealed class PdfDateParserTests
{
    [Fact]
    public void Parses_full_date_with_positive_offset()
    {
        DateTimeOffset? result = PdfDateParser.Parse("D:20260601100000+02'30'");

        Assert.NotNull(result);
        Assert.Equal(new DateTimeOffset(2026, 6, 1, 10, 0, 0, new TimeSpan(2, 30, 0)), result);
    }

    [Fact]
    public void Parses_utc_z_suffix()
    {
        DateTimeOffset? result = PdfDateParser.Parse("D:20260601100000Z");
        Assert.Equal(TimeSpan.Zero, result!.Value.Offset);
        Assert.Equal(2026, result.Value.Year);
    }

    [Fact]
    public void Parses_date_only()
    {
        DateTimeOffset? result = PdfDateParser.Parse("D:20260601");
        Assert.Equal(new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero), result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a date")]
    [InlineData("D:2026139999")] // invalid month/day
    public void Returns_null_for_missing_or_malformed(string? input)
        => Assert.Null(PdfDateParser.Parse(input));
}
