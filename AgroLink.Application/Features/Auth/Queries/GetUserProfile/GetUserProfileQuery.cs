using AgroLink.Application.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Auth.Queries.GetUserProfile;

public record GetUserProfileQuery(string Token) : IRequest<UserDto?>;
