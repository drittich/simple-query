using System.Text;

using Dapper;

using Microsoft.Data.Sqlite;

namespace Linq3Sql
{
	public class DbContextService
	{
		private readonly string _connectionString;
		private readonly string _modelFolder;
		private readonly (string table, string column) _ignoreFks;
		public int QueryCount = 0;

		const string indent = "\t";

		public DbContextService(string connectionString, string modelFolder, (string table, string column) ignoreFks)
		{
			_connectionString = connectionString;
			_modelFolder = modelFolder;
			_ignoreFks = ignoreFks;

			// TODO: only trigger this if the model folder is empty or a change is detected in the database schema
			// Consider that we use DbUp to manage database schema changes, so we could trigger this on a successful migration
			GenerateTableTypes();
		}

		public async Task<T> GetFirstAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : DbContextEntity
		{
			var ret = await GetEntityAsync<T>(id, fetchReferencesType, entitiesToFetch);
			if (ret is null)
			{
				throw new InvalidOperationException($"No entity of type {typeof(T).Name} with id {id} was found.");
			}
			return ret;
		}

		public async Task<T?> GetFirstOrDefaultAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : DbContextEntity
		{
			return await GetEntityAsync<T>(id, fetchReferencesType, entitiesToFetch);
		}

		internal async Task<T?> GetEntityAsync<T>(object id, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : DbContextEntity
		{
			var idList = new List<object> { id };
			return (await GetAllAsync<T>(idList, fetchReferencesType, entitiesToFetch)).FirstOrDefault();
		}

		public async Task<IEnumerable<T>> GetAllAsync<T>(ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : DbContextEntity
		{
			return await GetAllAsync<T>(null, fetchReferencesType, entitiesToFetch);
		}

		public async Task<IEnumerable<T>> GetAllAsync<T>(IEnumerable<object>? ids, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : DbContextEntity
		{
			var whereClause = ids is null ? string.Empty : $"WHERE {typeof(T).Name}Id in @ids";
			var sql = $"SELECT * FROM {typeof(T).Name} {whereClause}";
			var parameters = ids is null ? null : new { ids };

			using var cn = new SqliteConnection(_connectionString);
			var ret = await cn.QueryAsync<T>(sql, parameters);
			QueryCount++;

			foreach (var item in ret)
			{
				item._dbContext = this;
				item._fetchReferencesType = fetchReferencesType;
				item._entitiesToFetch = entitiesToFetch;
			}

			return ret;
		}

		public async Task<IEnumerable<T>> QueryAsync<T>(string columnName, object columnValue, ReferenceFetchMode fetchReferencesType = ReferenceFetchMode.None, ICollection<string>? entitiesToFetch = null) where T : DbContextEntity
		{
			var sql = $"SELECT * FROM {typeof(T).Name} WHERE {columnName} = @columnValue";

			using var cn = new SqliteConnection(_connectionString);
			var results = await cn.QueryAsync<T>(sql, new { columnValue });
			QueryCount++;

			foreach (var item in results)
			{
				item._dbContext = this;
				item._fetchReferencesType = fetchReferencesType;
				item._entitiesToFetch = entitiesToFetch;
			}

			return results;
		}

		private void GenerateTableTypes()
		{
			Directory.CreateDirectory(_modelFolder);

			using var connection = new SqliteConnection(_connectionString);
			connection.Open();

			var tableNames = GetTableNames(connection);

			foreach (var tableName in tableNames)
			{
				var tableType = GetTableType(connection, tableName);
				var code = GenerateCode(tableType, _ignoreFks);
				var filePath = Path.Combine(_modelFolder, $"{tableName}.cs");
				File.WriteAllText(filePath, code);
			}
		}

		private string GenerateCode(TableType tableType, (string table, string column) ignoreFks)
		{
			var code = new StringBuilder();
			code.AppendLine("using Linq3Sql;");
			code.AppendLine();
			code.AppendLine($"public class {tableType.Name} : DbContextEntity");
			code.AppendLine("{");

			foreach (var property in tableType.Properties)
			{
				var typeName = property.TypeName;
				if (property.IsNullable)
				{
					typeName += "?";
				}

				var defaultValue = property.TypeName == "string" ? " = string.Empty;" : string.Empty;
				code.AppendLine($"{indent}public {typeName} {property.Name} {{ get; set; }}{defaultValue}");
			}

			// add foreign key properties
			var fkCode = new StringBuilder();
			var hasForeignKey = false;
			foreach (var property in tableType.Properties.Where(p => !p.IsPrimaryKey))
			{
				// Assume that if it ends with Id, it's a foreign key
				if (property.Name.EndsWith("Id"))
				{
					if (property.Name == ignoreFks.column && tableType.Name == ignoreFks.table) continue;

					var foreignKeyTableName = property.Name.Substring(0, property.Name.Length - 2);
					fkCode.AppendLine($@"
	private {foreignKeyTableName}? _{foreignKeyTableName} = null;
	public {foreignKeyTableName}? {foreignKeyTableName} {{ 
		get {{
			if (GetFetchReferences(""{foreignKeyTableName}"") && _{foreignKeyTableName} is null{(property.IsNullable ? $" && {property.Name} is not null" : string.Empty)}) {{
				_{foreignKeyTableName} = _dbContext!.GetFirst{(property.IsNullable ? "OrDefault" : string.Empty)}Async<{foreignKeyTableName}>({property.Name}{(property.IsNullable ? ".Value" : string.Empty)}, GetChildrenReferenceFetchMode()).Result;
			}}
			return _{foreignKeyTableName};
		}}  
	}}");
					hasForeignKey = true;
				}
			}

			if (hasForeignKey)
			{
				code.AppendLine();
				code.AppendLine($"{indent}// Foreign key references");
				code.Append(fkCode);
			}

			code.AppendLine("}");

			return code.ToString();
		}

		private List<string> GetTableNames(SqliteConnection connection)
		{
			var tableNames = new List<string>();

			var command = connection.CreateCommand();
			command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
			using var reader = command.ExecuteReader();

			while (reader.Read())
			{
				var tableName = reader.GetString(0);

				if (tableName == "sqlite_sequence") continue;

				tableNames.Add(tableName);
			}

			return tableNames;
		}

		private TableType GetTableType(SqliteConnection connection, string tableName)
		{
			var tableType = new TableType
			{
				Name = tableName,
				Properties = new List<PropertyType>(),
			};

			var command = connection.CreateCommand();
			command.CommandText = $"PRAGMA table_info({tableName})";
			using var reader = command.ExecuteReader();

			while (reader.Read())
			{
				var columnName = reader.GetString(1);
				var columnType = reader.GetString(2);
				var isNullable = !reader.GetBoolean(3);
				var isPrimaryKey = reader.GetBoolean(5);

				var propertyType = new PropertyType
				{
					Name = columnName,
					TypeName = GetColumnType(columnName, columnType),
					IsNullable = isNullable,
					IsPrimaryKey = isPrimaryKey,
				};

				tableType.Properties.Add(propertyType);
			}

			return tableType;
		}

		private string GetColumnType(string columnName, string columnType)
		{
			if (columnName.EndsWith("Date") || columnName == "Created" || columnName == "Modified")
				return "DateTime";

			return columnType.ToLower() switch
			{
				"int" or "integer" => "int",
				"real" => "double",
				"text" => "string",
				"blob" => "byte[]",
				_ => throw new NotSupportedException($"The column type '{columnType}' is not supported"),
			};
		}
	}

	record TableType
	{
		public required string Name { get; init; }
		public required List<PropertyType> Properties { get; init; }
	}

	record PropertyType
	{
		public required string Name { get; init; }
		public required string TypeName { get; init; }
		public required bool IsNullable { get; init; }
		public required bool IsPrimaryKey { get; init; }
	}
}
