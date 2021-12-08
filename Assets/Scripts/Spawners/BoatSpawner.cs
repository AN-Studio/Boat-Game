using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class BoatSpawner : MonoBehaviour
{
    public CinemachineVirtualCamera cam;
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
        boatInstance.layer = LayerMask.NameToLayer("Player");

        cam.Follow = boatInstance.transform;

        ShipController controller = boatInstance.GetComponentInChildren<ShipController>();

        controller.SetProperties(boatData);
    }

    void OnBeginRun() 
    {
        Destroy(gameObject);
    }
}
