using System;

namespace ABTesting
{
	[Serializable]
	public class PersistedABEntry
	{
		public string key;

		public float participationDraw;

		public int? variantId;

		public PersistedABEntry(string key, float participationDraw, int? variantId = null)
		{
			this.key = key;
			this.participationDraw = participationDraw;
			this.variantId = variantId;
		}
	}
}
