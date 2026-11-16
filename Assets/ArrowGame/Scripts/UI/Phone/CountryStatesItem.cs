using System;
using UnityEngine;
#if ENABLE_OBFUZ
[Obfuz.ObfuzIgnore(Obfuz.ObfuzScope.TypeName)]
#endif
public partial class CountryStatesItem : UIItemBase
{
    private int m_Index;
    private Action<int> m_OnSelected;
    protected override void OnInit()
    {
        base.OnInit();
        varBtnSelect.onClick.AddListener(OnSelect);
    }

    public void SetData(string label, int index, Action<int> onSelected)
    {
        varTxt.text = label;
        m_Index = index;
        m_OnSelected = onSelected;
    }

    private void OnSelect() => m_OnSelected?.Invoke(m_Index);
    private void OnDisable() => m_OnSelected = null;
}
