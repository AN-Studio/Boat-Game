using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatController : MonoBehaviour
{
    public float forwardForce;
    public BoatProperties properties;
    
    #region Private Variables
        Rigidbody2D rb;
        new CapsuleCollider2D collider;
        new SpriteRenderer renderer;

        public bool gameStarted = false;
        public bool gameEnded = false;
    #endregion

    #region MonoBehaviour Functions
        private void Awake() 
        {
            rb = GetComponent<Rigidbody2D>();
            collider = GetComponent<CapsuleCollider2D>();
            renderer = GetComponent<SpriteRenderer>();
        }
        
        // Start is called before the first frame update
        void Start()
        {
            renderer.sprite = properties.sprite;

            collider.offset = properties.colliderOffset;
            collider.size = properties.colliderSize;
            collider.density = properties.colliderDensity;

            rb.angularDrag = 2 * Mathf.Max(properties.dragCoefficient.x, properties.dragCoefficient.y);
            
            UpdateDrag();
        }

        private void FixedUpdate() 
        {
            UpdateDrag();
            ApplyForwardForce();
            ApplyKeelWeight();    
        }
    #endregion

    void ApplyForwardForce()
    {
        if (gameStarted && !gameEnded)
            rb.AddForce(Vector2.right * forwardForce);
    }

    void UpdateDrag()
    {
        Vector2 dragCoefficient = WaterGenerator.RotateVector(
            properties.dragCoefficient * properties.colliderSize, 
            rb.rotation
        );

        rb.drag = (dragCoefficient * rb.velocity.normalized).magnitude;
    }

    void ApplyKeelWeight()
    {
        Vector2 size = WaterGenerator.GetColliderSize(collider);
        Vector2 center = rb.worldCenterOfMass;
        
        Vector2 keelWeight = rb.mass * properties.keelWeightRatio * Physics2D.gravity;
        Vector2 keelPos = center + WaterGenerator.RotateVector((Vector2.down * size) * properties.keelRelativePos, rb.rotation);

        Debug.Log($"Keel Position: {keelPos}");

        rb.AddForceAtPosition(keelWeight, keelPos);
    }
}
