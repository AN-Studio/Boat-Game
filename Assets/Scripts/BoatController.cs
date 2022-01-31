using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.Universal;

public class BoatController : MonoBehaviour
{
    #region References
        [Header("References")]
        Rigidbody2D rb;
        new CapsuleCollider2D collider;
        new SpriteRenderer renderer;
    #endregion

    #region Settings
        [Header("Settings")]
        public float forwardForce;
        public float jumpForce;
        public float tiltTorque;
        public BoatProperties properties;
    #endregion
    
    #region Private Variables
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

        private void Update() 
        {
            ReadInputs();
            if (!GameManager.Instance.gameStarted && Input.GetButtonDown("Fire1")) 
            {
                GameManager.Instance.gameStarted = true;
                WaterGenerator.Instance.waveIntensity = 5;
            }
        }

        private void FixedUpdate() 
        {
            UpdateDrag();
            UpdateAngularDrag();
            ApplyKeelWeight();    
            
            GameManager gameManager = GameManager.Instance;
            if (gameManager.gameStarted && !gameManager.gameEnded)
            {
                ApplyForwardForce();
                ApplyJumpForce();
                ApplyTilt();
            }

        }
    #endregion
    void ApplyForwardForce()
    {
        LayerMask mask = LayerMask.GetMask("Water");
        GameManager gameManager = GameManager.Instance;

        if (collider.IsTouchingLayers(mask))
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
        Vector2 keelPos = center + ((Vector2.down * size) * properties.keelRelativePos).Rotate(rb.rotation);

        // Debug.Log($"Keel Position: {keelPos}");

        rb.AddForceAtPosition(keelWeight, keelPos);
    }

    void ApplyJumpForce()
    {
        LayerMask mask = LayerMask.GetMask("Water");
        bool isTouchingWater = collider.IsTouchingLayers(mask);

        // Debug.Log($"IsTouchingWater: {isTouchingWater}");

        if (wantsToJump && isTouchingWater && !GameManager.Instance.gameEnded)
        {
            rb.AddForce((jumpForce * (new Vector2(.75f,1)).normalized).Rotate(rb.rotation), ForceMode2D.Impulse);
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
            // if (Input.touchCount > 0)
            // {
            //     Touch touch = Input.GetTouch(0);
                
            //     switch(touch.phase)
            //     {
            //         case TouchPhase.Began:
            //             wantsToJump = true;
            //             break;
            //         case TouchPhase.Ended:    
            //             wantsToJump = false;
            //             break;
            //         default:
            //             break;
            //     }

            // }

            tilt = Input.acceleration.x;
        #endif

        // #if (UNITY_EDITOR || UNITY_STANDALONE)
            tilt = -Input.GetAxis("Horizontal");
            tilt = Mathf.Clamp(tilt, -1, 1);

            if (Input.GetButtonDown("Fire1")){
                wantsToJump = true;
            } else {
                wantsToJump = false;
            }
        // #endif
    }

}