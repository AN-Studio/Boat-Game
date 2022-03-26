using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Controller Tweaks", menuName = "Scriptable Objects/Controller Tweaks", order = 0)]
public class ControllerTweaks : ScriptableObject
{   
    [Header("Settings")]
    [Range(0f,1f)] public float TimeUntilNextJump = 1f;
    [Range(0f,1f)] public float GyroBias = 0.1f;
    [Range(0f,90f)] public float MaxTiltAngle = 45f;
    public float AngularSpeedBias = 20f;
    public float AngularDamping = 10f;

    [Header("Parameter Multipliers")]
    public float JumpForce = 2f;
    public float MaxTorque = 2f;
    public float AngularDrag = 6f;
    public float InWaterTorque = 2f;
}
