using UnityEngine;

[CreateAssetMenu(fileName = "New Ship", menuName = "Boat-Game/Ship", order = 0)]
public class Ship : ScriptableObject 
{
    #region Basic Details
        [Header("Details")]
        public GameObject prefab;
        public new string name;
        [TextArea]public string description;
    #endregion

    #region Boat Properties
        [Header("Properties")]
        public Vector2 bodyDrag;
        [Range(0,3f)] public float averageSailDrag = 2.2f;
        public float mastStrength = 500f;
        [Min(0f)] public float mastRigidity = 2f;
        public float mastDensity = 0.6f;
        [Range(0,1)] [SerializeField] float bodyDensity = 0.5f;
        public Vector2 keelRelativePos;
        [Min(0f)] public float keelWeightRatio = 1f;
    #endregion

    public float colliderDensity {
        get => bodyDensity / (1 + keelWeightRatio);
    }
    // public Vector2 colliderOffset {
    //     get => Vector2.down * sprite.bounds.size * (1f-spriteColliderRatio) / 2;
    // }
    // public Vector2 colliderSize {
    //     get => sprite.bounds.size * new Vector2(1, spriteColliderRatio);
    // }
}