using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Dapper;

using Microsoft.Data.Sqlite;

namespace drittich.SimpleQuery.CodeGen
{
	public class CodeGenerator
	{
		private readonly string _connectionString;
		private readonly string _modelFolder;
		private readonly string[] _excludeTables;

		public CodeGenerator(string connectionString, string modelFolder, string[] excludeTables)
		{
			_connectionString = connectionString;
			_modelFolder = modelFolder;
			_excludeTables = excludeTables;
		}

		public async Task GenerateCodeAsync()
		{
			Directory.CreateDirectory(_modelFolder);

			using var connection = new SqliteConnection(_connectionString);
			await connection.OpenAsync();

			var tableNames = await GetTableNamesAsync(connection);

			foreach (var tableName in tableNames)
			{
				var tableType = await GetTableTypeAsync(connection, tableName);
				var code = GenerateCode(tableNames, tableType);
				var filePath = Path.Combine(_modelFolder, $"{tableName}.cs");
				await File.WriteAllTextAsync(filePath, code);
			}
		}

		private string GenerateCode(List<string> tableNames, TableType tableType)
		{
			var code = new StringBuilder();
			code.AppendLine("using drittich.SimpleQuery;");
			code.AppendLine();
			code.AppendLine($"public class {tableType.Name} : SimpleQueryEntity");
			code.AppendLine("{");

			foreach (var property in tableType.Properties)
			{
				var typeName = property.TypeName;
				if (property.IsNullable)
				{
					typeName += "?";
				}

				var defaultValue = property.TypeName == "string" ? " = string.Empty;" : string.Empty;
				code.AppendLine($"\tpublic {typeName} {property.Name} {{ get; set; }}{defaultValue}");
			}

			// add foreign key properties
			var fkCode = new StringBuilder();
			var hasForeignKey = false;
			foreach (var property in tableType.Properties.Where(p => !p.IsPrimaryKey))
			{
				// Assume that if it ends with Id, it's a foreign key
				if (property.Name.EndsWith("Id"))
				{
					var foreignKeyTableName = property.Name.Substring(0, property.Name.Length - 2);

					if (!tableNames.Contains(foreignKeyTableName)) continue;

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
				code.AppendLine($"\t// Foreign key references");
				code.Append(fkCode);
			}

			code.AppendLine("}");

			return code.ToString();
		}

		private async Task<List<string>> GetTableNamesAsync(SqliteConnection connection)
		{
			var sql = @"
				SELECT name 
				FROM sqlite_master 
				WHERE type='table'
					and name COLLATE BINARY not in @excludeTables";
			return (await connection.QueryAsync<string>(sql, new { excludeTables = _excludeTables })).ToList();
		}

		private async Task<TableType> GetTableTypeAsync(SqliteConnection connection, string tableName)
		{
			var tableType = new TableType
			{
				Name = tableName,
				Properties = new List<PropertyType>(),
			};

			var query = $"PRAGMA table_info({tableName})";
			var columnInfos = await connection.QueryAsync(query);

			foreach (var row in columnInfos)
			{
				var columnName = (string)row.name;
				var columnType = (string)row.type;
				var isNullable = ((long)row.notnull) == 0;
				var isPrimaryKey = ((long)row.pk) == 1;

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
				"int" => "int",
				"integer" => "int",
				"real" => "double",
				"text" => "string",
				"blob" => "byte[]",
				_ => throw new NotSupportedException($"The column type '{columnType}' is not supported"),
			};
		}
	}
}
