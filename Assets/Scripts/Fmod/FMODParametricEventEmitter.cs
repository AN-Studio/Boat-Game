using System.Collections.Generic;
using UnityEngine;

public class FMODParametricEventEmitter : MonoBehaviour 
{
    private FMOD.Studio.EventInstance instance;
    [FMODUnity.EventRef] public string fmodEvent;
    [SerializeField] List<FMODParameter> parameters;

    void Start() 
    {
        instance = FMODUnity.RuntimeManager.CreateInstance(fmodEvent);
        instance.start();
    }

    void Update() 
    {
        foreach (FMODParameter param in parameters)
        {
            if (param.isGlobal)
                FMODUnity.RuntimeManager.StudioSystem.setParameterByName(param.name, param.value);
            else
                instance.setParameterByName(param.name, param.value);
        }
    }

    private void OnDestroy() {
        instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);    
    }
}