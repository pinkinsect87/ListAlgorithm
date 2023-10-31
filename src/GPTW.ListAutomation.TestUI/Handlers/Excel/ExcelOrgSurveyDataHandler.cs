using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.Core.Services.Caching;
using GPTW.ListAutomation.Core.Services.Data;
using GPTW.ListAutomation.TestUI.Infrastructure;
using GPTW.ListAutomation.TestUI.Models;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GPTW.ListAutomation.TestUI.Handlers;

public sealed class ExcelOrgSurveyDataHandler : IExcelWorksheetDataHandler
{
    private readonly IListCompanyService _listCompanyService;
    private readonly IListSurveyRespondentService _listSurveyRespondentService;
    private readonly ICacheManager _cacheManager;
    private readonly ILogger<ExcelOrgSurveyDataHandler> _logger;

    public ExcelOrgSurveyDataHandler(
        IListCompanyService listCompanyService,
        IListSurveyRespondentService listSurveyRespondentService,
        ICacheManager cacheManager,
        ILogger<ExcelOrgSurveyDataHandler> logger)
    {
        _listCompanyService = listCompanyService;
        _listSurveyRespondentService = listSurveyRespondentService;
        _cacheManager = cacheManager;
        _logger = logger;
    }

    public WorksheetDataSource Source => WorksheetDataSource.Org;

    public async Task Process(int listSourceFileId, DataTable dt)
    {
        var orgData = new List<ListSurveyRespondentItemModel>();
        IEnumerable<ListCompanyModel> listCompanies;
        List<ListSurveyRespondentModel> listSurveyRespondents;

        using (_logger.MeasureOperation("Adjust ORG Datatable"))
        {
            foreach (DataRow dataRow in dt.Rows)
            {
                var engagement_id = dataRow["engagement_id"];
                var client_id = dataRow["client_id"];
                var survey_ver_id = dataRow["survey_ver_id"];
                var respondent_key = dataRow["respondent_key"];

                if (engagement_id != null
                        && client_id != null
                        && survey_ver_id != null
                        && respondent_key != null
                        && int.TryParse(engagement_id.ToString(), out var engagementId)
                        && int.TryParse(client_id.ToString(), out var clientId)
                        && int.TryParse(survey_ver_id.ToString(), out var surveyVersionId)
                        && int.TryParse(respondent_key.ToString(), out var respondentKey))
                {
                    var listSurveyRespondentItem = new ListSurveyRespondentItemModel
                    {
                        EngagementId = engagementId,
                        ClientId = clientId,
                        SurveyVersionId = surveyVersionId.ToString(),
                        RespondentKey = respondentKey
                    };

                    for (var j = 4; j < dt.Columns.Count; j++)
                    {
                        var statement_id = dt.Columns[j];
                        var response = dataRow[j];

                        if (statement_id != null
                            && int.TryParse(statement_id.ToString(), out var statementId)
                            && CultureSurveyConstants.RespondentColumns.Contains(statementId))
                        {
                            if (response != null && int.TryParse(response.ToString(), out var _response))
                            {
                                listSurveyRespondentItem.SetProperty($"Response_{statementId}", _response);
                            }
                        }
                    }

                    orgData.Add(listSurveyRespondentItem);
                }
            }
        }

        using (_logger.MeasureOperation("Create ListCompany"))
        {
            var companies = orgData.GroupBy(r => new { r.EngagementId, r.ClientId, r.SurveyVersionId })
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

        using (_logger.MeasureOperation("Prepare ListSurveyRespondents"))
        {
            listSurveyRespondents = orgData.Select(r => new ListSurveyRespondentModel
            {
                ListCompanyResponseId = 0,
                ListCompanyId = _cacheManager.Get($"LC_E{r.EngagementId}_C{r.ClientId}_SV{r.SurveyVersionId}", () =>
                {
                    return listCompanies.First(lc => lc.EngagementId == r.EngagementId
                        && lc.ClientId == r.ClientId && lc.SurveyVersionId == r.SurveyVersionId).ListCompanyId;
                }),
                RespondentKey = r.RespondentKey,
                Response_1 = r.Response_1,
                Response_2 = r.Response_2,
                Response_3 = r.Response_3,
                Response_4 = r.Response_4,
                Response_5 = r.Response_5,
                Response_6 = r.Response_6,
                Response_7 = r.Response_7,
                Response_8 = r.Response_8,
                Response_9 = r.Response_9,
                Response_10 = r.Response_10,
                Response_11 = r.Response_11,
                Response_12 = r.Response_12,
                Response_13 = r.Response_13,
                Response_14 = r.Response_14,
                Response_15 = r.Response_15,
                Response_16 = r.Response_16,
                Response_17 = r.Response_17,
                Response_18 = r.Response_18,
                Response_19 = r.Response_19,
                Response_20 = r.Response_20,
                Response_21 = r.Response_21,
                Response_22 = r.Response_22,
                Response_23 = r.Response_23,
                Response_24 = r.Response_24,
                Response_25 = r.Response_25,
                Response_26 = r.Response_26,
                Response_27 = r.Response_27,
                Response_28 = r.Response_28,
                Response_29 = r.Response_29,
                Response_30 = r.Response_30,
                Response_31 = r.Response_31,
                Response_32 = r.Response_32,
                Response_33 = r.Response_33,
                Response_34 = r.Response_34,
                Response_35 = r.Response_35,
                Response_36 = r.Response_36,
                Response_37 = r.Response_37,
                Response_38 = r.Response_38,
                Response_39 = r.Response_39,
                Response_40 = r.Response_40,
                Response_41 = r.Response_41,
                Response_42 = r.Response_42,
                Response_43 = r.Response_43,
                Response_44 = r.Response_44,
                Response_45 = r.Response_45,
                Response_46 = r.Response_46,
                Response_47 = r.Response_47,
                Response_48 = r.Response_48,
                Response_49 = r.Response_49,
                Response_50 = r.Response_50,
                Response_51 = r.Response_51,
                Response_52 = r.Response_52,
                Response_53 = r.Response_53,
                Response_54 = r.Response_54,
                Response_55 = r.Response_55,
                Response_56 = r.Response_56,
                Response_57 = r.Response_57,
                Response_672 = r.Response_672,
                Response_12211 = r.Response_12211,
                Response_12212 = r.Response_12212,
                Response_12213 = r.Response_12213,
                Response_12214 = r.Response_12214,
                Response_12215 = r.Response_12215
            }).ToList();
        }

        using (_logger.MeasureOperation("BulkInsert ListSurveyRespondents"))
        {
            await _listSurveyRespondentService.BulkInsert(listSurveyRespondents);
        }
    }
}
