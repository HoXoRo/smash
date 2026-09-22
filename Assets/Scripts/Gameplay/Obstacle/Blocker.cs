using Level;
using UnityEngine;

namespace Gameplay.Obstacle
{
	public class Blocker : MonoBehaviour
	{
		[SerializeField]
		private Transform rotatingPart;

		[SerializeField]
		private Collider partCollider;

		[SerializeField]
		private Collider poleCollider;

		[SerializeField]
		private LevelBlockerData data;

		private float _positionT;

		private Vector3 _rotationVector;

		private int _positionChangeDirection;

		public void Init()
		{
			if (data == null)
			{
				return;
			}
			_positionChangeDirection = 1;
			_positionT = data.startNormalized;
			_rotationVector = new Vector3(0f, 0f, data.rotInSecEuler);
			if (data.posHalfCycleDuration <= 0f)
			{
				data.posHalfCycleDuration = 1f;
			}
			if (rotatingPart != null)
			{
				rotatingPart.localRotation = Quaternion.Euler(0f, 0f, data.initRotEuler);
			}
			transform.localPosition = GetPositionByT(_positionT);
			GetComponent<BlockerParts>()?.Resize(data.width);
			if (partCollider != null)
			{
				partCollider.enabled = true;
			}
			if (poleCollider != null)
			{
				poleCollider.enabled = true;
			}
		}

		private void OnValidate()
		{
			if (data == null)
			{
				return;
			}
			transform.localPosition = GetPositionByT(data.startNormalized);
			if (rotatingPart != null)
			{
				rotatingPart.localRotation = Quaternion.Euler(0f, 0f, data.initRotEuler);
			}
			GetComponent<BlockerParts>()?.Resize(data.width);
		}

		public void SetData(LevelBlockerData levelBlockerData)
		{
			data = levelBlockerData;
		}

		public LevelBlockerData GetDataForLevel()
		{
			return data;
		}

		private void Update()
		{
			if (data == null)
			{
				return;
			}
			if (rotatingPart != null && _rotationVector != Vector3.zero)
			{
				rotatingPart.Rotate(_rotationVector * Time.deltaTime, Space.Self);
			}
			if (data.posHalfCycleDuration > 0f)
			{
				_positionT += _positionChangeDirection * Time.deltaTime / data.posHalfCycleDuration;
				transform.localPosition = GetPositionByT(_positionT);
				if (_positionChangeDirection == -1)
				{
					if (_positionT < 0f)
					{
						_positionChangeDirection = 1;
					}
				}
				else if (_positionChangeDirection == 1 && _positionT > 1f)
				{
					_positionChangeDirection = -1;
				}
			}
		}

		private Vector3 GetPositionByT(float t)
		{
			if (data == null)
			{
				return transform.localPosition;
			}
			float clampedT = Mathf.Clamp01(t);
			Vector3 min = new Vector3(data.minPos.x, data.minPos.y, data.initZ);
			Vector3 max = new Vector3(data.maxPos.x, data.maxPos.y, data.initZ);
			return Vector3.Lerp(min, max, clampedT);
		}
	}
}
