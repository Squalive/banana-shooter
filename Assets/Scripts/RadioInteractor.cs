using System;
using UnityEngine;

public class RadioInteractor : MonoBehaviour
{
    public LayerMask whatIsRadio;

    private void Update()
    {
        if (Physics.SphereCast(transform.position,1f, transform.forward, out var hit,5f, whatIsRadio, QueryTriggerInteraction.Collide))
        {
            Radio radio = hit.transform.root.GetComponent<Radio>();

            if (radio != null)
            {
                radio.Show();

                if (Input.GetKeyDown(KeyCode.E))
                {
                    radio.ChangePlayState();
                }

                if (Input.GetKeyDown(KeyCode.RightArrow))
                {
                    radio.NextSong(1);
                }
                if (Input.GetKeyDown(KeyCode.LeftArrow))
                {
                    radio.NextSong(-1);
                }

            }
        }
    }
}