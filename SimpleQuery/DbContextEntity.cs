namespace Linq3Sql
{
	public class DbContextEntity
	{
		internal DbContextService? _dbContext = null;
		internal ReferenceFetchMode _fetchReferencesType = ReferenceFetchMode.None;
		internal ICollection<string>? _entitiesToFetch = null;

		internal ReferenceFetchMode GetChildrenReferenceFetchMode()
		{
			return _fetchReferencesType == ReferenceFetchMode.SingleLevel ? ReferenceFetchMode.None : _fetchReferencesType;
		}

		internal bool GetFetchReferences(string entityName)
		{
			return _fetchReferencesType == ReferenceFetchMode.SingleLevel
				|| _fetchReferencesType == ReferenceFetchMode.Recursive
				|| (_fetchReferencesType == ReferenceFetchMode.ByName && _entitiesToFetch is not null && _entitiesToFetch.Contains(entityName));
		}
	}
}
