using GameFramework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public partial class UtilityEx
{
    public static bool CheckNetwork()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }
    /// <summary>
    /// 用于移动平台检测是否点击到UI元素上。
    /// 仅统计 GraphicRaycaster 的命中，忽略 Physics2DRaycaster 等（避免点击场景中的箭头等对象被误判为 UI）
    /// </summary>
    /// <param name="screenPosition"></param>
    /// <returns></returns>
    public static bool IsPointerOverUIObject(Vector2 screenPosition)
    {
        if (EventSystem.current == null) return false;
        PointerEventData eventDataCurrentPosition = new PointerEventData(EventSystem.current);
        eventDataCurrentPosition.position = new Vector2(screenPosition.x, screenPosition.y);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventDataCurrentPosition, results);

        foreach (var result in results)
        {
            if (result.module is GraphicRaycaster)
                return true;
        }
        return false;
    }
}
