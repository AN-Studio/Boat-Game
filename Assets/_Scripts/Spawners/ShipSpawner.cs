using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class ShipSpawner : MonoBehaviour
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
    [SerializeField] Ship ship;
    [SerializeField] AudioEventSheet audioSheet;

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

        boatInstance = Instantiate(ship.prefab, transform.position, Quaternion.identity);
        boatInstance.tag = "Player";
        boatInstance.layer = LayerMask.NameToLayer("Default");

        cam.Follow = boatInstance.transform;

        ShipController controller = boatInstance.GetComponentInChildren<ShipController>();

        controller.SetShip(ship);
        slider.onValueChanged.AddListener(controller.OnThrottleChange);
        controller.jumpRegion = jumpRegion;
        controller.jumpAcceleration = jumpAcceleration;
        controller.gui = gUI;
        controller.audioSheet = audioSheet;
    }

    void OnBeginRun() 
    {
        Destroy(gameObject);
    }
}
