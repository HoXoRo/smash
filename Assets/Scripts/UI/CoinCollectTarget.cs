using System;
using System.Collections.Generic;
using Audio;
using DG.Tweening;
using Haptics;
using Service;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util;

namespace UI
{
	public class CoinCollectTarget : MonoBehaviour
	{
		private Vector3 _scaledBaseScale = Vector3.one;

		private bool _hasScaledBaseScale;

		private readonly List<Sequence> _activeSequences = new List<Sequence>();

		private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

		private int _playVersion;

		[SerializeField]
		private RectTransform targetTransform;

		[SerializeField]
		private GameObject coinCollectPrefab;

		[SerializeField]
		private GameObject amountTextPrefab;

		[SerializeField]
		private ParticleSystem hitParticle;

		[SerializeField]
		private RectTransform scaledTransform;

		[SerializeField]
		private AnimationCurve collectXCurve;

		[SerializeField]
		private AnimationCurve collectYCurve;

		public void Play(int count, float windowLength, int shownCount, Action onSingleHit, Action onLastHit)
		{
			ClearActiveEffects();
			int playVersion = ++_playVersion;
			RectTransform sourceTransform = GetComponent<RectTransform>();
			Canvas rootCanvas = sourceTransform != null ? sourceTransform.GetRootCanvas() : null;
			if (rootCanvas == null || coinCollectPrefab == null || targetTransform == null)
			{
				for (int i = 0; i < count; i++)
				{
					onSingleHit?.Invoke();
				}
				onLastHit?.Invoke();
				return;
			}

			Transform parent = rootCanvas.transform;
			Vector2 startPosition = GetCanvasPosition(rootCanvas, sourceTransform != null ? sourceTransform : targetTransform);
			Vector2 targetPosition = GetCanvasPosition(rootCanvas, targetTransform);
			int totalHits = Mathf.Max(0, count);
			int hitCount = 0;
			float delayStep = totalHits > 1 ? Mathf.Max(0f, windowLength) / totalHits : 0f;

			ServiceLocator.Get<AudioHelper>()?.PlaySfx(Audio.AudioType.CoinAppear);
			PlayAmountText(parent, startPosition, shownCount, playVersion);
			if (totalHits == 0)
			{
				onLastHit?.Invoke();
				return;
			}

			for (int i = 0; i < totalHits; i++)
			{
				GameObject coin = Instantiate(coinCollectPrefab, parent, false);
				RegisterSpawnedObject(coin);
				RectTransform coinTransform = coin.GetComponent<RectTransform>();
				if (coinTransform == null)
				{
					UnregisterSpawnedObject(coin);
					Destroy(coin);
					continue;
				}
				coinTransform.anchoredPosition = startPosition;
				coinTransform.localScale = Vector3.zero;

				float delay = i * delayStep;
				Vector2 burstOffset = UnityEngine.Random.insideUnitCircle * 100f;
				Vector2 burstPosition = startPosition + burstOffset;
				Vector2 collectStart = burstPosition;
				Sequence sequence = DOTween.Sequence();
				RegisterSequence(sequence);
				sequence.SetDelay(delay);
				sequence.Append(coinTransform.DOAnchorPos(burstPosition, 0.195f).SetEase(Ease.Linear));
				sequence.Insert(0f, coinTransform.DOScale(Vector3.one, 0.195f).SetEase(Ease.InCubic));
				sequence.AppendCallback(delegate
				{
					collectStart = coinTransform.anchoredPosition;
				});
				sequence.AppendInterval(0.005f);
				Tweener xTween = DOVirtual.Float(0f, 1f, 0.6f, delegate(float t)
				{
					Vector2 anchoredPosition = coinTransform.anchoredPosition;
					anchoredPosition.x = Mathf.LerpUnclamped(collectStart.x, targetPosition.x, t);
					coinTransform.anchoredPosition = anchoredPosition;
				});
				xTween.SetEase(collectXCurve != null && collectXCurve.length > 0 ? collectXCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f));
				sequence.Append(xTween);
				Tweener yTween = DOVirtual.Float(0f, 1f, 0.6f, delegate(float t)
				{
					Vector2 anchoredPosition = coinTransform.anchoredPosition;
					anchoredPosition.y = Mathf.LerpUnclamped(collectStart.y, targetPosition.y, t);
					coinTransform.anchoredPosition = anchoredPosition;
				});
				yTween.SetEase(collectYCurve != null && collectYCurve.length > 0 ? collectYCurve : AnimationCurve.Linear(0f, 0f, 1f, 1f));
				sequence.Join(yTween);
				sequence.AppendCallback(delegate
				{
					if (!IsPlayVersionActive(playVersion))
					{
						return;
					}
					Image image = coinTransform.GetComponent<Image>();
					if (image != null)
					{
						image.enabled = false;
					}
					hitParticle?.Play();
					PunchTarget();
					HapticManager.PlayEmphasis(0.45f, 0.55f);
					ServiceLocator.Get<AudioHelper>()?.PlaySfx(Audio.AudioType.CoinHit);
					onSingleHit?.Invoke();
					hitCount++;
					if (hitCount >= totalHits)
					{
						onLastHit?.Invoke();
					}
				});
				sequence.AppendInterval(0.05f);
				sequence.OnComplete(delegate
				{
					UnregisterSequence(sequence);
					UnregisterSpawnedObject(coin);
					if (coin != null)
					{
						Destroy(coin);
					}
				});
				sequence.OnKill(delegate
				{
					UnregisterSequence(sequence);
				});
			}
		}

