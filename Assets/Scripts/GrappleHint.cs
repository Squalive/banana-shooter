

using Manager;
using UnityEngine;

public class GrappleHint : MonoBehaviour
{
    public static GrappleHint Instance;
    
     public Transform hint;

    private Transform cameraTransform;
    private Camera camera;
    public bool show;

    public Vector3 desiredPos;

    private RectTransform canvasRect;

    private float size = 0;


    private void Awake()
    {
        Instance = this;

        canvasRect = GetComponent<RectTransform>();

        show = GameManager.Instance.setting.grappleHint;
    }
    private void OnEnable()
    {
        GameManager.Instance.PlayerSpawn += SetCamera;
        GameManager.Instance.PlayerDestroy += Disable;
    }

    private void OnDisable()
    {
        GameManager.Instance.PlayerSpawn -= SetCamera;
        GameManager.Instance.PlayerDestroy -= Disable;
    }
    private bool init = false;
    void SetCamera(Camera cam)
    {
        camera = cam;
        cameraTransform = cam.transform;
        init = true;
    }
    float multiplier = 0.85f;
    Vector2 localPoint;

    public void SetTargetPos(Vector3 pos)
    {
        if (!show) return;
        desiredPos = pos;
        size = 1;
        Vector3 targetPos = VectorExtension.CalculateWorldPosition(desiredPos, cameraTransform);
        Vector2 screenPoint = camera.WorldToScreenPoint(targetPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);

        Vector2 sizeDelta = canvasRect.sizeDelta;

        Vector2 max = new Vector2(sizeDelta.x / 2f, sizeDelta.y / 2f) * multiplier;
        Vector2 min = new Vector2(-sizeDelta.x / 2f, -sizeDelta.y / 2f) * multiplier;
        
        if (localPoint.x > max.x)
        {
            localPoint.x = max.x;
        }
        if (localPoint.x < min.x)
        {
            localPoint.x = min.x;
        }
        if (localPoint.y > max.y)
        {
            localPoint.y = max.y;
        }
        if (localPoint.y < min.y)
        {
            localPoint.y = min.y;
        }

        hint.localPosition = localPoint;
    }

    public void DisableTarget()
    {
        size = 0;
    }

    void Disable()
    {
        init = false;
    }

    private void LateUpdate()
    {
        if (!show) return;
        hint.localScale = Vector3.Lerp(hint.localScale,size*Vector3.one,Time.deltaTime*15f);
        
        if (!init) return;
        hint.Rotate(Vector3.forward*Mathf.PingPong(Time.time,1f),1f);

        Vector3 targetPos = VectorExtension.CalculateWorldPosition(desiredPos, cameraTransform);
        Vector2 screenPoint = camera.WorldToScreenPoint(targetPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, null, out localPoint);

        Vector2 sizeDelta = canvasRect.sizeDelta;

        Vector2 max = new Vector2(sizeDelta.x / 2f, sizeDelta.y / 2f) * multiplier;
        Vector2 min = new Vector2(-sizeDelta.x / 2f, -sizeDelta.y / 2f) * multiplier;
        
        if (localPoint.x > max.x)
        {
            localPoint.x = max.x;
        }
        if (localPoint.x < min.x)
        {
            localPoint.x = min.x;
        }
        if (localPoint.y > max.y)
        {
            localPoint.y = max.y;
        }
        if (localPoint.y < min.y)
        {
            localPoint.y = min.y;
        }

        hint.localPosition = Vector3.Lerp(hint.localPosition,localPoint,Time.deltaTime*15f);
    }

}
