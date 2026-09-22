using System;
using System.Collections;
using System.Text;
using Core;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Backend
{
	public static class ApiClient
	{
		private const string BaseUrl = "https://us-central1-cannon-fest.cloudfunctions.net";

		private const int TimeoutSeconds = 10;

		public static void UserLogin(string userId, int level, Action<UserLoginResponseDto> onOk, Action<string> onError)
		{
			UserLoginRequestDto request = new UserLoginRequestDto
			{
				userId = userId,
				level = level
			};
			Run(PostJson(BuildUrl("userLogin"), request, onOk, onError));
		}

		public static void UpsertScore(string userId, int level, Action<UpsertScoreResponseDto> onOk, Action<string> onError)
		{
			UpsertScoreRequestDto request = new UpsertScoreRequestDto
			{
				userId = userId,
				level = level
			};
			Run(PostJson(BuildUrl("upsertScore"), request, onOk, onError));
		}

		public static void GetTopScores(int n, Action<GetTopScoresResponseDto> onOk, Action<string> onError)
		{
			GetTopScoresRequestDto request = new GetTopScoresRequestDto
			{
				n = n
			};
			Run(PostJson(BuildUrl("getTopScores"), request, onOk, onError));
		}

		public static void GetTopScoresByCountry(string countryCode, int n, Action<GetTopScoresResponseDto> onOk, Action<string> onError)
		{
			GetTopScoresByCountryRequestDto request = new GetTopScoresByCountryRequestDto
			{
				country = (countryCode ?? string.Empty).Trim().ToUpperInvariant(),
				n = n
			};
			Run(PostJson(BuildUrl("getTopScoresByCountry"), request, onOk, onError));
		}

		private static string BuildUrl(string functionName)
		{
			return BaseUrl + "/" + functionName;
		}

		private static void Run(IEnumerator routine)
		{
			RoutineRunner.Instance.StartCoroutine(routine);
		}

		private static IEnumerator PostJson<TRequest, TResponse>(string url, TRequest request, Action<TResponse> onOk, Action<string> onError)
		{
			string json = JsonConvert.SerializeObject(request);
			byte[] body = Encoding.UTF8.GetBytes(json);
			using (UnityWebRequest uwr = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
			{
				uwr.uploadHandler = new UploadHandlerRaw(body);
				uwr.downloadHandler = new DownloadHandlerBuffer();
				uwr.timeout = TimeoutSeconds;
				uwr.SetRequestHeader("Content-Type", "application/json");
				yield return uwr.SendWebRequest();
				HandleResponse(uwr, onOk, onError);
			}
		}

		private static void HandleResponse<TResponse>(UnityWebRequest uwr, Action<TResponse> onOk, Action<string> onError)
		{
			string body = uwr.downloadHandler != null ? uwr.downloadHandler.text : string.Empty;
			if (uwr.result != UnityWebRequest.Result.Success)
			{
				onError?.Invoke(BuildError(uwr, body));
				return;
			}
			onOk?.Invoke(JsonConvert.DeserializeObject<TResponse>(body));
		}

		private static string BuildError(UnityWebRequest uwr, string body)
		{
			try
			{
				ErrorResponseDto error = JsonConvert.DeserializeObject<ErrorResponseDto>(body);
				if (error != null && !string.IsNullOrEmpty(error.error))
				{
					return string.Format("{0} ({1}) - {2}", error.error, uwr.responseCode, error.details);
				}
			}
			catch
			{
			}
			return string.Format("HTTP {0} - {1} - Body: {2}", uwr.responseCode, uwr.error, body);
		}
	}
}
