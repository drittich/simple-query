namespace drittich.SimpleQuery
{
	/// <summary>
	/// Represents a property type in a query.
	/// </summary>
	public class PropertyType
	{
		/// <summary>
		/// Gets or sets the name of the property.
		/// </summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the type name of the property.
		/// </summary>
		public string TypeName { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets a value indicating whether the property is nullable.
		/// </summary>
		public bool IsNullable { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the property is a primary key.
		/// </summary>
		public bool IsPrimaryKey { get; set; }
	}
}
