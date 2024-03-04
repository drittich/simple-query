namespace drittich.SimpleQuery
{
	public class PropertyType
	{
		public string Name { get; set; } = string.Empty;
		public string TypeName { get; set; } = string.Empty;
		public bool IsNullable { get; set; }
		public bool IsPrimaryKey { get; set; }
	}
}
