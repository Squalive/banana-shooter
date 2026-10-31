using System.Collections;
using System.Collections.Generic;

using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FailedWindow : MonoBehaviour
{
    private Vector3 desiredSize;
    public TextMeshProUGUI title, reason;

    public void SetTitle(string title) => this.title.SetText(title);
    public void SetReason(string reason) => this.reason.SetText(reason);

    public float removeTime = 2;
    // Menu popups stay until closed. In game the cursor is locked, so those close on their own.
    [System.NonSerialized] public bool closeByItself;

    private Camera canvasCamera;

    void Start()
    {
        transform.localScale = Vector3.zero;
        desiredSize = Vector3.one;
        if (closeByItself) Invoke("Clear", removeTime);

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) canvasCamera = canvas.worldCamera;
    }

    // Update is called once per frame
    void Update()
    {
        if (!cleared && !closeByItself && ClosePressed()) Clear();

        transform.localScale = Vector3.Lerp(transform.localScale, desiredSize, Time.unscaledDeltaTime * 30f);
        if (cleared)
        {
            if (Vector3.Distance(transform.localScale, desiredSize) < 0.1f)
            {
                Destroy(gameObject);
            }
        }
    }

    // Escape, or a click anywhere outside the popup
    bool ClosePressed()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;

        Mouse mouse = Mouse.current;
        return mouse != null && mouse.leftButton.wasPressedThisFrame
            && !RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, mouse.position.ReadValue(), canvasCamera);
    }

    private bool cleared = false;

    public void Clear()
    {
        desiredSize = Vector3.zero;
        cleared = true;
    }

}
