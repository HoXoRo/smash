using Service;
using UnityEngine;

namespace Gameplay.Camera
{
	public class CameraHelper : ServiceMonoBehaviour
	{
		public UnityEngine.Camera mainCamera;

		public float GetHorizontalFOV()
		{
			UnityEngine.Camera cam = mainCamera != null ? mainCamera : UnityEngine.Camera.main;
			if (cam == null)
			{
				return 0f;
			}
			float verticalRad = cam.fieldOfView * Mathf.Deg2Rad;
			return 2f * Mathf.Atan(Mathf.Tan(verticalRad * 0.5f) * cam.aspect) * Mathf.Rad2Deg;
		}

		public Vector3 GetPointFromPointToCamera(Vector3 point, float distance)
		{
			UnityEngine.Camera cam = mainCamera != null ? mainCamera : UnityEngine.Camera.main;
			if (cam == null)
			{
				return point;
			}
			Vector3 direction = (cam.transform.position - point).normalized;
			return point + direction * distance;
		}
	}
}
