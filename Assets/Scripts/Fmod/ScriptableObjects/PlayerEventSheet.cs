using UnityEngine;

[CreateAssetMenu(fileName = "New Player Sheet", menuName = "Scriptable Objects/Audio/Player", order = 0)]
public class PlayerEventSheet : ScriptableObject 
{
    [Header("Player Input Events")]
    [FMODUnity.EventRef] public string jump = null;
    [FMODUnity.EventRef] public string raiseSail = null;
    [FMODUnity.EventRef] public string lowerSail = null;

}