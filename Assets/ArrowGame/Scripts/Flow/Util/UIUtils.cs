using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Util
{
	public static class UIUtils
	{
		public static bool IsPointerOverUIElement()
		{
			return GetEventSystemRayCastResults().Count > 0;
		}

		private static List<RaycastResult> GetEventSystemRayCastResults()
		{
			List<RaycastResult> results = new List<RaycastResult>();
			if (EventSystem.current == null)
			{
				return results;
			}
			PointerEventData eventData = new PointerEventData(EventSystem.current)
			{
				position = Input.mousePosition
			};
			EventSystem.current.RaycastAll(eventData, results);
			return results;
		}

		public static Canvas GetRootCanvas(this RectTransform rt)
		{
			Canvas canvas = rt.GetComponentInParent<Canvas>();
			return canvas != null ? canvas.rootCanvas : null;
		}
	}
}
