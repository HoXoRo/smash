using System.Collections.Generic;
using UnityEngine;

namespace Popups
{
	public class BasicPopup : BasePopup
	{
		[SerializeField]
		private FlowButton continueButton;

		[SerializeField]
		private FlowButton closeButton;

		public override void Prepare(Dictionary<string, object> openParameters)
		{
			if (continueButton != null)
			{
				continueButton.OnClick.RemoveListener(OnContinueClicked);
				continueButton.OnClick.AddListener(OnContinueClicked);
			}
			if (closeButton != null)
			{
				closeButton.OnClick.RemoveListener(OnCloseClicked);
				closeButton.OnClick.AddListener(OnCloseClicked);
			}
		}

		private void OnContinueClicked()
		{
			Close();
		}

		private void OnCloseClicked()
		{
			Close();
		}
	}
}
