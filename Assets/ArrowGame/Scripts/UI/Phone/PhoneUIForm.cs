using System;
using UnityEngine;
using UnityGameFramework.Runtime;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/PhoneUIForm")]
public partial class PhoneUIForm : UIFormBase
{
    [SerializeField] private TMPro.TextMeshProUGUI fragmentTime;
    [SerializeField] private TMPro.TextMeshProUGUI txtLeftAd;
    private PlayerDataModel m_Player;
    private PhoneTask m_Task;
    private bool m_IsOpen;
    private long m_LastRefreshSecond = -1;
    private static bool s_AdPending;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        varBtnRedeem.onClick.AddListener(Redeem);
        varBtnCollect.onClick.AddListener(Collect);
        varBtnTime.onClick.AddListener(FragmentCooldownHint);
        varBtnLock.onClick.AddListener(FragmentLockHint);
        varBtnAd.onClick.AddListener(WatchTaskAd);
        varBtnCd.onClick.AddListener(AdCooldownHint);
        varBtnView.onClick.AddListener(OnClickView);
        varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        CommonHelper.HideBannerAd();
        m_IsOpen = true;
        m_Player = GF.DataModel.GetOrCreate<PlayerDataModel>();
        m_Task = PhoneManager.Instance.Task;
        if (m_Task == null)
        {
            Log.Error("PhoneUIForm: PhoneTask row 1 is not loaded.");
            varTask1.SetActive(false);
            varTask2.SetActive(false);
            varTask3.SetActive(false);
            return;
        }
        Refresh();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        m_IsOpen = false;
        base.OnClose(isShutdown, userData);
        if (!isShutdown) CommonHelper.ShowBannerAd();
    }

    protected override void OnUpdate(float elapseSeconds, float realElapseSeconds)
    {
        base.OnUpdate(elapseSeconds, realElapseSeconds);
        if (m_IsOpen && m_Task != null && m_LastRefreshSecond != Now) Refresh();
    }

    private static long Now => DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    private static long Remaining(long end) => Math.Max(0, end - Now);
    private static string FormatTime(long seconds) => $"{seconds / 60:00}:{seconds % 60:00}";

    private void Refresh()
    {
        m_LastRefreshSecond = Now;
        int stage = m_Player.PhoneStage;
        varTask1.SetActive(stage == 1);
        varTask2.SetActive(stage == 2);
        varTask3.SetActive(stage == 3);
        varTxtCurFragment.text = m_Player.PhoneFragments.ToString();
        varTxtTargetFragment.text = $"/{m_Task.Fragment.ToString()}";
        SetProgress(varFragmentProgress, m_Player.PhoneFragments, m_Task.Fragment);
        SetProgress(varAdProgress, m_Player.PhoneAdCount, m_Task.AdNum);
        SetProgress(varProgress, m_Player.PhoneAdCount, m_Task.AdNum);
        string adProgress = $"{m_Player.PhoneAdCount}/{m_Task.AdNum}";
        varAdTxtProgress.text = adProgress;
        varTxtProgress.text = adProgress;
        txtLeftAd.text = GF.Localization.GetString("PhoneUIForm.leftAd", Math.Max(0, m_Task.AdNum - m_Player.PhoneAdCount));
        bool full = m_Player.PhoneFragments >= m_Task.Fragment;
        varAdCollect.SetActive(!full);
        bool locked = PhoneManager.Instance.IsPhoneFragmentLocked(m_Task);
        long fragmentSeconds = Remaining(m_Player.PhoneFragmentCooldownEnd);
        varBtnLock.gameObject.SetActive(!full && locked);
        varBtnLock.interactable = true;
        varBtnTime.gameObject.SetActive(!full && !locked && fragmentSeconds > 0);
        varBtnTime.interactable = true;
        varBtnCollect.gameObject.SetActive(!full && !locked && fragmentSeconds == 0);
        varBtnCollect.interactable = !s_AdPending;
        varBtnRedeem.interactable = !s_AdPending;
        varBtnView.interactable = !s_AdPending;
        fragmentTime.text = FormatTime(fragmentSeconds);
        long adSeconds = Remaining(m_Player.PhoneAdCooldownEnd);
        varBtnAd.gameObject.SetActive(adSeconds == 0);
        varBtnAd.interactable = !s_AdPending;
        varBtnCd.gameObject.SetActive(adSeconds > 0);
        varBtnCd.interactable = true;
        varTxtCd.text = FormatTime(adSeconds);
    }

    private static void SetProgress(UnityEngine.UI.Slider slider, int current, int target)
    {
        slider.interactable = false;
        slider.wholeNumbers = false;
        slider.minValue = 0;
        slider.maxValue = 1;
        slider.value = target > 0 ? Mathf.Clamp01((float)current / target) : 1;
    }

    private void Redeem()
    {
        if (m_Task == null || s_AdPending || m_Player.PhoneStage != 1) return;
        if (m_Player.PhoneFragments < m_Task.Fragment)
        {
            GF.UI.ShowToast($"Collect {m_Task.Fragment} phone fragments first");
            return;
        }
        OpenSubUIForm(UIViews.PhoneAddressUIForm);
    }

    private void Collect()
    {
        if (m_Task == null || s_AdPending || m_Player.PhoneStage != 1 || m_Player.PhoneFragments >= m_Task.Fragment) return;
        if (PhoneManager.Instance.IsPhoneFragmentLocked(m_Task)) { FragmentLockHint(); return; }
        if (Remaining(m_Player.PhoneFragmentCooldownEnd) > 0) { FragmentCooldownHint(); return; }
        ShowAd(1);
    }

    private void WatchTaskAd()
    {
        if (m_Task == null || s_AdPending || m_Player.PhoneStage != 2) return;
        if (Remaining(m_Player.PhoneAdCooldownEnd) > 0) { AdCooldownHint(); return; }
        ShowAd(2);
    }

    private void ShowAd(int stage)
    {
        s_AdPending = true;
        Refresh();
        // Preserve successful rewards even if the UI closes while the ad is playing.
        var player = m_Player;
        var task = m_Task;
        bool completed = false;
        try
        {
            CommonHelper.ShowVideoAd(stage == 1 ? "phone_fragment" : "phone_task", success =>
            {
                if (completed) return;
                completed = true;
                s_AdPending = false;
                if (success) PhoneManager.Instance.CompletePhoneAd(player, task, stage);
                if (this != null && m_IsOpen) Refresh();
            });
        }
        catch (Exception exception)
        {
            completed = true;
            s_AdPending = false;
            Log.Error("PhoneUIForm: failed to show ad: {0}", exception);
            if (this != null && m_IsOpen) Refresh();
        }
    }

    private void FragmentCooldownHint() => CooldownHint(m_Player.PhoneFragmentCooldownEnd);
    private void AdCooldownHint() => CooldownHint(m_Player.PhoneAdCooldownEnd);
    private void CooldownHint(long end)
    {
        long seconds = Remaining(end);
        if (seconds > 0) GF.UI.ShowToast($"Please wait {seconds} seconds");
        Refresh();
    }

    private void FragmentLockHint()
    {
        if (PhoneManager.Instance.IsPhoneFragmentLocked(m_Task))
            GF.UI.ShowToast("That's all the phone fragments for today, come back tomorrow for more!");
        Refresh();
    }

    protected virtual void OnClickView()
    {
        if (m_Task == null || s_AdPending || !m_IsOpen) return;
        s_AdPending = true;
        Refresh();
        bool completed = false;
        try
        {
            CommonHelper.ShowVideoAd("phone_view", success =>
            {
                if (completed) return;
                completed = true;
                s_AdPending = false;
                if (this == null || !m_IsOpen) return;
                Refresh();
                if (success) OpenSubUIForm(UIViews.PhoneAddressStateUIForm);
            });
        }
        catch (Exception exception)
        {
            completed = true;
            s_AdPending = false;
            Log.Error("PhoneUIForm: failed to show view ad: {0}", exception);
            if (this != null && m_IsOpen) Refresh();
        }
    }
}