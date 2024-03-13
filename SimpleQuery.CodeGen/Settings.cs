using System;
using System.Diagnostics.CodeAnalysis;

namespace drittich.SimpleQuery.CodeGen
{
	[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]
	public class Settings
	{
		public string ConnectionString { get; set; } = string.Empty;
		public string TargetFolder { get; set; } = string.Empty;
		public string[] ExcludeTables { get; set; } = Array.Empty<string>();
		public string ModelNamespace { get; set; } = string.Empty;
		public bool OneLineNamespaceDeclaration { get; set; }
	}
}
