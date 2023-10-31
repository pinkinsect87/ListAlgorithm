using Dapper;
using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IListSurveyRespondentDemographicService : IListAutomationService
{
    IQueryable<ListSurveyRespondentDemographic> GetTable();
    void Delete(ListSurveyRespondentDemographic mListSurveyRespondentDemographic);
    ListSurveyRespondentDemographic? GetById(int Id);
    void Insert(ListSurveyRespondentDemographic mListSurveyRespondentDemographic);
    void Update(ListSurveyRespondentDemographic mListSurveyRespondentDemographic);
    Task BulkInsert(List<ListSurveyRespondentDemographicModel> mListSurveyRespondentDemographics);
    Task<IEnumerable<string>> GetExistingAnswerOptionsForCompany(string columnName, int clientId, int engagementId);
    Task<double> GetNetDemographicScore(string filter, string columnName, string confidenceAnswerOption, string nonConfidenceAnswerOption, int clientId, int engagementId);
}

public class ListSurveyRespondentDemographicService : IListSurveyRespondentDemographicService
{
    private readonly IRepository<ListSurveyRespondentDemographic> _listSurveyRespondentDemographicRepository;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISettings _settings;
    private readonly ILogger<IListCompanyCultureBriefService> _logger;

    public ListSurveyRespondentDemographicService(
        IRepository<ListSurveyRespondentDemographic> listSurveyRespondentDemographicRepository,
        IDbConnectionFactory connectionFactory,
        ISettings settings,
        ILogger<IListCompanyCultureBriefService> logger)
    {
        _listSurveyRespondentDemographicRepository = listSurveyRespondentDemographicRepository;
        _connectionFactory = connectionFactory;
        _settings = settings;
        _logger = logger;
    }

    public IQueryable<ListSurveyRespondentDemographic> GetTable()
    {
        return _listSurveyRespondentDemographicRepository.Table;
    }

    public void Delete(ListSurveyRespondentDemographic mListSurveyRespondentDemographic)
    {
        if (mListSurveyRespondentDemographic == null)
            throw new ArgumentNullException("ListSurveyRespondentDemographic");

        _listSurveyRespondentDemographicRepository.Delete(mListSurveyRespondentDemographic);
    }

    public ListSurveyRespondentDemographic? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.ListCompanyDemographicsId == Id);
    }

    public void Insert(ListSurveyRespondentDemographic mListSurveyRespondentDemographic)
    {
        if (mListSurveyRespondentDemographic == null)
            throw new ArgumentNullException("ListSurveyRespondentDemographic");

        _listSurveyRespondentDemographicRepository.Insert(mListSurveyRespondentDemographic);
    }

    public void Update(ListSurveyRespondentDemographic mListSurveyRespondentDemographic)
    {
        if (mListSurveyRespondentDemographic == null)
            throw new ArgumentNullException("ListSurveyRespondentDemographic");

        _listSurveyRespondentDemographicRepository.Update(mListSurveyRespondentDemographic);
    }

    public async Task BulkInsert(List<ListSurveyRespondentDemographicModel> mListSurveyRespondentDemographics)
    {
        if (mListSurveyRespondentDemographics.Any())
        {
            using var bulkCopy = new SqlBulkCopy(_settings.DbConnectionString, SqlBulkCopyOptions.TableLock);
            {
                bulkCopy.DestinationTableName = "ListSurveyRespondentDemographics";
                bulkCopy.BatchSize = _settings.SqlBulkCopyBatchSize;
                bulkCopy.BulkCopyTimeout = 0;

                var batchCount = 0;
                var recordCount = 0;
                var dt = new DataTable();
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.ListCompanyDemographicsId), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.ListCompanyId), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.RespondentKey), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.Gender), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.Age), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.CountryRegion), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.JobLevel), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.LgbtOrLgbtQ), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.RaceEthniticity), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.Responsibility), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.Tenure), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.WorkStatus), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.WorkType), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.WorkerType), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.BirthYear), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.Confidence), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.Disabilities), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.ManagerialLevel), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.MeaningfulInnovationOpportunities), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.PayType), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.Zipcode), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.CreatedDateTime), typeof(DateTime));
                dt.Columns.Add(nameof(ListSurveyRespondentDemographicModel.ModifiedDateTime), typeof(DateTime));

                foreach (var record in mListSurveyRespondentDemographics)
                {
                    recordCount += 1;

                    var row = dt.NewRow();
                    row[0] = record.ListCompanyDemographicsId;
                    row[1] = record.ListCompanyId;
                    row[2] = record.RespondentKey;
                    row[3] = record.Gender;
                    row[4] = record.Age;
                    row[5] = record.CountryRegion;
                    row[6] = record.JobLevel;
                    row[7] = record.LgbtOrLgbtQ;
                    row[8] = record.RaceEthniticity;
                    row[9] = record.Responsibility;
                    row[10] = record.Tenure;
                    row[11] = record.WorkStatus;
                    row[12] = record.WorkType;
                    row[13] = record.WorkerType;
                    row[14] = record.BirthYear;
                    row[15] = record.Confidence;
                    row[16] = record.Disabilities;
                    row[17] = record.ManagerialLevel;
                    row[18] = record.MeaningfulInnovationOpportunities;
                    row[19] = record.PayType;
                    row[20] = record.Zipcode;
                    row[21] = record.CreatedDateTime;
                    row[22] = record.ModifiedDateTime;
                    dt.Rows.Add(row);

                    if (recordCount == _settings.BulkLoadMaxPerBatch)
                    {
                        batchCount += 1;

                        using (_logger.MeasureOperation($"SqlBulkCopy {batchCount}"))
                        {
                            await bulkCopy.WriteToServerAsync(dt);
                        }

                        recordCount = 0;
                        dt.Clear();
                    }
                }

                if (dt.Rows.Count > 0)
                {
                    batchCount += 1;

                    using (_logger.MeasureOperation($"SqlBulkCopy {batchCount}"))
                    {
                        await bulkCopy.WriteToServerAsync(dt);
                    }
                }
            }
        }
    }

    public async Task<IEnumerable<string>> GetExistingAnswerOptionsForCompany(string columnName, int clientId, int engagementId)
    {
        var sql = $@"
                SELECT DISTINCT [{columnName}]
                FROM [dbo].[ListSurveyRespondentDemographics] AS LS with(nolock) 
                INNER JOIN [dbo].[ListCompany] AS LC with(nolock) ON LS.[ListCompanyId] = LC.[ListCompanyId]
                WHERE LC.[ClientId] = @clientId 
                AND LC.[EngagementId] = @engagementId
				AND LS.[{columnName}] IS NOT NULL;";

        using var cn = await _connectionFactory.GetDbConnection();
        {
            return await cn.QueryAsync<string>(sql, new { clientId, engagementId });
        }
    }

    public async Task<double> GetNetDemographicScore(string filter, string columnName, string confidenceAnswerOption, string nonConfidenceAnswerOption, int clientId, int engagementId)
    {
        using var cn = await _connectionFactory.GetDbConnection();
        {
            return await cn.QueryFirstOrDefaultAsync<double>("[dbo].[sp_GetNetDemographicScore]", 
                new { filter, clientId, engagementId, columnName, confidenceAnswerOption, nonConfidenceAnswerOption }, commandType: CommandType.StoredProcedure);
        }
    }
}
