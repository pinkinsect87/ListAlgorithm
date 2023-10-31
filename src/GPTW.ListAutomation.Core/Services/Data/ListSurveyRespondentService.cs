using Dapper;
using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IListSurveyRespondentService : IListAutomationService
{
    IQueryable<ListSurveyRespondent> GetTable();
    void Delete(ListSurveyRespondent mListSurveyRespondent);
    ListSurveyRespondent? GetById(int Id);
    void Insert(ListSurveyRespondent mListSurveyRespondent);
    void Update(ListSurveyRespondent mListSurveyRespondent);
    Task BulkInsert(List<ListSurveyRespondentModel> mListSurveyRespondents);
    Task<double> GetTrustIndexScore(string filter, int clientId, int engagementId, int[] statementIds);
    Task<int> GetNumberOfRespondents(string filter, int clientId, int engagementId, int? statementId = null);
    Task<int> GetProduceRank(int listCompanyId);
}

public class ListSurveyRespondentService : IListSurveyRespondentService
{
    private readonly IRepository<ListSurveyRespondent> _listSurveyRespondentRepository;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISettings _settings;
    private readonly ILogger<IListSurveyRespondentService> _logger;

    public ListSurveyRespondentService(
        IRepository<ListSurveyRespondent> listSurveyRespondentRepository,
        IDbConnectionFactory connectionFactory,
        ISettings settings,
        ILogger<IListSurveyRespondentService> logger)
    {
        _listSurveyRespondentRepository = listSurveyRespondentRepository;
        _connectionFactory = connectionFactory;
        _settings = settings;
        _logger = logger;
    }

    public IQueryable<ListSurveyRespondent> GetTable()
    {
        return _listSurveyRespondentRepository.Table;
    }

    public void Delete(ListSurveyRespondent mListSurveyRespondent)
    {
        if (mListSurveyRespondent == null)
            throw new ArgumentNullException("ListSurveyRespondent");

        _listSurveyRespondentRepository.Delete(mListSurveyRespondent);
    }

