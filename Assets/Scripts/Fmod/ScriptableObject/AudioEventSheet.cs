using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Audio Sheet", menuName = "Scriptable Objects/Audio/Event Sheet", order = 0)]
public class AudioEventSheet : ScriptableObject 
{
    [System.Serializable]
    public class EventIDPair
    {
        public string identifier;
        [FMODUnity.EventRef] public string eventRef;
    } 

    public List<EventIDPair> events = new List<EventIDPair>();

    public string this[string id] 
    {
        get {
            foreach (EventIDPair e in events)
            {
                if(e.identifier == id) return e.eventRef;                
            } 

            throw new System.Exception($"No FMOD audio event matched the id '{id}'.");
        }

        set {
            for (int i= 0; i < events.Count; i++)
            {
                if(events[i].identifier == id) events[i].eventRef = value;                
            }
        }

    }

}