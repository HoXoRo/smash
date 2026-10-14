using System;
using TMPro;
using UnityEngine;

namespace Util
{
	[ExecuteAlways]
	[RequireComponent(typeof(TMP_Text))]
	public class TMPEffects : MonoBehaviour
	{
		public enum PivotMode
		{
			Center = 0,
			BottomLeft = 1,
			BaselineCenter = 2
		}

		[SerializeField]
		[Header("Curvature (Optional)")]
		private bool enableCurvature = true;

		[SerializeField]
		private AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

		[SerializeField]
		private float curveStrength = 50f;

		[SerializeField]
		private float slopeSample = 0.0025f;

		[Header("Type Scale Reveal (Optional)")]
		[SerializeField]
		private bool enableTypeScale = true;

		[SerializeField]
		private float charDelay = 0.05f;

		[SerializeField]
		private float animDuration = 0.2f;

		[SerializeField]
		private AnimationCurve scaleCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

		[SerializeField]
		private PivotMode pivotMode;

		private TMP_Text _text;

		private TMP_MeshInfo[] _baselineMeshInfo;

		private bool _effectsDisabledDueToError;

		private bool _isRefreshing;

		private bool _isSubscribedToTextChanges;

		private bool _needsRefresh;

		private bool _isPlaying;

		private bool _isHidden;

		private float _startTime;

		private bool HasAnyEffect => enableCurvature || enableTypeScale;

		private void Awake()
		{
			_text = GetComponent<TMP_Text>();
			_isHidden = false;
		}

		private void OnEnable()
		{
			if (_text == null)
			{
				_text = GetComponent<TMP_Text>();
			}

			_effectsDisabledDueToError = false;
			EnsureTextChangedSubscription();
			_needsRefresh = true;
		}

		private void OnDisable()
		{
			RemoveTextChangedSubscription();
			_isRefreshing = false;
			_needsRefresh = false;
		}

		private void OnTextChanged(UnityEngine.Object changedObject)
		{
			if (changedObject == _text)
			{
				_needsRefresh = true;
			}
		}

		private void LateUpdate()
		{
			if (_effectsDisabledDueToError || _text == null)
			{
				return;
			}

			try
			{
				if (_needsRefresh)
				{
					RefreshMeshes();
					return;
				}

				if (_text.havePropertiesChanged)
				{
					RefreshMeshes();
					return;
				}

				if (_isPlaying)
				{
					if (_baselineMeshInfo == null || _baselineMeshInfo.Length == 0)
					{
						RefreshMeshes();
						return;
					}

					float num = Time.unscaledTime - _startTime;
					ApplyTypeScale(num);
					if (num >= GetTypeScaleDuration())
					{
						_isPlaying = false;
					}
				}
			}
			catch (Exception ex)
			{
				DisableEffectsAfterException(ex);
			}
		}

		public void RefreshMeshes()
		{
			if (_effectsDisabledDueToError || _isRefreshing)
			{
				return;
			}

			if (_text == null)
			{
				_text = GetComponent<TMP_Text>();
			}

			if (_text == null)
			{
				return;
			}

			if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
			{
				_needsRefresh = true;
				return;
			}

			try
			{
				_isRefreshing = true;
				_needsRefresh = false;
				_text.ForceMeshUpdate();
				TMP_TextInfo textInfo = _text.textInfo;
				if (textInfo == null || textInfo.meshInfo == null)
				{
					_baselineMeshInfo = null;
					return;
				}

				_baselineMeshInfo = textInfo.CopyMeshInfoVertexData();
				if (_baselineMeshInfo == null)
				{
					return;
				}

				if (enableCurvature)
				{
					ApplyCurvatureToCache(_baselineMeshInfo, textInfo);
				}

				ApplyImmediateState();
			}
			catch (Exception ex)
			{
				DisableEffectsAfterException(ex);
			}
			finally
			{
				_isRefreshing = false;
			}
		}

		public void PlayTypeScale()
		{
			if (!enableTypeScale)
			{
				ShowInstant();
				return;
			}

			_isHidden = false;
			_isPlaying = true;
			_startTime = Time.unscaledTime;
			ApplyImmediateState();
		}

		public void HideInstant()
		{
			_isPlaying = false;
			_isHidden = true;
			ApplyImmediateState();
		}

		public void ShowInstant()
		{
			_isPlaying = false;
			_isHidden = false;
			ApplyImmediateState();
		}

