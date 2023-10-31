using Microsoft.Data.SqlClient;
using System.Data;

namespace GPTW.ListAutomation.Core.Services;

public interface ISqlBulkCopyWrapper : IListAutomationService
{
    Task WriteToServerAsync(DataTable dataTable);
    Task WriteToServerAsync(string tableName, DataTable dataTable);
}

public sealed class SqlBulkCopyWrapper : ISqlBulkCopyWrapper
{
    private readonly ISettings _settings;

    public SqlBulkCopyWrapper(ISettings settings)
    {
        _settings = settings;
    }

    public async Task WriteToServerAsync(DataTable dataTable)
    {
        await WriteToServerAsync(dataTable.TableName, dataTable);
    }

    public async Task WriteToServerAsync(string tableName, DataTable dataTable)
    {
        using (var bulkCopy = new SqlBulkCopy(_settings.DbConnectionString, SqlBulkCopyOptions.TableLock))
        {
            bulkCopy.DestinationTableName = tableName;
            bulkCopy.BatchSize = _settings.SqlBulkCopyBatchSize;
            bulkCopy.BulkCopyTimeout = 0;

            await bulkCopy.WriteToServerAsync(dataTable);
        }
    }
}
