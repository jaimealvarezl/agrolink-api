using AgroLink.Application.Interfaces;

namespace AgroLink.Infrastructure.Services;

public class StoragePathProvider : IStoragePathProvider
{
    public string GetAnimalPhotoPath(int farmId, int animalId, int photoId, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLower();
        return $"farms/{farmId}/animals/{animalId}/{photoId}{extension}";
    }
}
