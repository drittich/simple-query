using System;

namespace drittich.SimpleQuery.CodeGen
{
	public class Settings
	{
		public string ConnectionString { get; set; } = string.Empty;
		public string TargetFolder { get; set; } = string.Empty;
		public string[] ExcludeTables { get; set; } = Array.Empty<string>();
	}
}
