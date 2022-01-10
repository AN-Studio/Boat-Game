using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Boat", menuName = "Scriptable Objects/Data/Boat")]
public class BoatProperties : ScriptableObject
{
    #region Basic Details
        [Header("Details")]
        public Sprite sprite;
        public new string name;
        [TextArea]public string description;
    #endregion

    #region Boat Properties
        [Header("Properties")]
        public Vector2 dragCoefficient;
        [Range(0,1)]public float density = 0.5f;
        [Range(0,1)]public float spriteColliderRatio;
        [Range(1f,2f)]public float keelRelativePos;
        [Min(0f)]public float keelWeightRatio = 1f;
    #endregion

    public float colliderDensity {
        get => density / (1 + keelWeightRatio);
    }
    public Vector2 colliderOffset {
        get => Vector2.down * sprite.bounds.size * (1f-spriteColliderRatio) / 2;
    }
    public Vector2 colliderSize {
        get => sprite.bounds.size * new Vector2(1, spriteColliderRatio);
    }
}
