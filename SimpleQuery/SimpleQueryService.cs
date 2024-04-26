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

		/// <summary>
		/// Retrieves the first entity of the specified type with the given ID.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="id">The ID of the entity to retrieve.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>The first entity of the specified type with the given ID.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity with the specified ID is found.</exception>
		public async Task<T> QueryFirstAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
		{
			var ret = await QueryEntityFirstOrDefaultAsync<T>(id, fetchReferencesType, entityToFetch)
				?? throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			return ret;
		}

		/// <summary>
		/// Retrieves the first entity of the specified type with the given ID.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="id">The ID of the entity to retrieve.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type with the given ID.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity with the specified ID is found.</exception>
		public async Task<T> QueryFirstAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			var ret = await QueryEntityFirstOrDefaultAsync<T>(id, fetchReferencesType, entitiesToFetch)
				?? throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			return ret;
		}

		/// <summary>
		/// Retrieves the first entity of the specified type with the given ID, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="id">The ID of the entity to retrieve.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>The first entity of the specified type with the given ID, or null if no such entity exists.</returns>
		public async Task<T?> QueryFirstOrDefaultAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
		{
			return await QueryEntityFirstOrDefaultAsync<T>(id, fetchReferencesType, entityToFetch);
		}

		/// <summary>
		/// Retrieves the first entity of the specified type with the given ID, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="id">The ID of the entity to retrieve.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type with the given ID, or null if no such entity exists.</returns>
		public async Task<T?> QueryFirstOrDefaultAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return await QueryEntityFirstOrDefaultAsync<T>(id, fetchReferencesType, entitiesToFetch);
		}

		/// <summary>
		/// Retrieves an entity of the specified type with the given ID.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="id">The ID of the entity to retrieve.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>The entity of the specified type with the given ID, or null if no such entity exists.</returns>
		internal async Task<T?> QueryEntityFirstOrDefaultAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
		{
			var idList = new List<object> { id };
			return (await QueryAsync<T>(idList, fetchReferencesType, entityToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Retrieves an entity of the specified type with the given ID.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="id">The ID of the entity to retrieve.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The entity of the specified type with the given ID, or null if no such entity exists.</returns>
		internal async Task<T?> QueryEntityFirstOrDefaultAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			var idList = new List<object> { id };
			return (await QueryAsync<T>(idList, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Retrieves all entities of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>A collection of all entities of the specified type.</returns>
		public async Task<List<T>> QueryAsync<T>(ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
		{
			return await QueryAsync<T>(null, fetchReferencesType, entityToFetch);
		}

		/// <summary>
		/// Retrieves all entities of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>A collection of all entities of the specified type.</returns>
		public async Task<List<T>> QueryAsync<T>(ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return await QueryAsync<T>(null, fetchReferencesType, entitiesToFetch);
		}

		/// <summary>
		/// Retrieves all entities of the specified type with the given IDs.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="ids">A collection of IDs for the entities to retrieve.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>A collection of entities of the specified type with the given IDs.</returns>
		public async Task<List<T>> QueryAsync<T>(IEnumerable<object>? ids, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
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
				item._fetchReferencesType = fetchReferencesType;
				item._entitiesToFetch = entityToFetch is null ? null : new List<string> { entityToFetch };
			}

			return ret;
		}

		/// <summary>
		/// Retrieves all entities of the specified type with the given IDs.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="ids">A collection of IDs for the entities to retrieve.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>A collection of entities of the specified type with the given IDs.</returns>
		public async Task<List<T>> QueryAsync<T>(IEnumerable<object>? ids, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
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
				item._fetchReferencesType = fetchReferencesType;
				item._entitiesToFetch = entitiesToFetch;
			}

			return ret;
		}

		/// <summary>
		/// Retrieves the first entity of the specified type where the specified column matches the given value.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="columnName">The name of the column to filter by.</param>
		/// <param name="columnValue">The value to match in the specified column.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>The first entity of the specified type where the specified column matches the given value.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity matches the column value.</exception>
		public async Task<T> QueryFirstByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValueAsync<T>(columnName, columnValue, fetchReferencesType, entityToFetch)).First();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type where the specified column matches the given value.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="columnName">The name of the column to filter by.</param>
		/// <param name="columnValue">The value to match in the specified column.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type where the specified column matches the given value.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity matches the column value.</exception>
		public async Task<T> QueryFirstByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValueAsync<T>(columnName, columnValue, fetchReferencesType, entitiesToFetch)).First();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type where the specified column matches the given value, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="columnName">The name of the column to filter by.</param>
		/// <param name="columnValue">The value to match in the specified column.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>The first entity of the specified type where the specified column matches the given value, or null if no such entity exists.</returns>
		public async Task<T?> QueryFirstOrDefaultByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValueAsync<T>(columnName, columnValue, fetchReferencesType, entityToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type where the specified column matches the given value, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="columnName">The name of the column to filter by.</param>
		/// <param name="columnValue">The value to match in the specified column.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type where the specified column matches the given value, or null if no such entity exists.</returns>
		public async Task<T?> QueryFirstOrDefaultByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValueAsync<T>(columnName, columnValue, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Queries the database for entities of the specified type where the specified column matches the given value.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="columnName">The name of the column to filter by.</param>
		/// <param name="columnValue">The value to match in the specified column.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>A list of entities of the specified type where the specified column matches the given value.</returns>
		public async Task<List<T>> QueryByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
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
				item._fetchReferencesType = fetchReferencesType;
				item._entitiesToFetch = entityToFetch is null ? null : new List<string> { entityToFetch };
			}

			return results;
		}

		/// <summary>
		/// Queries the database for entities of the specified type where the specified column matches the given value.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="columnName">The name of the column to filter by.</param>
		/// <param name="columnValue">The value to match in the specified column.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>A list of entities of the specified type where the specified column matches the given value.</returns>
		public async Task<List<T>> QueryByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
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
				item._fetchReferencesType = fetchReferencesType;
				item._entitiesToFetch = entitiesToFetch;
			}

			return results;
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided column values.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="columnValues">The column values to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>The first entity of the specified type that matches the column values.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity matches the column values.</exception>
		public async Task<T> QueryFirstByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValuesAsync<T>(columnValues, fetchReferencesType, entityToFetch)).First();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided column values.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="columnValues">The column values to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type that matches the column values.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity matches the column values.</exception>
		public async Task<T> QueryFirstByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValuesAsync<T>(columnValues, fetchReferencesType, entitiesToFetch)).First();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided column values, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="columnValues">The column values to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>The first entity of the specified type that matches the column values, or null if no such entity exists.</returns>
		public async Task<T?> QueryFirstOrDefaultByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValuesAsync<T>(columnValues, fetchReferencesType, entityToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided column values, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="columnValues">The column values to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type that matches the column values, or null if no such entity exists.</returns>
		public async Task<T?> QueryFirstOrDefaultByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByColumnValuesAsync<T>(columnValues, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Retrieves a list of entities of the specified type based on the provided column values.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="columnValues">The column values to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>A list of entities of the specified type that match the column values.</returns>
		public async Task<List<T>> QueryByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
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
				item._fetchReferencesType = fetchReferencesType;
				item._entitiesToFetch = entityToFetch is null ? null : new List<string> { entityToFetch };
			}

			return results;
		}

		/// <summary>
		/// Retrieves a list of entities of the specified type based on the provided column values.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="columnValues">The column values to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>A list of entities of the specified type that match the column values.</returns>
		public async Task<List<T>> QueryByColumnValuesAsync<T>(Dictionary<string, object> columnValues, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
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
				item._fetchReferencesType = fetchReferencesType;
				item._entitiesToFetch = entitiesToFetch;
			}

			return results;
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided WHERE clause.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="whereClause">The WHERE clause to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type that matches the WHERE clause.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity matches the WHERE clause.</exception>
		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByWhereClauseAsync<T>(whereClause, null, fetchReferencesType, entitiesToFetch)).First();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided WHERE clause and parameters.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="whereClause">The WHERE clause to filter the entities.</param>
		/// <param name="parameters">The parameters to use in the WHERE clause.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type that matches the WHERE clause and parameters.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity matches the WHERE clause and parameters.</exception>
		public async Task<T> QueryFirstByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByWhereClauseAsync<T>(whereClause, parameters, fetchReferencesType, entitiesToFetch)).First();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided WHERE clause, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="whereClause">The WHERE clause to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities. Default is None.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch. Default is null.</param>
		/// <returns>The first entity of the specified type that matches the WHERE clause, or null if no such entity exists.</returns>
		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByWhereClauseAsync<T>(whereClause, null, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided WHERE clause and parameters, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="whereClause">The WHERE clause to filter the entities.</param>
		/// <param name="parameters">The parameters to use in the WHERE clause.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities. Default is None.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch. Default is null.</param>
		/// <returns>The first entity of the specified type that matches the WHERE clause and parameters, or null if no such entity exists.</returns>
		public async Task<T?> QueryFirstOrDefaultByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryByWhereClauseAsync<T>(whereClause, parameters, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Retrieves a list of entities of the specified type based on the provided WHERE clause.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="whereClause">The WHERE clause to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>A list of entities of the specified type that match the WHERE clause.</returns>
		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
		{
			return await QueryByWhereClauseAsync<T>(whereClause, null, fetchReferencesType, entityToFetch);
		}

		/// <summary>
		/// Retrieves a list of entities of the specified type based on the provided WHERE clause.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="whereClause">The WHERE clause to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>A list of entities of the specified type that match the WHERE clause.</returns>
		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return await QueryByWhereClauseAsync<T>(whereClause, null, fetchReferencesType, entitiesToFetch);
		}

		/// <summary>
		/// Retrieves a list of entities of the specified type based on the provided WHERE clause and parameters.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="whereClause">The WHERE clause to filter the entities.</param>
		/// <param name="parameters">The parameters to use in the WHERE clause.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>A list of entities of the specified type that match the WHERE clause and parameters.</returns>
		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
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
				item._fetchReferencesType = fetchReferencesType;
				item._entitiesToFetch = entityToFetch is null ? null : new List<string> { entityToFetch };
			}

			return results;
		}

		/// <summary>
		/// Retrieves a list of entities of the specified type based on the provided WHERE clause and parameters.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="whereClause">The WHERE clause to filter the entities.</param>
		/// <param name="parameters">The parameters to use in the WHERE clause.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>A list of entities of the specified type that match the WHERE clause and parameters.</returns>
		public async Task<List<T>> QueryByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
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
				item._fetchReferencesType = fetchReferencesType;
				item._entitiesToFetch = entitiesToFetch;
			}

			return results;
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided query and parameters.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="parameters">The parameters to use in the query.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>The first entity of the specified type that matches the query and parameters.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity matches the query and parameters.</exception>
		public async Task<T> QueryFirstByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryBySqlAsync<T>(query, parameters, fetchReferencesType, entityToFetch)).First();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided query and parameters.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="parameters">The parameters to use in the query.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type that matches the query and parameters.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity matches the query and parameters.</exception>
		public async Task<T> QueryFirstByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryBySqlAsync<T>(query, parameters, fetchReferencesType, entitiesToFetch)).First();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided query and parameters, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="parameters">The parameters to use in the query.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>The first entity of the specified type that matches the query and parameters, or null if no such entity exists.</returns>
		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryBySqlAsync<T>(query, parameters, fetchReferencesType, entityToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided query and parameters, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="parameters">The parameters to use in the query.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type that matches the query and parameters, or null if no such entity exists.</returns>
		public async Task<T?> QueryFirstOrDefaultByQueryAsync<T>(string query, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await QueryBySqlAsync<T>(query, parameters, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Retrieves a list of entities of the specified type based on the provided query and parameters.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="parameters">The parameters to use in the query.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entityToFetch">The entity name to fetch.</param>
		/// <returns>A list of entities of the specified type that match the query and parameters.</returns>
		public async Task<List<T>> QueryBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null)
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
					sqe._fetchReferencesType = fetchReferencesType;
					sqe._entitiesToFetch = entityToFetch is null ? null : new List<string> { entityToFetch };
				}
			}

			return results;
		}

		/// <summary>
		/// Retrieves a list of entities of the specified type based on the provided query and parameters.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="parameters">The parameters to use in the query.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>A list of entities of the specified type that match the query and parameters.</returns>
		public async Task<List<T>> QueryBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null)
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
					sqe._fetchReferencesType = fetchReferencesType;
					sqe._entitiesToFetch = entitiesToFetch;
				}
			}

			return results;
		}

		/// <summary>
		/// Executes the first query by SQL asynchronously.
		/// </summary>
		/// <typeparam name="T">The type of the SimpleQueryEntity.</typeparam>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="parameters">The parameters for the SQL query.</param>
		/// <param name="fetchReferencesType">The type of reference fetch mode.</param>
		/// <param name="entityToFetch">The entity to fetch.</param>
		/// <returns>The first result of the query.</returns>
		public async Task<T> QueryFirstBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null)
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var result = (await cn.QueryAsync<T>(query, parameters)).First();
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;

			if (result is SimpleQueryEntity sqe)
			{
				sqe._dbContext = this;
				sqe._fetchReferencesType = fetchReferencesType;
				sqe._entitiesToFetch = entityToFetch is null ? null : new List<string> { entityToFetch };
			}

			return result;
		}

		/// <summary>
		/// Executes the first query by SQL asynchronously.
		/// </summary>
		/// <typeparam name="T">The type of the SimpleQueryEntity.</typeparam>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="parameters">The parameters for the SQL query.</param>
		/// <param name="fetchReferencesType">The type of reference fetch mode.</param>
		/// <param name="entitiesToFetch">The collection of entities to fetch.</param>
		/// <returns>The first result of the query.</returns>
		public async Task<T> QueryFirstBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null)
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var result = (await cn.QueryAsync<T>(query, parameters)).First();
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;

			if (result is SimpleQueryEntity sqe)
			{
				sqe._dbContext = this;
				sqe._fetchReferencesType = fetchReferencesType;
				sqe._entitiesToFetch = entitiesToFetch;
			}

			return result;
		}

		/// <summary>
		/// Executes the first or default query by SQL asynchronously.
		/// </summary>
		/// <typeparam name="T">The type of the SimpleQueryEntity.</typeparam>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="parameters">The parameters for the SQL query.</param>
		/// <param name="fetchReferencesType">The type of reference fetch mode.</param>
		/// <param name="entityToFetch">The entity to fetch.</param>
		/// <returns>The first result of the query or default if no results are found.</returns>
		public async Task<T> QueryFirstOrDefaultBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, string? entityToFetch = null)
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var result = (await cn.QueryAsync<T>(query, parameters)).FirstOrDefault();
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;

			if (result != null && result is SimpleQueryEntity sqe)
			{
				sqe._dbContext = this;
				sqe._fetchReferencesType = fetchReferencesType;
				sqe._entitiesToFetch = entityToFetch is null ? null : new List<string> { entityToFetch };
			}

			// Assuming default(T) is acceptable for your use case when there's no result.
			return result;
		}

		/// <summary>
		/// Executes the first or default query by SQL asynchronously.
		/// </summary>
		/// <typeparam name="T">The type of the SimpleQueryEntity.</typeparam>
		/// <param name="query">The SQL query to execute.</param>
		/// <param name="parameters">The parameters for the SQL query.</param>
		/// <param name="fetchReferencesType">The type of reference fetch mode.</param>
		/// <param name="entitiesToFetch">The collection of entities to fetch.</param>
		/// <returns>The first result of the query or default if no results are found.</returns>
		public async Task<T> QueryFirstOrDefaultBySqlAsync<T>(string query, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null)
		{
			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = await GetConnectionAsync();
			var result = (await cn.QueryAsync<T>(query, parameters)).FirstOrDefault();
			QueryTotalMs += (int)sw.ElapsedMilliseconds;
			QueryCount++;

			if (result != null && result is SimpleQueryEntity sqe)
			{
				sqe._dbContext = this;
				sqe._fetchReferencesType = fetchReferencesType;
				sqe._entitiesToFetch = entitiesToFetch;
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
	}
}
