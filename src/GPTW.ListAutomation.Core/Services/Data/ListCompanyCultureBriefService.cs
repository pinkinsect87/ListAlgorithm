using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IListCompanyCultureBriefService : IListAutomationService
{
    IQueryable<ListCompanyCultureBrief> GetTable();
    void Delete(ListCompanyCultureBrief mListCompanyCultureBrief);
    ListCompanyCultureBrief? GetById(int Id);
    void Insert(ListCompanyCultureBrief mListCompanyCultureBrief);
    void Update(ListCompanyCultureBrief mListCompanyCultureBrief);
    List<ListCompanyCultureBriefModel> GetCultureBriefVariablesByListCompanyId(int listCompanyId);
    Task BulkInsert(List<ListCompanyCultureBriefModel> mListCompanyCultureBriefs);
}

public class ListCompanyCultureBriefService : IListCompanyCultureBriefService
{
    private readonly IRepository<ListCompanyCultureBrief> _listCompanyCultureBriefRepository;
    private readonly ISettings _settings;
    private readonly ILogger<IListCompanyCultureBriefService> _logger;

    public ListCompanyCultureBriefService(
        IRepository<ListCompanyCultureBrief> listCompanyCultureBriefRepository,
        ISettings settings,
        ILogger<IListCompanyCultureBriefService> logger)
    {
        _listCompanyCultureBriefRepository = listCompanyCultureBriefRepository;
        _settings = settings;
        _logger = logger;
    }

    public IQueryable<ListCompanyCultureBrief> GetTable()
    {
        return _listCompanyCultureBriefRepository.Table;
    }

    public void Delete(ListCompanyCultureBrief mListCompanyCultureBrief)
    {
        if (mListCompanyCultureBrief == null)
            throw new ArgumentNullException("ListCompanyCultureBrief");

        _listCompanyCultureBriefRepository.Delete(mListCompanyCultureBrief);
    }

    public ListCompanyCultureBrief? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.ListCompanyCultureBriefId == Id);
    }

    public void Insert(ListCompanyCultureBrief mListCompanyCultureBrief)
    {
        if (mListCompanyCultureBrief == null)
            throw new ArgumentNullException("ListCompanyCultureBrief");

        _listCompanyCultureBriefRepository.Insert(mListCompanyCultureBrief);
    }

    public void Update(ListCompanyCultureBrief mListCompanyCultureBrief)
    {
        if (mListCompanyCultureBrief == null)
            throw new ArgumentNullException("ListCompanyCultureBrief");

        _listCompanyCultureBriefRepository.Update(mListCompanyCultureBrief);
    }

    public List<ListCompanyCultureBriefModel> GetCultureBriefVariablesByListCompanyId(int listCompanyId)
    {
        return GetTable().Where(cb => cb.ListCompanyId == listCompanyId)
            .Select(cb => new ListCompanyCultureBriefModel
            {
                ListCompanyCultureBriefId = cb.ListCompanyCultureBriefId,
                VariableName = cb.VariableName,
                VariableValue = cb.VariableValue,
                ListCompanyId = cb.ListCompanyId
            }).ToList();
    }

    public async Task BulkInsert(List<ListCompanyCultureBriefModel> mListCompanyCultureBriefs)
    {
        if (mListCompanyCultureBriefs.Any())
        {
            using var bulkCopy = new SqlBulkCopy(_settings.DbConnectionString, SqlBulkCopyOptions.TableLock);
            {
                bulkCopy.DestinationTableName = "ListCompanyCultureBrief";
                bulkCopy.BatchSize = _settings.SqlBulkCopyBatchSize;
                bulkCopy.BulkCopyTimeout = 0;

                var batchCount = 0;
                var recordCount = 0;
                var dt = new DataTable();
                dt.Columns.Add(nameof(ListCompanyCultureBriefModel.ListCompanyCultureBriefId), typeof(Int32));
                dt.Columns.Add(nameof(ListCompanyCultureBriefModel.VariableName), typeof(string));
                dt.Columns.Add(nameof(ListCompanyCultureBriefModel.VariableValue), typeof(string));
                dt.Columns.Add(nameof(ListCompanyCultureBriefModel.ListCompanyId), typeof(Int32));

                foreach (var record in mListCompanyCultureBriefs)
                {
                    recordCount += 1;

                    var row = dt.NewRow();
                    row[0] = record.ListCompanyCultureBriefId;
                    row[1] = record.VariableName;
                    row[2] = record.VariableValue;
                    row[3] = record.ListCompanyId;
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
}
