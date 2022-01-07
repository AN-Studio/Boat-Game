using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShipController : MonoBehaviour
{
    public BoatSpecs properties;

    Rigidbody2D rb;
    CapsuleCollider2D body;
    FixedJoint2D[] masts;
    Sail[] sails;

    [Range(0,1)] public float sailThrottle = 0;
    public float jumpForce = 100;
    
    bool wantsToJump = false;
    float totalMass;
    Vector2 centerOfMass;

    #region MonoBehaviour Functions
        // Start is called before the first frame update
        void Start()
        {
            body = GetComponentInChildren<CapsuleCollider2D>();
            masts = GetComponentsInChildren<FixedJoint2D>();
            sails = GetComponentsInChildren<Sail>();
            
            rb = body.attachedRigidbody;

            centerOfMass = rb.centerOfMass * rb.mass;
            totalMass = rb.mass;
            foreach (var mast in masts) 
            {
                centerOfMass += mast.attachedRigidbody.mass * mast.attachedRigidbody.centerOfMass;
                totalMass += mast.attachedRigidbody.mass;
            }
            centerOfMass /= totalMass;

            Setup();
        }

        private void Update() 
        {
            ReadInputs();
            if (!GameManager.Instance.gameStarted && Input.GetButtonDown("Fire1")) 
            {
                GameManager.Instance.gameStarted = true;
                // GameManager.Instance.waveIntensity = 2;
            }

            foreach(Sail sail in sails) sail.SetThrottle(sailThrottle);
        }

        private void FixedUpdate() 
        {
            UpdateDrag();
            UpdateAngularDrag();
            ApplyKeelWeight();    
            
            GameManager gameManager = GameManager.Instance;
            if (gameManager.gameStarted && !gameManager.gameEnded)
            {
                // ApplyForwardForce();
                // ApplyJumpForce();
                // ApplyTilt();
            }

            // print("This FixedUpdate:");
            // foreach (var mast in masts) 
            // {
            //     if (mast != null) print($"Reaction Force: {mast.reactionForce}");
            // }

        }
    #endregion

    public void OnThrottleChange(float value) => sailThrottle = value;

    public void SetProperties(BoatSpecs data) => properties = data;
    public void Setup()
    {
        body.density = properties.colliderDensity;

        foreach (var mast in masts) 
        {
            // mast.breakForce = properties.mastStrength;
            mast.frequency = properties.mastRigidity;
            mast.gameObject.GetComponent<Collider2D>().density = properties.mastDensity;
            mast.gameObject.tag = "Mast";
        }

        foreach (Sail sail in sails) 
        {
            sail.dragCoefficient = properties.averageSailDrag;
        }
    }

    void UpdateDrag()
    {
        float depth = Mathf.Min(body.size.x, body.size.y);
        
        Vector2 dragCoefficient = 
            properties.bodyDrag * body.size * depth
        ;

        LayerMask mask;
        if (gameObject.layer == LayerMask.NameToLayer("Back Entities"))
            mask = LayerMask.GetMask("Back Water");
        else 
            mask = LayerMask.GetMask("Front Water");

        if (!body.IsTouchingLayers(mask))
            dragCoefficient *= .001f;

        rb.drag = (dragCoefficient * rb.velocity.normalized).magnitude;
    }

    void UpdateAngularDrag()
    {
        float depth = Mathf.Min(body.size.x, body.size.y);

        LayerMask mask;
        if (gameObject.layer == LayerMask.NameToLayer("Back Entities"))
            mask = LayerMask.GetMask("Back Water");
        else 
            mask = LayerMask.GetMask("Front Water");

        if (body.IsTouchingLayers(mask))
        {
            rb.angularDrag = 2 * depth * Mathf.Max(properties.bodyDrag.x, properties.bodyDrag.y);
        }
        else
        {
            rb.angularDrag = 2 * .001f * depth * Mathf.Max(properties.bodyDrag.x, properties.bodyDrag.y);
        }
    }

    void ApplyKeelWeight()
    {
        Vector2 size = WaterGenerator.GetColliderSize(body);
        Vector2 center = rb.worldCenterOfMass;
        
        Vector2 keelWeight = rb.mass * properties.keelWeightRatio * Physics2D.gravity;
        Vector2 keelPos = center + ((Vector2.down * size) * properties.keelRelativePos).Rotate(rb.rotation);

        // Debug.Log($"Keel Position: {keelPos}");

        rb.AddForceAtPosition(keelWeight, keelPos);
    }

    void ApplyJumpForce()
    {
        LayerMask mask;
        if (gameObject.layer == LayerMask.NameToLayer("Back Entities"))
            mask = LayerMask.GetMask("Back Water");
        else 
            mask = LayerMask.GetMask("Front Water");

        bool isTouchingWater = body.IsTouchingLayers(mask);

        // Debug.Log($"IsTouchingWater: {isTouchingWater}");

        if (wantsToJump && isTouchingWater && !GameManager.Instance.gameEnded)
        {
            rb.AddForceAtPosition((jumpForce * Vector2.up).Rotate(rb.rotation), transform.TransformPoint(centerOfMass), ForceMode2D.Impulse);
            wantsToJump = false;
        }
    }

    // void ApplyTilt()
    // {
    //     rb.AddTorque(tilt * tiltTorque);
    // }

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

            // tilt = Input.acceleration.x;
        #endif

        // #if (UNITY_EDITOR || UNITY_STANDALONE)
            // tilt = -Input.GetAxis("Horizontal");
            // tilt = Mathf.Clamp(tilt, -1, 1);


            if (Input.GetButtonDown("Fire1")){
                wantsToJump = true;
            } else {
                wantsToJump = false;
            }
        // #endif
    }
}
