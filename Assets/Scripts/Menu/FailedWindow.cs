using System.Collections;
using System.Collections.Generic;

using TMPro;
using UnityEngine;

public class FailedWindow : MonoBehaviour
{
    private Vector3 desiredSize;
    public TextMeshProUGUI title, reason;

    public void SetTitle(string title) => this.title.SetText(title);
    public void SetReason(string reason) => this.reason.SetText(reason);

    public float removeTime = 2;
    
    void Start()
    {
        transform.localScale = Vector3.zero;
        desiredSize=Vector3.one;
        Invoke("Clear",2f);
    }

    // Update is called once per frame
    void Update()
    {
        transform.localScale=Vector3.Lerp(transform.localScale,desiredSize,Time.unscaledDeltaTime*30f);
        if (cleared)
        {
            if (Vector3.Distance(transform.localScale, desiredSize) < 0.1f)
            {
                Destroy(gameObject);
            }
        }
    }

    private bool cleared=false;
    
    public void Clear()
    {
        desiredSize=Vector3.zero;
        cleared = true;
    }
    
}
