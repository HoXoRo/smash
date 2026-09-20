using DG.Tweening;
using TMPro;
using UnityEngine;

namespace ArrowMaze
{
    /// <summary> 西瓜环路西瓜（Entity 系统，参数为 <see cref="MelonPathWatermelonEntityParams"/>） </summary>
    public class MelonPathWatermelonEntity : EntityBase
    {
        public const string MelonImageNodeName = "MelonImage";
        public const string HpLabelNodeName = "HpLabel";
        public const string HpFloatLabelNodeName = "HpFloatLabel";
        public const string DestroyFxNodeName = "fx_xigua";

        const float HpFloatDurationSec = 0.5f;
        const float HpFloatRiseLocalY = 0.55f;
        const float DefaultDestroyFxDurationSec = 1.5f;

        public Transform MelonImageTransform { get; private set; }

        Transform m_HpFloatLabelTransform;
        Transform m_DestroyFxRoot;
        TMP_Text m_HpFloatLabelTmp;
        Vector3 m_HpFloatLabelRestLocalPos;
        Tween m_HpFloatTween;

        /// <summary>
        /// <see cref="WatermelonPathExtension"/> 在关结束时会 <c>HideEntity</c> 并立刻 <c>Destroy(m_Root)</c>。
        /// 若实体仍挂在该根下，回收队列下一轮 <c>OnRecycle</c> 时物体可能已被销毁。隐藏时须先挂回对象池父节点。
        /// </summary>
        Transform m_AttachRestoreParent;
        bool m_HasAttachRestoreParent;

        protected override void OnShow(object userData)
        {
            base.OnShow(userData);
            MelonImageTransform = CachedTransform != null
                ? CachedTransform.Find(MelonImageNodeName)
                : null;
            EnsureHpFloatLabel();
            EnsureDestroyFx();
            if (m_DestroyFxRoot != null)
                m_DestroyFxRoot.gameObject.SetActive(false);
            if (Params is MelonPathWatermelonEntityParams melonParams)
            {
                m_HasAttachRestoreParent = false;
                m_AttachRestoreParent = null;
                if (melonParams.AttachParent != null && CachedTransform != null)
                {
                    m_AttachRestoreParent = CachedTransform.parent;
                    m_HasAttachRestoreParent = true;
                    CachedTransform.SetParent(melonParams.AttachParent, true);
                }

                melonParams.OnWatermelonShown?.Invoke(this);
            }
        }

        protected override void OnHide(bool isShutdown, object userData)
        {
            StopHpFloatLabelTween();
            StopDestroyFx();
            if (m_HasAttachRestoreParent && CachedTransform != null && m_AttachRestoreParent != null)
            {
                CachedTransform.SetParent(m_AttachRestoreParent, false);
                m_HasAttachRestoreParent = false;
                m_AttachRestoreParent = null;
            }

            base.OnHide(isShutdown, userData);
        }

        /// <summary> 生命归零：隐藏西瓜/文本并播放 fx_xigua，返回特效预估时长（秒）。 </summary>
        public float PlayDestroyFxAndHideVisuals()
        {
            HideGameplayVisuals();
            return PlayDestroyFx();
        }

        public void HideGameplayVisuals()
        {
            HideHpFloatLabel();
            if (MelonImageTransform != null)
                MelonImageTransform.gameObject.SetActive(false);

            if (CachedTransform == null) return;
            var hpLabel = CachedTransform.Find(HpLabelNodeName);
            if (hpLabel != null)
                hpLabel.gameObject.SetActive(false);
        }

        void EnsureDestroyFx()
        {
            if (m_DestroyFxRoot != null || CachedTransform == null) return;
            m_DestroyFxRoot = CachedTransform.Find(DestroyFxNodeName);
        }

        float PlayDestroyFx()
        {
            EnsureDestroyFx();
            if (m_DestroyFxRoot == null) return DefaultDestroyFxDurationSec;

            m_DestroyFxRoot.gameObject.SetActive(true);
            float maxDuration = DefaultDestroyFxDurationSec;
            var particleSystems = m_DestroyFxRoot.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                if (ps == null) continue;
                ps.gameObject.SetActive(true);
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.Play(true);
                var main = ps.main;
                float lifeMax = main.startLifetime.mode == ParticleSystemCurveMode.TwoConstants
                    ? main.startLifetime.constantMax
                    : main.startLifetime.constant;
                maxDuration = Mathf.Max(maxDuration, main.duration + lifeMax);
            }

