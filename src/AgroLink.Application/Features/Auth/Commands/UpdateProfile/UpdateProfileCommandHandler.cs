using AgroLink.Application.Features.Auth.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Auth.Commands.UpdateProfile;

public class UpdateProfileCommandHandler(
    IUserRepository userRepository,
    IOwnerRepository ownerRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateProfileCommand, UserDto>
{
    public async Task<UserDto> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserService.GetRequiredUserId();
        var user = await userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            throw new UnauthorizedAccessException($"User with ID {userId} not found.");
        }

        // 1. Update User Name
        user.Name = request.Request.Name;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);

        // 2. Sync with Owner record (if exists)
        var owner = await ownerRepository.FirstOrDefaultAsync(o => o.UserId == userId);
        if (owner != null)
        {
            owner.Name = request.Request.Name;
            owner.UpdatedAt = DateTime.UtcNow;
            ownerRepository.Update(owner);
        }

        await unitOfWork.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
        };
    }
}
