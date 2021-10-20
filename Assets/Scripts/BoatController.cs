using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoatController : MonoBehaviour
{
    public float forwardForce;
    public float jumpForce;
    public float tiltTorque;
    public BoatProperties properties;
    
    #region Private Variables
        Rigidbody2D rb;
        new CapsuleCollider2D collider;
        new SpriteRenderer renderer;

        public bool gameStarted = false;
        public bool gameEnded = false;
        bool wantsToJump = false;
        float tilt = 0;
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

            float depth = Mathf.Min(properties.colliderSize.x, properties.colliderSize.y);
            rb.angularDrag = 2 * depth * Mathf.Max(properties.dragCoefficient.x, properties.dragCoefficient.y);
            
            UpdateDrag();
        }

        private void Update() {
            ReadInputs();
        }

        private void FixedUpdate() 
        {
            UpdateDrag();
            UpdateAngularDrag();
            
            ApplyAcceleration();
            ApplyTilt();

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
        float depth = Mathf.Min(properties.colliderSize.x, properties.colliderSize.y);
        
        Vector2 dragCoefficient = //WaterGenerator.RotateVector(
            properties.dragCoefficient * properties.colliderSize * depth//, 
            // rb.rotation
        ;

        LayerMask mask = LayerMask.GetMask("Water");
        if (!collider.IsTouchingLayers(mask))
            dragCoefficient *= .001f;

        rb.drag = (dragCoefficient * rb.velocity.normalized).magnitude;
    }

    void UpdateAngularDrag()
    {
        float depth = Mathf.Min(properties.colliderSize.x, properties.colliderSize.y);
        
        LayerMask mask = LayerMask.GetMask("Water");
        if (collider.IsTouchingLayers(mask))
        {
            rb.angularDrag = 2 * depth * Mathf.Max(properties.dragCoefficient.x, properties.dragCoefficient.y);
        }
        else
        {
            rb.angularDrag = 2 * .001f * depth * Mathf.Max(properties.dragCoefficient.x, properties.dragCoefficient.y);
        }
    }

    void ApplyKeelWeight()
    {
        Vector2 size = WaterGenerator.GetColliderSize(collider);
        Vector2 center = rb.worldCenterOfMass;
        
        Vector2 keelWeight = rb.mass * properties.keelWeightRatio * Physics2D.gravity;
        Vector2 keelPos = center + WaterGenerator.RotateVector((Vector2.down * size) * properties.keelRelativePos, rb.rotation);

        // Debug.Log($"Keel Position: {keelPos}");

        rb.AddForceAtPosition(keelWeight, keelPos);
    }

    void ApplyAcceleration()
    {
        LayerMask mask = LayerMask.GetMask("Water");
        bool isTouchingWater = collider.IsTouchingLayers(mask);

        // Debug.Log($"IsTouchingWater: {isTouchingWater}");

        if (wantsToJump && isTouchingWater && !gameEnded)
        {
            rb.AddForce(WaterGenerator.RotateVector(jumpForce * (new Vector2(.75f,1)).normalized, rb.rotation), ForceMode2D.Impulse);
            wantsToJump = false;
        }
    }

    void ApplyTilt()
    {
        rb.AddTorque(tilt * tiltTorque);
    }

    void ReadInputs()
    {
        #if (UNITY_ANDROID || UNITY_IOS) 
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                
                switch(touch.phase)
                {
                    case TouchPhase.Began:
                        wantsToJump = true;
                        break;
                    case TouchPhase.Ended:    
                        wantsToJump = false;
                        break;
                    default:
                        break;
                }

            }

            tilt = Input.acceleration.x;
        #endif

        #if (UNITY_EDITOR || UNITY_STANDALONE)
            tilt = -Input.GetAxis("Horizontal");
            tilt = Mathf.Clamp(tilt, -1, 1);

            if (Input.GetMouseButtonDown(0)){
                wantsToJump = true;
            } else {
                wantsToJump = false;
            }
        #endif
    }

}
