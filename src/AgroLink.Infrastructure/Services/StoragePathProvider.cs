using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Interfaces;

namespace AgroLink.Infrastructure.Services;

public class StoragePathProvider : IStoragePathProvider
{
    public string GetAnimalPhotoPath(int farmId, int animalId, int photoId, string fileName)
    {
        var f = IdSerializer.Encode("Farm", farmId);
        var a = IdSerializer.Encode("Animal", animalId);
        var p = IdSerializer.Encode("Photo", photoId);
        var extension = Path.GetExtension(fileName).ToLower();

        return $"f/{f}/a/{a}/{p}{extension}";
    }

    public string GetOwnerBrandPhotoPath(int farmId, int brandId, string fileName)
    {
        var f = IdSerializer.Encode("Farm", farmId);
        var b = IdSerializer.Encode("OwnerBrand", brandId);
        var extension = Path.GetExtension(fileName).ToLower();

        return $"f/{f}/ob/{b}{extension}";
    }

    public string GetVoiceAudioPath(Guid jobId, string contentType)
    {
        var ext = contentType.ToLowerInvariant() switch
        {
            "audio/x-m4a" or "audio/m4a" or "audio/mp4" => ".m4a",
            "audio/mpeg" => ".mp3",
            "audio/wav" or "audio/wave" => ".wav",
            "audio/ogg" => ".ogg",
            "audio/webm" => ".webm",
            _ => ".m4a",
        };
        return $"voice-commands/temp/{jobId}{ext}";
    }
}
