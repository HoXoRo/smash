using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PrelevelUIForm")]
public partial class PrelevelUIForm : UIFormBase
{
    private PlayerDataModel playerData;
    private bool useRocket;
    private bool isStarting;
    private int m_GuideSerialId = -1;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnProp.onClick.AddListener(ToggleRocket);
        varBtnPlay.onClick.AddListener(Play);
        varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        playerData = GF.DataModel.GetOrCreate<PlayerDataModel>();
        useRocket = false;
        isStarting = false;
        varTxtLevel.text = GF.Localization.GetString("level", playerData.LevelId);
        RefreshRocket();
        ShowRocketGuide();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (!isShutdown && m_GuideSerialId >= 0 &&
            (GF.UI.HasUIForm(m_GuideSerialId) || GF.UI.IsLoadingUIForm(m_GuideSerialId)))
            GF.UI.CloseUIForm(m_GuideSerialId);
        m_GuideSerialId = -1;
        base.OnClose(isShutdown, userData);
    }

    private void ShowRocketGuide()
    {
        if (isStarting || playerData.RocketCount <= 0 || GuideManager.Instance.IsGuideCompleted(4)) return;
        if (m_GuideSerialId >= 0 &&
            (GF.UI.HasUIForm(m_GuideSerialId) || GF.UI.IsLoadingUIForm(m_GuideSerialId))) return;
        m_GuideSerialId = GuideManager.Instance.ShowGuide(4, varBtnProp.transform,
            onComplete: () => m_GuideSerialId = -1);
    }

    private void RefreshRocket()
    {
        varTxtCount.text = playerData.RocketCount.ToString();
        varLeft.SetActive(!useRocket);
        varUsed.SetActive(useRocket);
        varBtnProp.interactable = !isStarting;
        varBtnPlay.interactable = !isStarting;
        varBtnClose.interactable = !isStarting;
    }

    private void ToggleRocket()
    {
        if (isStarting) return;
        if (!useRocket && playerData.RocketCount <= 0)
        {
            OpenAddProp();
            return;
        }
        useRocket = !useRocket;
        RefreshRocket();
    }

    private void OpenAddProp()
    {
        var uiParams = UIParams.Create();
        uiParams.CloseCallback = _ =>
        {
            useRocket = false;
            RefreshRocket();
            ShowRocketGuide();
        };
        GF.UI.OpenUIForm(UIViews.AddPropUIForm, uiParams);
    }

    private void Play()
    {
        if (isStarting) return;
        if (useRocket && playerData.RocketCount <= 0)
        {
            useRocket = false;
            RefreshRocket();
            OpenAddProp();
            return;
        }
        StartGameplayAsync().Forget();
    }

    private async UniTaskVoid StartGameplayAsync()
    {
        isStarting = true;
        RefreshRocket();
        if (await HomePageUIForm.TryEnterGameplayAsync(useRocket)) return;
        isStarting = false;
        RefreshRocket();
    }
}
