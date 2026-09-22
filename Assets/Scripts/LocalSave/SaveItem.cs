using System.Collections.Generic;

namespace LocalSave
{
	public class SaveItem<T>
	{
		private T _value;

		private readonly SaveBehaviour _saveBehaviour;

		public T Value
		{
			get
			{
				return _value;
			}
			set
			{
				if (!EqualityComparer<T>.Default.Equals(_value, value))
				{
					_value = value;
					SaveService.HandleChange(_saveBehaviour);
				}
			}
		}

		public SaveItem()
		{
			_value = default(T);
			_saveBehaviour = SaveBehaviour.Dirty;
		}

		public SaveItem(T defaultValue, SaveBehaviour saveBehaviour)
		{
			_value = defaultValue;
			_saveBehaviour = saveBehaviour;
		}

		public static implicit operator T(SaveItem<T> p)
		{
			return p.Value;
		}
	}
}
