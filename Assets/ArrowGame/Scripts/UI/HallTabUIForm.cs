using DG.Tweening;
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
    const float TabScaleDuration = 0.2f;

   
    UIViews _currentPage;
    int _currentPageId = -1;
    Vector3 _taskNormalScale;
    Vector3 _homeNormalScale;
    Vector3 _settingsNormalScale;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        _taskNormalScale = varBtnTask.transform.localScale;
        _homeNormalScale = varBtnHome.transform.localScale;
        _settingsNormalScale = varBtnSettings.transform.localScale;
         
        varBtnTask.onClick.AddListener(() => OnTabClicked(UIViews.TaskPageUIForm, varBtnTask));
        varBtnHome.onClick.AddListener(() => OnTabClicked(UIViews.HomePageUIForm, varBtnHome));
        varBtnSettings.onClick.AddListener(() => OnTabClicked(UIViews.SettingPageUIForm, varBtnSettings));
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        ArrowUITopbarSub.SetHallActive(true);
        ArrowUITopbar.SetCoinVisible(true);
        ArrowUITopbarSub.SetLifeVisible(true);
        ArrowUITopbar.EnsureOpen();
        SwitchPage(UIViews.HomePageUIForm, varBtnHome);
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        ArrowUITopbarSub.SetHallActive(false);
        DOTween.Kill(varBtnTask);
        DOTween.Kill(varBtnHome);
        DOTween.Kill(varBtnSettings);
        ArrowUITopbarSub.SetLifeVisible(false);
        if (!isShutdown)
        {
            CloseCurrentPage();
        }
        base.OnClose(isShutdown, userData);
    }

    void OnTabClicked(UIViews page, Button button)
    {
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
        if (view == UIViews.TaskPageUIForm)
        {
            var uiParams = UIParams.Create();
            uiParams.Set<VarBoolean>(TaskPageUIForm.P_FromHallTab, true);
            return GF.UI.OpenUIForm(view, uiParams);
        }

        return GF.UI.OpenUIForm(view);
    }

    public void ShowTaskPage()
    {
        OnTabClicked(UIViews.TaskPageUIForm, varBtnTask);
    }

    public void ShowHomePage()
    {
        SwitchPage(UIViews.HomePageUIForm, varBtnHome);
    }

    public void CloseBeforeEnterGameplay()
    {
        CloseCurrentPage();
        GF.UI.CloseUIForm(Id);
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
        ApplyTabVisual(varBtnTask, selectedButton == varBtnTask, _taskNormalScale);
        ApplyTabVisual(varBtnHome, selectedButton == varBtnHome, _homeNormalScale);
        ApplyTabVisual(varBtnSettings, selectedButton == varBtnSettings, _settingsNormalScale);
    }

    static void ApplyTabVisual(Button button, bool selected, Vector3 normalScale)
    {
        if (button == null)
        {
            return;
        }

        DOTween.Kill(button);
        button.transform.DOScale(selected ? normalScale * 1.2f : normalScale, TabScaleDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetId(button)
            .SetLink(button.gameObject, LinkBehaviour.KillOnDisable);

        Transform selectedIndicator = button.transform.Find("selected");
        if (selectedIndicator != null)
        {
            selectedIndicator.gameObject.SetActive(selected);
        }

        button.transition = Selectable.Transition.None;
        if (button.targetGraphic != null)
        {
            button.targetGraphic.color = selected ? SelectedColor : NormalColor;
        }
    }
}
