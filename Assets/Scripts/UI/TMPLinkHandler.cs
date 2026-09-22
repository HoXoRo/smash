using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
	public class TMPLinkHandler : MonoBehaviour, IPointerClickHandler, IEventSystemHandler
	{
		[SerializeField]
		private TextMeshProUGUI text;

		public void OnPointerClick(PointerEventData eventData)
		{
			int linkIndex = TMP_TextUtilities.FindIntersectingLink(text, eventData.position, eventData.pressEventCamera);
			if (linkIndex == -1)
			{
				return;
			}
			string linkId = text.textInfo.linkInfo[linkIndex].GetLinkID();
			if (linkId == "privacy")
			{
				Application.OpenURL("https://www.flowgames.net/privacy");
			}
			else if (linkId == "terms")
			{
				Application.OpenURL("https://www.flowgames.net/termsofservice");
			}
		}
	}
}
