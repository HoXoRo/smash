using System;

[Serializable]
public sealed class UpsertScoreResponseDto
{
	public bool ok;

	public string userId;

	public string country;

	public int bestLevel;
}
