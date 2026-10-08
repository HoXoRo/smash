using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/QuitConfirmUIForm")]
public partial class QuitConfirmUIForm : UIFormBase
{
    private bool m_IsQuitting;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);

        Button[] buttons = GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button.name == "btnClose")
            {
                button.onClick.AddListener(OnClickClose);
            }
            else if (button.name == "btnQuit")
            {
                button.onClick.AddListener(OnQuitClicked);
            }
        }
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        m_IsQuitting = false;
    }

    private void OnQuitClicked()
    {
        if (m_IsQuitting)
        {
            return;
        }

        QuitGameplayAsync().Forget();
    }

    private async UniTaskVoid QuitGameplayAsync()
    {
        m_IsQuitting = true;
        Interactable = false;

        try
        {
            string sceneAssetName = UtilityBuiltin.AssetsPath.GetScenePath("Gameplay");
            if (!await GF.Scene.UnLoadSceneAwait(sceneAssetName))
            {
                m_IsQuitting = false;
                Interactable = true;
                return;
            }
        }
        catch (System.Exception exception)
        {
            m_IsQuitting = false;
            Interactable = true;
            Log.Error(exception.ToString());
            return;
        }

        GF.UI.CloseUIForm(Id);
        GF.UI.CloseUIForms(UIViews.SettingPageUIForm);
        GF.UI.CloseUIForms(UIViews.PlayUIForm);

        if (!GF.UI.HasUIForm(UIViews.HallTabUIForm) &&
            !GF.UI.IsLoadingUIForm(UIViews.HallTabUIForm))
        {
            GF.UI.OpenUIForm(UIViews.HallTabUIForm);
        }
    }
}
