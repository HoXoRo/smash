using UnityEngine;
using UnityEngine.UI;

[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
[AddComponentMenu("UI/MakeupRecordUIForm")]
public partial class MakeupRecordUIForm : UIFormBase
{
    [SerializeField] private MakeupRecordItem makeupRecordItemPrefab;

    MakeupRecordItem m_RecordItem;

    protected override void OnInit(object userData)
    {
        base.OnInit(userData);
        if (varBtnClose != null)
            varBtnClose.onClick.AddListener(OnClickClose);
    }

    protected override void OnOpen(object userData)
    {
        base.OnOpen(userData);
        EnsureRecordItem();
        ResetRecordScroll();
    }

    protected override void OnClose(bool isShutdown, object userData)
    {
        m_RecordItem = null;
        base.OnClose(isShutdown, userData);
    }

    void EnsureRecordItem()
    {
        if (makeupRecordItemPrefab == null || varList == null || varList.content == null)
            return;

        if (m_RecordItem != null)
            return;

        UIItemObject itemObject = SpawnItem<UIItemObject>(makeupRecordItemPrefab.gameObject, varList.content);
        m_RecordItem = (MakeupRecordItem)itemObject.itemLogic;
        m_RecordItem.transform.localScale = Vector3.one;
        m_RecordItem.transform.localRotation = Quaternion.identity;
        m_RecordItem.transform.localPosition = Vector3.zero;
        m_RecordItem.transform.SetAsLastSibling();
        m_RecordItem.gameObject.SetActive(true);
    }

    void ResetRecordScroll()
    {
        if (varList == null || varList.content == null)
            return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(varList.content);
        varList.StopMovement();
        varList.verticalNormalizedPosition = 1f;
    }
}