    public ListSurveyRespondent? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.ListCompanyResponseId == Id);
    }

    public void Insert(ListSurveyRespondent mListSurveyRespondent)
    {
        if (mListSurveyRespondent == null)
            throw new ArgumentNullException("ListSurveyRespondent");

        _listSurveyRespondentRepository.Insert(mListSurveyRespondent);
    }

    public void Update(ListSurveyRespondent mListSurveyRespondent)
    {
        if (mListSurveyRespondent == null)
            throw new ArgumentNullException("ListSurveyRespondent");

        _listSurveyRespondentRepository.Update(mListSurveyRespondent);
    }

    public async Task BulkInsert(List<ListSurveyRespondentModel> mListSurveyRespondents)
    {
        if (mListSurveyRespondents.Any())
        {
            using var bulkCopy = new SqlBulkCopy(_settings.DbConnectionString, SqlBulkCopyOptions.TableLock);
            {
                bulkCopy.DestinationTableName = "ListSurveyRespondentNew";
                bulkCopy.BatchSize = _settings.SqlBulkCopyBatchSize;
                bulkCopy.BulkCopyTimeout = 0;

                var batchCount = 0;
                var recordCount = 0;
                var dt = new DataTable();

                #region DataTable Columns

                dt.Columns.Add(nameof(ListSurveyRespondentModel.ListCompanyResponseId), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.ListCompanyId), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.RespondentKey), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_1), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_2), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_3), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_4), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_5), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_6), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_7), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_8), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_9), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_10), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_11), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_12), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_13), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_14), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_15), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_16), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_17), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_18), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_19), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_20), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_21), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_22), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_23), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_24), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_25), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_26), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_27), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_28), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_29), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_30), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_31), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_32), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_33), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_34), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_35), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_36), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_37), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_38), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_39), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_40), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_41), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_42), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_43), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_44), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_45), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_46), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_47), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_48), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_49), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_50), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_51), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_52), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_53), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_54), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_55), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_56), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_57), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_672), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_12211), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_12212), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_12213), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_12214), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentModel.Response_12215), typeof(Int32));

                #endregion

                foreach (var record in mListSurveyRespondents)
                {
                    recordCount += 1;

                    var row = dt.NewRow();

                    #region Populate DataRow

                    row[0] = record.ListCompanyResponseId;
                    row[1] = record.ListCompanyId;
                    row[2] = record.RespondentKey;
                    row[3] = record.Response_1;
                    row[4] = record.Response_2;
                    row[5] = record.Response_3;
                    row[6] = record.Response_4;
                    row[7] = record.Response_5;
                    row[8] = record.Response_6;
                    row[9] = record.Response_7;
                    row[10] = record.Response_8;
                    row[11] = record.Response_9;
                    row[12] = record.Response_10;
                    row[13] = record.Response_11;
                    row[14] = record.Response_12;
                    row[15] = record.Response_13;
                    row[16] = record.Response_14;
                    row[17] = record.Response_15;
                    row[18] = record.Response_16;
                    row[19] = record.Response_17;
                    row[20] = record.Response_18;
                    row[21] = record.Response_19;
                    row[22] = record.Response_20;
                    row[23] = record.Response_21;
                    row[24] = record.Response_22;
                    row[25] = record.Response_23;
                    row[26] = record.Response_24;
                    row[27] = record.Response_25;
                    row[28] = record.Response_26;
                    row[29] = record.Response_27;
                    row[30] = record.Response_28;
                    row[31] = record.Response_29;
                    row[32] = record.Response_30;
                    row[33] = record.Response_31;
                    row[34] = record.Response_32;
                    row[35] = record.Response_33;
                    row[36] = record.Response_34;
                    row[37] = record.Response_35;
                    row[38] = record.Response_36;
                    row[39] = record.Response_37;
                    row[40] = record.Response_38;
                    row[41] = record.Response_39;
                    row[42] = record.Response_40;
                    row[43] = record.Response_41;
                    row[44] = record.Response_42;
                    row[45] = record.Response_43;
                    row[46] = record.Response_44;
                    row[47] = record.Response_45;
                    row[48] = record.Response_46;
                    row[49] = record.Response_47;
                    row[50] = record.Response_48;
                    row[51] = record.Response_49;
                    row[52] = record.Response_50;
                    row[53] = record.Response_51;
                    row[54] = record.Response_52;
                    row[55] = record.Response_53;
                    row[56] = record.Response_54;
                    row[57] = record.Response_55;
                    row[58] = record.Response_56;
                    row[59] = record.Response_57;
                    row[60] = record.Response_672;
                    row[61] = record.Response_12211;
                    row[62] = record.Response_12212;
                    row[63] = record.Response_12213;
                    row[64] = record.Response_12214;
                    row[65] = record.Response_12215;
                    dt.Rows.Add(row);

                    #endregion

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

    public async Task<double> GetTrustIndexScore(string filter, int clientId, int engagementId, int[] statementIds)
    {
        using var cn = await _connectionFactory.GetDbConnection();
        {
            var statementIdsFilter = statementIds.Any() ? string.Join(",", statementIds.Select(statementId => $"'Response_{statementId}'")) : "";
            return await cn.QueryFirstOrDefaultAsync<double>("[dbo].[sp_GetTrustIndexScore]",
                new { filter, clientId, engagementId, statementIdsFilter }, commandType: CommandType.StoredProcedure);
        }
    }

    public async Task<int> GetNumberOfRespondents(string filter, int clientId, int engagementId, int? statementId)
    {
        using var cn = await _connectionFactory.GetDbConnection();
        {
            var statementIdFilter = statementId.HasValue ? $"Response_{statementId}" : "";
            return await cn.QueryFirstOrDefaultAsync<int>("[dbo].[sp_GetNumberOfRespondents]",
                new { filter, clientId, engagementId, statementIdFilter }, commandType: CommandType.StoredProcedure);
        }
    }

    public async Task<int> GetProduceRank(int listCompanyId)
    {
        using var cn = await _connectionFactory.GetDbConnection();
        {
            return await cn.QueryFirstOrDefaultAsync<int>("[dbo].[sp_GetListCompanyProduceRank]", new { listCompanyId }, commandType: CommandType.StoredProcedure);
        }
    }
}
