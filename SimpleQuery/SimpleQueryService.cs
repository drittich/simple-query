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
		private readonly string _connectionString;
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
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type with the given ID.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity with the specified ID is found.</exception>
		public async Task<T> GetFirstAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			var ret = await GetEntityAsync<T>(id, fetchReferencesType, entitiesToFetch);
			if (ret is null)
			{
				throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			}
			return ret;
		}

		/// <summary>
		/// Retrieves the first entity of the specified type with the given ID, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="id">The ID of the entity to retrieve.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type with the given ID, or null if no such entity exists.</returns>
		public async Task<T?> GetFirstOrDefaultAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return await GetEntityAsync<T>(id, fetchReferencesType, entitiesToFetch);
		}

		/// <summary>
		/// Retrieves an entity of the specified type with the given ID.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="id">The ID of the entity to retrieve.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The entity of the specified type with the given ID, or null if no such entity exists.</returns>
		internal async Task<T?> GetEntityAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			var idList = new List<object> { id };
			return (await GetAllAsync<T>(idList, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Retrieves all entities of the specified type.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>A collection of all entities of the specified type.</returns>
		public async Task<IEnumerable<T>> GetAllAsync<T>(ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return await GetAllAsync<T>(null, fetchReferencesType, entitiesToFetch);
		}

		/// <summary>
		/// Retrieves all entities of the specified type with the given IDs.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="ids">A collection of IDs for the entities to retrieve.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>A collection of entities of the specified type with the given IDs.</returns>
		public async Task<IEnumerable<T>> GetAllAsync<T>(IEnumerable<object>? ids, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			var whereClause = ids is null ? string.Empty : $"WHERE {typeof(T).Name}Id in @ids";
			var sql = $"SELECT * FROM {typeof(T).Name} {whereClause}";
			var parameters = ids is null ? null : new { ids };

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = new SqliteConnection(_connectionString);
			await cn.OpenAsync();
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

		public async Task<T?> GetFirstByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await GetAllByColumnValueAsync<T>(columnName, columnValue, fetchReferencesType, entitiesToFetch)).First();
		}

		public async Task<T?> GetFirstOrDefaultByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await GetAllByColumnValueAsync<T>(columnName, columnValue, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
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
		public async Task<List<T>> GetAllByColumnValueAsync<T>(string columnName, object columnValue, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			var sql = $"SELECT * FROM {typeof(T).Name} WHERE {columnName} = @columnValue";

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = new SqliteConnection(_connectionString);
			await cn.OpenAsync();
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
		/// Retrieves the first entity of the specified type based on the provided WHERE clause.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="whereClause">The WHERE clause to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>The first entity of the specified type that matches the WHERE clause.</returns>
		/// <exception cref="InvalidOperationException">Thrown if no entity matches the WHERE clause.</exception>
		public async Task<T> GetFirstByWhereClauseAsync<T>(string whereClause, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await GetAllByWhereClauseAsync<T>(whereClause, null, fetchReferencesType, entitiesToFetch)).First();
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
		public async Task<T> GetFirstByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await GetAllByWhereClauseAsync<T>(whereClause, parameters, fetchReferencesType, entitiesToFetch)).First();
		}

		/// <summary>
		/// Retrieves the first entity of the specified type based on the provided WHERE clause, or null if no such entity exists.
		/// </summary>
		/// <typeparam name="T">The type of the entity to retrieve.</typeparam>
		/// <param name="whereClause">The WHERE clause to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities. Default is None.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch. Default is null.</param>
		/// <returns>The first entity of the specified type that matches the WHERE clause, or null if no such entity exists.</returns>
		public async Task<T?> GetFirstOrDefaultByWhereClauseAsync<T>(string whereClause, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await GetAllByWhereClauseAsync<T>(whereClause, null, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
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
		public async Task<T?> GetFirstOrDefaultByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return (await GetAllByWhereClauseAsync<T>(whereClause, parameters, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
		}

		/// <summary>
		/// Retrieves a list of entities of the specified type based on the provided WHERE clause.
		/// </summary>
		/// <typeparam name="T">The type of the entities to retrieve.</typeparam>
		/// <param name="whereClause">The WHERE clause to filter the entities.</param>
		/// <param name="fetchReferencesType">The fetch mode for related entities.</param>
		/// <param name="entitiesToFetch">A collection of entity names to fetch.</param>
		/// <returns>A list of entities of the specified type that match the WHERE clause.</returns>
		public async Task<List<T>> GetAllByWhereClauseAsync<T>(string whereClause, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return await GetAllByWhereClauseAsync<T>(whereClause, null, fetchReferencesType, entitiesToFetch);
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
		public async Task<List<T>> GetAllByWhereClauseAsync<T>(string whereClause, object? parameters, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			var sql = $"SELECT * FROM {typeof(T).Name} WHERE {whereClause}";

			var sw = System.Diagnostics.Stopwatch.StartNew();
			using var cn = new SqliteConnection(_connectionString);
			await cn.OpenAsync();
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
	}
}
