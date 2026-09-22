using System;
using System.Collections.Generic;

[Serializable]
public sealed class GetTopScoresResponseDto
{
	public int count;

	public List<LeaderboardEntryDto> entries;
}
