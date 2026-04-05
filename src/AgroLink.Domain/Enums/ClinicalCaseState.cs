namespace AgroLink.Domain.Enums;

public enum ClinicalCaseState
{
    NewCase,
    AwaitingRequiredData,
    PendingConfirmation,
    Recommended,
    Alerted,
    Closed,
}
