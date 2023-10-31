using Dapper;
using GPTW.ListAutomation.Core.Infrastructure;
using GPTW.ListAutomation.Core.Models;
using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using System.Data;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IListCompanyService : IListAutomationService
{
    IQueryable<ListCompany> GetTable();
    void Delete(ListCompany mListCompany);
    ListCompany? GetById(int Id);
    void Insert(ListCompany mListCompany);
    void Update(ListCompany mListCompany);
    ListCompanyModel? GetListCompanyById(int listCompanyById);
    Task BulkUpdate(int listSourceFileId, List<ListCompanyModel> mListCompanies);
    Task<IEnumerable<ListCompanyModel>> GetListCompanies(List<ListCompanyModel> mListCompanies);
}

public class ListCompanyService : IListCompanyService
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ISqlBulkCopyWrapper _sqlBulkCopyWrapper;
    private readonly IRepository<ListCompany> _listCompanyRepository;
    private readonly IRepository<ListSourceFile> _listSourceFileRepository;

    public ListCompanyService(
        IDbConnectionFactory dbConnectionFactory,
        ISqlBulkCopyWrapper sqlBulkCopyWrapper,
        IRepository<ListCompany> listCompanyRepository,
        IRepository<ListSourceFile> listSourceFileRepository)
    {
        _connectionFactory = dbConnectionFactory;
        _sqlBulkCopyWrapper = sqlBulkCopyWrapper;
        _listCompanyRepository = listCompanyRepository;
        _listSourceFileRepository = listSourceFileRepository;
    }

    public IQueryable<ListCompany> GetTable()
    {
        return _listCompanyRepository.Table;
    }

    public void Delete(ListCompany mListCompany)
    {
        if (mListCompany == null)
            throw new ArgumentNullException("ListCompany");

        _listCompanyRepository.Delete(mListCompany);
    }

    public ListCompany? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.ListCompanyId == Id);
    }

    public void Insert(ListCompany mListCompany)
    {
        if (mListCompany == null)
            throw new ArgumentNullException("ListCompany");

        _listCompanyRepository.Insert(mListCompany);
    }

    public void Update(ListCompany mListCompany)
    {
        if (mListCompany == null)
            throw new ArgumentNullException("ListCompany");

        _listCompanyRepository.Update(mListCompany);
    }

    public ListCompanyModel? GetListCompanyById(int listCompanyById)
    {
        return GetTable()
            .Select(lc => new ListCompanyModel
            {
                ListCompanyId = lc.ListCompanyId,
                EngagementId = lc.EngagementId ?? 0,
                ClientId = lc.ClientId ?? 0,
                ClientName = lc.ClientName,
                SurveyVersionId = lc.SurveyVersionId
            }).FirstOrDefault(lc => lc.ListCompanyId == listCompanyById);
    }

    public async Task BulkUpdate(int listSourceFileId, List<ListCompanyModel> mListCompanies)
    {
        var listSourceFile = _listSourceFileRepository.Table.FirstOrDefault(o => o.ListSourceFileId == listSourceFileId);

        if (listSourceFile != null && mListCompanies.Any())
        {
            var tempTableName = "tmpListCompany";

            var colNames = mListCompanies.GetTableColumnNames();

            using var cn = await _connectionFactory.GetDbConnection();
            {
                var sql = $@"
                    IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE [object_id] = OBJECT_ID(N'{tempTableName}') AND [type] = 'U') BEGIN                    
                        CREATE TABLE [dbo].[{tempTableName}]({string.Join(", ", colNames)});
                    END";

                await cn.ExecuteAsync(sql);

                var dt = mListCompanies.ConvertToDataTable();
                dt.TableName = tempTableName;
                await _sqlBulkCopyWrapper.WriteToServerAsync(dt);

                var updateSql = $@"
                    -- merge ListCompany
		            ;WITH ListCompanyData AS (
			            SELECT [EngagementId], [ClientId], [ClientName], [SurveyVersionId]
			            FROM [dbo].[{tempTableName}]
		            )

		            MERGE INTO [dbo].[ListCompany] AS Target
		            USING ListCompanyData AS Source
		            ON Target.[ClientId] = Source.[ClientId] AND Target.[EngagementId] = Source.[EngagementId] AND Target.[SurveyVersionId] = Source.[SurveyVersionId] 
		            WHEN MATCHED THEN
			            UPDATE SET Target.[ClientName] = (case when Source.[ClientName] is null or len(Source.[ClientName]) > 0 then Source.[ClientName] else Target.[ClientName] end)
		            WHEN NOT MATCHED by Target THEN
			            INSERT([ClientId],[ClientName],[EngagementId],[SurveyVersionId],[CertificationDateTime],[IsCertified],[IsDisqualified],[ListSourceFileId],[ListRequestId],[SurveyDateTime])
			            VALUES(Source.[ClientId], Source.[ClientName], Source.[EngagementId], Source.[SurveyVersionId], null, null, null, @ListSourceFileId, @ListRequestId, GETDATE());

                    DROP TABLE [dbo].[{tempTableName}];";

                await cn.ExecuteAsync(updateSql, new { listSourceFile.ListRequestId, listSourceFile.ListSourceFileId });
            }
        }
    }

    public async Task<IEnumerable<ListCompanyModel>> GetListCompanies(List<ListCompanyModel> mListCompanies)
    {
        var dataTable = new DataTable("ListCompanyTableType");
        dataTable.Columns.Add("EngagementId", typeof(Int32));
        dataTable.Columns.Add("ClientId", typeof(Int32));
        dataTable.Columns.Add("SurveyVersionId", typeof(string));

        foreach (var company in mListCompanies)
        {
            dataTable.Rows.Add(company.EngagementId, company.ClientId, company.SurveyVersionId);
        }

        using var cn = await _connectionFactory.GetDbConnection();
        {
            const string sql =
                    @"SELECT lc.ListCompanyId, dt.EngagementId, dt.ClientId, dt.SurveyVersionId
                      FROM @ListCompanyTableType as dt
                      INNER JOIN dbo.ListCompany as lc 
                      ON dt.EngagementId = lc.EngagementId AND dt.ClientId = lc.ClientId AND dt.SurveyVersionId = lc.SurveyVersionId;";

            return await cn.QueryAsync<ListCompanyModel>(sql, new { ListCompanyTableType = dataTable.AsTableValuedParameter("ListCompanyTableType") });
        }
    }
}
