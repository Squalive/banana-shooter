
using System.Text;
using Multiplayer;
using Riptide;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;

public class Fps : MonoBehaviour
{
    public static Fps Instance;
    
    private float deltaTime;
    public TextMeshProUGUI fps;

    public bool enableNetworkingStats = false,enableMemoryStatics=false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            fps.SetText("");
            DontDestroyOnLoad(gameObject);
        }
        else if(Instance!=this)
        {
            Destroy(gameObject);
        }
    }

    public bool enable = false;
    private void Update()
    {
        StringBuilder text = new StringBuilder("");
        if (enable)
        {
            deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
            float num = deltaTime * 1000f;
            float num2 = 1f / deltaTime;
            text.Append($"{num:0.0} ms ({num2:0.} fps)\n");
        }

        if (enableNetworkingStats)
        {
            if (NetworkManager.Instance.Client.IsConnected)
            {
                string color = "green";

                float rtt = NetworkManager.Instance.Client.SmoothRTT;

                if (rtt > 200) color = "red";
                else if (rtt > 100) color = "yellow";
                text.Append($"<color={color}>ping = {rtt}ms</color>\nclient id: {NetworkManager.Instance.Client.Id}\n");
            }
            text.Append($"byte up/s: {NetworkManager.Instance.ByteUp}\nbyte down/s: {NetworkManager.Instance.ByteDown}\n");
        }

        if (enableMemoryStatics)
        {
            long totalAllocated = Profiler.GetTotalAllocatedMemoryLong() / 1048576;
            long totalReserved = Profiler.GetTotalReservedMemoryLong() / 1048576;

            int gpuMemorySize = SystemInfo.graphicsMemorySize;
            int sysMemorySize = SystemInfo.systemMemorySize;

            text.Append(
                $"memory usage: {totalAllocated}mb | {totalReserved}mb / {sysMemorySize}mb\ngpu memory size: {gpuMemorySize}mb");
        }
        
        fps.SetText(text.ToString());
    }
}
