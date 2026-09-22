using UnityEngine;

namespace Menu.Page
{
	public abstract class BasePage : MonoBehaviour
	{
		public abstract void Prepare();

		public virtual void Show()
		{
			gameObject.SetActive(true);
		}

		public virtual void Hide()
		{
			gameObject.SetActive(false);
		}
	}
}
