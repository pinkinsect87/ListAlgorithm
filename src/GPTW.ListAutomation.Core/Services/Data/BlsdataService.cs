using Dapper;
using GPTW.ListAutomation.DataAccessLayer;
using GPTW.ListAutomation.DataAccessLayer.Domain;
using System.Data;

namespace GPTW.ListAutomation.Core.Services.Data;

public interface IBlsdataService : IListAutomationService
{
    IQueryable<Blsdata> GetTable();
    void Delete(Blsdata mBlsdata);
    Blsdata? GetById(int Id);
    void Insert(Blsdata mBlsdata);
    void Update(Blsdata mBlsdata);
    Task BatchSave(IEnumerable<Blsdata> entities);
}

public class BlsdataService : IBlsdataService
{
    private readonly IRepository<Blsdata> _blsdataRepository;
    private readonly IDbConnectionFactory _connectionFactory;

    public BlsdataService(IRepository<Blsdata> blsdataRepository, IDbConnectionFactory connectionFactory)
    {
        _blsdataRepository = blsdataRepository;
        _connectionFactory = connectionFactory;
    }

    public IQueryable<Blsdata> GetTable()
    {
        return _blsdataRepository.Table;
    }

    public void Delete(Blsdata mBlsdata)
    {
        if (mBlsdata == null)
            throw new ArgumentNullException("Blsdata");

        _blsdataRepository.Delete(mBlsdata);
    }

    public Blsdata? GetById(int Id)
    {
        if (Id == 0) return null;

        return GetTable().FirstOrDefault(d => d.BlsdataId == Id);
    }

    public void Insert(Blsdata mBlsdata)
    {
        if (mBlsdata == null)
            throw new ArgumentNullException("Blsdata");

        _blsdataRepository.Insert(mBlsdata);
    }

    public void Update(Blsdata mBlsdata)
    {
        if (mBlsdata == null)
            throw new ArgumentNullException("Blsdata");

        _blsdataRepository.Update(mBlsdata);
    }

    public async  Task BatchSave(IEnumerable<Blsdata> entities)
    {
        var blsDataTable = new DataTable("BLSDataInsertUpdateType");
        blsDataTable.Columns.Add("BLSDataKey", typeof(string));
        blsDataTable.Columns.Add("BLSDataValue", typeof(string));

        foreach (var bls in entities)
        {
            blsDataTable.Rows.Add(bls.BlsdataKey, bls.BlsdataValue);
        }

        using var cn = await _connectionFactory.GetDbConnection();
        using var transaction = cn.BeginTransaction();
        try
        {
            const string sql =
                @"UPDATE [dbo].[BLSData]
                      SET [BLSDataValue] = dt.[BLSDataValue]
                      FROM [dbo].[BLSData]
                      INNER JOIN @blsDataTable as dt on [dbo].[BLSData].[BLSDataKey] = dt.[BLSDataKey];

                      INSERT [dbo].[BLSData]([BLSDataKey], [BLSDataValue])
                      SELECT dt.[BLSDataKey], dt.[BLSDataValue]
                      FROM @blsDataTable as dt
                      LEFT JOIN [dbo].[BLSData] as bls on dt.[BLSDataKey] = bls.[BLSDataKey]
                      WHERE bls.[BLSDataKey] IS NULL;";
            await cn.ExecuteAsync(sql, new { blsDataTable = blsDataTable.AsTableValuedParameter("BLSDataTableType") }, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