		private void PlayAmountText(Transform parent, Vector2 startPosition, int shownCount, int playVersion)
		{
			if (amountTextPrefab == null || shownCount <= 0)
			{
				return;
			}
			GameObject amountObject = Instantiate(amountTextPrefab, parent, false);
			RegisterSpawnedObject(amountObject);
			RectTransform amountTransform = amountObject.GetComponent<RectTransform>();
			TextMeshProUGUI text = amountObject.GetComponent<TextMeshProUGUI>();
			if (amountTransform != null)
			{
				amountTransform.anchoredPosition = startPosition;
				amountTransform.localScale = Vector3.zero;
			}
			if (text != null)
			{
				text.text = string.Format("+{0}", shownCount);
				text.alpha = 1f;
			}
			Sequence sequence = DOTween.Sequence();
			RegisterSequence(sequence);
			if (amountTransform != null)
			{
				sequence.Join(amountTransform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
				sequence.Join(amountTransform.DOAnchorPosY(-153f, 2f).SetEase(Ease.Linear));
			}
			if (text != null)
			{
				sequence.Insert(1.2f, text.DOFade(0f, 0.3f));
			}
			sequence.OnComplete(delegate
			{
				UnregisterSequence(sequence);
				UnregisterSpawnedObject(amountObject);
				if (amountObject != null)
				{
					Destroy(amountObject);
				}
			});
			sequence.OnKill(delegate
			{
				UnregisterSequence(sequence);
				if (!IsPlayVersionActive(playVersion))
				{
					UnregisterSpawnedObject(amountObject);
				}
			});
		}

		private void PunchTarget()
		{
			if (scaledTransform == null)
			{
				return;
			}
			EnsureScaledBaseScale();
			scaledTransform.DOKill();
			scaledTransform.localScale = _scaledBaseScale;
			scaledTransform.DOBlendableScaleBy(Vector3.one * 0.1f, 0.05f).SetEase(Ease.OutCubic).OnComplete(delegate
			{
				if (scaledTransform != null)
				{
					scaledTransform.DOBlendableScaleBy(Vector3.one * -0.1f, 0.05f).SetEase(Ease.InCubic);
				}
			});
		}

		private void OnDisable()
		{
			ClearActiveEffects();
			ResetTargetScale();
		}

		private void OnDestroy()
		{
			ClearActiveEffects();
			ResetTargetScale();
		}

		private void ClearActiveEffects()
		{
			_playVersion++;
			for (int i = _activeSequences.Count - 1; i >= 0; i--)
			{
				_activeSequences[i]?.Kill();
			}
			_activeSequences.Clear();
			for (int j = _spawnedObjects.Count - 1; j >= 0; j--)
			{
				GameObject spawnedObject = _spawnedObjects[j];
				if (spawnedObject != null)
				{
					Destroy(spawnedObject);
				}
			}
			_spawnedObjects.Clear();
		}

		private bool IsPlayVersionActive(int playVersion)
		{
			return this != null && isActiveAndEnabled && playVersion == _playVersion;
		}

		private void RegisterSequence(Sequence sequence)
		{
			if (sequence != null)
			{
				_activeSequences.Add(sequence);
			}
		}

		private void UnregisterSequence(Sequence sequence)
		{
			if (sequence != null)
			{
				_activeSequences.Remove(sequence);
			}
		}

		private void RegisterSpawnedObject(GameObject spawnedObject)
		{
			if (spawnedObject != null)
			{
				_spawnedObjects.Add(spawnedObject);
			}
		}

		private void UnregisterSpawnedObject(GameObject spawnedObject)
		{
			if (spawnedObject != null)
			{
				_spawnedObjects.Remove(spawnedObject);
			}
		}

		private void EnsureScaledBaseScale()
		{
			if (!_hasScaledBaseScale && scaledTransform != null)
			{
				_scaledBaseScale = scaledTransform.localScale;
				_hasScaledBaseScale = true;
			}
		}

		private void ResetTargetScale()
		{
			if (scaledTransform == null)
			{
				return;
			}
			EnsureScaledBaseScale();
			scaledTransform.DOKill();
			scaledTransform.localScale = _scaledBaseScale;
		}

		private static Vector2 GetCanvasPosition(Canvas rootCanvas, RectTransform rectTransform)
		{
			Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(rootCanvas.worldCamera, rectTransform.position);
			RectTransform canvasTransform = rootCanvas.transform as RectTransform;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasTransform, screenPoint, rootCanvas.worldCamera, out Vector2 localPoint);
			return localPoint;
		}
	}
}
