using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetTheftAlertPdf;

public record GetTheftAlertPdfQuery(int AnimalId, int UserId) : IRequest<byte[]?>;
