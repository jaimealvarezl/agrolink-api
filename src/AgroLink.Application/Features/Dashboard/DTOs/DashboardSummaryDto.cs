namespace AgroLink.Application.Features.Dashboard.DTOs;

public class DashboardSummaryDto
{
    public int HerdCount { get; set; }
    public int SickCount { get; set; }
    public int NovedadCount { get; set; }
    public List<OverdueLotDto> OverdueLots { get; set; } = [];
    public DateTime? LastChecklistDate { get; set; }
    public int LastChecklistIssueCount { get; set; }
    public decimal? MilkToday { get; set; }
}

public class OverdueLotDto
{
    public int LotId { get; set; }
    public string LotName { get; set; } = string.Empty;
    public int DaysSinceLastChecklist { get; set; }
}
