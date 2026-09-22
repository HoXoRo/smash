using System;
using System.Collections.Generic;
using UnityEngine;

namespace Audio
{
	[CreateAssetMenu(menuName = "Flow/Audio Database")]
	public class AudioData : ScriptableObject
	{
		[Serializable]
		public struct Entry
		{
			public AudioType type;

			public List<AudioClip> clips;
		}

		public List<Entry> entries;
	}
}
