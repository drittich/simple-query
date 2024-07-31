namespace drittich.SimpleQuery.CodeGen
{
	public class ForeignKey
	{
		public long id { get; set; }

		public long seq { get; set; }

		public string table { get; set; } = string.Empty;

		public string from { get; set; } = string.Empty;

		public string to { get; set; } = string.Empty;

		public string on_update { get; set; } = string.Empty;

		public string on_delete { get; set; } = string.Empty;

		public string match { get; set; } = string.Empty;

		public ForeignKey() { }
	}
}
