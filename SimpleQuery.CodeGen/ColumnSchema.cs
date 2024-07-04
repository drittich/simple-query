namespace drittich.SimpleQuery
{
	/// <summary>
	/// Represents the schema of a column in a database table.
	/// </summary>
	public class ColumnSchema
	{
		/// <summary>
		/// Gets or sets the name of the column.
		/// </summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the type name of the column.
		/// </summary>
		public string TypeName { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether the column is nullable.
		/// </summary>
		public bool IsNullable { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the column is a primary key.
		/// </summary>
		public bool IsPrimaryKey { get; set; }
	}
}
