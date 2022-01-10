using UnityEngine;

public class WindGustInterface : MonoBehaviour 
{
    private FMOD.Studio.EventInstance instance;
    [FMODUnity.EventRef] public string fmodEvent;


    [SerializeField] [Range(0,60f)] 
    private float windSpeed;

    void Start() 
    {
        instance = FMODUnity.RuntimeManager.CreateInstance(fmodEvent);
        instance.start();
    }

    void Update() 
    {
        // windSpeed = GameManager.Instance.windSpeed;
        instance.setParameterByName("EQ", windSpeed);
    }

    private void OnDestroy() {
        instance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);    
    }
}