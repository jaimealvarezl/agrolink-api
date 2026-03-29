using AgroLink.Application.Common.Utilities;
using Shouldly;

namespace AgroLink.Application.Tests.Common.Utilities;

[TestFixture]
public class ImageFileValidatorTests
{
    // JPEG magic bytes: FF D8 FF
    private static byte[] JpegHeader =>
        [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01];

    // PNG magic bytes: 89 50 4E 47 0D 0A 1A 0A
    private static byte[] PngHeader =>
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    // WebP magic bytes: 52 49 46 46 ?? ?? ?? ?? 57 45 42 50
    private static byte[] WebpHeader =>
        [0x52, 0x49, 0x46, 0x46, 0x24, 0x00, 0x00, 0x00, 0x57, 0x45, 0x42, 0x50];

    private static Stream MakeStream(byte[] header)
    {
        var data = new byte[100];
        Array.Copy(header, data, header.Length);
        return new MemoryStream(data);
    }

    [Test]
    [TestCase("photo.jpg", "image/jpeg")]
    [TestCase("photo.jpeg", "image/jpeg")]
    public async Task ValidateAsync_ValidJpeg_DoesNotThrow(string fileName, string contentType)
    {
        await Should.NotThrowAsync(() =>
            ImageFileValidator.ValidateAsync(MakeStream(JpegHeader), fileName, contentType, 100)
        );
    }

    [Test]
    public async Task ValidateAsync_ValidPng_DoesNotThrow()
    {
        await Should.NotThrowAsync(() =>
            ImageFileValidator.ValidateAsync(MakeStream(PngHeader), "photo.png", "image/png", 100)
        );
    }

    [Test]
    public async Task ValidateAsync_ValidWebp_DoesNotThrow()
    {
        await Should.NotThrowAsync(() =>
            ImageFileValidator.ValidateAsync(
                MakeStream(WebpHeader),
                "photo.webp",
                "image/webp",
                100
            )
        );
    }

    [Test]
    [TestCase("photo.gif")]
    [TestCase("photo.bmp")]
    [TestCase("photo.tiff")]
    [TestCase("photo.pdf")]
    [TestCase("photo.exe")]
    public async Task ValidateAsync_DisallowedExtension_ThrowsArgumentException(string fileName)
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            ImageFileValidator.ValidateAsync(MakeStream(JpegHeader), fileName, "image/jpeg", 100)
        );
        ex.Message.ShouldContain("not allowed");
    }

    [Test]
    [TestCase("image/gif")]
    [TestCase("image/bmp")]
    [TestCase("application/octet-stream")]
    [TestCase("text/plain")]
    public async Task ValidateAsync_DisallowedContentType_ThrowsArgumentException(
        string contentType
    )
    {
        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            ImageFileValidator.ValidateAsync(MakeStream(JpegHeader), "photo.jpg", contentType, 100)
        );
        ex.Message.ShouldContain("not allowed");
    }

    [Test]
    public async Task ValidateAsync_FileTooSmall_ThrowsArgumentException()
    {
        var stream = new MemoryStream([0xFF, 0xD8, 0xFF]);

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            ImageFileValidator.ValidateAsync(stream, "photo.jpg", "image/jpeg", 3)
        );
        ex.Message.ShouldContain("too small");
    }

    [Test]
    public async Task ValidateAsync_MagicBytesMismatch_ThrowsArgumentException()
    {
        // Valid extension + MIME but content is not a real image
        var fakeContent = new byte[100];
        fakeContent[0] = 0x00;
        var stream = new MemoryStream(fakeContent);

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            ImageFileValidator.ValidateAsync(stream, "photo.jpg", "image/jpeg", 100)
        );
        ex.Message.ShouldContain("does not match");
    }

    [Test]
    public async Task ValidateAsync_JpegMagicBytesWithPngExtension_DoesNotThrow()
    {
        // The validator checks that content is a valid image (any allowed format),
        // not that magic bytes match the declared extension/MIME type.
        // JPEG bytes with .png extension still passes because the content IS a valid image.
        await Should.NotThrowAsync(() =>
            ImageFileValidator.ValidateAsync(MakeStream(JpegHeader), "photo.png", "image/png", 100)
        );
    }

    [Test]
    public async Task ValidateAsync_NonSeekableStreamWithValidContent_DoesNotThrow()
    {
        // Non-seekable streams are copied to MemoryStream so magic bytes validation always runs.
        var stream = new NonSeekableStream(JpegHeader.Concat(new byte[88]).ToArray());

        var result = await ImageFileValidator.ValidateAsync(stream, "photo.jpg", "image/jpeg", 100);
        result.ShouldNotBeNull();
        result.CanSeek.ShouldBeTrue();
        result.Position.ShouldBe(0);
    }

    [Test]
    public async Task ValidateAsync_NonSeekableStreamWithInvalidContent_ThrowsArgumentException()
    {
        // Even for non-seekable streams, magic bytes are validated via the MemoryStream copy.
        var fakeContent = new byte[100]; // all zeros
        var stream = new NonSeekableStream(fakeContent);

        var ex = await Should.ThrowAsync<ArgumentException>(() =>
            ImageFileValidator.ValidateAsync(stream, "photo.jpg", "image/jpeg", 100)
        );
        ex.Message.ShouldContain("does not match");
    }

    [Test]
    public async Task ValidateAsync_StreamPositionNonZero_RewindsBeforeReading()
    {
        var data = new byte[100];
        Array.Copy(JpegHeader, data, JpegHeader.Length);
        var stream = new MemoryStream(data);
        stream.Position = 50; // advance position

        await Should.NotThrowAsync(() =>
            ImageFileValidator.ValidateAsync(stream, "photo.jpg", "image/jpeg", 100)
        );
    }

    [Test]
    public async Task ValidateAsync_CancellationRequested_ThrowsOperationCanceledException()
    {
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() =>
            ImageFileValidator.ValidateAsync(
                MakeStream(JpegHeader),
                "photo.jpg",
                "image/jpeg",
                100,
                cts.Token
            )
        );
    }

    /// <summary>
    ///     Simulates a stream that does not support seeking (e.g. a network stream).
    /// </summary>
    private sealed class NonSeekableStream(byte[] data) : Stream
    {
        private readonly MemoryStream _inner = new(data);

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _inner.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
