using Dapper;
using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using System.Data;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IListAutomationResultService : IListAutomationService
{
    IQueryable<ListAutomationResult> GetTable();
    void Delete(ListAutomationResult mListAutomationResult);
    ListAutomationResult? GetById(int Id);
    void Insert(ListAutomationResult mListAutomationResult);
    void BulkInsert(IEnumerable<ListAutomationResult> mListAutomationResults);
    void Update(ListAutomationResult mListAutomationResult);
    Task<DataTable> GetListAutomationResultByListRequestId(int listRequestId);
    void ClearListAutomationResultByListCompanyId(int listRequestId);
}

public class ListAutomationResultService : IListAutomationResultService
{
    private readonly IRepository<ListAutomationResult> _listAutomationResultRepository;
    private readonly IDbConnectionFactory _connectionFactory;

    public ListAutomationResultService(
        IRepository<ListAutomationResult> listAutomationResultRepository,
        IDbConnectionFactory connectionFactory)
    {
        _listAutomationResultRepository = listAutomationResultRepository;
        _connectionFactory = connectionFactory;
    }

    public IQueryable<ListAutomationResult> GetTable()
    {
        return _listAutomationResultRepository.Table;
    }

    public void Delete(ListAutomationResult mListAutomationResult)
    {
        if (mListAutomationResult == null)
            throw new ArgumentNullException("ListAutomationResult");

        _listAutomationResultRepository.Delete(mListAutomationResult);
    }

    public ListAutomationResult? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.ListAutomationResultId == Id);
    }

    public void Insert(ListAutomationResult mListAutomationResult)
    {
        if (mListAutomationResult == null)
            throw new ArgumentNullException("ListAutomationResult");

        _listAutomationResultRepository.Insert(mListAutomationResult);
    }

    public void BulkInsert(IEnumerable<ListAutomationResult> mListAutomationResults)
    {
        if (mListAutomationResults == null)
            throw new ArgumentNullException("ListAutomationResult");

        _listAutomationResultRepository.Insert(mListAutomationResults);
    }

    public void Update(ListAutomationResult mListAutomationResult)
    {
        if (mListAutomationResult == null)
            throw new ArgumentNullException("ListAutomationResult");

        _listAutomationResultRepository.Update(mListAutomationResult);
    }

    public async Task<DataTable> GetListAutomationResultByListRequestId(int listRequestId)
    {
        using var cn = await _connectionFactory.GetDbConnection();
        {
            DataTable dt = new DataTable();

            var reader = await cn.ExecuteReaderAsync(
                "[dbo].[sp_GetListAutomationResultByListRequestId]", new { listRequestId }, commandType: CommandType.StoredProcedure);

            dt.Load(reader);

            return dt;
        }
    }

    public void ClearListAutomationResultByListCompanyId(int listRequestId)
    {
        var results = GetTable().Where(la => la.ListCompanyId == listRequestId).ToList();

        _listAutomationResultRepository.Delete(results);
    }
}
