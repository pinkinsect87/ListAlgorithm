using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.Core.Services.Caching;
using GPTW.ListAutomation.Core.Services.Data;
using GPTW.ListAutomation.TestUI.Models;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GPTW.ListAutomation.TestUI.Handlers;

public sealed class ExcelCultureBriefDataHandler : IExcelWorksheetDataHandler
{
    private readonly IListCompanyService _listCompanyService;
    private readonly IListCompanyCultureBriefService _listCompanyCultureBriefService;
    private readonly ICacheManager _cacheManager;
    private readonly ILogger<ExcelCultureBriefDataHandler> _logger;

    private readonly string[] _variablePrefixes = new string[] { "ca_", "ca1_", "ca2_", "cb_", "gr_", "sr_" };

    public ExcelCultureBriefDataHandler(
        IListCompanyService listCompanyService,
        IListCompanyCultureBriefService listCompanyCultureBriefService,
        ICacheManager cacheManager,
        ILogger<ExcelCultureBriefDataHandler> logger)
    {
        _listCompanyService = listCompanyService;
        _listCompanyCultureBriefService = listCompanyCultureBriefService;
        _cacheManager = cacheManager;
        _logger = logger;
    }

    public WorksheetDataSource Source => WorksheetDataSource.CultureBrief;

    public async Task Process(int listSourceFileId, DataTable dt)
    {
        var cultureBriefsData = new List<CultureBriefItemModel>();
        IEnumerable<ListCompanyModel> listCompanies;
        List<ListCompanyCultureBriefModel> listCultureBriefs;

        using (_logger.MeasureOperation("Adjust CultureBriefs Datatable"))
        {
            foreach (DataRow dataRow in dt.Rows)
            {
                var client_id = dataRow["ClientId"];
                var clientName = dataRow["ECRClientName"];
                var engagement_id = dataRow["EngagementId"];
                var survey_ver_id = dataRow["SurveyVersionId"];

                for (var j = 4; j < dt.Columns.Count; j++)
                {
                    var variableName = dt.Columns[j];
                    var variableValue = dataRow[j];

                    if (engagement_id != null
                        && client_id != null
                        && survey_ver_id != null
                        && variableName != null
                        && variableValue != null
                        && int.TryParse(engagement_id.ToString(), out var engagementId)
                        && int.TryParse(client_id.ToString(), out var clientId)
                        && int.TryParse(survey_ver_id.ToString(), out var surveyVersionId)
                        && variableName.ToString().IsPresent()
                        && _variablePrefixes.Any(prefix => variableName.ToString().StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    {
                        cultureBriefsData.Add(new CultureBriefItemModel
                        {
                            EngagementId = engagementId,
                            ClientId = clientId,
                            ClientName = clientName?.ToString(),
                            SurveyVersionId = surveyVersionId.ToString(),
                            VariableName = variableName.ToString(),
                            VariableValue = variableValue == null ? "" : variableValue.ToString()
                        });
                    }
                }
            }
        }

        using (_logger.MeasureOperation("Create ListCompany"))
        {
            var companies = cultureBriefsData.GroupBy(r => new { r.EngagementId, r.ClientId, r.ClientName, r.SurveyVersionId })
            .Select(r => new ListCompanyModel
            {
                EngagementId = r.Key.EngagementId,
                ClientId = r.Key.ClientId,
                ClientName = r.Key.ClientName,
                SurveyVersionId = r.Key.SurveyVersionId
            }).ToList();

            await _listCompanyService.BulkUpdate(listSourceFileId, companies);

            listCompanies = await _listCompanyService.GetListCompanies(companies);
        }

        using (_logger.MeasureOperation("Prepare ListCompanyCultureBriefs"))
        {
            listCultureBriefs = cultureBriefsData.Select(r => new ListCompanyCultureBriefModel
            {
                ListCompanyCultureBriefId = 0,
                ListCompanyId = _cacheManager.Get($"LC_E{r.EngagementId}_C{r.ClientId}_SV{r.SurveyVersionId}", () =>
                {
                    return listCompanies.First(lc => lc.EngagementId == r.EngagementId
                        && lc.ClientId == r.ClientId && lc.SurveyVersionId == r.SurveyVersionId).ListCompanyId;
                }),
                VariableName = r.VariableName,
                VariableValue = r.VariableValue
            }).ToList();
        }

        using (_logger.MeasureOperation("BulkInsert ListCompanyCultureBriefs"))
        {
            await _listCompanyCultureBriefService.BulkInsert(listCultureBriefs);
        }
    }
}
