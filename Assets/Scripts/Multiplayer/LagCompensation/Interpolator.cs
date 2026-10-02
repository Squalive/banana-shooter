using System.Collections.Generic;
using Demo;
using UnityEngine;

namespace Multiplayer.LagCompensation
{
    public class Interpolator : MonoBehaviour
    {
        [SerializeField] private float timeElapsed = 0f;
        [SerializeField] private float timeToReachTarget = 0.05f;
        [SerializeField] private float movementThreshold = 0.02f;

        private readonly List<TransformUpdate> futureTransformUpdates = new List<TransformUpdate>();

        private float squareMovementThreshold;
        private TransformUpdate to;
        private TransformUpdate from;
        private TransformUpdate previous;

        private void Start()
        {
            if(DemoManager.Replaying) Destroy(this);
            squareMovementThreshold = movementThreshold * movementThreshold;
            var position = transform.position;
            to = new TransformUpdate(NetworkManager.Instance.ServerTick, false,position);
            from = new TransformUpdate(NetworkManager.Instance.InterpolationTick,false, position);
            previous = new TransformUpdate(NetworkManager.Instance.InterpolationTick,false, position);
        }

        private void Update()
        {
            for (int i = 0; i < futureTransformUpdates.Count; i++)
            {
                if (NetworkManager.Instance.ServerTick >= futureTransformUpdates[i].Tick)
                {
                    if (futureTransformUpdates[i].IsTeleport)
                    {
                        to = futureTransformUpdates[i];
                        from = to;
                        previous = to;
                        transform.position = to.Position;
                    }
                    else
                    {
                        previous = to;
                        to = futureTransformUpdates[i];
                        from = new TransformUpdate(NetworkManager.Instance.InterpolationTick, false, transform.position);
                    }

                    futureTransformUpdates.RemoveAt(i);
                    i--;
                    timeElapsed = 0f;
                    timeToReachTarget = (to.Tick - from.Tick) * Time.fixedDeltaTime;
                }
            }

            timeToReachTarget = Mathf.Max(0.02f, timeToReachTarget);

            timeElapsed += Time.deltaTime;
            InterpolatePosition(timeElapsed / timeToReachTarget);
        }

        private void InterpolatePosition(float lerpAmount)
        {
            if ((to.Position - previous.Position).sqrMagnitude < squareMovementThreshold)
            {
                if (to.Position != from.Position)
                    transform.position = Vector3.Lerp(from.Position, to.Position, lerpAmount);

                return;
            }

            transform.position = Vector3.LerpUnclamped(from.Position, to.Position, lerpAmount);
        }

        public void NewUpdate(uint tick, bool isTeleport, Vector3 position)
        {
            if (tick <= NetworkManager.Instance.InterpolationTick && !isTeleport)
                return;

            for (int i = 0; i < futureTransformUpdates.Count; i++)
            {
                if (tick < futureTransformUpdates[i].Tick)
                {
                    futureTransformUpdates.Insert(i, new TransformUpdate(tick, isTeleport, position));
                    return;
                }
            }

            futureTransformUpdates.Add(new TransformUpdate(tick, isTeleport, position));
        }
    }
}