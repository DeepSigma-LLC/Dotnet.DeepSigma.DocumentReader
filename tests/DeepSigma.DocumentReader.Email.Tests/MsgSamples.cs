using System.Text;
using MsgKit;

namespace DeepSigma.DocumentReader.Email.Tests;

/// <summary>Builds an Outlook <c>.msg</c> in memory (via MsgKit) so reader tests need no binary fixtures.</summary>
internal static class MsgSamples
{
    public static byte[] Create()
    {
        string path = Path.Combine(Path.GetTempPath(), $"dsread-{Guid.NewGuid():N}.msg");
        try
        {
            using (var email = new MsgKit.Email(
                new Sender("alice@example.com", "Alice"),
                new Representing("alice@example.com", "Alice"),
                "Quarterly Review"))
            {
                email.Recipients.AddTo("bob@example.com", "Bob");
                email.Recipients.AddCc("carol@example.com", "Carol");
                email.Subject = "Quarterly Review";
                email.BodyText = "Revenue increased year over year.";

                using var attachment = new MemoryStream(Encoding.UTF8.GetBytes("attached note body"));
                email.Attachments.Add(attachment, "notes.txt");

                email.Save(path);
            }

            return File.ReadAllBytes(path);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
