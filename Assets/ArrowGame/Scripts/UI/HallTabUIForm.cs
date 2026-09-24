using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/HallTabUIForm")]

public partial class HallTabUIForm : UIFormBase
{
    static readonly Color SelectedColor = new Color(0.25f, 0.72f, 1f, 1f);
    static readonly Color NormalColor = Color.white;

   
    UIViews _currentPage;
    int _currentPageId = -1;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
         
        varBtnTask.onClick.AddListener(() => OnTabClicked(UIViews.TaskPageUIForm, varBtnTask));
        varBtnHome.onClick.AddListener(() => OnTabClicked(UIViews.HomePageUIForm, varBtnHome));
        varBtnSettings.onClick.AddListener(() => OnTabClicked(UIViews.SettingPageUIForm, varBtnSettings));
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        SwitchPage(UIViews.HomePageUIForm, varBtnHome);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        if (!isShutdown)
        {
            CloseCurrentPage();
        }
        base.OnClose(isShutdown, userData);
    }

    void OnTabClicked(UIViews page, Button button)
    {
        GF.Sound.PlayEffect("ui/ui_click.mp3");
        SwitchPage(page, button);
    }

    void SwitchPage(UIViews page, Button selectedButton)
    {
        if (_currentPageId != -1 && _currentPage == page && IsPageAlive(_currentPageId))
        {
            RefreshTabVisual(selectedButton);
            return;
        }

        CloseCurrentPage();
        _currentPage = page;
        _currentPageId = OpenPage(page);
        RefreshTabVisual(selectedButton);
    }

    static bool IsPageAlive(int serialId)
    {
        return GF.UI.HasUIForm(serialId) || GF.UI.IsLoadingUIForm(serialId);
    }

    int OpenPage(UIViews view)
    {
        
        return GF.UI.OpenUIForm(view);
    }

    void CloseCurrentPage()
    {
        if (_currentPageId == -1)
        {
            return;
        }

        if (IsPageAlive(_currentPageId))
        {
            GF.UI.CloseUIForm(_currentPageId);
        }

        _currentPageId = -1;
    }

    void RefreshTabVisual(Button selectedButton)
    {
        ApplyTabColor(varBtnTask, selectedButton == varBtnTask);
        ApplyTabColor(varBtnHome, selectedButton == varBtnHome);
        ApplyTabColor(varBtnSettings, selectedButton == varBtnSettings);
    }

    static void ApplyTabColor(Button button, bool selected)
    {
        if (button == null)
        {
            return;
        }

        button.transition = Selectable.Transition.None;
        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = selected ? SelectedColor : NormalColor;
        }
    }
}
