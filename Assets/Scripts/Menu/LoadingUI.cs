
using System;
using UnityEngine;
using UnityEngine.UI;

public class LoadingUI : MonoBehaviour
{
    [SerializeField] private Image image;

    private bool flag = false;
    private float desiredFillAmount = 1f;
    private void Start()
    {
        image.fillOrigin = 0;
        desiredFillAmount = 1f;
        flag = false;
    }

    private void Update()
    {
        image.fillAmount = Mathf.Lerp(image.fillAmount, desiredFillAmount, Time.deltaTime * 10f);
        if (Mathf.Abs(image.fillAmount - desiredFillAmount) < 0.01f)
        {
            flag = !flag;
            if (flag)
            {
                desiredFillAmount = 0;
                image.fillOrigin = 1;
            }
            else
            {
                image.fillOrigin = 0;
                desiredFillAmount = 1f;
            }
        }
    }
}
