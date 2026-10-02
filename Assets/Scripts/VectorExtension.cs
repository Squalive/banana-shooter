using UnityEngine;

public static class VectorExtension
{
    public static Vector3 CalculateWorldPosition(Vector3 targetPos,Transform cameraTransform)
    {
        Vector3 forward = cameraTransform.forward;
        Vector3 dir = targetPos - cameraTransform.position;         
        float dot = Vector3.Dot(forward, dir.normalized);
    
        if (dot <= 0f)
        {
            Vector3 vec =  dot * 1.01f*forward;
            targetPos = (cameraTransform.position + (dir+vec));
                
        }
    
        return targetPos;
    }
}