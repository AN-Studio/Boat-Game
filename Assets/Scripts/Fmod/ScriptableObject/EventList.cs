using UnityEngine;

[CreateAssetMenu(fileName = "New Ambient Sheet", menuName = "Scriptable Objects/Audio/Ambient", order = 0)]
public class EventList : ScriptableObject 
{
    [FMODUnity.EventRef]
    public string calmWaters = null;
    [FMODUnity.EventRef]
    public string surfBreeze = null;
    [FMODUnity.EventRef]
    public string seagulls = null;


}