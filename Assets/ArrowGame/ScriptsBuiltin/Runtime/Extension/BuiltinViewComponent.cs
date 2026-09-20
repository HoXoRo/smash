using System.Collections;
using UnityEngine;
using GameFramework;
using UnityGameFramework.Runtime;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 内置的UI界面(热更之前)
/// </summary>
public class BuiltinViewComponent : GameFrameworkComponent
{
    [Header("Loading Progress:")]
    [SerializeField] GameObject loadingProgressNode = null;
    [SerializeField] private TMP_Text loadSliderText;
    [SerializeField] private Image loadSlider;

    [SerializeField] GameObject loadingProgressNode_A = null;
    [SerializeField] private TMP_Text loadSliderText_A;
    [SerializeField] private Image loadSlider_A;

    [Space(20)]
    [Header("Tips Dialog:")]
    [SerializeField] GameObject tipsDialog = null;
    [SerializeField] Text tipsTitleText;
    [SerializeField] Text tipsContentText;
    [SerializeField] Button tipsPositiveBtn;
    [SerializeField] Button tipsNegativeBtn;

    [Header("Privacy Policy Progress:")]
    [SerializeField] GameObject privacyPolicyNode = null;
    [SerializeField] private Button privacyPolicyCloseBtn;
    [SerializeField] private Button privacyPolicyAgreeBtn;
    //[Header("Waiting View:")]
    //[SerializeField] GameObject waitingView = null;

    //public void WaitAndShowVideoAd(float loadingOutTime, GameFrameworkAction onAdReady)
    //{
    //    adLoadingMask.SetActive(true);
    //    StopAllCoroutines();
    //    StartCoroutine(WaitAdLoading(loadingOutTime, onAdReady));
    //}
    //IEnumerator WaitAdLoading(float loadingOutTime, GameFrameworkAction onAdReady)
    //{
    //    float adLoadCd = 0;

    //    while (adLoadCd < loadingOutTime)
    //    {
    //        if (GFBuiltin.AD.IsRewardedAdReady())
    //        {
    //            onAdReady?.Invoke();
    //            break;
    //        }
    //        yield return new WaitForSeconds(0.2f);
    //        adLoadCd += 0.2f;
    //    }
    //    if (adLoadCd >= loadingOutTime)
    //    {
    //        GFBuiltin.AD.ShowToast("Loading failed! Please try again later.");
    //        //GFBuiltin.UserData.RecodEvent("missing_videoAd");
    //    }
    //    adLoadingMask.SetActive(false);
    //}
    private void Start()
    {
        ShowLoadingProgress();
        //waitingView.SetActive(false);
    }
    public void ShowLoadingProgress(float defaultProgress = 0)
    {
        bool isB = true;//GFBuiltin.Setting.GetBool("UserType", false);
        loadingProgressNode.SetActive(isB);
        // loadingProgressNode_A.SetActive(!isB);
        SetLoadingProgress(defaultProgress);
    }
    public void SetLoadingProgress(float progress)
    {
        loadSlider.fillAmount = progress;
        loadSliderText.text = Utility.Text.Format("Loading...{0:N0}%", progress * 100);

        // loadSlider_A.fillAmount = progress;
        // loadSliderText_A.text = Utility.Text.Format("Loading...{0:N0}%", progress * 100);
    }
    public bool IsShowPrivacyPolicy()
    {
        return privacyPolicyNode.activeSelf;
    }
    public void ShowPrivacyPolicy()
    {
        privacyPolicyNode.SetActive(true);
        privacyPolicyCloseBtn.onClick.RemoveAllListeners();
        privacyPolicyAgreeBtn.onClick.RemoveAllListeners();
        privacyPolicyCloseBtn.onClick.AddListener(() => { OnClickPrivacyPolicyCloseBtn(); });
        privacyPolicyAgreeBtn.onClick.AddListener(() => { OnClickPrivacyPolicyAgreeBtn(); });
    }
    public void HidePrivacyPolicy()
    {
        privacyPolicyNode.SetActive(false);
    }
    public void OnClickPrivacyPolicyCloseBtn()
    {
        HidePrivacyPolicy();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void OnClickPrivacyPolicyAgreeBtn()
    {
        HidePrivacyPolicy();
        GFBuiltin.Setting.SetBool("PrivacyPolicyAgree", true);
    }
    public void HideLoadingProgress()
    {
        loadingProgressNode.SetActive(false);
        // loadingProgressNode_A.SetActive(false);
        privacyPolicyNode.SetActive(false);
    }

    public void ShowDialog(string title, string content, string yes_btn_title = "YES", string no_btn_title = "NO", UnityEngine.Events.UnityAction yes_cb = null, UnityEngine.Events.UnityAction no_cb = null)
    {
        tipsDialog.SetActive(true);
        if (yes_cb == null && no_cb == null)
        {
            yes_cb = HideDialog;
        }
        tipsNegativeBtn.gameObject.SetActive(no_cb != null);
        tipsNegativeBtn.GetComponentInChildren<Text>().text = no_btn_title;

        tipsPositiveBtn.gameObject.SetActive(yes_cb != null);
        tipsPositiveBtn.GetComponentInChildren<Text>().text = yes_btn_title;
        var dialog_bg = tipsDialog.transform.Find("DialogBG");
        tipsTitleText.text = title.ToUpper();
        tipsContentText.text = content;
        tipsNegativeBtn.onClick.RemoveAllListeners();
        tipsPositiveBtn.onClick.RemoveAllListeners();
        if (no_cb != null) tipsNegativeBtn.onClick.AddListener(() => { no_cb.Invoke(); HideDialog(); });
        if (yes_cb != null) tipsPositiveBtn.onClick.AddListener(() => { yes_cb.Invoke(); HideDialog(); });
    }

    public void HideDialog()
    {
        tipsDialog.SetActive(false);
    }
}
