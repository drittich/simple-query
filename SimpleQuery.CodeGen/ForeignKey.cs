namespace drittich.SimpleQuery.CodeGen
{
	public class ForeignKey
	{
		public int id { get; set; }
		public int seq { get; set; }
		public string table { get; set; }
		public string from { get; set; }
		public string to { get; set; }

		public ForeignKey()
		{
		}
	}
}
