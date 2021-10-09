using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatBehaviour : MonoBehaviour
{
    new Collider2D collider;

    [Range(0,2)] public float keelWeightRatio = 2f;

    private void Awake() 
    {
        collider = GetComponent<Collider2D>();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void FixedUpdate() {
        ApplyKeelWeight();    
    }

    void ApplyKeelWeight()
    {
        Rigidbody2D rb = collider.attachedRigidbody;
        Vector2 size = WaterGenerator.GetColliderSize(collider);
        Vector2 center = rb.worldCenterOfMass;
        
        Vector2 keelWeight = rb.mass * keelWeightRatio * Physics2D.gravity;
        Vector2 keelPos = center + WaterGenerator.RotateVector(Vector2.down * size.y, rb.rotation);

        Debug.Log($"Keel Position: {keelPos}");

        rb.AddForceAtPosition(keelWeight, keelPos);
    }
}
