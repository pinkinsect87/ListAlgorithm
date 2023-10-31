namespace GPTW.ListAutomation.Core.Models;

public sealed class ListCompanyModel
{
    public int ListCompanyId { get; set; }
    public int EngagementId { get; set; }
    public int ClientId { get; set; }
    public string? ClientName { get; set; }
    public string? SurveyVersionId { get; set; }
}
