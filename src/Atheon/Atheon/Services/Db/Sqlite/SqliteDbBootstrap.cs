using Atheon.Attributes;
using Atheon.DapperExtensions;
using Atheon.Models.Database.Sqlite;
using Atheon.Models.Destiny;
using Atheon.Options;
using Atheon.Services.Interfaces;
using Dapper;
using DotNetBungieAPI.Models.Destiny.Definitions.Collectibles;
using DotNetBungieAPI.Models.Destiny.Definitions.Metrics;
using DotNetBungieAPI.Models.Destiny.Definitions.Progressions;
using DotNetBungieAPI.Models.Destiny.Definitions.Records;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Atheon.Services.Db.Sqlite;

public class SqliteDbBootstrap : IDbBootstrap
{
    private readonly ILogger<SqliteDbBootstrap> _logger;
    private readonly IOptions<DatabaseOptions> _databaseOptions;
    private readonly IDbAccess _dbAccess;
    private readonly IOptions<JsonOptions> _jsonOptions;

    public SqliteDbBootstrap(
        ILogger<SqliteDbBootstrap> logger,
        IOptions<DatabaseOptions> databaseOptions,
        IDbAccess dbAccess,
        IOptions<JsonOptions> jsonOptions)
    {
        _logger = logger;
        _databaseOptions = databaseOptions;
        _dbAccess = dbAccess;
        _jsonOptions = jsonOptions;
    }

    private void RegisterDapperMappings()
    {
        _logger.LogDebug("Registering DB mappings...");

        var assemblyTypes = Assembly.GetAssembly(GetType())!.GetTypes();
        var automappedTypes = assemblyTypes.Where(x => x.GetCustomAttribute<DapperAutomapAttribute>() is not null);

        foreach (var type in automappedTypes)
        {
            SqlMapper.SetTypeMap(
                type: type,
                new CustomPropertyTypeMap(
                    type,
                    (type, columnName) =>
                    {
                        return type
                            .GetProperties()
                            .FirstOrDefault(prop =>
                                prop.GetCustomAttributes(false)
                                .OfType<DapperColumnAttribute>()
                                .Any(attr => attr.ColumnName == columnName));
                    }));
        }

        RegisterJsonHandler<DefinitionTrackSettings<DestinyMetricDefinition>>();
        RegisterJsonHandler<DefinitionTrackSettings<DestinyRecordDefinition>>();
        RegisterJsonHandler<DefinitionTrackSettings<DestinyCollectibleDefinition>>();
        RegisterJsonHandler<DefinitionTrackSettings<DestinyProgressionDefinition>>();
        RegisterJsonHandler<HashSet<long>>();
    }

    public async Task InitialiseDb(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initialising database...");

        RegisterDapperMappings();

        var settings = _databaseOptions.Value;

        foreach (var (tableName, tableSettings) in settings.Tables)
        {
            var result = await _dbAccess.QueryFirstOrDefaultAsync<int?>("SELECT 1 FROM sqlite_master WHERE type = 'table' AND name = @Name", new { Name = tableName });

            if (result != 1)
            {
                await CreateTable(tableName, tableSettings);
            }
            else
            {
                var columns = await GetTableColumns(tableName);

                var columnMapping = new Dictionary<DatabaseTableColumn, ColumnInfo>();

                foreach (var column in tableSettings.Columns)
                {
                    var mappedColumn = columns.FirstOrDefault(x => x.Name == column.Name);

                    if (mappedColumn is not null)
                    {
                        columnMapping.Add(column, mappedColumn);

                        if (!mappedColumn.IsEqualTo(column))
                        {
                            // well fuck we have an issue there
                        }
                    }
                    else
                    {
                        await _dbAccess.ExecuteAsync($"ALTER TABLE {tableName} ADD COLUMN {column.FormatForCreateQuery(DatabaseOptions.SqliteKey)}");
                    }
                }
            }
        }
    }

    private async Task<List<ColumnInfo>> GetTableColumns(string tableName)
    {
        var result = await _dbAccess.QueryAsync<ColumnInfo>($"PRAGMA table_info({tableName})");

        return result.ToList();
    }

    private async Task CreateTable(string tableName, DatabaseTableEntry tableSettings)
    {
        var sb = new StringBuilder();

        sb.Append($"CREATE TABLE {tableName}(");

        sb.AppendJoin(", ", tableSettings.Columns.Select(x => x.FormatForCreateQuery(DatabaseOptions.SqliteKey)));

        sb.Append(");");

        await _dbAccess.ExecuteAsync(sb.ToString());
    }

    private void RegisterJsonHandler<THandledType>()
    {
        SqlMapper.AddTypeHandler(typeof(THandledType), new JsonTypeHandler<THandledType>(_jsonOptions.Value.SerializerOptions));
    }

}
