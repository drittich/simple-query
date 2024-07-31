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
		private readonly string _modelNamespace;
		private readonly bool _oneLineNamespaceDeclaration;

		/// <summary>
		/// Initializes a new instance of the <see cref="CodeGenerator"/> class.
		/// </summary>
		/// <param name="connectionString">The connection string to the database.</param>
		/// <param name="modelFolder">The folder where the generated models will be saved.</param>
		/// <param name="excludeTables">An array of table names to be excluded from the code generation.</param>
		public CodeGenerator(string connectionString, string modelFolder, string[] excludeTables, string modelNamespace, bool oneLineNamespaceDeclaration)
		{
			_connectionString = connectionString;
			_modelFolder = modelFolder;
			_excludeTables = excludeTables;
			_modelNamespace = modelNamespace;
			_oneLineNamespaceDeclaration = oneLineNamespaceDeclaration;
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
			var generatedTime = DateTime.Now.ToString("yyyy-MM-dd h:mm:ss tt");
			Directory.CreateDirectory(_modelFolder);

			using var connection = new SqliteConnection(_connectionString);
			await connection.OpenAsync();

			var tableNames = (await GetTableNamesAsync(connection)).OrderBy(t => t);

			foreach (var tableName in tableNames)
			{
				var tableType = await GetTableSchemaAsync(connection, tableName);
				var code = GenerateCode(tableType, generatedTime);
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
		private string GenerateCode(TableSchema tableType, string generatedTime)
		{
			var assembly = System.Reflection.Assembly.GetExecutingAssembly();
			var version = assembly.GetName().Version!;

			var code = new StringBuilder();
			code.AppendLine("/// <remarks>");
			code.AppendLine($"/// This code was generated with SimpleQuery v{version.Major}.{version.Minor}.{version.Build}, {generatedTime}. Do not edit.");
			code.AppendLine("/// For more information, visit https://github.com/drittich/simple-query.");
			code.AppendLine("/// </remarks>");
			code.AppendLine("using drittich.SimpleQuery;");
			code.AppendLine();
			if (_oneLineNamespaceDeclaration)
			{
				code.AppendLine($"namespace {_modelNamespace};");
				code.AppendLine();
			}
			else
			{
				code.AppendLine($"namespace {_modelNamespace} {{");
			}
			code.AppendLine($"public partial class {tableType.Name} : SimpleQueryEntity, IPrimaryKeyProvider");
			code.AppendLine("{");

			// implement the interface IPrimaryKeyProvider
			var primaryKeys = tableType.Properties.Where(p => p.IsPrimaryKey);
			code.AppendLine("\t/// <summary>");
			code.AppendLine($"\t/// Gets the primary key column names for the {tableType.Name} entity.");
			code.AppendLine("\t/// </summary>");
			code.AppendLine("\t/// <returns>An array containing the primary key column names.</returns>");
			code.AppendLine($"\tpublic string[] GetPrimaryKeyColumnNames() => new[] {{ \"{string.Join("\", \"", primaryKeys.Select(p => p.Name))}\" }};");

			foreach (var property in tableType.Properties)
			{
				var typeName = property.TypeName;
				if (property.IsNullable && !property.IsPrimaryKey)
				{
					typeName += "?";
				}

				var defaultValue = property.TypeName == "string" ? " = string.Empty;" : string.Empty;
				code.AppendLine();
				code.AppendLine("\t/// <summary>");
				code.AppendLine($"\t/// Gets or sets the {property.Name} property for the {tableType.Name} entity.");
				code.AppendLine("\t/// </summary>");
				code.AppendLine($"\tpublic {typeName} {property.Name} {{ get; set; }}{defaultValue}");
			}

			// add foreign key properties using foreign key constraints
			var foreignKeys = GetForeignKeys(tableType.Name, new SqliteConnection(_connectionString)).Result;
			if (foreignKeys.Any())
			{
				var fkCode = new StringBuilder();
				foreach (var key in foreignKeys)
				{
					var newPropertyName = key.from.EndsWith("Id", StringComparison.InvariantCultureIgnoreCase) && key.from.Length > 2
						? key.from.Substring(0, key.from.Length - 2)
						: $"{key.from}Object";

					fkCode.AppendLine($@"
    private {key.table}? _{newPropertyName};
	/// <summary>
	/// Gets or sets the associated {newPropertyName} object.
	/// </summary>
	/// <remarks>
	/// The {newPropertyName} property fetches the {newPropertyName} object based on the {key.from} if it has not already been retrieved.
	/// </remarks>
	public {key.table}? {newPropertyName} 
	{{ 
		get => _{newPropertyName} ??= _FetchById<{key.table}>({key.from});
		set => _{newPropertyName} = value;
	}}");
				}

				code.AppendLine();
				code.AppendLine($"\t// Foreign key references");
				code.Append(fkCode);
			}


			code.AppendLine("}");
			if (!_oneLineNamespaceDeclaration)
			{
				code.AppendLine("}");
			}

			return code.ToString();
		}

		private async Task<List<string>> GetTableNamesAsync(SqliteConnection connection)
		{
			var parameters = new DynamicParameters();
			var inClause = new List<string>();
			int index = 0;
			foreach (var table in _excludeTables)
			{
				var paramName = $"@excludeTable{index}";
				inClause.Add(paramName);
				parameters.Add(paramName, table);
				index++;
			}

			var sql = $@"
            SELECT name 
            FROM sqlite_master 
            WHERE type='table'
                AND name COLLATE BINARY NOT IN ({string.Join(", ", inClause)})";

			return (await connection.QueryAsync<string>(sql, parameters)).ToList();
		}

		private async Task<TableSchema> GetTableSchemaAsync(SqliteConnection connection, string tableName)
		{
			var tableType = new TableSchema
			{
				Name = tableName,
				Properties = new List<ColumnSchema>(),
			};

			var query = $"PRAGMA table_info({tableName})";
			var columnInfos = await connection.QueryAsync(query);

			foreach (var row in columnInfos)
			{
				var columnName = (string)row.name;
				var columnType = (string)row.type;
				var isNullable = ((long)row.notnull) == 0;
				var isPrimaryKey = ((long)row.pk) != 0;

				var propertyType = new ColumnSchema
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

			if (columnType.StartsWith("Text ", StringComparison.OrdinalIgnoreCase))
				columnType = "text";

			return columnType.ToLower() switch
			{
				"int" => "int",
				"integer" => "int",
				"real" => "double",
				"text" => "string",
				"blob" => "byte[]",
				"datetime" => "DateTime",
				_ => throw new NotSupportedException($"The column type '{columnType}' is not supported"),
			};
		}

		private async Task<List<ForeignKey>> GetForeignKeys(string tableName, SqliteConnection connection)
		{
			var sql = @$"PRAGMA foreign_key_list('{tableName}');";
			return (await connection.QueryAsync<ForeignKey>(sql, new { tableName })).ToList();
		}
	}
}
