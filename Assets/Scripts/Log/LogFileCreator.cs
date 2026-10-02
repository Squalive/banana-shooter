
using System.IO;
using UnityEngine;

namespace Log
{
    [DefaultExecutionOrder(-200)]
    public class LogFileCreator : MonoBehaviour
    {
        private string _logsPath;
        private BaseLog _log, _steamNetworkingLog;

        private void Awake()
        {
            _logsPath = Directory.GetCurrentDirectory();

            Application.logMessageReceived += OnLog;

            _log = new BaseLog("output_log.txt", _logsPath);
            // _steamNetworkingLog = new BaseLog("steam_networking_log", _logsPath);
        }

        private void OnDestroy()
        {
            Application.logMessageReceived -= OnLog;
            
            _log.Destroy();
            // _steamNetworkingLog.Destroy();
        }

        void OnLog(string message,string stackTrace,LogType logType)
        {
            if (_log == null) return;
            string logEntry = $"{message}\n{stackTrace}";

            switch (logType)
            {
                case LogType.Error:
                    logEntry = $"[Error] {message}\n{stackTrace}";
                    break;
                case LogType.Warning:
                    logEntry = $"[Warning] {message}\n{stackTrace}";
                    break;
            }
            
            _log.Log(logEntry);
        }

        // public void SteamNetworkingLog(string log)
        // {
        //     Debug.Log(log);
        //     // _steamNetworkingLog?.Log(log);
        // }
    }
}