		public void SetText(string value)
		{
			if (_text == null)
			{
				_text = GetComponent<TMP_Text>();
			}

			if (_text == null)
			{
				return;
			}

			_text.SetText(value);
			RefreshMeshes();
		}

		public void SetTextAndHide(string value)
		{
			_isHidden = enableTypeScale;
			_isPlaying = false;
			SetText(value);
		}

		public float GetTypeScaleDuration()
		{
			if (!enableTypeScale || _text == null)
			{
				return 0f;
			}

			int characterCount = _text.textInfo != null ? _text.textInfo.characterCount : 0;
			if (characterCount <= 0)
			{
				return 0f;
			}

			return Mathf.Max(0f, animDuration) + Mathf.Max(0, characterCount - 1) * Mathf.Max(0f, charDelay);
		}

		private void ApplyImmediateState()
		{
			if (_baselineMeshInfo == null || _baselineMeshInfo.Length == 0)
			{
				return;
			}

			if (!HasAnyEffect)
			{
				return;
			}

			if (!enableTypeScale)
			{
				_isPlaying = false;
				CopyBaselineToOutput();
				PushToGeometry();
				return;
			}

			if (_isHidden)
			{
				ApplyTypeScale(0f, true);
				return;
			}

			if (_isPlaying)
			{
				ApplyTypeScale(Time.unscaledTime - _startTime);
				return;
			}

			CopyBaselineToOutput();
			PushToGeometry();
		}

		private void CopyBaselineToOutput()
		{
			if (_text == null || _baselineMeshInfo == null)
			{
				return;
			}

			TMP_TextInfo textInfo = _text.textInfo;
			for (int i = 0; i < textInfo.meshInfo.Length && i < _baselineMeshInfo.Length; i++)
			{
				Vector3[] vertices = textInfo.meshInfo[i].vertices;
				Vector3[] vertices2 = _baselineMeshInfo[i].vertices;
				if (vertices == null || vertices2 == null)
				{
					continue;
				}

				Array.Copy(vertices2, vertices, Mathf.Min(vertices.Length, vertices2.Length));
			}
		}

		private void ApplyCurvatureToCache(TMP_MeshInfo[] targetMeshInfo, TMP_TextInfo textInfo)
		{
			if (curve == null || targetMeshInfo == null || textInfo == null || textInfo.characterCount == 0)
			{
				return;
			}

			float num = float.PositiveInfinity;
			float num2 = float.NegativeInfinity;
			for (int i = 0; i < textInfo.characterCount; i++)
			{
				TMP_CharacterInfo tMP_CharacterInfo = textInfo.characterInfo[i];
				if (!tMP_CharacterInfo.isVisible)
				{
					continue;
				}

				num = Mathf.Min(num, tMP_CharacterInfo.bottomLeft.x);
				num2 = Mathf.Max(num2, tMP_CharacterInfo.topRight.x);
			}

			if (float.IsInfinity(num) || Mathf.Approximately(num, num2))
			{
				return;
			}

			float num3 = num2 - num;
			float num4 = Mathf.Max(0.0001f, slopeSample);
			for (int j = 0; j < textInfo.characterCount; j++)
			{
				TMP_CharacterInfo tMP_CharacterInfo2 = textInfo.characterInfo[j];
				if (!tMP_CharacterInfo2.isVisible)
				{
					continue;
				}

				int materialReferenceIndex = tMP_CharacterInfo2.materialReferenceIndex;
				int vertexIndex = tMP_CharacterInfo2.vertexIndex;
				Vector3[] vertices = targetMeshInfo[materialReferenceIndex].vertices;
				if (vertices == null || vertexIndex + 3 >= vertices.Length)
				{
					continue;
				}

				Vector3 vector = new Vector3((vertices[vertexIndex].x + vertices[vertexIndex + 2].x) * 0.5f, tMP_CharacterInfo2.baseLine, 0f);
				float num5 = Mathf.Clamp01((vector.x - num) / num3);
				float y = curve.Evaluate(num5) * curveStrength;
				float num6 = curve.Evaluate(Mathf.Clamp01(num5 - num4));
				float num7 = curve.Evaluate(Mathf.Clamp01(num5 + num4));
				float z = Mathf.Atan2((num7 - num6) * curveStrength, num4 * 2f * num3) * 57.29578f;
				Matrix4x4 matrix4x = Matrix4x4.TRS(new Vector3(0f, y, 0f), Quaternion.Euler(0f, 0f, z), Vector3.one);
				for (int k = 0; k < 4; k++)
				{
					vertices[vertexIndex + k] -= vector;
					vertices[vertexIndex + k] = matrix4x.MultiplyPoint3x4(vertices[vertexIndex + k]);
					vertices[vertexIndex + k] += vector;
				}
			}
		}

