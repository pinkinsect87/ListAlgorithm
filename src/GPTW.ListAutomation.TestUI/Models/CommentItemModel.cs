namespace GPTW.ListAutomation.TestUI.Models;

public sealed class CommentItemModel
{
    public int EngagementId { get; set; }
    public int ClientId { get; set; }
    public string SurveyVersionId { get; set; }
    public int RespondentKey { get; set; }
    public string Question { get; set; }
    public string Response { get; set; }
}
