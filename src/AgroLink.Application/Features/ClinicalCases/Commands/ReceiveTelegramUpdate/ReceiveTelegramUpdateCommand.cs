using MediatR;

namespace AgroLink.Application.Features.ClinicalCases.Commands.ReceiveTelegramUpdate;

public record ReceiveTelegramUpdateCommand(string RawUpdateJson)
    : IRequest<ReceiveTelegramUpdateResult>;
