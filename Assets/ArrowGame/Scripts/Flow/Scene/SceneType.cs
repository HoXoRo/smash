namespace Scene
{
	// Scene asset paths depend on these enum names via ToString().
	[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.Field)]
	public enum SceneType
	{
		None = 0,
		Splash = 1,
		Menu = 2,
		Gameplay = 3
	}
}
