
using System;
using UnityEngine;

public class SRBlank : MonoBehaviour
{
    private LineRenderer _left, _right;

    public Material mat;

    [SerializeField] private Transform leftFixed, rightFixed, leftBlank, rightBlank;

    [SerializeField] private AnimationCurve widthCurve;

    private void Awake()
    {
        _left = leftFixed.gameObject.AddComponent<LineRenderer>();
        _right = rightFixed.gameObject.AddComponent<LineRenderer>();

        _left.material = _right.material = mat;
        _left.widthCurve = _right.widthCurve = widthCurve;

        _left.positionCount = 2;
        _right.positionCount = 2;
        
        _left.SetPosition(0, leftFixed.position);
        _left.SetPosition(1, leftBlank.position);
        
        _right.SetPosition(0, rightFixed.position);
        _right.SetPosition(1, rightBlank.position);
    }

    private void LateUpdate()
    {
        _left.SetPosition(0, leftFixed.position);
        _left.SetPosition(1, leftBlank.position);
        
        _right.SetPosition(0, rightFixed.position);
        _right.SetPosition(1, rightBlank.position);
    }
}
