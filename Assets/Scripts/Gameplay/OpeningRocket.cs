using System;
using System.Collections;
using System.Collections.Generic;
using Audio;
using Gameplay.Camera;
using Gameplay.Collisions;
using Gameplay.Objects;
using Gameplay.Particles;
using Service;
using SRDebugger;
using UnityEngine;

namespace Gameplay
{
	public class OpeningRocket : MonoBehaviour
	{
		private const float HitProximitySqr = 0.25f;

		private const float FirstHitBoomPower = 3f;

		private const int FrontRenderQueue = 3500;

		private const int FrontSortingOrder = 80;

		private const float ExplodeParticleCameraDistance = 2f;

		private const string FrontSortingLayerName = "UI";

		[SROption]
		public static float WaitAfterQuadraticSeconds;

		private const int ArcLengthLutSegments = 64;

		[SROption]
		public static float peakScale;

		[SROption]
		public static float quadraticLegDuration;

		[SROption]
		public static float quarticLegDuration;

		[SROption]
		public static float quadraticRotateSpeed;

		[SROption]
		public static float waitRotateSpeedStart;

		[SROption]
		public static float waitRotateSpeedEnd;

		[SROption]
		public static float quarticRotateSpeed;

		[SROption]
		public static bool ShouldRocketsSync;

		[SerializeField]
		private GameObject rocketVisual;

		[SerializeField]
		private ParticleSystem[] flyParticles;

		[SerializeField]
		private ParticleSystem[] starParticles;

		[SerializeField]
		private GameObject explodeParticle;

		[SerializeField]
		private MeshRenderer[] rendererParts;

		[SROption]
		public static float waitBobOffset;

		[SROption]
		public static float bobDescentDuration;

		private Func<BaseObject> _targetSelector;

		private BaseObject _targetObject;

		private Vector3 _targetPoint;

		private bool _completed;

		private float _durationQuadratic;

		private float _durationWait;

		private float _durationStraight;

		private Coroutine _flightCoroutine;

		private Quaternion _spinOffset;

		private float _lastQuadraticPathZ;

		private int[] _originalRenderQueues;

		private string[] _originalSortingLayerNames;

		private int[] _originalSortingOrders;

		private int[] _originalFlyParticleRenderQueues;

		private string[] _originalFlyParticleSortingLayerNames;

		private int[] _originalFlyParticleSortingOrders;

		private int[] _originalStarParticleRenderQueues;

		private string[] _originalStarParticleSortingLayerNames;

		private int[] _originalStarParticleSortingOrders;

		private Vector3 _worldPathStart;

		private Vector3 _quadraticControl;

		private Vector3 _quadraticEnd;

		private float _quadraticArcLength;

		private float[] _quadArcU;

		private float[] _quadArcCumulative;

		private Vector3 _quarticSegmentStart;

		private Vector3 _quarticP0;

		private Vector3 _quarticP1;

		private Vector3 _quarticP2;

		private Vector3 _quarticP3;

		private Vector3 _quarticP4;

		private bool _quarticTargetIsRight;

		private float _quarticP2XOffset;

		private float _quarticP3OffsetX;

		private float _quarticP3OffsetY;

		private float _quarticArcLength;

		private float[] _quarticArcU;

		private float[] _quarticArcCumulative;

		private int _rocketIndex;

		private List<int> _rocketIndexesWithAudio;

		public bool IsComplete => _completed;

		public static Quaternion RotationWithUpAlong(Vector3 travelDirection)
		{
			if (travelDirection.sqrMagnitude <= 1E-05f)
			{
				return Quaternion.identity;
			}
			return Quaternion.FromToRotation(Vector3.up, travelDirection.normalized);
		}

