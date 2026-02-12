namespace AgroLink.Application.Interfaces;

public interface IStoragePathProvider
{
    /// <summary>
    ///     Returns the path for an animal photo: farms/{farmId}/animals/{animalId}/{photoId}.{extension}
    /// </summary>
    string GetAnimalPhotoPath(int farmId, int animalId, int photoId, string fileName);
}
