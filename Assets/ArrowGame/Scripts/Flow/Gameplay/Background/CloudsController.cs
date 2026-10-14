using System.Collections.Generic;
using Service;
using UnityEngine;

namespace Gameplay.Background
{
	public class CloudsController : ServiceMonoBehaviour
	{
		private const int TotalCloudCount = 70;

		private const float CloudsDistance = 89.5f;

		private const float CloudMinY = 19f;

		private const float CloudMaxY = 24f;

		private const float MinAnglePerSecond = 0.2f;

		private const float MaxAnglePerSecond = 0.5f;

		[SerializeField]
		private List<GameObject> cloudPrefabs;

		private Transform[] _cloudTransforms;

		private float[] _rotateSpeeds;

		private Vector3 _pivot;

		private void Start()
		{
			_pivot = transform.position;
			SpawnClouds();
		}

		private void SpawnClouds()
		{
			if (cloudPrefabs == null || cloudPrefabs.Count == 0)
			{
				Debug.LogWarning("[CloudsController] cloudPrefabs is empty, clouds will not be spawned.");
				return;
			}
			_cloudTransforms = new Transform[TotalCloudCount];
			_rotateSpeeds = new float[TotalCloudCount];
			for (int i = 0; i < TotalCloudCount; i++)
			{
				GameObject prefab = cloudPrefabs[Random.Range(0, cloudPrefabs.Count)];
				if (prefab == null)
				{
					continue;
				}
				float angle = Random.Range(0f, 360f);
				float y = Random.Range(CloudMinY, CloudMaxY);
				Vector3 offset = Quaternion.Euler(0f, angle, 0f) * (Vector3.forward * CloudsDistance);
				GameObject cloud = Object.Instantiate(prefab, transform);
				cloud.transform.position = _pivot + new Vector3(offset.x, y, offset.z);
				cloud.transform.LookAt(_pivot);
				_cloudTransforms[i] = cloud.transform;
				_rotateSpeeds[i] = Random.Range(MinAnglePerSecond, MaxAnglePerSecond) * (Random.value < 0.5f ? -1f : 1f);
			}
		}

		private void Update()
		{
			if (_cloudTransforms == null || _rotateSpeeds == null)
			{
				return;
			}
			for (int i = 0; i < _cloudTransforms.Length; i++)
			{
				Transform cloud = _cloudTransforms[i];
				if (cloud != null)
				{
					cloud.RotateAround(_pivot, Vector3.up, _rotateSpeeds[i] * Time.deltaTime);
				}
			}
		}
	}
}
