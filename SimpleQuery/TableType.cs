using System.Collections.Generic;

namespace drittich.SimpleQuery
{
	/// <summary>
	/// Represents a table in the database.
	/// </summary>
	public class TableType
	{
		/// <summary>
		/// Gets or sets the name of the table.
		/// </summary>
		public string Name { get; set; } = string.Empty;

		/// <summary>
		/// Gets or sets the properties of the table.
		/// </summary>
		public List<PropertyType> Properties { get; set; } = new List<PropertyType>();
	}
}
