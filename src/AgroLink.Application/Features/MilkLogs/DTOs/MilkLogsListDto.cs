namespace AgroLink.Application.Features.MilkLogs.DTOs;

public class MilkLogsListDto
{
    public IEnumerable<MilkLogDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize == 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
    public decimal? LastUsedPricePerLiter { get; set; }
}
