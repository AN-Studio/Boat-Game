using UnityEngine;

[CreateAssetMenu(fileName = "New Player Sheet", menuName = "Scriptable Objects/Audio/Player", order = 0)]
public class PlayerEvents : ScriptableObject 
{
    [Header("Controller Events")]
    [FMODUnity.EventRef] public string jump = null;
    [FMODUnity.EventRef] public string raiseSail = null;
    [FMODUnity.EventRef] public string lowerSail = null;

    [Header("Game Events")]
    [FMODUnity.EventRef] public string collectCoin = null;
    [FMODUnity.EventRef] public string collectCrate = null;
    [FMODUnity.EventRef] public string collectChest = null;

}