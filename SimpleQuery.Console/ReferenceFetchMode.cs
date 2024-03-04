namespace Linq3Sql
{
	public enum ReferenceFetchMode
	{
		None, // Indicates no references will be populated.
		SingleLevel, // Indicates only references for the initially requested object will be populated.
		Recursive, // Indicates all references, recursively, will be populated.
		ByName // Indicates only specifically named references will be populated.
	}

}
