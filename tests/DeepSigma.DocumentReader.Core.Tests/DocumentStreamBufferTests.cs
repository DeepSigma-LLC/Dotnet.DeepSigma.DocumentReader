using System.Text;
using DeepSigma.DocumentReader.Core.Streams;
using Xunit;

namespace DeepSigma.DocumentReader.Core.Tests;

public sealed class DocumentStreamBufferTests
{
    [Fact]
    public async Task Seekable_source_is_used_directly_and_not_owned()
    {
        byte[] data = Encoding.UTF8.GetBytes("hello world");
        using var source = DocumentSource.FromBytes(data);

        await using var buffered = await DocumentStreamBuffer.CreateAsync(source, maxBytes: null);

        Assert.Equal(data.Length, buffered.Length);
        Assert.True(buffered.Stream.CanSeek);
        Assert.Equal(0, buffered.Stream.Position);
    }

    [Fact]
    public async Task Non_seekable_source_is_buffered_with_content_preserved()
    {
        byte[] data = Encoding.UTF8.GetBytes("the quick brown fox");
        using var source = DocumentSource.FromStream(new ForwardOnlyStream(data));

        await using var buffered = await DocumentStreamBuffer.CreateAsync(source, maxBytes: null);

        Assert.True(buffered.Stream.CanSeek);
        Assert.Equal(data.Length, buffered.Length);

        using var reader = new StreamReader(buffered.Stream, Encoding.UTF8, leaveOpen: true);
        Assert.Equal("the quick brown fox", await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task Seekable_source_exceeding_limit_throws()
    {
        byte[] data = new byte[1024];
        using var source = DocumentSource.FromBytes(data);

        await Assert.ThrowsAsync<DocumentSizeLimitExceededException>(
            async () => await DocumentStreamBuffer.CreateAsync(source, maxBytes: 512));
    }

    [Fact]
    public async Task Non_seekable_source_exceeding_limit_throws()
    {
        byte[] data = new byte[1024];
        using var source = DocumentSource.FromStream(new ForwardOnlyStream(data));

        await Assert.ThrowsAsync<DocumentSizeLimitExceededException>(
            async () => await DocumentStreamBuffer.CreateAsync(source, maxBytes: 512));
    }
}
