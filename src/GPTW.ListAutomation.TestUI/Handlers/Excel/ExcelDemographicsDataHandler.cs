using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.Core.Services.Caching;
using GPTW.ListAutomation.Core.Services.Data;
using GPTW.ListAutomation.TestUI.Infrastructure;
using GPTW.ListAutomation.TestUI.Models;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GPTW.ListAutomation.TestUI.Handlers;

public sealed class ExcelDemographicsDataHandler : IExcelWorksheetDataHandler
{
    private readonly IListCompanyService _listCompanyService;
    private readonly IListSurveyRespondentDemographicService _listSurveyRespondentDemographicService;
    private readonly ICacheManager _cacheManager;
    private readonly ILogger<ExcelDemographicsDataHandler> _logger;

    public ExcelDemographicsDataHandler(
        IListCompanyService listCompanyService,
        IListSurveyRespondentDemographicService listSurveyRespondentDemographicService,
        ICacheManager cacheManager,
        ILogger<ExcelDemographicsDataHandler> logger)
    {
        _listCompanyService = listCompanyService;
        _listSurveyRespondentDemographicService = listSurveyRespondentDemographicService;
        _cacheManager = cacheManager;
        _logger = logger;
    }

    public WorksheetDataSource Source => WorksheetDataSource.Demographics;

    public async Task Process(int listSourceFileId, DataTable dt)
    {
        var demographicsData = new List<DemographicItemModel>();
        IEnumerable<ListCompanyModel> listCompanies;
        List<ListSurveyRespondentDemographicModel> listSurveyRespondentDemographics;

        using (_logger.MeasureOperation("Adjust Demographics Datatable"))
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
                    var demographicItem = new DemographicItemModel()
                    {
                        EngagementId = engagementId,
                        ClientId = clientId,
                        SurveyVersionId = surveyVersionId.ToString(),
                        RespondentKey = respondentKey
                    };

                    foreach (DataColumn col in dt.Columns)
                    {
                        var dColumn = CultureSurveyConstants.DemographicsColumns.FirstOrDefault(dc => dc.Key.Equals(col.ColumnName, StringComparison.OrdinalIgnoreCase));
                        if (!dColumn.IsDefault())
                        {
                            demographicItem.SetProperty(dColumn.Value, dataRow[col.ColumnName]);
                        }
                    }

                    demographicsData.Add(demographicItem);
                }
            }
        }

        using (_logger.MeasureOperation("Create ListCompany"))
        {
            var companies = demographicsData.GroupBy(r => new { r.EngagementId, r.ClientId, r.SurveyVersionId })
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

        using (_logger.MeasureOperation("Prepare Demographics"))
        {
            listSurveyRespondentDemographics = demographicsData.Select(r => new ListSurveyRespondentDemographicModel
            {
                ListCompanyDemographicsId = 0,
                ListCompanyId = _cacheManager.Get($"LC_E{r.EngagementId}_C{r.ClientId}_SV{r.SurveyVersionId}", () =>
                {
                    return listCompanies.First(lc => lc.EngagementId == r.EngagementId
                        && lc.ClientId == r.ClientId && lc.SurveyVersionId == r.SurveyVersionId).ListCompanyId;
                }),
                RespondentKey = r.RespondentKey,
                Gender = r.Gender,
                Age = r.Age,
                CountryRegion = r.CountryRegion,
                JobLevel = r.JobLevel,
                LgbtOrLgbtQ = r.LgbtOrLgbtQ,
                RaceEthniticity = r.RaceEthniticity,
                Responsibility = r.Responsibility,
                Tenure = r.Tenure,
                WorkStatus = r.WorkStatus,
                WorkType = r.WorkType,
                WorkerType = r.WorkerType,
                BirthYear = r.BirthYear,
                Confidence = r.Confidence,
                Disabilities = r.Disabilities,
                ManagerialLevel = r.ManagerialLevel,
                MeaningfulInnovationOpportunities = r.MeaningfulInnovationOpportunities,
                PayType = r.PayType,
                Zipcode = r.Zipcode,
                CreatedDateTime = DateTime.Now,
                ModifiedDateTime = DateTime.Now
            }).ToList();
        }

        using (_logger.MeasureOperation("BulkInsert Demographics"))
        {
            await _listSurveyRespondentDemographicService.BulkInsert(listSurveyRespondentDemographics);
        }
    }
}
