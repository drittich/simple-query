using System.Collections.Generic;

namespace drittich.SimpleQuery
{
	public class TableType
	{
		public string Name { get; set; } = string.Empty;
		public List<PropertyType> Properties { get; set; } = new List<PropertyType>();
	}
}
