namespace Audio
{
	public enum AudioType
	{
		None = 0,
		UIClick = 1,
		MenuMusic = 2,
		GameplayMusic = 3,
		CannonShoot = 4,
		CanHit = 5,
		CanBreak = 6,
		StoneHit = 7,
		StoneBreak = 8,
		WormholeHit = 9,
		WormholeBreak = 10,
		BlockerHit = 11,
		Win = 12,
		Lose = 13,
		CartArrive = 14,
		CartDepart = 15,
		CoinAppear = 16,
		CoinHit = 17,
		JarBreak = 18,
		BoxHit = 19,
		BoxBreak = 20,
		IceHit = 21,
		IceBreak = 22,
		TntExplode = 23,
		BoxCylinderHit = 24,
		BoxCylinderBreak = 25,
		RocketIntro = 26,
		RocketFlight = 27,
		RocketHit = 28
	}

	public static class AudioTypeExtensions
	{
		public static string ToSoundName(this AudioType type)
		{
			return type switch
			{
				AudioType.UIClick => "UI_Click.ogg",
				AudioType.MenuMusic => "Homepage_Music.ogg",
				AudioType.GameplayMusic => "Gameplay_Music.ogg",
				AudioType.CannonShoot => "Cannon.ogg",
				AudioType.CanHit => "Can_Hit.ogg",
				AudioType.CanBreak => "Can_Break.ogg",
				AudioType.StoneHit => "Stone Hit.ogg",
				AudioType.StoneBreak => "Stone_Break.ogg",
				AudioType.WormholeHit => "Portal_Hit.ogg",
				AudioType.WormholeBreak => "Portal_Break.ogg",
				AudioType.BlockerHit => "Blocker_Hit.ogg",
				AudioType.Win => "Level_Win.ogg",
				AudioType.Lose => "Level_Fail.ogg",
				AudioType.CartArrive => "Cart_Arrive.ogg",
				AudioType.CartDepart => "Cart_Depart.ogg",
				AudioType.CoinAppear => "Coin_Appear.ogg",
				AudioType.CoinHit => "Coin_Hit.ogg",
				AudioType.JarBreak => "Jar_Break.ogg",
				AudioType.BoxHit => "Box_Hit.ogg",
				AudioType.BoxBreak => "Box_Break.ogg",
				AudioType.IceHit => "Ice_Hit.ogg",
				AudioType.IceBreak => "Ice_Break.ogg",
				AudioType.BoxCylinderHit => "BoxCylinder_Hit.ogg",
				AudioType.BoxCylinderBreak => "BoxCylinder_Break.ogg",
				AudioType.RocketIntro => "Rocket_Intro.ogg",
				AudioType.RocketFlight => "Rocket_Flight.ogg",
				AudioType.RocketHit => "Rocket_Hit.ogg",
				_ => null
			};
		}
	}
}
