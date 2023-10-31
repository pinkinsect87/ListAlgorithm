using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace GPTW.ListAutomation.Core;

public interface IDbConnectionFactory
{
    Task<IDbConnection> GetDbConnection();
}

public class DapperDbConnectionFactory : IDbConnectionFactory
{

    private readonly ISettings _settings;

    public DapperDbConnectionFactory(ISettings settings)
    {
        _settings = settings;

        SqlMapper.Settings.CommandTimeout = 240;
    }

    public async Task<IDbConnection> GetDbConnection()
    {
        var cn = new SqlConnection(_settings.DbConnectionString);
        await cn.OpenAsync();
        return cn;
    }
}
