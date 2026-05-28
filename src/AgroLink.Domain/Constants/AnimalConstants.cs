using AgroLink.Domain.Enums;

namespace AgroLink.Domain.Constants;

public static class AnimalConstants
{
    public const int MaxTagsPerAnimal = 8;
    public const int TagNameMinLength = 2;
    public const int TagNameMaxLength = 24;
    public static readonly LifeStatus[] ActiveStatuses = [LifeStatus.Active, LifeStatus.Missing];
}
