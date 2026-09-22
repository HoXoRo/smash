using System;
using System.Collections;
using Core;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace Geo
{
	public static class GeoClient
	{
		private const string GeoServiceBaseUrl = "https://geo-parser-srggdyq22a-uc.a.run.app";

		private const int TimeoutSeconds = 10;

		public static void GetInstallCountry(Action<GetCountryResponseDto> onOk, Action<string> onError)
		{
			RoutineRunner.Instance.StartCoroutine(GetJson(GeoServiceBaseUrl, onOk, onError));
		}

		private static IEnumerator GetJson<TResponse>(string url, Action<TResponse> onOk, Action<string> onError)
		{
			using (UnityWebRequest uwr = UnityWebRequest.Get(url))
			{
				uwr.timeout = TimeoutSeconds;
				uwr.SetRequestHeader("Content-Type", "application/json");
				yield return uwr.SendWebRequest();
				string body = uwr.downloadHandler != null ? uwr.downloadHandler.text : string.Empty;
				if (uwr.result != UnityWebRequest.Result.Success)
				{
					onError?.Invoke(string.Format("HTTP {0} - {1} - Body: {2}", uwr.responseCode, uwr.error, body));
					yield break;
				}
				onOk?.Invoke(JsonConvert.DeserializeObject<TResponse>(body));
			}
		}
	}
}
