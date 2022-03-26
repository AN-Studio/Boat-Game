using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OceanMesh : WaterMesh
{
    GameObject oceanFloor;

    #region WaveFunctions
        protected float WaveFunction(float t) 
        {
            float waveIntensity = GameManager.Instance.waveIntensity;
            float waveNoiseFactor = GameManager.Instance.waveNoiseFactor;

            // float t = nodes[nodes.Count-1].position.x * waveDeltaTime / wavePeriod + time;
            
            return waveIntensity * (
                (1 / (2*waveNoiseFactor)) * Mathf.Sin(t) + 
                (waveNoiseFactor) * Mathf.Pow( Mathf.Cos(t), 3 ) * Mathf.Sin(t)
            );
        }
    #endregion

    protected override void Start() 
    {
        base.Start();

        oceanFloor = new GameObject("Ocean Floor Collider", typeof(BoxCollider2D));
        oceanFloor.transform.parent = transform;
        oceanFloor.transform.localPosition = new Vector3(0, transform.position.y-waterDepth-.5f , 0);
        
        BoxCollider2D floorCollider = oceanFloor.GetComponent<BoxCollider2D>();
        floorCollider.size = new Vector2(longitude+despawnDistance, 1);
    }

    void Update() 
    {
        CheckCameraBounds();
        SyncWithShipTransform();
    }

    protected override void FixedUpdate()
    {
        GenerateWaves();
        base.FixedUpdate();
    }

    void GenerateWaves()
    {
        float wavePeriod = GameManager.Instance.wavePeriod;
        float waveDeltaTime = spreadSpeed * Time.fixedDeltaTime;
        float t = nodes[nodes.Count-1].position.x * waveDeltaTime / wavePeriod + time;
        
        nodes[nodes.Count-1].Disturb( WaveFunction(t) );

        time = ((time + Time.fixedDeltaTime) / wavePeriod) % (2*Mathf.PI);
    }
    void CheckCameraBounds() 
    {
        // Vector2 WorldUnitsInCamera;
        // WorldUnitsInCamera.y = cam.orthographicSize * 2;
        // WorldUnitsInCamera.x = WorldUnitsInCamera.y * Screen.width / Screen.height;
        
        // Vector2 leftMostPos = nodes[0].position;
        // Vector2 rightMostPos = nodes[nodes.Count - 1].position;
        Vector2 centerPos = nodes[nodes.Count/2].position;
        
        // float leftBound = Camera.main.transform.position.x - WorldUnitsInCamera.x / 2 - despawnDistance;
        // float rightBound = Camera.main.transform.position.x + WorldUnitsInCamera.x / 2 + despawnDistance;

        // if (leftMostPos.x < leftBound) {
        if (transform.position.x - centerPos.x > despawnDistance) {
            for (int i = 0; i < transform.position.x - centerPos.x; i++) CycleNodesRight(transform.position.x - centerPos.x);
        }

        // if (rightMostPos.x > rightBound) {
        if (centerPos.x - transform.position.x > despawnDistance) {
            for (int i = 0; i < centerPos.x - transform.position.x; i++) CycleNodesLeft(centerPos.x - transform.position.x);
        }
    }
    void SyncWithShipTransform()
    {
        Vector2 position = ShipController.Instance.transform.position;
        position.y = transform.parent.position.y;
        transform.parent.position = position;
    }

    public void CycleNodesRight(float cycleDelta)
    {
        float disturbance;
        WaterNode cycledNode;
        float waveIntensity = GameManager.Instance.waveIntensity;
        float wavePeriod = GameManager.Instance.wavePeriod;
        float waveDeltaTime = (spreadSpeed*Time.fixedDeltaTime);

        Vector2 position;
        for (int i = 1; i <= nodesPerUnit; i++)
        {
            cycledNode = nodes[0];
            nodes.Remove(cycledNode);

            cycledNode.Reset();
            position = cycledNode.position;
            position.x = nodes[nodes.Count-1].position.x + (positionDelta);
            cycledNode.position = position;

            float t = cycledNode.position.x * waveDeltaTime / wavePeriod + time;
            disturbance = WaveFunction(t);
            
            position = cycledNode.position;
            position.y = transform.position.y + disturbance;
            cycledNode.position = position;                
            cycledNode.velocity = (-nodes[nodes.Count-1].position.y + cycledNode.position.y) / waveDeltaTime;
            cycledNode.acceleration = (-nodes[nodes.Count-1].velocity + cycledNode.velocity) / waveDeltaTime;
            // cycledNode.disturbance = disturbance;
            
            nodes.Add(cycledNode);

            time = ((time + Time.fixedDeltaTime) / wavePeriod) % (2*Mathf.PI); 
        }
    }

    public void CycleNodesLeft(float cycleDelta)
    {
        float disturbance;
        WaterNode cycledNode;
        float waveIntensity = GameManager.Instance.waveIntensity;
        float wavePeriod = GameManager.Instance.wavePeriod;
        float waveDeltaTime = (spreadSpeed*Time.fixedDeltaTime);

        Vector2 position;
        for (int i = 1; i <= nodesPerUnit; i++)
        {
            cycledNode = nodes[nodes.Count -1];
            nodes.Remove(cycledNode);

            cycledNode.Reset();
            position = cycledNode.position;
            position.x = nodes[0].position.x - (positionDelta);
            cycledNode.position = position;
            
            float t = cycledNode.position.x * waveDeltaTime / wavePeriod + time;
            disturbance = WaveFunction(t);
            
            position = cycledNode.position;
            position.y = transform.position.y + disturbance;
            cycledNode.position = position;
            cycledNode.velocity = (nodes[nodes.Count-1].position.y - cycledNode.position.y) / waveDeltaTime;
            cycledNode.acceleration = (nodes[nodes.Count-1].velocity - cycledNode.velocity) / waveDeltaTime;
            // cycledNode.disturbance = disturbance;

            nodes.Insert(0, cycledNode);
            
            time = ((time + Time.fixedDeltaTime / wavePeriod)) % (2*Mathf.PI); 
        }
    }
}
