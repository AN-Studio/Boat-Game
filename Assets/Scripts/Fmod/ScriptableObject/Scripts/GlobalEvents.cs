using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Global Sheet", menuName = "Scriptable Objects/Audio/GlobalEvents", order = 0)]
public class GlobalEvents : ScriptableObject 
{
    [System.Serializable]
    public struct GlobalEvent
    {
        [FMODUnity.EventRef] public string fmodEvent;
        [SerializeField]  public List<FMODParameter> parameters;    
    }

    public GlobalEvent gustingWind;
}