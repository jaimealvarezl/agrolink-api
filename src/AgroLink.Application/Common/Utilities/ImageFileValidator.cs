namespace AgroLink.Application.Common.Utilities;

public static class ImageFileValidator
{
    private const int MagicBytesLength = 12;

    private static readonly string[] AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp"];
    private static readonly string[] AllowedMimeTypes = ["image/jpeg", "image/png", "image/webp"];

    /// <summary>
    ///     Validates extension, MIME type, and magic bytes.
    ///     If <paramref name="fileStream" /> is not seekable it is copied to a <see cref="MemoryStream" />
    ///     so that magic-byte validation always runs.
    ///     Returns a seekable stream positioned at 0 (either the original or the copy).
    ///     Throws <see cref="ArgumentException" /> on any validation failure.
    /// </summary>
    public static async Task<Stream> ValidateAsync(
        Stream fileStream,
        string fileName,
        string contentType,
        long size,
        CancellationToken cancellationToken = default
    )
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException(
                $"File extension {extension} is not allowed. Allowed: {string.Join(", ", AllowedExtensions)}"
            );
        }

        if (!AllowedMimeTypes.Contains(contentType.ToLowerInvariant()))
        {
            throw new ArgumentException(
                $"Content type {contentType} is not allowed. Allowed: {string.Join(", ", AllowedMimeTypes)}"
            );
        }

        if (size < MagicBytesLength)
        {
            throw new ArgumentException("File is too small to be a valid image.");
        }

        Stream seekableStream;
        if (fileStream.CanSeek)
        {
            seekableStream = fileStream;
            if (seekableStream.Position != 0)
            {
                seekableStream.Position = 0;
            }
        }
        else
        {
            var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms, cancellationToken);
            ms.Position = 0;
            seekableStream = ms;
        }

        var buffer = new byte[MagicBytesLength];
        await seekableStream.ReadExactlyAsync(buffer, 0, MagicBytesLength, cancellationToken);
        seekableStream.Position = 0;

        var isJpeg = buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF;
        var isPng =
            buffer[0] == 0x89
            && buffer[1] == 0x50
            && buffer[2] == 0x4E
            && buffer[3] == 0x47
            && buffer[4] == 0x0D
            && buffer[5] == 0x0A
            && buffer[6] == 0x1A
            && buffer[7] == 0x0A;
        var isWebp =
            buffer[0] == 0x52
            && buffer[1] == 0x49
            && buffer[2] == 0x46
            && buffer[3] == 0x46
            && buffer[8] == 0x57
            && buffer[9] == 0x45
            && buffer[10] == 0x42
            && buffer[11] == 0x50;

        if (!isJpeg && !isPng && !isWebp)
        {
            throw new ArgumentException(
                "File content does not match the expected image format (JPEG, PNG, WebP). The file may be corrupted."
            );
        }

        return seekableStream;
    }
}
