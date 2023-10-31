using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IListSurveyRespondentCommentService : IListAutomationService
{
    IQueryable<ListSurveyRespondentComment> GetTable();
    void Delete(ListSurveyRespondentComment mListSurveyRespondentComment);
    ListSurveyRespondentComment? GetById(int Id);
    void Insert(ListSurveyRespondentComment mListSurveyRespondentComment);
    void Update(ListSurveyRespondentComment mListSurveyRespondentComment);
    Task BulkInsert(List<ListSurveyRespondentCommentModel> mListSurveyRespondentComments);
}

public class ListSurveyRespondentCommentService : IListSurveyRespondentCommentService
{
    private readonly IRepository<ListSurveyRespondentComment> _listSurveyRespondentCommentRepository;
    private readonly ISettings _settings;
    private readonly ILogger<IListSurveyRespondentCommentService> _logger;

    public ListSurveyRespondentCommentService(
        IRepository<ListSurveyRespondentComment> listSurveyRespondentCommentRepository,
        ISettings settings,
        ILogger<IListSurveyRespondentCommentService> logger)
    {
        _listSurveyRespondentCommentRepository = listSurveyRespondentCommentRepository;
        _settings = settings;
        _logger = logger;
    }

    public IQueryable<ListSurveyRespondentComment> GetTable()
    {
        return _listSurveyRespondentCommentRepository.Table;
    }

    public void Delete(ListSurveyRespondentComment mListSurveyRespondentComment)
    {
        if (mListSurveyRespondentComment == null)
            throw new ArgumentNullException("ListSurveyRespondentComment");

        _listSurveyRespondentCommentRepository.Delete(mListSurveyRespondentComment);
    }

    public ListSurveyRespondentComment? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.ListSurveyRespondentCommentsId == Id);
    }

    public void Insert(ListSurveyRespondentComment mListSurveyRespondentComment)
    {
        if (mListSurveyRespondentComment == null)
            throw new ArgumentNullException("ListSurveyRespondentComment");

        _listSurveyRespondentCommentRepository.Insert(mListSurveyRespondentComment);
    }

    public void Update(ListSurveyRespondentComment mListSurveyRespondentComment)
    {
        if (mListSurveyRespondentComment == null)
            throw new ArgumentNullException("ListSurveyRespondentComment");

        _listSurveyRespondentCommentRepository.Update(mListSurveyRespondentComment);
    }

    public async Task BulkInsert(List<ListSurveyRespondentCommentModel> mListSurveyRespondentComments)
    {
        if (mListSurveyRespondentComments.Any())
        {
            using var bulkCopy = new SqlBulkCopy(_settings.DbConnectionString, SqlBulkCopyOptions.TableLock);
            {
                bulkCopy.DestinationTableName = "ListSurveyRespondentComments";
                bulkCopy.BatchSize = _settings.SqlBulkCopyBatchSize;
                bulkCopy.BulkCopyTimeout = 0;

                var batchCount = 0;
                var recordCount = 0;
                var dt = new DataTable();
                dt.Columns.Add(nameof(ListSurveyRespondentCommentModel.ListSurveyRespondentCommentsId), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentCommentModel.ListCompanyId), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentCommentModel.RespondentKey), typeof(Int32));
                dt.Columns.Add(nameof(ListSurveyRespondentCommentModel.Question), typeof(string));
                dt.Columns.Add(nameof(ListSurveyRespondentCommentModel.Response), typeof(string));

                foreach (var record in mListSurveyRespondentComments)
                {
                    recordCount += 1;

                    var row = dt.NewRow();
                    row[0] = record.ListSurveyRespondentCommentsId;
                    row[1] = record.ListCompanyId;
                    row[2] = record.RespondentKey;
                    row[3] = record.Question;
                    row[4] = record.Response;
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