		public void BeginFlight(int index, int totalCount, Func<BaseObject> targetSelector)
		{
			StopFlightIfRunning();
			_targetSelector = targetSelector;
			_targetObject = null;
			_completed = false;
			_rocketIndex = index;
			_durationQuadratic = Mathf.Max(0.2f, quadraticLegDuration);
			_durationWait = Mathf.Max(0f, WaitAfterQuadraticSeconds);
			_durationStraight = Mathf.Max(0.3f, quarticLegDuration);
			_spinOffset = Quaternion.identity;
			_rocketIndexesWithAudio = new List<int>();
			switch (totalCount)
			{
				case 2:
					_rocketIndexesWithAudio.Add(0);
					_rocketIndexesWithAudio.Add(1);
					break;
				case 3:
					_rocketIndexesWithAudio.Add(0);
					_rocketIndexesWithAudio.Add(1);
					_rocketIndexesWithAudio.Add(2);
					break;
				case 4:
					_rocketIndexesWithAudio.Add(0);
					_rocketIndexesWithAudio.Add(1);
					_rocketIndexesWithAudio.Add(2);
					_rocketIndexesWithAudio.Add(3);
					break;
				case 5:
					_rocketIndexesWithAudio.Add(0);
					_rocketIndexesWithAudio.Add(1);
					_rocketIndexesWithAudio.Add(3);
					_rocketIndexesWithAudio.Add(4);
					break;
				case 6:
					_rocketIndexesWithAudio.Add(0);
					_rocketIndexesWithAudio.Add(2);
					_rocketIndexesWithAudio.Add(4);
					_rocketIndexesWithAudio.Add(5);
					break;
				default:
					_rocketIndexesWithAudio.Add(0);
					break;
			}
			CacheOriginalRenderState();
			ApplyFrontRenderState();
			ConfigureLaunchPath(index, totalCount);
			if (!ApplyDeferredTarget())
			{
				return;
			}
			_flightCoroutine = StartCoroutine(FlightRoutine());
		}

		private void OnDestroy()
		{
			CleanupFlightState();
		}

		private void OnDisable()
		{
			CleanupFlightState();
		}

		private void StopFlightIfRunning()
		{
			if (_flightCoroutine != null)
			{
				StopCoroutine(_flightCoroutine);
				_flightCoroutine = null;
			}
		}

		private void CleanupFlightState()
		{
			_completed = true;
			StopFlightIfRunning();
			StopParticles(flyParticles);
			StopParticles(starParticles);
			if (rocketVisual != null)
			{
				rocketVisual.SetActive(false);
			}
			RestoreOriginalRenderState();
		}

		private IEnumerator FlightRoutine()
		{
			if (_rocketIndexesWithAudio != null && _rocketIndexesWithAudio.Contains(_rocketIndex))
			{
				ServiceLocator.Get<AudioHelper>()?.PlaySfx(Audio.AudioType.RocketIntro);
			}
			rocketVisual?.SetActive(true);
			PlayParticles(flyParticles);
			PlayParticles(starParticles);
			yield return RunQuadraticSegment();
			if (_completed)
			{
				yield break;
			}
			yield return RunWaitSegment();
			if (_completed)
			{
				yield break;
			}
			yield return RunQuarticSegment();
			if (!_completed)
			{
				ArriveAndExplode();
			}
		}

		private IEnumerator RunQuadraticSegment()
		{
			float peak = Mathf.Max(peakScale, 0.0001f);
			BuildQuadraticArcLengthLut(_worldPathStart, _quadraticControl, _quadraticEnd, out _quadraticArcLength, out _quadArcU, out _quadArcCumulative);
			float elapsed = 0f;
			while (!_completed && elapsed < _durationQuadratic)
			{
				elapsed += Time.deltaTime;
				float normalized = Mathf.Clamp01(elapsed / _durationQuadratic);
				float u = SampleQuadraticU(normalized);
				Vector3 position = QuadraticBezierPoint(_worldPathStart, _quadraticControl, _quadraticEnd, u);
				Vector3 derivative = QuadraticBezierDerivative(_worldPathStart, _quadraticControl, _quadraticEnd, u);
				Quaternion deltaSpin = Quaternion.AngleAxis(quadraticRotateSpeed * Time.deltaTime, Vector3.up);
				_spinOffset *= deltaSpin;
				transform.position = position;
				transform.rotation = RotationWithUpAlong(derivative) * _spinOffset;
				transform.localScale = Vector3.one * (1f + normalized * (peak - 1f));
				_lastQuadraticPathZ = position.z;
				yield return null;
			}
			transform.position = _quadraticEnd;
			transform.localScale = Vector3.one * peak;
		}

