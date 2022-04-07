using UnityEngine;
using UnityEngine.UI;
using Cinemachine;

public class ShipSpawner : StaticInstance<ShipSpawner>
{
    #region References
        [Header("References")]
        public CinemachineVirtualCamera cam;
        public Slider slider;
        public GUIDisplay gUI;
        public ActionRegion jumpRegion;
        public OceanMesh ocean;
        public CinemachineTargetGroup targetGroup;
        GameObject boatInstance;
    #endregion

    // #region Settings
    //     [Header("Global Ship Settings")]
    //     [SerializeField] float maxTiltAngle = 45;
    // #endregion

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

        Transform setupObject = GameObject.Find("Setup").transform;
        boatInstance = Instantiate(ship.prefab, transform.position, Quaternion.identity, setupObject);
        boatInstance.tag = "Player";
        boatInstance.layer = LayerMask.NameToLayer("Player");

        // cam.Follow = boatInstance.transform;
        // CinemachineTargetGroup targetGroup = cam.GetComponent<CinemachineTargetGroup>();
        targetGroup.AddMember(boatInstance.transform, 2, boatInstance.GetComponent<SpriteRenderer>().bounds.size.x);
        targetGroup.AddMember(ocean.transform, 1, ocean.waterDepth/2f);

        ShipController controller = boatInstance.GetComponentInChildren<ShipController>();

        controller.SetShip(ship);
        slider.onValueChanged.AddListener(controller.OnThrottleChange);
        controller.jumpRegion = jumpRegion;
        controller.gui = gUI;
        controller.audioSheet = audioSheet;
    }

    void OnBeginRun() 
    {
        Destroy(gameObject);
    }
}
