using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShipController : MonoBehaviour
{
    public ControllerTweaks tweaks;
    public AudioEventSheet audioSheet;
    public Ship ship;
    public ActionRegion jumpRegion;
    public GUIDisplay gui;

    Rigidbody2D rb;
    CapsuleCollider2D body;
    FixedJoint2D[] masts;
    Mast[] sails;
    SpriteRenderer[] renderers;

    [Range(0,1)] public float sailThrottle = 0;
    public float JumpForce {
        get => -tweaks.JumpForce * rb.mass * (1 + ship.keelWeightRatio) * Physics2D.gravity.y;
    }
    public float maxTiltAngle;
    
    bool wantsToJump = false;
    float totalMass;
    Vector2 centerOfMass;
    float tilt;
    static readonly Vector2 vector2UpRight = Vector2.up + Vector2.right;

    private FMOD.Studio.EventInstance woodCreakSFX;
    private FMOD.Studio.EventInstance sailWindUpSFX;

    float MaxTorque {
        get {
            float keelWeight = rb.mass * ship.keelWeightRatio * Physics2D.gravity.y;
            return tweaks.MaxTorque * keelWeight * ship.keelRelativePos.y * Mathf.Sin(maxTiltAngle * Mathf.Deg2Rad);
        }
    }

    bool hasReleasedTiltInput {
        get => tilt * rb.angularVelocity <= 0 || Mathf.Abs(tilt) < tweaks.GyroBias;
    }

    float JumpForceOffset {
        get => tilt * body.size.x * .1f;
    }

    #region MonoBehaviour Functions
        // Start is called before the first frame update
        void Start()
        {
            sailWindUpSFX = FMODUnity.RuntimeManager.CreateInstance(audioSheet["sailWindUp"]);
            woodCreakSFX = FMODUnity.RuntimeManager.CreateInstance(audioSheet["mastCreak"]);
            woodCreakSFX.start();

            tweaks = Resources.Load<ControllerTweaks>("Tweaks/Standard Config");
            body = GetComponentInChildren<CapsuleCollider2D>();
            masts = GetComponentsInChildren<FixedJoint2D>();
            sails = GetComponentsInChildren<Mast>();
            renderers = GetComponentsInChildren<SpriteRenderer>();
            
            rb = body.attachedRigidbody;
            rb.useAutoMass = true;

            centerOfMass = rb.centerOfMass * rb.mass;
            totalMass = rb.mass;
            foreach (var mast in masts) 
            {
                mast.connectedBody = rb;
                mast.anchor = mast.GetComponent<Collider2D>().bounds.extents * Vector2.down; 
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

            foreach(Mast sail in sails) sail.SetThrottle(sailThrottle);

            tilt = -GyroInput.GetTilt();
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
                ApplyTilt();
            }

            // print("This FixedUpdate:");
            // foreach (var mast in masts) 
            // {
            //     if (mast != null) print($"Reaction Force: {mast.reactionForce}");
            // }

        }

        private void OnDisable() {
            woodCreakSFX.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);    
        }

    #endregion

    public void OnThrottleChange(float value)
    {
        sailThrottle = value;

        FMOD.Studio.PLAYBACK_STATE playbackState;    
        sailWindUpSFX.getPlaybackState(out playbackState);
        
        if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
            sailWindUpSFX.start();
    } 

    public void SetShip(Ship data) => ship = data;
    public void Setup()
    {
        body.density = ship.colliderDensity;

        foreach (var mast in masts) 
        {
            mast.frequency = ship.mastRigidity;
            mast.gameObject.tag = "Mast";
        }

        foreach (Mast sail in sails) 
        {
            sail.ship = ship;
            sail.audioSheet = audioSheet;
        }

        jumpRegion.onBegin += InitiateJump;
        // jumpRegion.OnEnded ;
    }

    void UpdateDrag()
    {
        float depth = Mathf.Min(body.size.x, body.size.y);
        Vector2 crossSection = new Vector2(body.size.y, body.size.x);

        Vector2 dragCoefficient = 
            ship.bodyDrag * crossSection * depth
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
            rb.angularDrag = tweaks.AngularDrag * depth * Mathf.Max(ship.bodyDrag.x, ship.bodyDrag.y);
        }
        else
        {
            rb.angularDrag = tweaks.AngularDrag * .001f * depth * Mathf.Max(ship.bodyDrag.x, ship.bodyDrag.y);
            rb.angularDrag *= hasReleasedTiltInput && Mathf.Abs(rb.angularVelocity) > tweaks.AngularSpeedBias ? 
                tweaks.AngularDamping : 1
            ; 
        }
    }

    void ApplyKeelWeight()
    {
        Vector2 size = Geometry.GetColliderSize(body);
        Vector2 center = rb.worldCenterOfMass;
        
        Vector2 keelWeight = rb.mass * ship.keelWeightRatio * Physics2D.gravity;
        // TODO: Rememberto restore this to just assigning the second value when done with testing.
        // I just did this to test if the tilt control felt better during airborne.
        Vector2 keelPos = //body.IsTouchingLayers(LayerMask.GetMask("Water")) ?
            //center :
            center + ((vector2UpRight * size/2) * ship.keelRelativePos).Rotate(rb.rotation)
        ;

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
            // print("Jumping!");
            
            Vector2 point = body.bounds.center;
            point.x += JumpForceOffset;
            point = point.Rotate(transform.rotation.z);
            
            rb.AddForceAtPosition((JumpForce * Vector2.up), point, ForceMode2D.Impulse);
            wantsToJump = false;

            // StartCoroutine(ScaleLerp(rb.velocity.y, gameObject.layer != LayerMask.NameToLayer("Back Entities")));
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

    void InitiateJump() => wantsToJump = true;

    void ApplyTilt()
    {
        // print($"Angular Velocity: {rb.angularVelocity}\nTilt Input:{tilt}");
        rb.AddTorque(tilt * MaxTorque);
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
