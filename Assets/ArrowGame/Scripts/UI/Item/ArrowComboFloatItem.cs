using System;
using DG.Tweening;
using GameFramework;
using UnityEngine;
using UnityGameFramework.Runtime;

public partial class ArrowComboFloatItem : UIItemBase
{
    const float ScaleUp = 1.45f;
    const float AnimDuration = 0.2f;

    RectTransform m_RectTransform;
    Sequence m_AnimationSequence;
    Action m_OnComplete;

    protected override void OnInit()
    {
        base.OnInit();
        m_RectTransform = transform as RectTransform;
        if (varTxtCombo == null)
        {
            var texts = GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i].name == "txtCombo" || texts[i].name == "TxtCombo" || texts[i].name == "txtCambo")
                {
                    varTxtCombo = texts[i];
                    break;
                }
            }
        }
    }

    public void Play(int comboCount, Vector2 anchoredPosition, Action onComplete)
    {
        StopAnimation();
        m_OnComplete = onComplete;

        if (m_RectTransform != null)
        {
            m_RectTransform.anchoredPosition = anchoredPosition;
            m_RectTransform.localScale = Vector3.one;
        }

        if (varTxtCombo != null)
        {
            varTxtCombo.text = Utility.Text.Format("Combo {0}", comboCount);
            varTxtCombo.gameObject.SetActive(true);
        }

        Transform animTarget = m_RectTransform != null ? m_RectTransform : transform;
        m_AnimationSequence = DOTween.Sequence();
        m_AnimationSequence.Append(
            animTarget.DOScale(ScaleUp, AnimDuration).SetEase(Ease.OutBack).SetUpdate(true));
        m_AnimationSequence.Append(
            animTarget.DOScale(1f, AnimDuration).SetEase(Ease.InOutSine).SetUpdate(true));
        m_AnimationSequence.AppendCallback(OnAnimationFinished);
        m_AnimationSequence.SetUpdate(true);
    }

    void OnAnimationFinished()
    {
        var callback = m_OnComplete;
        m_OnComplete = null;
        callback?.Invoke();
    }

    public void StopAnimation()
    {
        if (m_AnimationSequence != null && m_AnimationSequence.IsActive())
            m_AnimationSequence.Kill();

        m_AnimationSequence = null;
        m_OnComplete = null;
        DOTween.Kill(transform);

        if (m_RectTransform != null)
            m_RectTransform.localScale = Vector3.one;
    }

    void OnDisable()
    {
        StopAnimation();
    }
}
