namespace AgroLink.Application.Interfaces;

public interface IStoragePathProvider
{
    string GetAnimalPhotoPath(int farmId, int animalId, int photoId, string fileName);

    string GetOwnerBrandPhotoPath(int farmId, int brandId, string fileName);
}
