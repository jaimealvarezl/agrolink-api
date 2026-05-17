using AgroLink.Application.Features.Dashboard.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Dashboard.Queries.GetDashboardSummary;

public record GetDashboardSummaryQuery(int FarmId) : IRequest<DashboardSummaryDto>;
