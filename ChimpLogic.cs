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

        [Header("ChimpLogic Core")]
        public ChimpConfig config;

        [Header("Colliders")]
        public SphereCollider headCollider;
        public CapsuleCollider bodyCollider;

        [Header("Hands")]
        public TransformLogic leftHand;
        public TransformLogic rightHand;

        [Header("Tap sounds")]
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

        private Vector3[] velocityHistory;
        private int velocityIndex;
        private Vector3 denormalizedVelocityAverage;
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

            velocityHistory = new Vector3[Mathf.Max(1, config.velocityHistorySize)];
            denormalizedVelocityAverage = Vector3.zero;

            lastPosition = transform.position;
            lastHeadPosition = headCollider.transform.position;

            leftHand.Initialize(leftHand.CurrentPosition(lastHeadPosition, config.maxArmLength));
            rightHand.Initialize(rightHand.CurrentPosition(lastHeadPosition, config.maxArmLength));

            leftDevice = InputDevices.GetDeviceAtXRNode(leftHandNode);
            rightDevice = InputDevices.GetDeviceAtXRNode(rightHandNode);

            if (leftHandAudio != null) leftHandAudio.playOnAwake = false;
            if (rightHandAudio != null) rightHandAudio.playOnAwake = false;
        }

        private void Update()
        {
            if (config.syncBodyYawToHead)
            {
                bodyCollider.transform.eulerAngles =
                    new Vector3(0f, headCollider.transform.eulerAngles.y, 0f);
            }

            bool leftHit = HandleHand(leftHand, out Vector3 leftMove);
            bool rightHit = HandleHand(rightHand, out Vector3 rightMove);

            HandleHandFeedback(
                leftHand,
                ref lastLeftTouching,
                ref lastLeftTapTime,
                leftHandAudio,
                leftHandNode);

            HandleHandFeedback(
                rightHand,
                ref lastRightTouching,
                ref lastRightTapTime,
                rightHandAudio,
                rightHandNode);

            Vector3 movement =
                ((leftHit || leftHand.wasTouching) &&
                 (rightHit || rightHand.wasTouching))
                    ? (leftMove + rightMove) * config.dualHandMovementMultiplier
                    : leftMove + rightMove;

            if (config.enableVerticalContribution)
            {
                float vertical = Mathf.Clamp(
                    movement.y * config.verticalMultiplier,
                    -config.maxVerticalStep,
                    config.maxVerticalStep);

                movement.y = vertical;
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

            FinalizeHand(leftHand, leftHit, rightHit);
            FinalizeHand(rightHand, rightHit, leftHit);

            StoreVelocities();

            if (!disableMovement &&
                config.enableJumping &&
                (leftHit || rightHit))
            {
                if (denormalizedVelocityAverage.magnitude > config.velocityLimit)
                {
                    rb.linearVelocity =
                        Mathf.Min(
                            denormalizedVelocityAverage.magnitude * config.jumpMultiplier,
                            config.maxJumpSpeed
                        ) * denormalizedVelocityAverage.normalized;
                }
            }

            if (config.enableHandUnstick)
            {
                UnstickHand(leftHand);
                UnstickHand(rightHand);
            }
        }

        private void HandleHandFeedback(
            TransformLogic hand,
            ref bool lastTouching,
            ref float lastTapTime,
            AudioSource audioSource,
            XRNode node)
        {
            if (!lastTouching && hand.wasTouching)
            {
                if (Time.time - lastTapTime >= config.tapMinDelay)
                {
                    PlayTapSound(hand, audioSource);
                    PlayHaptics(node);
                    lastTapTime = Time.time;
                }
            }

            lastTouching = hand.wasTouching;
        }

        private void PlayTapSound(TransformLogic hand, AudioSource audioSource)
        {
            if (audioSource == null || config == null) return;

            Vector3 dir = (hand.lastPosition - headCollider.transform.position).normalized;

            if (Physics.Raycast(
                hand.lastPosition - dir * 0.02f,
                dir,
                out RaycastHit hit,
                0.1f,
                config.locomotionEnabledLayers,
                config.triggerInteraction))
            {
                Renderer rend = hit.collider.GetComponent<Renderer>();
                if (rend == null || rend.sharedMaterial == null) return;

                List<TapSounds.MaterialSound> list = config.tapMaterialSounds;

                for (int i = 0; i < list.Count; i++)
                {
                    var entry = list[i];
                    if (entry.material != rend.sharedMaterial || entry.sounds.Count == 0)
                        continue;

                    AudioClip clip = entry.sounds[Random.Range(0, entry.sounds.Count)];
                    audioSource.PlayOneShot(clip, entry.volume);
                    return;
                }
            }
        }

        private void PlayHaptics(XRNode node)
        {
            InputDevice device =
                node == leftHandNode ? leftDevice : rightDevice;

            if (!device.isValid)
                device = InputDevices.GetDeviceAtXRNode(node);

            if (!device.isValid)
                return;

            if (device.TryGetHapticCapabilities(out HapticCapabilities caps) &&
                caps.supportsImpulse)
            {
                device.SendHapticImpulse(0, hapticAmplitude, hapticDuration);
            }
        }

        private bool HandleHand(TransformLogic hand, out Vector3 handMovement)
        {
            handMovement = Vector3.zero;

            Vector3 current =
                hand.CurrentPosition(lastHeadPosition, config.maxArmLength);

            Vector3 gravity =
                hand.wasTouching
                    ? Vector3.zero
                    : Vector3.down *
                      config.handGravityStrength *
                      Time.deltaTime * Time.deltaTime;

            Vector3 delta =
                current - hand.lastPosition + gravity;

            if (IterativeCollisionSphereCast(
                hand.lastPosition,
                config.minimumRaycastDistance,
                delta,
                config.defaultPrecision,
                out Vector3 finalPos))
            {
                handMovement =
                    hand.wasTouching
                        ? hand.lastPosition - current
                        : finalPos - current;

                if (config.zeroVelocityOnHandContact)
                {
                    rb.linearVelocity = Vector3.zero;
                }

                return true;
            }

            return false;
        }

        private void FinalizeHand(
            TransformLogic hand,
            bool thisHandColliding,
            bool otherHandColliding)
        {
            Vector3 current =
                hand.CurrentPosition(headCollider.transform.position, config.maxArmLength);

            Vector3 delta = current - hand.lastPosition;

            if (IterativeCollisionSphereCast(
                hand.lastPosition,
                config.minimumRaycastDistance,
                delta,
                config.defaultPrecision,
                out Vector3 finalPos))
            {
                hand.lastPosition = finalPos;
                thisHandColliding = true;
            }
            else
            {
                hand.lastPosition = current;
            }

            hand.follower.position = hand.lastPosition;
            hand.wasTouching = thisHandColliding;
        }

        private bool ResolveHeadCollision(ref Vector3 movement)
        {
            if (Physics.SphereCast(
                lastHeadPosition,
                headCollider.radius * config.headCollisionRadiusMultiplier,
                movement,
                out RaycastHit hit,
                movement.magnitude,
                config.locomotionEnabledLayers,
                config.triggerInteraction))
            {
                float upDot = Vector3.Dot(hit.normal, Vector3.up);

                if (upDot < config.wallDotThreshold)
                {
                    movement = Vector3.Project(movement, -hit.normal);
                }
                else
                {
                    movement = Vector3.ProjectOnPlane(movement, hit.normal);
                }

                return true;
            }

            return false;
        }

        private void ResolveHeadOverlap()
        {
            Collider[] overlaps = Physics.OverlapSphere(
                headCollider.transform.position,
                headCollider.radius * config.headCollisionRadiusMultiplier,
                config.locomotionEnabledLayers,
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

        private void StoreVelocities()
        {
            velocityIndex = (velocityIndex + 1) % velocityHistory.Length;
            Vector3 old = velocityHistory[velocityIndex];

            Vector3 current =
                (transform.position - lastPosition) /
                Mathf.Max(Time.deltaTime, config.minDeltaTime);

            denormalizedVelocityAverage +=
                (current - old) / velocityHistory.Length;

            velocityHistory[velocityIndex] = current;
            lastPosition = transform.position;
        }

        private void UnstickHand(TransformLogic hand)
        {
            Vector3 current =
                hand.CurrentPosition(headCollider.transform.position, config.maxArmLength);

            if (hand.wasTouching &&
                (current - hand.lastPosition).magnitude > config.unStickDistance)
            {
                hand.wasTouching = false;
                hand.lastPosition = current;
            }
        }

        private bool IterativeCollisionSphereCast(
            Vector3 start,
            float radius,
            Vector3 move,
            float precision,
            out Vector3 end)
        {
            if (Physics.SphereCast(
                start,
                radius * precision,
                move,
                out RaycastHit hit,
                move.magnitude + radius * (1f - precision),
                config.locomotionEnabledLayers,
                config.triggerInteraction))
            {
                end = hit.point + hit.normal * radius;
                return true;
            }

            end = Vector3.zero;
            return false;
        }
    }
}
