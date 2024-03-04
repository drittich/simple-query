using System.Collections.Generic;

namespace drittich.SimpleQuery
{
	public class SimpleQueryEntity
	{
		public SimpleQueryService? _dbContext = null;
		internal ReferenceFetchMode _fetchReferencesType = ReferenceFetchMode.None;
		internal ICollection<string>? _entitiesToFetch = null;

		public ReferenceFetchMode GetChildrenReferenceFetchMode()
		{
			return _fetchReferencesType == ReferenceFetchMode.SingleLevel ? ReferenceFetchMode.None : _fetchReferencesType;
		}

		public bool GetFetchReferences(string entityName)
		{
			return _fetchReferencesType == ReferenceFetchMode.SingleLevel
				|| _fetchReferencesType == ReferenceFetchMode.Recursive
				|| (_fetchReferencesType == ReferenceFetchMode.ByName && _entitiesToFetch != null && _entitiesToFetch.Contains(entityName));
		}
	}
}