            return maxDuration;
        }

        void StopDestroyFx()
        {
            if (m_DestroyFxRoot == null) return;
            var particleSystems = m_DestroyFxRoot.GetComponentsInChildren<ParticleSystem>(true);
            foreach (var ps in particleSystems)
            {
                if (ps != null)
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }

            m_DestroyFxRoot.gameObject.SetActive(false);
        }

        /// <summary> 显示扣血飘字：自下而上飘出，1 秒后隐藏。 </summary>
        public void PlayHpLossFloat(int damage)
        {
            if (damage <= 0) return;
            EnsureHpFloatLabel();
            if (m_HpFloatLabelTransform == null || m_HpFloatLabelTmp == null) return;

            StopHpFloatLabelTween();
            m_HpFloatLabelTransform.localPosition = m_HpFloatLabelRestLocalPos;
            m_HpFloatLabelTmp.text = $"-{damage}";
            m_HpFloatLabelTmp.alpha = 1f;
            m_HpFloatLabelTransform.gameObject.SetActive(true);

            float targetY = m_HpFloatLabelRestLocalPos.y + HpFloatRiseLocalY;
            m_HpFloatTween = DOTween.Sequence()
                .Join(m_HpFloatLabelTransform.DOLocalMoveY(targetY, HpFloatDurationSec).SetEase(Ease.OutQuad))
                .Join(m_HpFloatLabelTmp.DOFade(0f, HpFloatDurationSec).SetEase(Ease.InQuad))
                .OnComplete(HideHpFloatLabel)
                .SetTarget(m_HpFloatLabelTransform);
        }

        void EnsureHpFloatLabel()
        {
            if (m_HpFloatLabelTransform != null && m_HpFloatLabelTmp != null) return;
            if (CachedTransform == null) return;

            m_HpFloatLabelTransform = CachedTransform.Find(HpFloatLabelNodeName);
            if (m_HpFloatLabelTransform == null) return;

            m_HpFloatLabelRestLocalPos = m_HpFloatLabelTransform.localPosition;
            m_HpFloatLabelTmp = m_HpFloatLabelTransform.GetComponent<TextMeshPro>();
            if (m_HpFloatLabelTmp == null)
                m_HpFloatLabelTmp = m_HpFloatLabelTransform.GetComponentInChildren<TextMeshPro>(true);
            if (m_HpFloatLabelTmp == null)
            {
                m_HpFloatLabelTmp = m_HpFloatLabelTransform.gameObject.AddComponent<TextMeshPro>();
                if (TMP_Settings.defaultFontAsset != null)
                    m_HpFloatLabelTmp.font = TMP_Settings.defaultFontAsset;
                m_HpFloatLabelTmp.fontSize = 7f;
                m_HpFloatLabelTmp.alignment = TextAlignmentOptions.Center;
                m_HpFloatLabelTmp.color = new Color(0.95f, 0.25f, 0.2f);
                // m_HpFloatLabelTmp.sortingOrder = 12;
            }
            else if (m_HpFloatLabelTmp.font == null && TMP_Settings.defaultFontAsset != null)
            {
                m_HpFloatLabelTmp.font = TMP_Settings.defaultFontAsset;
            }

            HideHpFloatLabel();
        }

        void StopHpFloatLabelTween()
        {
            if (m_HpFloatTween == null) return;
            m_HpFloatTween.Kill();
            m_HpFloatTween = null;
        }

        void HideHpFloatLabel()
        {
            StopHpFloatLabelTween();
            if (m_HpFloatLabelTransform != null)
            {
                m_HpFloatLabelTransform.localPosition = m_HpFloatLabelRestLocalPos;
                m_HpFloatLabelTransform.gameObject.SetActive(false);
            }

            if (m_HpFloatLabelTmp != null)
                m_HpFloatLabelTmp.alpha = 1f;
        }
    }
}
