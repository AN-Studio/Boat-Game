using System.Collections.Generic;
using UnityEngine;

public class FMODParamUpdater : MonoBehaviour 
{
    [System.Serializable]
    public struct Event
    {
        [FMODUnity.EventRef] public string name;
        public FMOD.Studio.EventInstance instance;
        public Event(string name)
        {
            this.name = name;
            instance = FMODUnity.RuntimeManager.CreateInstance(name);
        }
    }

    [SerializeField] GlobalEvents globalEvents;
    [SerializeField] PlayerEvents playerEvents;

    List<Event> events;
    private void Start() {
        events = new List<Event>();
    }

    private void Update() {
        
    }    

}