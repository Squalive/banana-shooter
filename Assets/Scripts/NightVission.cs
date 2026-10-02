
using Manager;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class NightVission : MonoBehaviour
{
    Volume volume;

    private ColorAdjustments _adjustments;

    private void Awake()
    {
        volume = GetComponent<Volume>();
        _adjustments = (ColorAdjustments) volume.profile.components[0];
    }


    private void OnEnable()
    {
        _adjustments.postExposure.value = 4;
    }

    private void Update()
    {
        _adjustments.postExposure.value = Mathf.Lerp(_adjustments.postExposure.value,  (GameManager.Instance.desiredExposure - 0.1f > 0) ? GameManager.Instance.desiredExposure : 2, Time.deltaTime * 2f);
    }
}
