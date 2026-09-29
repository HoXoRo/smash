using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityGameFramework.Runtime;

#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
[AddComponentMenu("UI/MakeupUIForm")]
public partial class MakeupUIForm : UIFormBase
{
    [SerializeField] private MakeupItem makeupItemPrefab;
    [SerializeField] private ScrollRect makeupScrollRect;

    readonly List<MakeupItem> makeupItems = new List<MakeupItem>();

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (varBtnClose != null)
            varBtnClose.onClick.AddListener(OnClickClose);
        if (varBtnInput != null)
            varBtnInput.onClick.AddListener(OnClickInput);
        if (varBtnRecord != null)
            varBtnRecord.onClick.AddListener(OnClickRecord);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        RefreshGemText();
        RefreshPcardText();
        RefreshAccountText();
        RefreshMakeupList();
        ResetMakeupScroll();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        makeupItems.Clear();
        base.OnClose(isShutdown, userData);
    }

    void OnClickInput()
    {
        if (GF.UI.HasUIForm(UIViews.MakeupInfoUIForm) || GF.UI.IsLoadingUIForm(UIViews.MakeupInfoUIForm))
            return;

        GF.Sound.PlayEffect("ui/ui_click.mp3");
        GF.UI.OpenUIForm(UIViews.MakeupInfoUIForm);
    }

    void OnClickRecord()
    {
        if (GF.UI.HasUIForm(UIViews.MakeupRecordUIForm) || GF.UI.IsLoadingUIForm(UIViews.MakeupRecordUIForm))
            return;

        GF.Sound.PlayEffect("ui/ui_click.mp3");
        GF.UI.OpenUIForm(UIViews.MakeupRecordUIForm);
    }

    void RefreshMakeupList()
    {
        var table = GF.DataTable.GetDataTable<MakeupTask>();
        if (table == null)
        {
            Log.Error("MakeupUIForm: MakeupTask data table is not loaded.");
            RefreshMakeupItems(0);
            return;
        }

        MakeupTask[] tasks = table.GetAllDataRows();
        System.Array.Sort(tasks, (left, right) => left.Id.CompareTo(right.Id));
        RefreshMakeupItems(tasks.Length);
        for (int index = 0; index < makeupItems.Count; index++)
        {
            makeupItems[index].SetData(tasks[index]);
        }
    }

    void RefreshMakeupItems(int itemCount)
    {
        if (makeupItemPrefab == null || makeupScrollRect == null || makeupScrollRect.content == null)
            return;

        itemCount = Mathf.Max(0, itemCount);
        for (int index = makeupItems.Count - 1; index >= itemCount; index--)
        {
            UnspawnItem<UIItemObject>(makeupItemPrefab.gameObject, makeupItems[index].gameObject);
            makeupItems.RemoveAt(index);
        }

        while (makeupItems.Count < itemCount)
        {
            UIItemObject itemObject = SpawnItem<UIItemObject>(makeupItemPrefab.gameObject, makeupScrollRect.content);
            MakeupItem makeupItem = (MakeupItem)itemObject.itemLogic;
            makeupItem.transform.localScale = Vector3.one;
            makeupItem.transform.localRotation = Quaternion.identity;
            makeupItem.transform.localPosition = Vector3.zero;
            makeupItem.transform.SetAsLastSibling();
            makeupItem.gameObject.SetActive(true);
            makeupItems.Add(makeupItem);
        }
    }

    void ResetMakeupScroll()
    {
        if (makeupScrollRect == null || makeupScrollRect.content == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(makeupScrollRect.content);
        makeupScrollRect.StopMovement();
        makeupScrollRect.verticalNormalizedPosition = 1f;
    }

    void RefreshGemText()
    {
        if (varTxtGem == null)
            return;

        PlayerDataModel playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        varTxtGem.text = CommonHelper.GetDollarString(playerDm.Dollars);
    }

    void RefreshPcardText()
    {
        if (varTxtPcard == null)
            return;

        PlayerDataModel playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        varTxtPcard.text = CommonHelper.GetDollarString(playerDm.Pcard);
    }

    public void RefreshAccountText()
    {
        if (varTxtAccount == null)
            return;

        PlayerDataModel playerDm = GF.DataModel.GetOrCreate<PlayerDataModel>();
        varTxtAccount.text = playerDm.MakeupAccount ?? string.Empty;
    }
}