		private void ConfigureLaunchPath(int index, int totalCount)
		{
			float startX = -5f;
			float endX = 0f;
			float sharedZ = -15f;
			if (totalCount == 2)
			{
				float direction = index % 2 == 0 ? -1f : 1f;
				startX = direction * 5f;
				endX = direction * 0.5f;
			}
			else if (totalCount == 3)
			{
				int safeIndex = Mathf.Clamp(index, 0, 2);
				startX = safeIndex % 2 == 0 ? -5f : 5f;
				endX = new float[3] { -0.5f, 0.5f, 0f }[safeIndex];
				sharedZ = new float[3] { -15f, -15f, -16f }[safeIndex];
			}
			else if (totalCount == 4)
			{
				int safeIndex = Mathf.Clamp(index, 0, 3);
				startX = safeIndex % 2 == 0 ? -5f : 5f;
				endX = new float[4] { 0f, 0.5f, -0.5f, 0f }[safeIndex];
				sharedZ = new float[4] { -15f, -16f, -16f, -17f }[safeIndex];
			}
			else if (totalCount == 5)
			{
				int safeIndex = Mathf.Clamp(index, 0, 4);
				startX = safeIndex % 2 == 0 ? -5f : 5f;
				endX = new float[5] { 0f, 1f, -1f, 0.5f, -0.5f }[safeIndex];
				sharedZ = new float[5] { -15f, -15f, -15f, -16f, -16f }[safeIndex];
			}
			else if (totalCount >= 6)
			{
				int safeIndex = Mathf.Clamp(index, 0, 5);
				startX = safeIndex % 2 == 0 ? -5f : 5f;
				endX = new float[6] { 0f, 1f, -1f, 0.5f, -0.5f, 0f }[safeIndex];
				sharedZ = new float[6] { -15f, -15f, -15f, -16f, -16f, -17f }[safeIndex];
			}
			_worldPathStart = new Vector3(startX, 1f, sharedZ);
			_quadraticEnd = new Vector3(endX, 3.390625f, sharedZ);
			_quadraticControl = _quadraticEnd - Vector3.up * 2.5f;
			transform.position = _worldPathStart;
			transform.rotation = RotationWithUpAlong((_quadraticControl - _worldPathStart) * 2f);
			transform.localScale = Vector3.one;
		}

		private IEnumerator RunWaitSegment()
		{
			Vector3 basePosition = transform.position;
			float elapsed = 0f;
			while (!_completed && elapsed < _durationWait)
			{
				elapsed += Time.deltaTime;
				if (elapsed > 0f)
				{
					float bobProgress = bobDescentDuration > 0f ? Mathf.Clamp01(elapsed / bobDescentDuration) : 1f;
					float bob = Mathf.SmoothStep(0f, 1f, bobProgress) * waitBobOffset;
					transform.position = basePosition + Vector3.down * bob;
				}
				float normalized = _durationWait > 0f ? Mathf.Clamp01(elapsed / _durationWait) : 1f;
				float easedNormalized = 1f - (1f - normalized) * (1f - normalized);
				float rotateSpeed = Mathf.Lerp(waitRotateSpeedStart, waitRotateSpeedEnd, easedNormalized);
				Quaternion deltaSpin = Quaternion.AngleAxis(rotateSpeed * Time.deltaTime, Vector3.up);
				_spinOffset *= deltaSpin;
				transform.rotation *= deltaSpin;
				yield return null;
			}
		}

		private IEnumerator RunQuarticSegment()
		{
			if (!TargetStillValid() && !ApplyDeferredTarget())
			{
				yield break;
			}
			ServiceLocator.Get<AudioHelper>()?.PlaySfx(Audio.AudioType.RocketFlight);
			_quarticSegmentStart = transform.position;
			float elapsed = 0f;
			while (!_completed && elapsed < _durationStraight)
			{
				if (!TargetStillValid() && !ApplyDeferredTarget())
				{
					yield break;
				}
				UpdateTargetPoint();
				UpdateQuarticControlPoints(_quarticSegmentStart, _targetPoint);
				BuildQuarticArcLengthLut(_quarticP0, _quarticP1, _quarticP2, _quarticP3, _quarticP4, out _quarticArcLength, out _quarticArcU, out _quarticArcCumulative);
				elapsed += Time.deltaTime;
				float normalized = Mathf.Clamp01(elapsed / _durationStraight);
				float u = SampleQuarticU(normalized);
				Vector3 position = QuarticBezierPoint(_quarticP0, _quarticP1, _quarticP2, _quarticP3, _quarticP4, u);
				Vector3 derivative = QuarticBezierDerivative(_quarticP0, _quarticP1, _quarticP2, _quarticP3, _quarticP4, u);
				transform.position = position;
				transform.rotation = RotationWithUpAlong(derivative) * Quaternion.AngleAxis(elapsed * quarticRotateSpeed, Vector3.up);
				if ((position - _targetPoint).sqrMagnitude <= HitProximitySqr)
				{
					ArriveAndExplode();
					yield break;
				}
				yield return null;
			}
		}

