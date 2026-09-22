using UnityEngine;

namespace UI
{
	public class JapaneseSunAutoRotator : MonoBehaviour
	{
		[SerializeField]
		private float degreesPerSecond;

		private void Update()
		{
			transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
		}
	}
}
