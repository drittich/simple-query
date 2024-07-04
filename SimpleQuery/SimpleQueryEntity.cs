using System.Collections.Generic;

namespace drittich.SimpleQuery
{
	/// <summary>
	/// Represents a simple query entity.
	/// </summary>
	public class SimpleQueryEntity
	{
		public SimpleQueryService? _dbContext = null;

		/// <summary>
		/// Represents the fetch mode for references.
		/// </summary>
		internal ReferenceFetchMode _fetchReferencesType = ReferenceFetchMode.None;

		/// <summary>
		/// Represents the collection of entities to fetch.
		/// </summary>
		internal ICollection<string>? _entitiesToFetch = null;

		/// <summary>
		/// Gets the fetch mode for child references.
		/// </summary>
		/// <returns>The fetch mode for child references.</returns>
		internal ReferenceFetchMode GetChildrenReferenceFetchMode()
		{
			return _fetchReferencesType == ReferenceFetchMode.SingleLevel || _fetchReferencesType == ReferenceFetchMode.SingleLevelByName
				? ReferenceFetchMode.None
				: _fetchReferencesType;
		}

		/// <summary>
		/// Determines whether to fetch references for a given entity.
		/// </summary>
		/// <param name="entityName">The name of the entity.</param>
		/// <returns>True if references should be fetched, false otherwise.</returns>
		internal bool GetFetchReferences(string entityName)
		{
			return _fetchReferencesType == ReferenceFetchMode.SingleLevel
				|| _fetchReferencesType == ReferenceFetchMode.Recursive
				|| (_fetchReferencesType == ReferenceFetchMode.ByName && _entitiesToFetch != null && _entitiesToFetch.Contains(entityName))
				|| (_fetchReferencesType == ReferenceFetchMode.SingleLevelByName && _entitiesToFetch != null && _entitiesToFetch.Contains(entityName))
				|| (_fetchReferencesType == ReferenceFetchMode.RecursiveByName && _entitiesToFetch != null && _entitiesToFetch.Contains(entityName));
		}

		/// <summary>
		/// Fetches an entity of type T by its id.
		/// Used to fetch entities with references.
		/// </summary>
		/// <param name="id">The id of the entity to fetch.</param>
		/// <typeparam name="T">The type of the entity to fetch. Must be a subclass of SimpleQueryEntity.</typeparam>
		/// <returns>The fetched entity if it exists and references should be fetched for its type, null otherwise.</returns>
		public T? _FetchById<T>(object? id) where T : SimpleQueryEntity, IPrimaryKeyProvider, new()
		{
			if (id != null && GetFetchReferences(typeof(T).Name))
			{
				return _dbContext!.QueryFirstAsync<T>(id, GetChildrenReferenceFetchMode(), (string?)null).Result;
			}

			return null;
		}
	}
}
