using System.Text;

namespace DeepSigma.DocumentReader.Email.Tests;

/// <summary>Raw .eml messages for reader tests (line endings normalized to CRLF).</summary>
internal static class EmailSamples
{
    public static byte[] MultipartWithAttachment() => Encode("""
        From: Alice <alice@example.com>
        To: Bob <bob@example.com>
        Cc: Carol <carol@example.com>
        Subject: Quarterly Review
        Date: Mon, 01 Jun 2026 10:00:00 +0000
        MIME-Version: 1.0
        Content-Type: multipart/mixed; boundary="BOUND"

        --BOUND
        Content-Type: text/plain; charset=utf-8

        Revenue increased year over year.
        --BOUND
        Content-Type: text/plain; name="notes.txt"
        Content-Disposition: attachment; filename="notes.txt"

        attached note body
        --BOUND--
        """);

    public static byte[] HtmlBodyOnly() => Encode("""
        From: Alice <alice@example.com>
        To: Bob <bob@example.com>
        Subject: HTML Only
        Date: Mon, 01 Jun 2026 10:00:00 +0000
        MIME-Version: 1.0
        Content-Type: text/html; charset=utf-8

        <html><body><p>Hello HTML world.</p></body></html>
        """);

    private static byte[] Encode(string raw)
        => Encoding.UTF8.GetBytes(raw.Replace("\r\n", "\n", StringComparison.Ordinal).Replace("\n", "\r\n", StringComparison.Ordinal));
}