		private bool TargetStillValid()
		{
			return _targetObject != null && !_targetObject.BeingDestroyed;
		}

		private float SampleQuadraticU(float tNorm)
		{
			if (_quadArcU == null || _quadArcCumulative == null || _quadraticArcLength <= 0f)
			{
				return Mathf.Clamp01(tNorm);
			}
			return ArcLengthToU(Mathf.Clamp01(tNorm) * _quadraticArcLength, _quadArcU, _quadArcCumulative);
		}

		private float SampleQuarticU(float tNorm)
		{
			if (_quarticArcU == null || _quarticArcCumulative == null || _quarticArcLength <= 0f)
			{
				return Mathf.Clamp01(tNorm);
			}
			return ArcLengthToU(Mathf.Clamp01(tNorm) * _quarticArcLength, _quarticArcU, _quarticArcCumulative);
		}

		private static void BuildQuadraticArcLengthLut(Vector3 p0, Vector3 p1, Vector3 p2, out float totalLength, out float[] uSamples, out float[] cumulative)
		{
			uSamples = new float[ArcLengthLutSegments + 1];
			cumulative = new float[ArcLengthLutSegments + 1];
			totalLength = 0f;
			Vector3 previous = p0;
			for (int i = 0; i <= ArcLengthLutSegments; i++)
			{
				float t = (float)i / ArcLengthLutSegments;
				uSamples[i] = t;
				Vector3 point = QuadraticBezierPoint(p0, p1, p2, t);
				if (i > 0)
				{
					totalLength += Vector3.Distance(previous, point);
				}
				cumulative[i] = totalLength;
				previous = point;
			}
		}

		private static float ArcLengthToU(float targetS, float[] uSamples, float[] cumulative)
		{
			if (uSamples == null || cumulative == null || uSamples.Length != cumulative.Length || uSamples.Length == 0)
			{
				return 0f;
			}
			if (targetS <= 0f)
			{
				return uSamples[0];
			}
			int last = cumulative.Length - 1;
			if (targetS >= cumulative[last])
			{
				return uSamples[last];
			}
			for (int i = 1; i < cumulative.Length; i++)
			{
				if (targetS > cumulative[i])
				{
					continue;
				}
				float startS = cumulative[i - 1];
				float endS = cumulative[i];
				float lerp = Mathf.Approximately(startS, endS) ? 0f : Mathf.InverseLerp(startS, endS, targetS);
				return Mathf.Lerp(uSamples[i - 1], uSamples[i], lerp);
			}
			return uSamples[last];
		}

		private static Vector3 QuadraticBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, float t)
		{
			float oneMinusT = 1f - t;
			return oneMinusT * oneMinusT * p0 + 2f * oneMinusT * t * p1 + t * t * p2;
		}

