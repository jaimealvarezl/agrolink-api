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
}
