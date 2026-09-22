using UnityEngine;

namespace Gameplay.Obstacle
{
	public class BlockerParts : MonoBehaviour
	{
		[SerializeField]
		private Transform background;

		[SerializeField]
		private Transform topEdge;

		[SerializeField]
		private Transform bottomEdge;

		[SerializeField]
		private Transform leftEdge;

		[SerializeField]
		private Transform rightEdge;

		[SerializeField]
		private Transform topLeft;

		[SerializeField]
		private Transform bottomLeft;

		[SerializeField]
		private Transform topRight;

		[SerializeField]
		private Transform bottomRight;

		[SerializeField]
		private BoxCollider boxCollider;

		public void Resize(float width)
		{
			float clampedWidth = Mathf.Max(0.1f, width);
			SetScale(background, 12.8f * clampedWidth, 2.4f, 1f);
			SetScale(topEdge, 2.45f * clampedWidth, 0.315f, 1f);
			SetScale(bottomEdge, 2.45f * clampedWidth, 0.315f, 1f);

			SetPosition(topEdge, 0f, 0.3f, -0.01f);
			SetPosition(bottomEdge, 0f, -0.3f, -0.01f);
			SetPosition(leftEdge, -1.982f * clampedWidth, 0f, -0.01f);
			SetPosition(rightEdge, 1.982f * clampedWidth, 0f, -0.01f);
			SetPosition(topLeft, -1.93f * clampedWidth, 0.42f, -0.02f);
			SetPosition(bottomLeft, -1.93f * clampedWidth, -0.42f, -0.02f);
			SetPosition(topRight, 1.93f * clampedWidth, 0.42f, -0.02f);
			SetPosition(bottomRight, 1.93f * clampedWidth, -0.42f, -0.02f);

			if (boxCollider != null)
			{
				boxCollider.size = new Vector3(4.5f * clampedWidth, 0.87f, 0.5f);
				boxCollider.center = Vector3.zero;
			}
		}

		private static void SetScale(Transform target, float x, float y, float z)
		{
			if (target != null)
			{
				target.localScale = new Vector3(x, y, z);
			}
		}

		private static void SetPosition(Transform target, float x, float y, float z)
		{
			if (target != null)
			{
				target.localPosition = new Vector3(x, y, z);
			}
		}
	}
}
