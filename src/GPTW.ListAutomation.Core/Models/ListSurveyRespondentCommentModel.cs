namespace GPTW.ListAutomation.Core.Models;

public sealed class ListSurveyRespondentCommentModel
{
    public int ListSurveyRespondentCommentsId { get; set; }
    public int ListCompanyId { get; set; }
    public int RespondentKey { get; set; }
    public string Question { get; set; }
    public string Response { get; set; }
}
