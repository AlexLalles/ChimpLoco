using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

namespace ChimpLoco
{
    public class ChimpLogic : MonoBehaviour
    {
        public static ChimpLogic Instance { get; private set; }

        //v1.0-pre3

        //made for Chimped. Created by AlexLalles
        //allowed to use and modify under the MIT license found at; https://github.com/AlexLalles/ChimpLoco (includes commerical and non-commercial use)
        //please don't share or reupload this code with a pricetag, thanks. (feel free to share it for free though)
        //the line above means that you can sell games that use this code, not sell the code itself.

        [Header("Core")]
        public ChimpConfig config;

        [Header("Colliders")]
        public SphereCollider headCollider;
        public CapsuleCollider bodyCollider;

        [Header("Hands")]
        public TransformLogic leftHand;
        public TransformLogic rightHand;

        [Header("Tap Audio")]
        public AudioSource leftHandAudio;
        public AudioSource rightHandAudio;

        [Header("Haptics")]
        public XRNode leftHandNode = XRNode.LeftHand;
        public XRNode rightHandNode = XRNode.RightHand;
        [Range(0f, 1f)] public float hapticAmplitude = 0.5f;
        public float hapticDuration = 0.15f;

        [Header("Runtime")]
        public bool disableMovement;

        private Rigidbody rb;

        private Vector3[] velocitySamples;
        private int velocityIndex;
        private Vector3 averagedVelocity;
        private Vector3 lastPosition;
        private Vector3 lastHeadPosition;

        private bool lastLeftTouching;
        private bool lastRightTouching;
        private float lastLeftTapTime;
        private float lastRightTapTime;

        private InputDevice leftDevice;
        private InputDevice rightDevice;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            rb = GetComponent<Rigidbody>();

            velocitySamples = new Vector3[Mathf.Max(1, config.velocitySampleCount)];
            averagedVelocity = Vector3.zero;

            lastPosition = transform.position;
            lastHeadPosition = headCollider.transform.position;

            leftHand.Initialize(leftHand.CurrentPosition(lastHeadPosition, config.maxArmReach));
            rightHand.Initialize(rightHand.CurrentPosition(lastHeadPosition, config.maxArmReach));

            leftDevice = InputDevices.GetDeviceAtXRNode(leftHandNode);
            rightDevice = InputDevices.GetDeviceAtXRNode(rightHandNode);
        }

        private void Update()
        {
            bodyCollider.transform.eulerAngles =
                new Vector3(0f, headCollider.transform.eulerAngles.y, 0f);

            bool leftHit = HandleHand(leftHand, out Vector3 leftMove);
            bool rightHit = HandleHand(rightHand, out Vector3 rightMove);

            HandleTapFeedback(leftHand, ref lastLeftTouching, ref lastLeftTapTime, leftHandAudio, leftHandNode);
            HandleTapFeedback(rightHand, ref lastRightTouching, ref lastRightTapTime, rightHandAudio, rightHandNode);

            Vector3 movement =
                ((leftHit || leftHand.wasTouching) && (rightHit || rightHand.wasTouching))
                    ? (leftMove + rightMove) * config.dualHandMovementScale
                    : leftMove + rightMove;

            if (config.allowVerticalMotion)
            {
                movement.y = Mathf.Clamp(
                    movement.y * config.verticalMotionScale,
                    -config.maxVerticalStep,
                    config.maxVerticalStep);
            }
            else
            {
                movement.y = 0f;
            }

            if (config.enableHeadCollision)
            {
                ResolveHeadCollision(ref movement);
            }

            if (movement != Vector3.zero)
            {
                transform.position += movement;
            }

            ResolveHeadOverlap();

            lastHeadPosition = headCollider.transform.position;

            FinalizeHand(leftHand, leftHit);
            FinalizeHand(rightHand, rightHit);

            StoreVelocity();

            if (!disableMovement && config.enableJumping && (leftHit || rightHit))
            {
                if (averagedVelocity.magnitude > config.jumpVelocityThreshold)
                {
                    rb.linearVelocity =
                        Mathf.Min(
                            averagedVelocity.magnitude * config.jumpForceMultiplier,
                            config.maxJumpVelocity)
                        * averagedVelocity.normalized;
                }
            }

            if (config.enableHandRelease)
            {
                UnstickHand(leftHand);
                UnstickHand(rightHand);
            }
        }

        private bool HandleHand(TransformLogic hand, out Vector3 handMovement)
        {
            handMovement = Vector3.zero;

            Vector3 current = hand.CurrentPosition(lastHeadPosition, config.maxArmReach);

            Vector3 gravity =
                hand.wasTouching
                    ? Vector3.zero
                    : Vector3.down * config.handGravity * config.handGravityMultiplier * Time.deltaTime * Time.deltaTime;

            Vector3 delta = current - hand.lastPosition + gravity;

            if (ResolveHandCollision(hand.lastPosition, delta, out Vector3 resolved))
            {
                Vector3 candidate =
                    hand.wasTouching
                        ? hand.lastPosition
                        : resolved;

                Vector3 applied = candidate - current;

                if (Vector3.Dot(applied, hand.lastPosition - current) < 0f)
                {
                    applied = Vector3.zero;
                }

                handMovement = applied;

                if (config.zeroVelocityOnContact)
                {
                    rb.linearVelocity = Vector3.zero;
                }

                return true;
            }

            return false;
        }