		private static Vector3 QuadraticBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, float t)
		{
			return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
		}

		private static Vector3 QuarticBezierPoint(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
		{
			float oneMinusT = 1f - t;
			float oneMinusTSqr = oneMinusT * oneMinusT;
			float tSqr = t * t;
			return p0 * (oneMinusTSqr * oneMinusTSqr) + p1 * (4f * oneMinusTSqr * oneMinusT * t) + p2 * (6f * oneMinusTSqr * tSqr) + p3 * (4f * oneMinusT * tSqr * t) + p4 * (tSqr * tSqr);
		}

		private static Vector3 QuarticBezierDerivative(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
		{
			float oneMinusT = 1f - t;
			return 4f * Mathf.Pow(oneMinusT, 3f) * (p1 - p0) + 12f * oneMinusT * oneMinusT * t * (p2 - p1) + 12f * oneMinusT * t * t * (p3 - p2) + 4f * t * t * t * (p4 - p3);
		}

		private static void BuildQuarticArcLengthLut(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, out float totalLength, out float[] uSamples, out float[] cumulative)
		{
			uSamples = new float[ArcLengthLutSegments + 1];
			cumulative = new float[ArcLengthLutSegments + 1];
			totalLength = 0f;
			Vector3 previous = p0;
			for (int i = 0; i <= ArcLengthLutSegments; i++)
			{
				float t = (float)i / ArcLengthLutSegments;
				uSamples[i] = t;
				Vector3 point = QuarticBezierPoint(p0, p1, p2, p3, p4, t);
				if (i > 0)
				{
					totalLength += Vector3.Distance(previous, point);
				}
				cumulative[i] = totalLength;
				previous = point;
			}
		}

		private bool ApplyDeferredTarget()
		{
			if (TargetStillValid())
			{
				return true;
			}
			if (_completed)
			{
				return false;
			}
			if (_targetSelector == null)
			{
				CancelFlightWithoutExplode();
				return false;
			}
			BaseObject targetObject = _targetSelector();
			if (targetObject == null || targetObject.BeingDestroyed)
			{
				CancelFlightWithoutExplode();
				return false;
			}
			_targetObject = targetObject;
			UpdateTargetPoint();
			CacheQuarticShapeOffsets();
			return true;
		}

		private void UpdateTargetPoint()
		{
			if (_targetObject == null)
			{
				return;
			}
			if (_targetObject.meshRenderer != null)
			{
				_targetPoint = _targetObject.meshRenderer.bounds.center;
			}
			else
			{
				_targetPoint = _targetObject.transform.position;
			}
		}

		private void CacheQuarticShapeOffsets()
		{
			_quarticTargetIsRight = _targetPoint.x >= _quadraticEnd.x;
			_quarticP2XOffset = UnityEngine.Random.Range(1f, 3f);
			if (!_quarticTargetIsRight)
			{
				_quarticP2XOffset = -_quarticP2XOffset;
			}
			bool useNegativeP3XOffset = Mathf.Abs(_targetPoint.x) <= 2.5f ? !_quarticTargetIsRight : _quarticTargetIsRight;
			_quarticP3OffsetX = useNegativeP3XOffset ? UnityEngine.Random.Range(-4f, -2f) : UnityEngine.Random.Range(2f, 4f);
			if (_targetPoint.y <= 3f)
			{
				_quarticP3OffsetY = UnityEngine.Random.value <= 0.5f ? UnityEngine.Random.Range(-2f, 0f) : UnityEngine.	Random.Range(4f, 8f);
			}
			else if (_targetPoint.y <= 4f)
			{
				_quarticP3OffsetY = UnityEngine.Random.value <= 0.5f ? UnityEngine.Random.Range(-4f, -2f) : UnityEngine.Random.Range(4f, 8f);
			}
			else
			{
				_quarticP3OffsetY = UnityEngine.Random.value <= 0.5f ? UnityEngine.Random.Range(-8f, -4f) : UnityEngine.Random.Range(4f, 8f);
			}
		}

		private void UpdateQuarticControlPoints(Vector3 p0, Vector3 target)
		{
			_quarticSegmentStart = p0;
			_quarticP0 = p0;
			float p1XOffset = target.x * 0.5f + 2f;
			if (_quarticTargetIsRight)
			{
				p1XOffset = -p1XOffset;
			}
			_quarticP1 = new Vector3(_quarticSegmentStart.x + p1XOffset, target.y + 5f, _quarticSegmentStart.z);
			_quarticP2 = new Vector3(target.x + _quarticP2XOffset, target.y - 3f, _quarticSegmentStart.z + 5f);
			_quarticP3 = new Vector3(target.x + _quarticP3OffsetX, target.y + _quarticP3OffsetY, target.z - 5f);
			_quarticP4 = target;
		}

		private void ArriveAndExplode()
		{
			if (_completed)
			{
				return;
			}
			_completed = true;
			StopParticles(flyParticles);
			StopParticles(starParticles);
			if (explodeParticle != null)
			{
				UnityEngine.Object.Instantiate(explodeParticle, transform.position, Quaternion.identity);
			}
			if (_targetObject != null && !_targetObject.BeingDestroyed)
			{
				ApplyBoomOnHit(_targetObject, transform.position);
				_targetObject.DestroyByRocket();
				if (_rocketIndexesWithAudio != null && _rocketIndexesWithAudio.Contains(_rocketIndex))
				{
					ServiceLocator.Get<AudioHelper>()?.PlaySfx(Audio.AudioType.RocketHit);
				}
				CameraHelper cameraHelper = ServiceLocator.Get<CameraHelper>();
				Vector3 particlePosition = cameraHelper != null ? cameraHelper.GetPointFromPointToCamera(transform.position, ExplodeParticleCameraDistance) : transform.position;
				ServiceLocator.Get<ParticleController>()?.PlayOnPosition(ParticleType.RocketExplode, particlePosition, Quaternion.identity, Vector3.zero);
			}
			Destroy(gameObject);
		}

		private static void ApplyBoomOnHit(BaseObject hitObject, Vector3 boomOrigin)
		{
			if (hitObject?.ContactNode == null)
			{
				return;
			}
			HashSet<ContactNode> cluster = new HashSet<ContactNode>();
			Queue<ContactNode> queue = new Queue<ContactNode>();
			ContactGraph.CollectConnected(hitObject.ContactNode, cluster, queue, ContactNodeType.Object);
			foreach (ContactNode node in cluster)
			{
				if (node == null || node == hitObject.ContactNode || node.MasterObject is not BaseObject baseObject || baseObject.rb == null || baseObject.BeingDestroyed)
				{
					continue;
				}
				Vector3 direction = baseObject.rb.worldCenterOfMass - boomOrigin;
				if (direction.sqrMagnitude <= 1E-05f)
				{
					direction = Vector3.up;
				}
				Vector3 impulse = baseObject.ApplyRocketHitBoomMultiplier(direction.normalized * FirstHitBoomPower);
				baseObject.rb.AddForce(impulse, ForceMode.VelocityChange);
			}
		}

		private void Abort()
		{
			_completed = true;
			CancelFlightWithoutExplode();
		}

		private void RestoreOriginalRenderState()
		{
			RestoreRendererState(rendererParts, _originalRenderQueues, _originalSortingLayerNames, _originalSortingOrders);
			RestoreParticleRendererState(flyParticles, _originalFlyParticleRenderQueues, _originalFlyParticleSortingLayerNames, _originalFlyParticleSortingOrders);
			RestoreParticleRendererState(starParticles, _originalStarParticleRenderQueues, _originalStarParticleSortingLayerNames, _originalStarParticleSortingOrders);
		}

		private void CancelFlightWithoutExplode()
		{
			_completed = true;
			StopParticles(flyParticles);
			StopParticles(starParticles);
			RestoreOriginalRenderState();
			Destroy(gameObject);
		}

		private void CacheOriginalRenderState()
		{
			CacheRendererState(rendererParts, out _originalRenderQueues, out _originalSortingLayerNames, out _originalSortingOrders);
			CacheParticleRendererState(flyParticles, out _originalFlyParticleRenderQueues, out _originalFlyParticleSortingLayerNames, out _originalFlyParticleSortingOrders);
			CacheParticleRendererState(starParticles, out _originalStarParticleRenderQueues, out _originalStarParticleSortingLayerNames, out _originalStarParticleSortingOrders);
		}

		private void ApplyFrontRenderState()
		{
			ApplyRendererFrontState(rendererParts);
			ApplyParticleFrontState(flyParticles);
			ApplyParticleFrontState(starParticles);
		}

		private static void CacheRendererState(MeshRenderer[] renderers, out int[] renderQueues, out string[] sortingLayerNames, out int[] sortingOrders)
		{
			if (renderers == null)
			{
				renderQueues = Array.Empty<int>();
				sortingLayerNames = Array.Empty<string>();
				sortingOrders = Array.Empty<int>();
				return;
			}
			renderQueues = new int[renderers.Length];
			sortingLayerNames = new string[renderers.Length];
			sortingOrders = new int[renderers.Length];
			for (int i = 0; i < renderers.Length; i++)
			{
				MeshRenderer renderer = renderers[i];
				if (renderer == null)
				{
					continue;
				}
				renderQueues[i] = renderer.material.renderQueue;
				sortingLayerNames[i] = renderer.sortingLayerName;
				sortingOrders[i] = renderer.sortingOrder;
			}
		}

		private static void RestoreRendererState(MeshRenderer[] renderers, int[] renderQueues, string[] sortingLayerNames, int[] sortingOrders)
		{
			if (renderers == null)
			{
				return;
			}
			for (int i = 0; i < renderers.Length; i++)
			{
				MeshRenderer renderer = renderers[i];
				if (renderer == null || renderQueues == null || i >= renderQueues.Length)
				{
					continue;
				}
				renderer.material.renderQueue = renderQueues[i];
				renderer.sortingLayerName = sortingLayerNames != null && i < sortingLayerNames.Length ? sortingLayerNames[i] : renderer.sortingLayerName;
				renderer.sortingOrder = sortingOrders != null && i < sortingOrders.Length ? sortingOrders[i] : renderer.sortingOrder;
			}
		}

		private static void CacheParticleRendererState(ParticleSystem[] particles, out int[] renderQueues, out string[] sortingLayerNames, out int[] sortingOrders)
		{
			if (particles == null)
			{
				renderQueues = Array.Empty<int>();
				sortingLayerNames = Array.Empty<string>();
				sortingOrders = Array.Empty<int>();
				return;
			}
			renderQueues = new int[particles.Length];
			sortingLayerNames = new string[particles.Length];
			sortingOrders = new int[particles.Length];
			for (int i = 0; i < particles.Length; i++)
			{
				ParticleSystemRenderer renderer = particles[i] != null ? particles[i].GetComponent<ParticleSystemRenderer>() : null;
				if (renderer == null)
				{
					continue;
				}
				renderQueues[i] = renderer.material.renderQueue;
				sortingLayerNames[i] = renderer.sortingLayerName;
				sortingOrders[i] = renderer.sortingOrder;
			}
		}

		private static void RestoreParticleRendererState(ParticleSystem[] particles, int[] renderQueues, string[] sortingLayerNames, int[] sortingOrders)
		{
			if (particles == null)
			{
				return;
			}
			for (int i = 0; i < particles.Length; i++)
			{
				ParticleSystemRenderer renderer = particles[i] != null ? particles[i].GetComponent<ParticleSystemRenderer>() : null;
				if (renderer == null || renderQueues == null || i >= renderQueues.Length)
				{
					continue;
				}
				renderer.material.renderQueue = renderQueues[i];
				renderer.sortingLayerName = sortingLayerNames != null && i < sortingLayerNames.Length ? sortingLayerNames[i] : renderer.sortingLayerName;
				renderer.sortingOrder = sortingOrders != null && i < sortingOrders.Length ? sortingOrders[i] : renderer.sortingOrder;
			}
		}

		private static void ApplyRendererFrontState(MeshRenderer[] renderers)
		{
			if (renderers == null)
			{
				return;
			}
			for (int i = 0; i < renderers.Length; i++)
			{
				MeshRenderer renderer = renderers[i];
				if (renderer == null)
				{
					continue;
				}
				renderer.material.renderQueue = FrontRenderQueue;
				renderer.sortingLayerName = FrontSortingLayerName;
				renderer.sortingOrder = FrontSortingOrder;
			}
		}

		private static void ApplyParticleFrontState(ParticleSystem[] particles)
		{
			if (particles == null)
			{
				return;
			}
			for (int i = 0; i < particles.Length; i++)
			{
				ParticleSystemRenderer renderer = particles[i] != null ? particles[i].GetComponent<ParticleSystemRenderer>() : null;
				if (renderer == null)
				{
					continue;
				}
				renderer.material.renderQueue = FrontRenderQueue;
				renderer.sortingLayerName = FrontSortingLayerName;
				renderer.sortingOrder = FrontSortingOrder;
			}
		}

		private static void PlayParticles(ParticleSystem[] particles)
		{
			if (particles == null)
			{
				return;
			}
			for (int i = 0; i < particles.Length; i++)
			{
				particles[i]?.Play();
			}
		}

		private static void StopParticles(ParticleSystem[] particles)
		{
			if (particles == null)
			{
				return;
			}
			for (int i = 0; i < particles.Length; i++)
			{
				particles[i]?.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
			}
		}

		static OpeningRocket()
		{
			WaitAfterQuadraticSeconds = 0.2f;
			peakScale = 0.7f;
			quadraticLegDuration = 0.7f;
			quarticLegDuration = 0.7f;
			quadraticRotateSpeed = 240f;
			waitRotateSpeedStart = 180f;
			waitRotateSpeedEnd = 90f;
			quarticRotateSpeed = 360f;
			ShouldRocketsSync = false;
			waitBobOffset = 0.5f;
			bobDescentDuration = 0.2f;
		}
	}
}
