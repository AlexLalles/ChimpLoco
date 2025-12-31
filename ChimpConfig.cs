using UnityEngine;
using System.Collections.Generic;

namespace ChimpLoco
{
    [CreateAssetMenu(menuName = "ChimpLoco/Chimp Config")]
    public class ChimpConfig : ScriptableObject
    {

        // written by LALLES
        [Header("Collision Filtering")]
        [Tooltip("Layers that movement works on (recommended: Default), (default: Nothing)")]
        public LayerMask locomotionLayers;

        [Tooltip("Trigger interaction mode used for all physic stuff (default: 1)")]
        public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

        [Header("Arm Constraints")]
        [Tooltip("Max distance your hand could go from your head (default: 1.5)")]
        public float maxArmReach = 1.5f;

        [Tooltip("forced distance for your hands to teleport (default: 1)")]
        public float handReleaseDistance = 1f;

        [Tooltip("Allows auto release of hands when over-extended. (default: true)")]
        public bool enableHandRelease = true;

        [Header("Hand Physics")]
        [Tooltip("Gravity of the hand. (default: 9.8)")]
        public float handGravity = 9.8f;

        [Tooltip("Multiplier applied to gravity force (default: 2)")]
        public float handGravityMultiplier = 2f;

        [Tooltip("0 rigidbody velocity on first hand contact (default: true)")]
        public bool zeroVelocityOnContact = true;

        [Header("Movement & Jumping")]
        [Tooltip("Sample count for velocity averaging (deafult: 10)")]
        public int velocitySampleCount = 10;

        [Tooltip("lowest velocity required to trigger a jump (default: 1)")]
        public float jumpVelocityThreshold = 1f;

        [Tooltip("Maximum allowed jump speed (default: 6)")]
        public float maxJumpVelocity = 6f;

        [Tooltip("Multiplier applied to average velocity on jump (default: 1.2)")]
        public float jumpForceMultiplier = 1.2f;

        [Tooltip("Allows jump movement generation (default: true)")]
        public bool enableJumping = true;

        [Tooltip("Movement scale when both hands are engaged (default: 0.5)")]
        public float dualHandMovementScale = 0.5f;

        [Header("Head & Body")]
        [Tooltip("Synchronizes body yaw rotation to head yaw (default: true)")]
        public bool syncBodyYawToHead = true;

        [Tooltip("Enables collision resolution for the head (default: true)")]
        public bool enableHeadCollision = true;

        [Tooltip("Scales the head collider radius during collision checks (defautl: 1)")]
        public float headCollisionRadiusScale = 1f;

        [Header("Collision Precision")]
        [Tooltip("Minimum distance used for spherecast checks (default: 0.05)")]
        public float minCastDistance = 0.05f;

        [Tooltip("Precision factor for spherecast resolution (default: 0.995)")]
        [Range(0.9f, 1f)]
        public float collisionPrecision = 0.995f;

        [Header("Vertical Handling")]
        [Tooltip("Dot threshold distinguishing walls from floors (default: 3)")]
        public float wallDotThreshold = 3f; //this could be changed, but i would not recommend that. 3, from what I've tested, is very balanced. anything under 1 sucks.

        [Tooltip("Allows vertical movement (default: true)")]
        public bool allowVerticalMotion = true; //for this, it means that if you want vertical movement, you NEED this. (recommended: true)

        [Tooltip("Vertical movement sizing factor (default: 0.15)")]
        public float verticalMotionScale = 0.15f;

        [Tooltip("Maximum vertical displacement per frame (default: 0.04)")]
        public float maxVerticalStep = 0.04f; //this could be lower, but its too much for most

        [Header("Timing Safety")]
        [Tooltip("Minimum delta time used in velocity calculations (default: 0.0001)")]
        public float minDeltaTime = 0.0001f;

        [Header("Tap Feedback")]
        [Tooltip("Minimum delay between tap sounds (default: 0.15)")]
        public float tapCooldown = 0.15f; //not recommended to change, anything lower feels spammy. Anything higher doesn't feel responsive.

        [Tooltip("Material-based tap sound definitions (add your tap-sounds in here!)")]
        public List<MaterialTapSound> tapSounds = new List<MaterialTapSound>();

        [System.Serializable]
        public class MaterialTapSound
        {
            [Tooltip("add the material which you'd like the hitsound for.")]
            public Material material;

            [Tooltip("add as many audio clips here for which you'd like to play a tap-sound for!")]
            public List<AudioClip> clips = new List<AudioClip>();

            [Tooltip("how loud should it be?")]
            [Range(0f, 1f)] //recommended is 1, depending on how loud the audio clips are.
            public float volume = 1f;
        }
    }
}
