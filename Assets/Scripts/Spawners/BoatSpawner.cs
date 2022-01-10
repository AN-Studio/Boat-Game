using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class BoatSpawner : MonoBehaviour
{
    #region References
        [Header("References")]
        public CinemachineVirtualCamera cam;
        public Slider slider;
        public GUIDisplay gUI;
        public ActionRegion jumpRegion;
        GameObject boatInstance;
    #endregion

    #region Settings
        [Header("Global Ship Settings")]
        [SerializeField] float jumpAcceleration = 10;
    #endregion

    [Space]
    [SerializeField] BoatSpecs boatData;

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
        controller.jumpRegion = jumpRegion;
        controller.jumpAcceleration = jumpAcceleration;
        controller.gui = gUI;
    }

    void OnBeginRun() 
    {
        Destroy(gameObject);
    }
}
