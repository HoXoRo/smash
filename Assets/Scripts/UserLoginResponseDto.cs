using System;

[Serializable]
public sealed class UserLoginResponseDto
{
	public bool ok;

	public string userId;

	public string country;

	public int bestLevel;

	public bool created;
}
