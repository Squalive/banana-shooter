
using UnityEngine;

public class CrateItem : MonoBehaviour
{
    public static CrateItem Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    public MeshRenderer render;
    public MeshFilter filter;
    public Camera cam;
}
