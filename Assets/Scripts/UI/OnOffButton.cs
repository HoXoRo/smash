using UnityEngine;

namespace UI
{
	public class OnOffButton : FlowButton
	{
		[SerializeField]
		private GameObject onState;

		[SerializeField]
		private GameObject offState;

		public bool CurrentState { get; private set; }

		public void SetState(bool on)
		{
			CurrentState = on;
			if (onState != null)
			{
				onState.SetActive(on);
			}
			if (offState != null)
			{
				offState.SetActive(!on);
			}
		}
	}
}
