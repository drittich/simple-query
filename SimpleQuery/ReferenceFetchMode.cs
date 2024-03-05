﻿namespace drittich.SimpleQuery
{
	/// <summary>
	/// Defines the modes for fetching references.
	/// </summary>
	public enum ReferenceFetchMode
	{
		/// <summary>
		/// Indicates no references will be populated.
		/// </summary>
		None,

		/// <summary>
		/// Indicates only references for the initially requested object will be populated.
		/// </summary>
		SingleLevel,

		/// <summary>
		/// Indicates all references, recursively, will be populated.
		/// </summary>
		Recursive,

		/// <summary>
		/// Indicates only specifically named references will be populated.
		/// </summary>
		ByName
	}
}
