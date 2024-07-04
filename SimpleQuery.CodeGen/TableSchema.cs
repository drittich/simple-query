using System.Collections.Generic;

namespace drittich.SimpleQuery
{
	/// <summary>
	/// Represents the schema of a table in the database.
	/// </summary>
	public class TableSchema
	{
		/// <summary>
		/// Gets or sets the name of the table.
		/// </summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the column schema of the table.
		/// </summary>
		public List<ColumnSchema> Properties { get; set; } = new List<ColumnSchema>();
	}
}
