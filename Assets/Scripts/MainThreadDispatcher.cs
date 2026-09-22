using System;
using System.Collections.Generic;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
	private static readonly Queue<Action> _queue;

	private static MainThreadDispatcher _instance;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void Init()
	{
		if (FindObjectOfType<MainThreadDispatcher>() != null)
		{
			return;
		}
		GameObject gameObject = new GameObject("MainThreadDispatcher");
		DontDestroyOnLoad(gameObject);
		gameObject.AddComponent<MainThreadDispatcher>();
	}

	private void Awake()
	{
		if (_instance != null && _instance != this)
		{
			Destroy(gameObject);
			return;
		}
		_instance = this;
		DontDestroyOnLoad(gameObject);
	}

	public static void Run(Action action)
	{
		if (action == null)
		{
			return;
		}
		lock (_queue)
		{
			_queue.Enqueue(action);
		}
	}

	private void Update()
	{
		while (true)
		{
			Action action;
			lock (_queue)
			{
				if (_queue.Count == 0)
				{
					return;
				}
				action = _queue.Dequeue();
			}
			action?.Invoke();
		}
	}

	private void OnDestroy()
	{
		if (_instance == this)
		{
			_instance = null;
		}
	}

	static MainThreadDispatcher()
	{
		_queue = new Queue<Action>();
	}
}
