using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.Core.Services.Caching;
using GPTW.ListAutomation.Core.Services.Data;
using GPTW.ListAutomation.TestUI.Models;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GPTW.ListAutomation.TestUI.Handlers;

public sealed class ExcelCommentsDataHandler : IExcelWorksheetDataHandler
{
    private readonly IListCompanyService _listCompanyService;
    private readonly IListSurveyRespondentCommentService _listSurveyRespondentCommentService;
    private readonly ICacheManager _cacheManager;
    private readonly ILogger<ExcelCommentsDataHandler> _logger;

    public ExcelCommentsDataHandler(
        IListCompanyService listCompanyService,
        IListSurveyRespondentCommentService listSurveyRespondentCommentService,
        ICacheManager cacheManager,
        ILogger<ExcelCommentsDataHandler> logger)
    {
        _listCompanyService = listCompanyService;
        _listSurveyRespondentCommentService = listSurveyRespondentCommentService;
        _cacheManager = cacheManager;
        _logger = logger;
    }

    public WorksheetDataSource Source => WorksheetDataSource.Comments;

    public async Task Process(int listSourceFileId, DataTable dt)
    {
        var commentsData = new List<CommentItemModel>();
        IEnumerable<ListCompanyModel> listCompanies;
        List<ListSurveyRespondentCommentModel> listComments;

        using (_logger.MeasureOperation("Adjust Comments Datatable"))
        {
            foreach (DataRow dataRow in dt.Rows)
            {
                var engagement_id = dataRow["engagement_id"];
                var client_id = dataRow["client_id"];
                var survey_ver_id = dataRow["survey_ver_id"];
                var respondent_key = dataRow["respondent_key"];
                var question = dataRow["question"];
                var response = dataRow["response"];

                if (engagement_id != null
                    && client_id != null
                    && survey_ver_id != null
                    && respondent_key != null
                    && question != null
                    && int.TryParse(engagement_id.ToString(), out var engagementId)
                    && int.TryParse(client_id.ToString(), out var clientId)
                    && int.TryParse(survey_ver_id.ToString(), out var surveyVersionId)
                    && int.TryParse(respondent_key.ToString(), out var respondentKey)
                    && question.ToString().IsPresent())
                {
                    commentsData.Add(new CommentItemModel
                    {
                        EngagementId = engagementId,
                        ClientId = clientId,
                        SurveyVersionId = surveyVersionId.ToString(),
                        RespondentKey = respondentKey,
                        Question = question.ToString(),
                        Response = response?.ToString()
                    });
                }
            }
        }

        using (_logger.MeasureOperation("Create ListCompany"))
        {
            var companies = commentsData.GroupBy(r => new { r.EngagementId, r.ClientId, r.SurveyVersionId })
            .Select(r => new ListCompanyModel
            {
                EngagementId = r.Key.EngagementId,
                ClientId = r.Key.ClientId,
                ClientName = null,
                SurveyVersionId = r.Key.SurveyVersionId
            }).ToList();

            await _listCompanyService.BulkUpdate(listSourceFileId, companies);

            listCompanies = await _listCompanyService.GetListCompanies(companies);
        }

        using (_logger.MeasureOperation("Prepare ListSurveyRespondentComments"))
        {
            listComments = commentsData.Select(r => new ListSurveyRespondentCommentModel
            {
                ListSurveyRespondentCommentsId = 0,
                ListCompanyId = _cacheManager.Get($"LC_E{r.EngagementId}_C{r.ClientId}_SV{r.SurveyVersionId}", () =>
                {
                    return listCompanies.First(lc => lc.EngagementId == r.EngagementId
                        && lc.ClientId == r.ClientId && lc.SurveyVersionId == r.SurveyVersionId).ListCompanyId;
                }),
                RespondentKey = r.RespondentKey,
                Question = r.Question,
                Response = r.Response
            }).ToList();
        }

        using (_logger.MeasureOperation("BulkInsert ListSurveyRespondentComments"))
        {
            await _listSurveyRespondentCommentService.BulkInsert(listComments);
        }
    }
}
