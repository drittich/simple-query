using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dapper;

using Microsoft.Data.Sqlite;

namespace drittich.SimpleQuery
{
	/// <summary>
	/// Provides a simple query service for interacting with a SQLite database.
	/// </summary>
	public class SimpleQueryService
	{
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
		/// Provides a simple query service for interacting with a SQLite database.
		/// </summary>
		/// <param name="connectionString">The connection string to the database.</param>
		public SimpleQueryService(string connectionString)
		{
			_connectionString = connectionString;
		}

		public async Task<T> QueryFirstAsync<T>(object id) where T : SimpleQueryEntity
		{
			var ret = await QueryEntityFirstOrDefaultAsync<T>(id)
				?? throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			return ret;
		}

		public async Task<T> QueryFirstAsync<T>(object id, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			var ret = await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode)
				?? throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			return ret;
		}

		public async Task<T> QueryFirstAsync<T>(object id, ReferenceFetchMode referenceFetchMode, string entityTypeToFetch) where T : SimpleQueryEntity
		{
			var ret = await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode, entityTypeToFetch)
				?? throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			return ret;
		}
		public async Task<T> QueryFirstAsync<T>(object id, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			var ret = await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode, entityTypesToFetch)
				?? throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			return ret;
		}

		public async Task<T?> QueryFirstOrDefaultAsync<T>(object id) where T : SimpleQueryEntity
		{
			return await QueryEntityFirstOrDefaultAsync<T>(id);
		}

		public async Task<T?> QueryFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode);
		}

		public async Task<T?> QueryFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode, string entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode, entityTypeToFetch);
		}

		public async Task<T?> QueryFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			return await QueryEntityFirstOrDefaultAsync<T>(id, referenceFetchMode, entityTypesToFetch);
		}


		public async Task<List<T>> QueryAsync<T>() where T : SimpleQueryEntity
		{
			return await QueryAsync<T>(null);
		}

		public async Task<List<T>> QueryAsync<T>(IEnumerable<object>? ids) where T : SimpleQueryEntity
		{
			return await QueryAsync<T>(ids, ReferenceFetchMode.None);
		}


		public async Task<List<T>> QueryAsync<T>(IEnumerable<object>? ids, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryAsync<T>(ids, referenceFetchMode, (string?)null);
		}

		public async Task<List<T>> QueryAsync<T>(IEnumerable<object>? ids, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryAsync<T>(ids, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<List<T>> QueryAsync<T>(IEnumerable<object>? ids, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			var whereClause = ids is null ? string.Empty : $"WHERE {typeof(T).Name}Id in @ids";
			var sql = $"SELECT * FROM {typeof(T).Name} {whereClause}";
			var parameters = ids is null ? null : new { ids };

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var ret = (await cn.QueryAsync<T>(sql, parameters)).ToList();
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;

			foreach (var item in ret)
			{
				item._dbContext = this;
				item._fetchReferencesType = referenceFetchMode;
				item._entitiesToFetch = entityTypesToFetch;
			}

			return ret;
		}





		public async Task<T> QueryFirstByColumnValueAsync<T>(string columnName, object columnValue) where T : SimpleQueryEntity
		{
			return (await QueryFirstByColumnValueAsync<T>(columnName, columnValue, ReferenceFetchMode.None));
		}


		public async Task<T> QueryFirstByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryFirstByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, (string?)null);
		}

		public async Task<T> QueryFirstByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryFirstByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, entityTypesToFetch)).First();
		}








		public async Task<T?> QueryFirstOrDefaultByColumnValueAsync<T>(string columnName, object columnValue) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByColumnValueAsync<T>(columnName, columnValue, ReferenceFetchMode.None);
		}


		public async Task<T?> QueryFirstOrDefaultByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, (string?)null);
		}

		public async Task<T?> QueryFirstOrDefaultByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T?> QueryFirstOrDefaultByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, entityTypesToFetch)).FirstOrDefault();
		}





		public async Task<List<T>> QueryByColumnValueAsync<T>(string columnName, object columnValue) where T : SimpleQueryEntity
		{
			return await QueryByColumnValueAsync<T>(columnName, columnValue, ReferenceFetchMode.None);
		}

		public async Task<List<T>> QueryByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, (string?)null);
		}

		public async Task<List<T>> QueryByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryByColumnValueAsync<T>(columnName, columnValue, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<List<T>> QueryByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			var sql = $"SELECT * FROM {typeof(T).Name} WHERE {columnName} = @columnValue";

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var results = (await cn.QueryAsync<T>(sql, new { columnValue })).ToList();
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;

			foreach (var item in results)
			{
				item._dbContext = this;
				item._fetchReferencesType = referenceFetchMode;
				item._entitiesToFetch = entityTypesToFetch;
			}

			return results;
		}

		public async Task<T> QueryFirstByColumnValuesAsync<T>(Dictionary<string, object> columnValues) where T : SimpleQueryEntity
		{
			return await QueryFirstByColumnValuesAsync<T>(columnValues, ReferenceFetchMode.None);
		}

		public async Task<T> QueryFirstByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryFirstByColumnValuesAsync<T>(columnValues, referenceFetchMode, (string?)null);
		}

		public async Task<T> QueryFirstByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryFirstByColumnValuesAsync<T>(columnValues, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValuesAsync<T>(columnValues, referenceFetchMode, entityTypesToFetch)).First();
		}

		public async Task<T?> QueryFirstOrDefaultByColumnValuesAsync<T>(Dictionary<string, object> columnValues) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByColumnValuesAsync<T>(columnValues, ReferenceFetchMode.None);
		}
		public async Task<T?> QueryFirstOrDefaultByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByColumnValuesAsync<T>(columnValues, referenceFetchMode, (string?)null);
		}
		public async Task<T?> QueryFirstOrDefaultByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByColumnValuesAsync<T>(columnValues, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T?> QueryFirstOrDefaultByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValuesAsync<T>(columnValues, referenceFetchMode, entityTypesToFetch)).FirstOrDefault();
		}

		public async Task<List<T>> QueryByColumnValuesAsync<T>(Dictionary<string, object> columnValues) where T : SimpleQueryEntity
		{
			return await QueryByColumnValuesAsync<T>(columnValues, ReferenceFetchMode.None);
		}

		public async Task<List<T>> QueryByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryByColumnValuesAsync<T>(columnValues, referenceFetchMode, (string?)null);
		}

		public async Task<List<T>> QueryByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryByColumnValuesAsync<T>(columnValues, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<List<T>> QueryByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			var sql = $"SELECT * FROM {typeof(T).Name} WHERE ";
			sql += string.Join(" AND ", columnValues.Select(kvp => $"{kvp.Key} = @{kvp.Key}"));

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var results = (await cn.QueryAsync<T>(sql, columnValues)).ToList();
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;

			foreach (var item in results)
			{
				item._dbContext = this;
				item._fetchReferencesType = referenceFetchMode;
				item._entitiesToFetch = entityTypesToFetch;
			}

			return results;
		}

		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause) where T : SimpleQueryEntity
		{
			return await QueryFirstByWhereClauseAsync<T>(whereClause, null);
		}

		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause, object? parameters) where T : SimpleQueryEntity
		{
			return await QueryFirstByWhereClauseAsync<T>(whereClause, parameters, ReferenceFetchMode.None);
		}

		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryFirstByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, (string?)null);
		}

		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryFirstByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			return (await QueryByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, entityTypesToFetch)).First();
		}

		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByWhereClauseAsync<T>(whereClause, null);
		}

		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause, object? parameters) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByWhereClauseAsync<T>(whereClause, parameters, ReferenceFetchMode.None);
		}

		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, (string?)null);
		}

		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			return (await QueryByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, entityTypesToFetch)).FirstOrDefault();
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause) where T : SimpleQueryEntity
		{
			return await QueryByWhereClauseAsync<T>(whereClause, null);
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, object? parameters) where T : SimpleQueryEntity
		{
			return await QueryByWhereClauseAsync<T>(whereClause, parameters, ReferenceFetchMode.None);
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryByWhereClauseAsync<T>(whereClause, null, referenceFetchMode, (string?)null);
		}
		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, (string?)null);
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			return await QueryByWhereClauseAsync<T>(whereClause, null, referenceFetchMode, entityTypesToFetch);
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryByWhereClauseAsync<T>(whereClause, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch = null) where T : SimpleQueryEntity
		{
			var sql = $"SELECT * FROM {typeof(T).Name} WHERE {whereClause}";

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var results = (await cn.QueryAsync<T>(sql, parameters)).ToList();
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;

			foreach (var item in results)
			{
				item._dbContext = this;
				item._fetchReferencesType = referenceFetchMode;
				item._entitiesToFetch = entityTypesToFetch;
			}

			return results;
		}

		public async Task<T> QueryFirstByQueryAsync<T>(string query) where T : SimpleQueryEntity
		{
			return await QueryFirstByQueryAsync<T>(query, null);
		}
		public async Task<T> QueryFirstByQueryAsync<T>(string query, object? parameters) where T : SimpleQueryEntity
		{
			return await QueryFirstByQueryAsync<T>(query, parameters, ReferenceFetchMode.None);
		}
		public async Task<T> QueryFirstByQueryAsync<T>(string query, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryFirstByQueryAsync<T>(query, null, referenceFetchMode, (string?)null);
		}
		public async Task<T> QueryFirstByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryFirstByQueryAsync<T>(query, parameters, referenceFetchMode, (string?)null);
		}
		public async Task<T> QueryFirstByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryFirstByQueryAsync<T>(query, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T> QueryFirstByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			return (await QueryBySqlAsync<T>(query, parameters, referenceFetchMode, entityTypesToFetch)).First();
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, null);
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, object? parameters) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, parameters, ReferenceFetchMode.None);
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, null, referenceFetchMode, (string?)null);
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, parameters, referenceFetchMode, (string?)null);
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, null, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, string? entityTypeToFetch) where T : SimpleQueryEntity
		{
			return await QueryFirstOrDefaultByQueryAsync<T>(query, parameters, referenceFetchMode, entityTypeToFetch is null ? null : new List<string> { entityTypeToFetch });
		}

		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
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
			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var results = (await cn.QueryAsync<T>(query, parameters)).ToList();
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;

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
			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var result = (await cn.QueryAsync<T>(query, parameters)).First();
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;

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
			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var result = (await cn.QueryAsync<T>(query, parameters)).FirstOrDefault();
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;

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
			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			await cn.ExecuteAsync(query, parameters);
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;
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

		internal async Task<T?> QueryEntityFirstOrDefaultAsync<T>(object id) where T : SimpleQueryEntity
		{
			var idList = new List<object> { id };
			return (await QueryAsync<T>(idList, ReferenceFetchMode.None, (string?)null)).FirstOrDefault();
		}

		internal async Task<T?> QueryEntityFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode) where T : SimpleQueryEntity
		{
			var idList = new List<object> { id };
			return (await QueryAsync<T>(idList, referenceFetchMode, (string?)null)).FirstOrDefault();
		}

		internal async Task<T?> QueryEntityFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode, string entityTypeToFetch) where T : SimpleQueryEntity
		{
			var idList = new List<object> { id };
			return (await QueryAsync<T>(idList, referenceFetchMode, entityTypeToFetch)).FirstOrDefault();
		}

		internal async Task<T?> QueryEntityFirstOrDefaultAsync<T>(object id, ReferenceFetchMode referenceFetchMode, ICollection<string>? entityTypesToFetch) where T : SimpleQueryEntity
		{
			var idList = new List<object> { id };
			return (await QueryAsync<T>(idList, referenceFetchMode, entityTypesToFetch)).FirstOrDefault();
		}
	}
}
