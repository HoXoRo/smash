using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

[AddComponentMenu("UI/Item/SettingController")]
public partial class ArrowSettingController : UIItemBase
{
    protected override void OnInit()
    {
        base.OnInit();
    }
    public void SetOnOff(bool isOn)
    {
        varOFF.SetActive(!isOn);
        varON.SetActive(isOn);
    }
}