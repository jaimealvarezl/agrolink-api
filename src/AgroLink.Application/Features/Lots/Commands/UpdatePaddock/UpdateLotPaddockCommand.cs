using AgroLink.Application.Features.Lots.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Lots.Commands.UpdatePaddock;

public record UpdateLotPaddockCommand(int FarmId, int LotId, int NewPaddockId) : IRequest<LotDto>;
