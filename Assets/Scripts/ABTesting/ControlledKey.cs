namespace ABTesting
{
	public readonly struct ControlledKey<T>
	{
		public string Name { get; }

		public T DefaultValue { get; }

		public ControlledKey(string name, T defaultValue)
		{
			Name = name;
			DefaultValue = defaultValue;
		}
	}
}
