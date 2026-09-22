using System.Collections;
using System;
using TMPro;
using UnityEngine;
using UnityObject = UnityEngine.Object;

[RequireComponent(typeof(TMP_Text))]
[ExecuteAlways]
public class TMPCurver : MonoBehaviour
{
	public AnimationCurve curve = AnimationCurve.Linear(0f, 0f, 1f, 0f);

	public float curveStrength = 50f;

	public float slopeSample = 0.0025f;

	private TMP_Text _text;

	private bool _isApplying;

	private bool _applyQueued;

	private Coroutine _applyRoutine;

	private bool _disabledDueToError;

	private void OnEnable()
	{
		_text = GetComponent<TMP_Text>();
		_disabledDueToError = false;
		TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
		QueueApply();
	}

	private void OnDisable()
	{
		TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
		CancelPendingApply();
	}

	private void OnTextChanged(UnityObject changedObject)
	{
		if (changedObject == _text)
		{
			QueueApply();
		}
	}

	private void QueueApply()
	{
		if (_disabledDueToError)
		{
			return;
		}
		_applyQueued = true;
		if (!_isApplying && isActiveAndEnabled)
		{
			_isApplying = true;
			_applyRoutine = StartCoroutine(ApplyNextFrame());
		}
	}

	private IEnumerator ApplyNextFrame()
	{
		yield return null;
		_applyRoutine = null;
		ApplyQueued();
	}

	private void ApplyQueued()
	{
		_applyQueued = false;
		if (_disabledDueToError)
		{
			_isApplying = false;
			return;
		}
		if (this != null && _text != null)
		{
			try
			{
				ApplyWarp();
			}
			catch (Exception ex)
			{
				DisableAfterException(ex);
			}
		}
		_isApplying = false;
		if (_applyQueued && isActiveAndEnabled)
		{
			QueueApply();
		}
	}

	private void OnDestroy()
	{
		CancelPendingApply();
	}

	private void CancelPendingApply()
	{
		if (_applyRoutine != null)
		{
			StopCoroutine(_applyRoutine);
			_applyRoutine = null;
		}
		_isApplying = false;
		_applyQueued = false;
	}

	private void ApplyWarp()
	{
		if (curve == null || _text == null)
		{
			return;
		}
		_text.ForceMeshUpdate();
		TMP_TextInfo textInfo = _text.textInfo;
		if (textInfo == null || textInfo.characterCount == 0)
		{
			return;
		}
		float minX = float.PositiveInfinity;
		float maxX = float.NegativeInfinity;
		for (int i = 0; i < textInfo.characterCount; i++)
		{
			TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
			if (!charInfo.isVisible)
			{
				continue;
			}
			minX = Mathf.Min(minX, charInfo.bottomLeft.x);
			maxX = Mathf.Max(maxX, charInfo.topRight.x);
		}
		if (float.IsInfinity(minX) || Mathf.Approximately(maxX, minX))
		{
			return;
		}
		float width = maxX - minX;
		for (int i = 0; i < textInfo.characterCount; i++)
		{
			TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
			if (!charInfo.isVisible)
			{
				continue;
			}
			int vertexIndex = charInfo.vertexIndex;
			Vector3[] vertices = textInfo.meshInfo[charInfo.materialReferenceIndex].vertices;
			Vector3 midBaseline = new Vector3((vertices[vertexIndex].x + vertices[vertexIndex + 2].x) * 0.5f, charInfo.baseLine, 0f);
			float normalizedX = Mathf.Clamp01((midBaseline.x - minX) / width);
			float y = curve.Evaluate(normalizedX) * curveStrength;
			float sample = Mathf.Max(0.0001f, slopeSample);
			float previous = curve.Evaluate(Mathf.Clamp01(normalizedX - sample));
			float next = curve.Evaluate(Mathf.Clamp01(normalizedX + sample));
			float angle = Mathf.Atan2((next - previous) * curveStrength, sample * 2f * width) * Mathf.Rad2Deg;
			Matrix4x4 matrix = Matrix4x4.TRS(new Vector3(0f, y, 0f), Quaternion.Euler(0f, 0f, angle), Vector3.one);
			for (int j = 0; j < 4; j++)
			{
				vertices[vertexIndex + j] -= midBaseline;
				vertices[vertexIndex + j] = matrix.MultiplyPoint3x4(vertices[vertexIndex + j]);
				vertices[vertexIndex + j] += midBaseline;
			}
		}
		_text.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
	}

	private void DisableAfterException(Exception ex)
	{
		_disabledDueToError = true;
		CancelPendingApply();
		Debug.LogException(ex, this);
	}
}
