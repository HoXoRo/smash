using System;
using System.Collections.Generic;

namespace ABTesting
{
	[Serializable]
	public class PersistedABEntries
	{
		public List<PersistedABEntry> data;

		public static PersistedABEntries GetDefault()
		{
			return new PersistedABEntries
			{
				data = new List<PersistedABEntry>()
			};
		}
	}
}
