using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    [SerializeField] Transform endPoint;
    public Transform EndPoint {get=>endPoint;}

    Camera cam;

    // Start is called before the first frame update
    void Start()
    {
        if (endPoint == null) 
            throw new System.Exception($"Cell '{name}' does not have an endpoint set");    

        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        CheckCameraBounds();   
    }

    void CheckCameraBounds()
    {
        Vector2 WorldUnitsInCamera;
        WorldUnitsInCamera.y = cam.orthographicSize * 2;
        WorldUnitsInCamera.x = WorldUnitsInCamera.y * Screen.width / Screen.height;
        
        Vector2 leftMostPos = endPoint.position;
        float bound = Camera.main.transform.position.x - WorldUnitsInCamera.x / 2 - 100;

        if (leftMostPos.x < bound) {
            Despawn();
        }
    }
    void Despawn() 
    {
        WorldGenerator.Instance.DespawnCell(this);

        // GameManager.Instance.DecreaseCellCount();
        // Destroy(gameObject);
    }

}
