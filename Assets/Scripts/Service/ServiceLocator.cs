using System;
using System.Collections.Generic;

namespace Service
{
	public static class ServiceLocator
	{
		private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

		public static void Register<T>(T service)
		{
			Register(typeof(T), service);
		}

		public static void Register<T>(Type type, T service)
		{
			if (type == null || service == null)
			{
				return;
			}
			_services[type] = service;
		}

		public static T Get<T>()
		{
			if (_services.TryGetValue(typeof(T), out object service))
			{
				return (T)service;
			}
			return default(T);
		}

		public static void Unregister<T>(Type type, T service)
		{
			if (type != null && _services.TryGetValue(type, out object current) && ReferenceEquals(current, service))
			{
				_services.Remove(type);
			}
		}
	}
}
