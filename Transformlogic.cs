using UnityEngine;

namespace ChimpLoco
{
    public class TransformLogic : MonoBehaviour
    {
        public Transform handTransform;
        public Transform follower;
        public Vector3 offset;

        public bool wasTouching;
        public Vector3 lastPosition;

        private const float separationBias = 0.0015f;
        private const float minDelta = 0.0001f;

        public Vector3 CurrentPosition(Vector3 headPos, float maxArmLength)
        {
            Vector3 raw =
                handTransform.position + handTransform.rotation * offset;

            Vector3 delta = raw - headPos;
            float dist = delta.magnitude;

            if (dist > maxArmLength)
                raw = headPos + delta.normalized * maxArmLength;

            if (wasTouching)
            {
                Vector3 away = (raw - headPos).normalized;
                raw += away * separationBias;
            }

            return raw;
        }

        public void Initialize(Vector3 startPos)
        {
            lastPosition = startPos;
            wasTouching = false;
            follower.position = startPos;
        }

        public void ForceRelease(Vector3 headPos, float maxArmLength)
        {
            Vector3 pos =
                handTransform.position + handTransform.rotation * offset;

            Vector3 delta = pos - headPos;

            if (delta.sqrMagnitude < minDelta)
                delta = Vector3.forward * minDelta;

            lastPosition =
                delta.magnitude > maxArmLength
                    ? headPos + delta.normalized * maxArmLength
                    : pos;

            wasTouching = false;
            follower.position = lastPosition;
        }
    }
}
