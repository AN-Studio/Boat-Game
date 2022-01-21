using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShipController : MonoBehaviour
{
    public BoatSpecs properties;
    // public PlayerEventSheet audioEvents;
    public ActionRegion jumpRegion;
    public GUIDisplay gui;

    Rigidbody2D rb;
    CapsuleCollider2D body;
    FixedJoint2D[] masts;
    Sail[] sails;
    SpriteRenderer[] renderers;

    [Range(0,1)] public float sailThrottle = 0;
    public float jumpAcceleration = 10;
    
    bool wantsToJump = false;
    float totalMass;
    Vector2 centerOfMass;
    Coroutine scaleLerp;

    #region Audio Event Instances
        FMOD.Studio.EventInstance jumpEvent;
        FMOD.Studio.EventInstance raiseSailEvent;
        FMOD.Studio.EventInstance lowerSailEvent;
    #endregion

    #region MonoBehaviour Functions
        private void OnDestroy() 
        {
            jumpEvent.release();
            raiseSailEvent.release();
            lowerSailEvent.release();
        }

        void Awake() 
        {
        }

        // Start is called before the first frame update
        void Start()
        {
            body = GetComponentInChildren<CapsuleCollider2D>();
            masts = GetComponentsInChildren<FixedJoint2D>();
            sails = GetComponentsInChildren<Sail>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
            
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

            // if (string.IsNullOrEmpty(audioEvents.jump)) 
                // jumpEvent = FMODUnity.RuntimeManager.CreateInstance(audioEvents.jump);
            // if (string.IsNullOrEmpty(audioEvents.raiseSail)) 
                // raiseSailEvent = FMODUnity.RuntimeManager.CreateInstance(audioEvents.raiseSail);
            // if (string.IsNullOrEmpty(audioEvents.lowerSail)) 
                // lowerSailEvent = FMODUnity.RuntimeManager.CreateInstance(audioEvents.lowerSail);
            
        }

        private void Update() 
        {
            ReadInputs();
            if (!GameManager.Instance.gameStarted && Input.GetButtonDown("Fire1")) 
            {
                GameManager.Instance.gameStarted = true;
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
                ApplyJumpForce();
            }

            // print("This FixedUpdate:");
            // foreach (var mast in masts) 
            // {
            //     if (mast != null) print($"Reaction Force: {mast.reactionForce}");
            // }

        }
    #endregion

    public void OnThrottleChange(float value)
    {
        if (value > sailThrottle && raiseSailEvent.isValid()) raiseSailEvent.start();
        else if (value < sailThrottle && lowerSailEvent.isValid()) lowerSailEvent.start();
        
        sailThrottle = value;    
    } 

    public void SetProperties(BoatSpecs data) => properties = data;
    // // public void SetAudioEvents(PlayerEventSheet sheet) => audioEvents = sheet;
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

        jumpRegion.onBegin += InitiateJump;
        // jumpRegion.OnEnded ;
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
            mask = LayerMask.GetMask("Water");

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
            mask = LayerMask.GetMask("Water");

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
            mask = LayerMask.GetMask("Water");

        bool isTouchingWater = body.IsTouchingLayers(mask);

        // Debug.Log($"IsTouchingWater: {isTouchingWater}");

        if (wantsToJump && isTouchingWater && !GameManager.Instance.gameEnded)
        {
            print("Triggering jump");
            rb.AddForceAtPosition((jumpAcceleration * rb.mass * Vector2.up), transform.TransformPoint(centerOfMass), ForceMode2D.Impulse);
            wantsToJump = false;
            if (jumpEvent.isValid()) jumpEvent.start();

            if (scaleLerp != null) StopCoroutine(scaleLerp);
            scaleLerp = StartCoroutine(ScaleLerp(rb.velocity.y, gameObject.layer != LayerMask.NameToLayer("Back Entities")));
        }
    }

    void SwitchLane()
    {
        gameObject.layer = gameObject.layer == LayerMask.NameToLayer("Back Entities") ? 
            LayerMask.NameToLayer("Default") :
            LayerMask.NameToLayer("Back Entities")
        ;

        foreach (var renderer in renderers)
        {
            renderer.sortingLayerName = gameObject.layer == LayerMask.NameToLayer("Back Entities") ?
                "Back Water" :
                "Default"
            ;
        }
    }

    IEnumerator ScaleLerp(float startSpeed, bool scalingToBackLane = true)
    {
        float startScale = scalingToBackLane? 1f : .75f;
        float endScale = scalingToBackLane? .75f : 1f;

        float currentScale = startScale; 
        Vector3 localScale = new Vector3(currentScale, currentScale, 1);
        while (rb.velocity.y > 0)
        {
            float t = (startSpeed - rb.velocity.y) / startSpeed;
            currentScale = Mathf.Lerp(startScale, endScale, t);

            localScale.x = currentScale;
            localScale.y = currentScale;
            transform.localScale = localScale;

            yield return null;
        }
        
        localScale.x = endScale;
        localScale.y = endScale;
        transform.localScale = localScale;

        SwitchLane();
    }

    IEnumerator LerpScaleAndSwitch(bool scalingToBackLane = true) 
    {
        while (rb.velocity.y > 0)
        {
            yield return ScaleLerp(rb.velocity.y);
        }

        SwitchLane();
    }

    void InitiateJump() => wantsToJump = true;

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


            // if (Input.GetButtonDown("Fire1")){
            //     wantsToJump = true;
            // } else {
            //     wantsToJump = false;
            // }
        // #endif
    }
}
