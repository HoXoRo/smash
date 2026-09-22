using System;

[Serializable]
public sealed class GetTopScoresByCountryRequestDto
{
	public string country;

	public int n;
}
