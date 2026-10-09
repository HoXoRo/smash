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
        varTxtLevel.text = $"Level {playerData.LevelId}";
        RefreshRocket();
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
