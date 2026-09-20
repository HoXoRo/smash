using System;
using DG.Tweening;
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
        varDot.transform.DOKill();
        varDot.transform.DOLocalMove(isOn ? new Vector3(65, 0, 0) : new Vector3(-65, 0, 0), 0.3f);
        varBg.SetSprite(isOn ? "UI/SettingAtlas/tc_szt1.png" : "UI/SettingAtlas/tc_szt2.png");
    }
}