
using UnityEngine;
using Random = UnityEngine.Random;

public class RandomColorGenerator : MonoBehaviour
{
    [SerializeField] private int matIdx = 0;

    private void Awake()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        renderer.materials[matIdx].color = Random.ColorHSV();
    }
}