		private void ApplyTypeScale(float elapsed, bool forceAllZero = false)
		{
			if (_text == null || _baselineMeshInfo == null)
			{
				return;
			}

			CopyBaselineToOutput();
			TMP_TextInfo textInfo = _text.textInfo;
			float num = Mathf.Max(0.0001f, animDuration);
			for (int i = 0; i < textInfo.characterCount; i++)
			{
				TMP_CharacterInfo tMP_CharacterInfo = textInfo.characterInfo[i];
				if (!tMP_CharacterInfo.isVisible)
				{
					continue;
				}

				int materialReferenceIndex = tMP_CharacterInfo.materialReferenceIndex;
				int vertexIndex = tMP_CharacterInfo.vertexIndex;
				Vector3[] vertices = textInfo.meshInfo[materialReferenceIndex].vertices;
				Vector3[] vertices2 = _baselineMeshInfo[materialReferenceIndex].vertices;
				if (vertices == null || vertices2 == null || vertexIndex + 3 >= vertices.Length || vertexIndex + 3 >= vertices2.Length)
				{
					continue;
				}

				float num2 = forceAllZero ? 0f : Mathf.Clamp01((elapsed - i * charDelay) / num);
				float num3 = forceAllZero ? 0f : ((scaleCurve != null) ? scaleCurve.Evaluate(num2) : num2);
				Vector3 pivot = GetPivot(tMP_CharacterInfo, vertices2, vertexIndex);
				Matrix4x4 matrix4x = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(num3, num3, 1f));
				for (int j = 0; j < 4; j++)
				{
					Vector3 point = vertices2[vertexIndex + j] - pivot;
					vertices[vertexIndex + j] = matrix4x.MultiplyPoint3x4(point) + pivot;
				}
			}

			PushToGeometry();
		}

		private Vector3 GetPivot(TMP_CharacterInfo c, Vector3[] srcVerts, int vertIndex)
		{
			switch (pivotMode)
			{
			case PivotMode.BottomLeft:
				return srcVerts[vertIndex];
			case PivotMode.BaselineCenter:
				return new Vector3((srcVerts[vertIndex].x + srcVerts[vertIndex + 2].x) * 0.5f, c.baseLine, (srcVerts[vertIndex].z + srcVerts[vertIndex + 2].z) * 0.5f);
			default:
				return (srcVerts[vertIndex] + srcVerts[vertIndex + 2]) * 0.5f;
			}
		}

		private void PushToGeometry()
		{
			if (_text == null)
			{
				return;
			}

			TMP_TextInfo textInfo = _text.textInfo;
			for (int i = 0; i < textInfo.meshInfo.Length; i++)
			{
				TMP_MeshInfo tMP_MeshInfo = textInfo.meshInfo[i];
				if (tMP_MeshInfo.mesh == null || tMP_MeshInfo.vertices == null)
				{
					continue;
				}

				tMP_MeshInfo.mesh.vertices = tMP_MeshInfo.vertices;
				_text.UpdateGeometry(tMP_MeshInfo.mesh, i);
			}
		}

		private void DisableEffectsAfterException(Exception ex)
		{
			_effectsDisabledDueToError = true;
			_isPlaying = false;
			_isHidden = false;
			_isRefreshing = false;
			_needsRefresh = false;
			_baselineMeshInfo = null;
			enableCurvature = false;
			enableTypeScale = false;
			if (_text != null)
			{
				_text.ForceMeshUpdate();
			}
			Debug.LogException(ex, this);
		}

		private void EnsureTextChangedSubscription()
		{
			if (_isSubscribedToTextChanges)
			{
				return;
			}

			TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
			_isSubscribedToTextChanges = true;
		}

		private void RemoveTextChangedSubscription()
		{
			if (!_isSubscribedToTextChanges)
			{
				return;
			}

			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
			_isSubscribedToTextChanges = false;
		}
	}
}
