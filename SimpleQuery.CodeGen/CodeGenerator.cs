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

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeGenerator"/> class.
		/// </summary>
		/// <param name="connectionString">The connection string to the database.</param>
		/// <param name="modelFolder">The folder where the generated models will be saved.</param>
		/// <param name="excludeTables">An array of table names to be excluded from the code generation.</param>
		public CodeGenerator(string connectionString, string modelFolder, string[] excludeTables)
		{
			_connectionString = connectionString;
			_modelFolder = modelFolder;
			_excludeTables = excludeTables;
		}

		/// <summary>
		/// Generates code for each table in the database, excluding the tables specified in the excludeTables parameter of the constructor.
		/// </summary>
		/// <remarks>
		/// This method performs the following steps for each table:
		/// 1. Retrieves the table type information from the database.
		/// 2. Generates the code for the table.
		/// 3. Writes the generated code to a file in the model folder.
		/// </remarks>
		/// <returns>A Task representing the asynchronous operation.</returns>
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

		/// <summary>
		/// Generates the C# code for a given table type.
		/// </summary>
		/// <param name="tableNames">A list of all table names in the database.</param>
		/// <param name="tableType">The type information of the table for which the code is to be generated.</param>
		/// <returns>A string containing the generated C# code.</returns>
		private string GenerateCode(List<string> tableNames, TableType tableType)
		{
			var code = new StringBuilder();
			code.AppendLine("using drittich.SimpleQuery;");
			code.AppendLine();
			code.AppendLine("namespace SimpleQuery {");
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

			var fkProperties = tableType.Properties
				.Where(p => !p.IsPrimaryKey && p.Name.EndsWith("Id") && p.Name.Length > 2);

			// add foreign key properties
			if (fkProperties.Any())
			{
				var fkCode = new StringBuilder();
				foreach (var property in fkProperties)
				{
					var fkTableName = property.Name.Substring(0, property.Name.Length - 2);

					if (!tableNames.Contains(fkTableName)) continue;

					fkCode.AppendLine($@"
    private {fkTableName}? _{fkTableName};
    public {fkTableName}? {fkTableName} => _{fkTableName} ??= _FetchById<{fkTableName}>({property.Name});");
				}

				code.AppendLine();
				code.AppendLine($"\t// Foreign key references");
				code.Append(fkCode);
			}

			code.AppendLine("}");
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
