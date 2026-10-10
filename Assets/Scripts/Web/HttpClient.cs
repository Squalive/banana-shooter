
#define ONLINE
using System;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;

namespace Web
{
    public static class HttpClient
    {
        public static async Task<T> Get<T>(string endpoint, bool debug = false)
        {
            string text = await GetText(endpoint, debug);
            if (string.IsNullOrWhiteSpace(text)) return default;

            try
            {
                return JsonConvert.DeserializeObject<T>(text, new JsonSerializerSettings
                {
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"GET request failed: {e.Message}");
                return default;
            }
        }

        public static async Task<string> GetText(string endpoint, bool debug = false)
        {
            try
            {
                using var request = CreateRequest(endpoint);
                if (!await SendAsync(request)) return null;

                string text = request.downloadHandler.text;
                if (debug) Debug.Log(text);
                return text;
            }
            catch (Exception e)
            {
                Debug.LogError($"GET request failed: {e.Message}");
                return null;
            }
        }

        public static async Task<string> Post(string endpoint, object payload)
        {
            try
            {
                using var request = CreateRequest(endpoint, RequestType.POST, payload);
                return await SendAsync(request) ? request.downloadHandler.text : null;
            }
            catch (Exception e)
            {
                Debug.LogError($"POST request failed: {e.Message}");
                return null;
            }
        }

        private static async Task<bool> SendAsync(UnityWebRequest request)
        {
            request.SendWebRequest();
            while (!request.isDone) await Task.Delay(10);
            if (request.result == UnityWebRequest.Result.Success) return true;
            Debug.LogError($"HTTP request failed ({request.responseCode}): {request.error}");
            return false;
        }

        private static UnityWebRequest CreateRequest(string path, RequestType type = RequestType.GET, object data = null)
        {
            byte[] bodyRaw = data == null ? null : Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(data));
            var request = new UnityWebRequest(path, type.ToString()) { timeout = 5 };

            if (data != null)
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            }

            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            return request;
        }
    }

    public static class EndPoint
    {

#if ONLINE
        const string Base = "https://www.bsserver.uk/";
#else
        const string Base = "https://localhost:44395/";
#endif


        public static string GetCheatingReports = $"{Base}Steamworks/GetCheatingReports/";
        public static string GetPlayerSummaries = $"{Base}Steamworks/GetPlayerSummaries/";
        public static string GetPlayerBansSummaries = $"{Base}Steamworks/GetPlayerBans/";

        public static string ReportCheating = $"{Base}Steamworks/ReportPlayerCheating/";
        public static string RequestDailyReward = $"{Base}Steamworks/RequestDailyReward/";
        public static string AntiCheatDetection = $"{Base}Steamworks/AntiCheatDetection/";

        public static string GetNews = $"{Base}Steamworks/News/";
    }

    public enum RequestType
    {
        GET = 0,
        POST,
    }
}
