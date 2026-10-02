
using System;
using UnityEngine;

public class DamageUI : MonoBehaviour
{
    public static DamageUI Instance;

    private void Awake()
    {
        Instance = this;
    }

    [SerializeField] private CanvasGroup group;
    private float desiredAlpha = 0;

    private float speed = 25f;
    public void Damage()
    {
        desiredAlpha = 1f;
        speed = 40f;
        Invoke(nameof(Clear),0.07f);
    }

    void Clear()
    {
        desiredAlpha = 0;
        speed = 17f;
    }

    private void Update()
    {
        @group.alpha = Mathf.Lerp(@group.alpha, desiredAlpha, Time.deltaTime * speed);
    }
}
