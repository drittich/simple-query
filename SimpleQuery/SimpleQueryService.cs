using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Dapper;

using Microsoft.Data.Sqlite;

namespace drittich.SimpleQuery
{
	public class SimpleQueryService
	{
		private readonly string _connectionString;
		public int QueryCount = 0;
		public int QueryTotalMs = 0;

		public SimpleQueryService(string connectionString, string modelFolder, (string table, string column) ignoreFks)
		{
			_connectionString = connectionString;
		}

		public async Task<T> GetFirstAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			var ret = await GetEntityAsync<T>(id, fetchReferencesType, entitiesToFetch);
			if (ret is null)
			{
				throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			}
			return ret;
		}

		public async Task<T?> GetFirstOrDefaultAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return await GetEntityAsync<T>(id, fetchReferencesType, entitiesToFetch);
		}

		internal async Task<T?> GetEntityAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			var idList = new List<object> { id };
			return (await GetAllAsync<T>(idList, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
		}

		public async Task<IEnumerable<T>> GetAllAsync<T>(ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
		{
			return await GetAllAsync<T>(null, fetchReferencesType, entitiesToFetch);
		}

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

		public async Task<List<T>> QueryAsync<T>(string columnName, object columnValue, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : SimpleQueryEntity
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
	}
}