        private void HandleTapFeedback(
    TransformLogic hand,
    ref bool lastTouching,
    ref float lastTapTime,
    AudioSource audioSource,
    XRNode node)
        {
            bool touching = hand.wasTouching;

            // Detect new tap (touch started this frame)
            if (touching && !lastTouching)
            {
                // Optional tap cooldown
                if (Time.time - lastTapTime >= config.tapCooldown)
                {
                    lastTapTime = Time.time;

                    // Play tap sound
                    if (audioSource != null && audioSource.clip != null)
                    {
                        audioSource.Play();
                    }

                    // Haptics
                    InputDevice device = InputDevices.GetDeviceAtXRNode(node);
                    if (device.isValid &&
                        device.TryGetHapticCapabilities(out HapticCapabilities caps) &&
                        caps.supportsImpulse)
                    {
                        device.SendHapticImpulse(
                            0,
                            hapticAmplitude,
                            hapticDuration);
                    }
                }
            }

            lastTouching = touching;
        }


        private void FinalizeHand(TransformLogic hand, bool colliding)
        {
            Vector3 current = hand.CurrentPosition(headCollider.transform.position, config.maxArmReach);
            Vector3 delta = current - hand.lastPosition;

            if (ResolveHandCollision(hand.lastPosition, delta, out Vector3 resolved))
            {
                if ((resolved - hand.lastPosition).sqrMagnitude >= 0f)
                {
                    hand.lastPosition = resolved;
                    colliding = true;
                }
            }
            else
            {
                hand.lastPosition = current;
            }

            hand.follower.position = hand.lastPosition;
            hand.wasTouching = colliding;
        }

        private bool ResolveHandCollision(Vector3 start, Vector3 move, out Vector3 end)
        {
            float radius = config.minCastDistance;
            float precision = config.collisionPrecision;

            if (Physics.SphereCast(
                start,
                radius * precision,
                move,
                out RaycastHit hit,
                move.magnitude + radius * (1f - precision),
                config.locomotionLayers,
                config.triggerInteraction))
            {
                Vector3 first = hit.point + hit.normal * radius;

                Vector3 slide =
                    Vector3.ProjectOnPlane(start + move - first, hit.normal) * 0.03f;

                if (Physics.SphereCast(
                    first,
                    radius,
                    slide,
                    out RaycastHit slideHit,
                    slide.magnitude,
                    config.locomotionLayers,
                    config.triggerInteraction))
                {
                    end = slideHit.point + slideHit.normal * radius;
                    return true;
                }

                end = first;
                return true;
            }

            if (Physics.SphereCast(
                start,
                radius * 0.66f,
                move.normalized * (move.magnitude + radius * 0.34f),
                out RaycastHit sanityHit,
                move.magnitude,
                config.locomotionLayers,
                config.triggerInteraction))
            {
                end = start;
                return true;
            }

            end = Vector3.zero;
            return false;
        }

        private bool ResolveHeadCollision(ref Vector3 movement)
        {
            if (Physics.SphereCast(
                lastHeadPosition,
                headCollider.radius * config.headCollisionRadiusScale,
                movement,
                out RaycastHit hit,
                movement.magnitude,
                config.locomotionLayers,
                config.triggerInteraction))
            {
                float upDot = Vector3.Dot(hit.normal, Vector3.up);

                movement =
                    upDot < config.wallDotThreshold
                        ? Vector3.Project(movement, -hit.normal)
                        : Vector3.ProjectOnPlane(movement, hit.normal);

                return true;
            }

            return false;
        }

        private void ResolveHeadOverlap()
        {
            Collider[] overlaps = Physics.OverlapSphere(
                headCollider.transform.position,
                headCollider.radius * config.headCollisionRadiusScale,
                config.locomotionLayers,
                config.triggerInteraction);

            foreach (Collider col in overlaps)
            {
                if (Physics.ComputePenetration(
                    headCollider,
                    headCollider.transform.position,
                    headCollider.transform.rotation,
                    col,
                    col.transform.position,
                    col.transform.rotation,
                    out Vector3 dir,
                    out float dist))
                {
                    transform.position += dir * dist;
                }
            }
        }

        private void StoreVelocity()
        {
            velocityIndex = (velocityIndex + 1) % velocitySamples.Length;
            Vector3 old = velocitySamples[velocityIndex];

            Vector3 current =
                (transform.position - lastPosition) /
                Mathf.Max(Time.deltaTime, config.minDeltaTime);

            averagedVelocity += (current - old) / velocitySamples.Length;

            velocitySamples[velocityIndex] = current;
            lastPosition = transform.position;
        }

        private void UnstickHand(TransformLogic hand)
        {
            Vector3 current = hand.CurrentPosition(headCollider.transform.position, config.maxArmReach);

            if (hand.wasTouching &&
                (current - hand.lastPosition).magnitude > config.handReleaseDistance)
            {
                hand.wasTouching = false;
                hand.lastPosition = current;
            }
        }

        [System.Serializable]
        public class TransformLogic
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
                Vector3 raw = handTransform.position + handTransform.rotation * offset;

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
        }
    }
}
