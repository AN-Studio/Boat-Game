using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class BoatSpawner : MonoBehaviour
{
    public CinemachineVirtualCamera cam;
    public Slider slider;
    [SerializeField] BoatSpecs boatData;
    GameObject boatInstance;

    // Start is called before the first frame update
    void Start()
    {
        SpawnBoat();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void SpawnBoat() 
    {
        if (boatInstance != null) {
            Destroy(boatInstance);
            boatInstance = null;
        }

        boatInstance = Instantiate(boatData.prefab, transform.position, Quaternion.identity);
        boatInstance.tag = "Player";
        boatInstance.layer = LayerMask.NameToLayer("Default");

        cam.Follow = boatInstance.transform;

        ShipController controller = boatInstance.GetComponentInChildren<ShipController>();

        controller.SetProperties(boatData);
        slider.onValueChanged.AddListener(controller.OnThrottleChange);
    }

    void OnBeginRun() 
    {
        Destroy(gameObject);
    }
}
