using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Dapper;

using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace drittich.SimpleQuery
{
	/// <summary>
	/// Provides a simple query service for interacting with a SQLite database.
	/// </summary>
	public class SimpleQueryService
	{
		private readonly ILogger<SimpleQueryService> _logger;
		private readonly LogLevel _logLevel;
		private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { WriteIndented = true };
		private readonly object _lock = new object();

		/// <summary>
		/// Connection string to the database.
		/// </summary>
		public readonly string _connectionString;
		/// <summary>
		/// Gets or sets the count of queries executed by this service.
		/// </summary>
		public int QueryCount = 0;
		/// <summary>
		/// Gets or sets the total time in milliseconds spent on queries executed by this service.
		/// </summary>
		public int QueryTotalMs = 0;

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleQueryService"/> class.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		/// <param name="logger">The logger.</param>
		/// <param name="logLevel">The log level.</param>
		public SimpleQueryService(string connectionString, ILogger<SimpleQueryService> logger, LogLevel logLevel)
		{
			_connectionString = connectionString;
			_logger = logger;
			_logLevel = logLevel;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleQueryService"/> class.
		/// Defaults to console logging for warnings and higher.
		/// </summary>
		/// <param name="connectionString">The connection string.</param>
		public SimpleQueryService(string connectionString) : this(connectionString, LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SimpleQueryService>(), LogLevel.Warning)
		{
			_connectionString = connectionString;
		}

		public async Task<T> QueryFirstAsync<T>(object id) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			var ret = await QueryEntityFirstOrDefaultAsync<T>(id)
				?? throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			return ret;
		}

		public async Task<T> QueryFirstAsync<T>(object id, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			var ret = await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode)
				?? throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			return ret;
		}

		public async Task<T> QueryFirstAsync<T>(object id, ReferenceFetchMode referenceFetchMode, string entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			var ret = await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode, entityTypeToFetch)
				?? throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			return ret;
		}
		public async Task<T> QueryFirstAsync<T>(object id, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			var ret = await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode, entityTypesToFetch)
				?? throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			return ret;
		}

		public async Task<T?> QueryFirstOrDefaultAsync<T>(object id) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryEntityFirstOrDefaultAsync<T>(id);
		}

		public async Task<T?> QueryFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode);
		}

		public async Task<T?> QueryFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode, string entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode, entityTypeToFetch);
		}

		public async Task<T?> QueryFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode, entityTypesToFetch);
		}


		public async Task<List<T>> QueryAsync<T>() where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryAsync<T>(null);
		}

		public async Task<List<T>> QueryAsync<T>(IEnumerable<object>? ids) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryAsync<T>(ids, ReferenceFetchMode.None);
		}


		public async Task<List<T>> QueryAsync<T>(IEnumerable<object>? ids, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryAsync<T>(ids, referenceFetchMode, (string?)null);
		}

		public async Task<List<T>> QueryAsync<T>(IEnumerable<object>? ids, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryAsync<T>(ids, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		/// <summary>
		/// Executes a query asynchronously to retrieve a list of entities of type T.
		/// Note: This assumes the PK column is named {EntityName}Id.
		/// </summary>
		/// <typeparam name="T">The type of the entities to query.</typeparam>
		/// <param name="ids">The collection of IDs to filter the query. If null, no filtering is applied.</param>
		/// <param name="referenceFetchMode">The reference fetch mode for the entities.</param>
		/// <param name="entityTypesToFetch">The collection of entity types to fetch references for.</param>
		/// <returns>A list of entities of type T.</returns>
		public async Task<List<T>> QueryAsync<T>(IEnumerable<object>? ids, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			_logger.LogTrace("Executing QueryAsync");

			string whereClause = string.Empty;
			DynamicParameters parameters = new DynamicParameters();

			if (ids != null && ids.Any())
			{
				// Access the primary key column names using the interface method
				var primaryKeyColumnNames = new T().GetPrimaryKeyColumnNames();

				// If the entity has multiple primary key columns, or none, we can't use this method
				if (primaryKeyColumnNames.Length != 1)
				{
					throw new InvalidOperationException($"Entity of type {typeof(T).Name} has missing or multiple primary key columns. This method only supports tables with a single primary key when passing ids.");
				}

				whereClause = $"WHERE {primaryKeyColumnNames.First()} in @ids";
				parameters.Add("ids", ids.ToArray());
			}

			var sql = $"SELECT * FROM {typeof(T).Name} {whereClause}";
			_logger.LogTrace($"SQL:\n{sql}");

			_logger.LogTrace($"Parameters:\n{JsonSerializer.Serialize(parameters, _jsonOptions)}");

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var ret = (await cn.QueryAsync<T>(sql, parameters)).ToList();
			var elapsedMs = sw.ElapsedMilliseconds;
			_logger.LogTrace($"Executed query in {elapsedMs:N0} ms");
			lock (_lock)
			{
				QueryTotalMs += (int)elapsedMs;
				QueryCount++;
			}

			foreach (var item in ret)
			{
				item._dbContext = this;
				item._fetchReferencesType = referenceFetchMode;
				item._entitiesToFetch = entityTypesToFetch;
			}

			return ret;
		}

		public async Task<T> QueryFirstByColumnValueAsync<T>(string columnName, object columnValue) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return (await QueryFirstByColumnValueAsync<T>(columnName, columnValue, ReferenceFetchMode.None));
		}

		public async Task<T> QueryFirstByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, (string?)null);
		}

		public async Task<T> QueryFirstByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return (await QueryByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, entityTypesToFetch)).First();
		}

		public async Task<T?> QueryFirstOrDefaultByColumnValueAsync<T>(string columnName, object columnValue) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByColumnValueAsync<T>(columnName, columnValue, ReferenceFetchMode.None);
		}

		public async Task<T?> QueryFirstOrDefaultByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, (string?)null);
		}

		public async Task<T?> QueryFirstOrDefaultByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T?> QueryFirstOrDefaultByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return (await QueryByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, entityTypesToFetch)).FirstOrDefault();
		}

		public async Task<List<T>> QueryByColumnValueAsync<T>(string columnName, object columnValue) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByColumnValueAsync<T>(columnName, columnValue, ReferenceFetchMode.None);
		}

		public async Task<List<T>> QueryByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, (string?)null);
		}

		public async Task<List<T>> QueryByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<List<T>> QueryByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			_logger.LogTrace("Executing QueryByColumnValueAsync");

			var sql = $"SELECT * FROM {typeof(T).Name} WHERE {columnName} = @columnValue";
			_logger.LogTrace($"SQL:\n{sql}");

			var parameters = new { columnValue };
			_logger.LogTrace($"Parameters:\n{JsonSerializer.Serialize(parameters, _jsonOptions)}");

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var results = (await cn.QueryAsync<T>(sql, parameters)).ToList();
			var elapsedMs = sw.ElapsedMilliseconds;
			_logger.LogTrace($"Executed query in {elapsedMs:N0} ms");
			lock (_lock)
			{
				QueryTotalMs += (int)elapsedMs;
				QueryCount++;
			}

			foreach (var item in results)
			{
				item._dbContext = this;
				item._fetchReferencesType = referenceFetchMode;
				item._entitiesToFetch = entityTypesToFetch;
			}

			return results;
		}

		public async Task<T> QueryFirstByColumnValuesAsync<T>(Dictionary<string, object> columnValues) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByColumnValuesAsync<T>(columnValues, ReferenceFetchMode.None);
		}

		public async Task<T> QueryFirstByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByColumnValuesAsync<T>(columnValues, referenceFetchMode, (string?)null);
		}

		public async Task<T> QueryFirstByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByColumnValuesAsync<T>(columnValues, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return (await QueryByColumnValuesAsync<T>(columnValues, referenceFetchMode, entityTypesToFetch)).First();
		}

		public async Task<T?> QueryFirstOrDefaultByColumnValuesAsync<T>(Dictionary<string, object> columnValues) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByColumnValuesAsync<T>(columnValues, ReferenceFetchMode.None);
		}
		public async Task<T?> QueryFirstOrDefaultByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByColumnValuesAsync<T>(columnValues, referenceFetchMode, (string?)null);
		}
		public async Task<T?> QueryFirstOrDefaultByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByColumnValuesAsync<T>(columnValues, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T?> QueryFirstOrDefaultByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return (await QueryByColumnValuesAsync<T>(columnValues, referenceFetchMode, entityTypesToFetch)).FirstOrDefault();
		}

		public async Task<List<T>> QueryByColumnValuesAsync<T>(Dictionary<string, object> columnValues) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByColumnValuesAsync<T>(columnValues, ReferenceFetchMode.None);
		}

		public async Task<List<T>> QueryByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByColumnValuesAsync<T>(columnValues, referenceFetchMode, (string?)null);
		}

		public async Task<List<T>> QueryByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByColumnValuesAsync<T>(columnValues, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<List<T>> QueryByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			_logger.LogTrace("Executing QueryByColumnValueAsync");

			var sql = $"SELECT * FROM {typeof(T).Name} WHERE ";
			sql += string.Join(" AND ", columnValues.Select(kvp => $"{kvp.Key} = @{kvp.Key}"));
			_logger.LogTrace($"SQL:\n{sql}");

			var parameters = columnValues;
			_logger.LogTrace($"Parameters:\n{JsonSerializer.Serialize(parameters, _jsonOptions)}");

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var results = (await cn.QueryAsync<T>(sql, columnValues)).ToList();
			var elapsedMs = sw.ElapsedMilliseconds;
			_logger.LogTrace($"Executed query in {elapsedMs:N0} ms");
			lock (_lock)
			{
				QueryTotalMs += (int)elapsedMs;
				QueryCount++;
			}

			foreach (var item in results)
			{
				item._dbContext = this;
				item._fetchReferencesType = referenceFetchMode;
				item._entitiesToFetch = entityTypesToFetch;
			}

			return results;
		}

		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByWhereClauseAsync<T>(whereClause, null);
		}

		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause, object? parameters) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByWhereClauseAsync<T>(whereClause, parameters, ReferenceFetchMode.None);
		}

		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, (string?)null);
		}

		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return (await QueryByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, entityTypesToFetch)).First();
		}

		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByWhereClauseAsync<T>(whereClause, null);
		}

		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause, object? parameters) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByWhereClauseAsync<T>(whereClause, parameters, ReferenceFetchMode.None);
		}

		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, (string?)null);
		}

		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return (await QueryByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, entityTypesToFetch)).FirstOrDefault();
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByWhereClauseAsync<T>(whereClause, null);
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, object? parameters) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByWhereClauseAsync<T>(whereClause, parameters, ReferenceFetchMode.None);
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByWhereClauseAsync<T>(whereClause, null, referenceFetchMode, (string?)null);
		}
		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, (string?)null);
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByWhereClauseAsync<T>(whereClause, null, referenceFetchMode, entityTypesToFetch);
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch = null) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			_logger.LogTrace("Executing QueryByWhereClauseAsync");

			var sql = $"SELECT * FROM {typeof(T).Name} WHERE {whereClause}";
			_logger.LogTrace($"SQL:\n{sql}");

			_logger.LogTrace($"Parameters:\n{JsonSerializer.Serialize(parameters, _jsonOptions)}");

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var results = (await cn.QueryAsync<T>(sql, parameters)).ToList();
			var elapsedMs = sw.ElapsedMilliseconds;
			_logger.LogTrace($"Executed query in {elapsedMs:N0} ms");
			lock (_lock)
			{
				QueryTotalMs += (int)elapsedMs;
				QueryCount++;
			}

			foreach (var item in results)
			{
				item._dbContext = this;
				item._fetchReferencesType = referenceFetchMode;
				item._entitiesToFetch = entityTypesToFetch;
			}

			return results;
		}

		public async Task<T> QueryFirstByQueryAsync<T>(string query) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByQueryAsync<T>(query, null);
		}
		public async Task<T> QueryFirstByQueryAsync<T>(string query, object? parameters) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByQueryAsync<T>(query, parameters, ReferenceFetchMode.None);
		}
		public async Task<T> QueryFirstByQueryAsync<T>(string query, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByQueryAsync<T>(query, null, referenceFetchMode, (string?)null);
		}
		public async Task<T> QueryFirstByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByQueryAsync<T>(query, parameters, referenceFetchMode, (string?)null);
		}
		public async Task<T> QueryFirstByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstByQueryAsync<T>(query, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return (await QueryBySqlAsync<T>(query, parameters, referenceFetchMode, entityTypesToFetch)).First();
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, null);
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, object? parameters) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, parameters, ReferenceFetchMode.None);
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, null, referenceFetchMode, (string?)null);
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, parameters, referenceFetchMode, (string?)null);
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, null, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			return (await QueryBySqlAsync<T>(query, parameters, referenceFetchMode, entityTypesToFetch)).FirstOrDefault();
		}

		public async Task<List<T>> QueryBySqlAsync<T>(string query)
		{
			return await QueryBySqlAsync<T>(query, null);
		}

		public async Task<List<T>> QueryBySqlAsync<T>(string query, object? parameters)
		{
			return await QueryBySqlAsync<T>(query, parameters, ReferenceFetchMode.None);
		}

		public async Task<List<T>> QueryBySqlAsync<T>(string query, ReferenceFetchMode referenceFetchMode)
		{
			return await QueryBySqlAsync<T>(query, null, referenceFetchMode, (string?)null);
		}

		public async Task<List<T>> QueryBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode)
		{
			return await QueryBySqlAsync<T>(query, parameters, referenceFetchMode, (string?)null);
		}

		public async Task<List<T>> QueryBySqlAsync<T>(string query, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch)
		{
			return await QueryBySqlAsync<T>(query, null, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<List<T>> QueryBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch)
		{
			return await QueryBySqlAsync<T>(query, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<List<T>> QueryBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch)
		{
			_logger.LogTrace("Executing QueryBySqlAsync");

			_logger.LogTrace($"SQL:\n{query}");

			_logger.LogTrace($"Parameters:\n{JsonSerializer.Serialize(parameters, _jsonOptions)}");

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var results = (await cn.QueryAsync<T>(query, parameters)).ToList();
			var elapsedMs = sw.ElapsedMilliseconds;
			_logger.LogTrace($"Executed query in {elapsedMs:N0} ms");
			lock (_lock)
			{
				QueryTotalMs += (int)elapsedMs;
				QueryCount++;
			}

			if (results.Any() && results.First() is SimpleQueryEntity)
			{
				foreach (var item in results)
				{
					var sqe = (item as SimpleQueryEntity)!;
					sqe._dbContext = this;
					sqe._fetchReferencesType = referenceFetchMode;
					sqe._entitiesToFetch = entityTypesToFetch;
				}
			}

			return results;
		}

		public async Task<T> QueryFirstBySqlAsync<T>(string query)
		{
			return await QueryFirstBySqlAsync<T>(query, null);
		}

		public async Task<T> QueryFirstBySqlAsync<T>(string query, object? parameters)
		{
			return await QueryFirstBySqlAsync<T>(query, parameters, ReferenceFetchMode.None);
		}

		public async Task<T> QueryFirstBySqlAsync<T>(string query, ReferenceFetchMode referenceFetchMode)
		{
			return await QueryFirstBySqlAsync<T>(query, null, referenceFetchMode, (string?)null);
		}

		public async Task<T> QueryFirstBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode)
		{
			return await QueryFirstBySqlAsync<T>(query, parameters, referenceFetchMode, (string?)null);
		}

		public async Task<T> QueryFirstBySqlAsync<T>(string query, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch)
		{
			return await QueryFirstBySqlAsync<T>(query, null, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch)
		{
			return await QueryFirstBySqlAsync<T>(query, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch)
		{
			_logger.LogTrace("Executing QueryFirstBySqlAsync");

			_logger.LogTrace($"SQL:\n{query}");

			_logger.LogTrace($"Parameters:\n{JsonSerializer.Serialize(parameters, _jsonOptions)}");

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var result = (await cn.QueryAsync<T>(query, parameters)).First();
			var elapsedMs = sw.ElapsedMilliseconds;
			_logger.LogTrace($"Executed query in {elapsedMs:N0} ms");
			lock (_lock)
			{
				QueryTotalMs += (int)elapsedMs;
				QueryCount++;
			}

			if (result is SimpleQueryEntity sqe)
			{
				sqe._dbContext = this;
				sqe._fetchReferencesType = referenceFetchMode;
				sqe._entitiesToFetch = entityTypesToFetch;
			}

			return result;
		}

		public async Task<T> QueryFirstOrDefaultBySqlAsync<T>(string query)
		{
			return await QueryFirstOrDefaultBySqlAsync<T>(query, null);
		}

		public async Task<T> QueryFirstOrDefaultBySqlAsync<T>(string query, object? parameters)
		{
			return await QueryFirstOrDefaultBySqlAsync<T>(query, parameters, ReferenceFetchMode.None);
		}

		public async Task<T> QueryFirstOrDefaultBySqlAsync<T>(string query, ReferenceFetchMode referenceFetchMode)
		{
			return await QueryFirstOrDefaultBySqlAsync<T>(query, null, referenceFetchMode, (string?)null);
		}

		public async Task<T> QueryFirstOrDefaultBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode)
		{
			return await QueryFirstOrDefaultBySqlAsync<T>(query, parameters, referenceFetchMode, (string?)null);
		}

		public async Task<T> QueryFirstOrDefaultBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch)
		{
			return await QueryFirstOrDefaultBySqlAsync<T>(query, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstOrDefaultBySqlAsync<T>(string query, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch)
		{
			return await QueryFirstOrDefaultBySqlAsync<T>(query, null, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstOrDefaultBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch)
		{
			_logger.LogTrace("Executing QueryFirstOrDefaultBySqlAsync");

			_logger.LogTrace($"SQL:\n{query}");

			_logger.LogTrace($"Parameters:\n{JsonSerializer.Serialize(parameters, _jsonOptions)}");

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var result = (await cn.QueryAsync<T>(query, parameters)).FirstOrDefault();
			var elapsedMs = sw.ElapsedMilliseconds;
			_logger.LogTrace($"Executed query in {elapsedMs:N0} ms");
			lock (_lock)
			{
				QueryTotalMs += (int)elapsedMs;
				QueryCount++;
			}

			if (result != null && result is SimpleQueryEntity sqe)
			{
				sqe._dbContext = this;
				sqe._fetchReferencesType = referenceFetchMode;
				sqe._entitiesToFetch = entityTypesToFetch;
			}

			// Assuming default(T) is acceptable for your use case when there's no result.
			return result;
		}

		/// <summary>
		/// Executes a SQL query asynchronously.
		/// </summary>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="parameters">The parameters to use in the query.</param>
		/// <returns>A Task representing the asynchronous operation.</returns>
		public async Task ExecuteAsync(string query, object? parameters)
		{
			_logger.LogTrace("Executing ExecuteAsync");

			_logger.LogTrace($"SQL:\n{query}");

			_logger.LogTrace($"Parameters:\n{JsonSerializer.Serialize(parameters, _jsonOptions)}");

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			await cn.ExecuteAsync(query, parameters);
			var elapsedMs = sw.ElapsedMilliseconds;
			_logger.LogTrace($"Executed query in {elapsedMs:N0} ms");
			lock (_lock)
			{
				QueryTotalMs += (int)elapsedMs;
				QueryCount++;
			}
		}

		/// <summary>
		/// Gets a connection to the SQLite database.
		/// </summary>
		/// <returns>A connection to the SQLite database.</returns>
		public SqliteConnection GetConnection()
		{
			using var cn = new SqliteConnection(_connectionString);
			cn.Open();
			return cn;
		}

		/// <summary>
		/// Asynchronously gets a connection to the SQLite database.
		/// </summary>
		/// <returns>A task that represents the asynchronous operation. The task result is a connection to the SQLite database.</returns>
		public async Task<SqliteConnection> GetConnectionAsync()
		{
			using var cn = new SqliteConnection(_connectionString);
			await cn.OpenAsync();
			return cn;
		}

		internal async Task<T?> QueryEntityFirstOrDefaultAsync<T>(object id) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			var idList = new List<object> { id };
			return (await QueryAsync<T>(idList, ReferenceFetchMode.None, (string?)null)).FirstOrDefault();
		}

		internal async Task<T?> QueryEntityFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			var idList = new List<object> { id };
			return (await QueryAsync<T>(idList, referenceFetchMode, (string?)null)).FirstOrDefault();
		}

		internal async Task<T?> QueryEntityFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode, string entityTypeToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			var idList = new List<object> { id };
			return (await QueryAsync<T>(idList, referenceFetchMode, entityTypeToFetch)).FirstOrDefault();
		}

		internal async Task<T?> QueryEntityFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			var idList = new List<object> { id };
			return (await QueryAsync<T>(idList, referenceFetchMode, entityTypesToFetch)).FirstOrDefault();
		}
	}
}
