using UnityEngine;
using System.Collections.Generic;

namespace ChimpLoco
{
    [CreateAssetMenu(menuName = "ChimpLoco/Chimp Config")]
    public class ChimpConfig : ScriptableObject
    {
        [Header("References")]
        public LayerMask locomotionEnabledLayers;
        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Arm Settings")]
        public float maxArmLength = 1.5f;
        public float unStickDistance = 1f;
        public bool enableHandUnstick = true;

        [Header("Hand Physics")]
        public float handGravityStrength = 9.8f;
        public float handGravityMultiplier = 2f;
        public bool zeroVelocityOnHandContact = true;

        [Header("Movement")]
        public int velocityHistorySize = 10;
        public float velocityLimit = 2f;
        public float maxJumpSpeed = 6f;
        public float jumpMultiplier = 1.2f;
        public bool enableJumping = true;
        public float dualHandMovementMultiplier = 0.5f;

        [Header("Head & Body")]
        public bool syncBodyYawToHead = true;
        public bool enableHeadCollision = true;
        public float headCollisionRadiusMultiplier = 1f;
        public bool teleportHandsOnHeadBlock = false;

        [Header("Collision Precision")]
        public float minimumRaycastDistance = 0.05f;
        public float defaultPrecision = 0.995f;

        [Header("Timing Safety")]
        public float minDeltaTime = 0.0001f;

        [Header("Wall Handling")]
        public float wallDotThreshold = 0.3f;
        public bool enableVerticalContribution = true;
        public float verticalMultiplier = 0.15f;
        public float maxVerticalStep = 0.06f;

        [Header("Tap Sounds")]
        public float tapMinDelay = 0.15f;
        public List<TapSounds.MaterialSound> tapMaterialSounds =
            new List<TapSounds.MaterialSound>();
    }
}
