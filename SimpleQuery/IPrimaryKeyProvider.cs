namespace drittich.SimpleQuery
{
	/// <summary>
	/// Represents a provider for retrieving primary key information.
	/// </summary>
	public interface IPrimaryKeyProvider
	{
		/// <summary>
		/// Gets the names of the primary key columns.
		/// </summary>
		/// <returns>An array of strings representing the names of the primary key columns.</returns>
		string[] GetPrimaryKeyColumnNames();
	}
}
