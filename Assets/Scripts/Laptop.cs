
using System;
using Audio;

using UnityEngine;

public class Laptop : MonoBehaviour
{
    [SerializeField] private GameObject normal, light;

    private void Start()
    {
        normal.SetActive(!on);
        light.SetActive(on);
    }

    private bool on=false;

    
    public void TurnLaptop()
    {
        AudioManager.Instance.Play("tip");
        on = !on;

        normal.SetActive(!on);
        light.SetActive(on);
    }
}
