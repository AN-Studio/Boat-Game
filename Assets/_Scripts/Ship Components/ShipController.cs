using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ShipController : StaticInstance<ShipController>
{
    #region References
        public ControllerTweaks tweaks;
        public AudioEventSheet audioSheet;
        public Ship ship;
        public ActionRegion jumpRegion;
        public GUIDisplay gui;

        #region Internal
            internal Rigidbody2D rb;
            internal CapsuleCollider2D body;
            FixedJoint2D[] masts;
            Mast[] sails;
            SpriteRenderer[] renderers;
        #endregion

    #endregion

    #region Variables
        [Range(0,1)] public float sailThrottle = 0;
        
        bool wantsToJump = false;
        bool jumpIsEnabled = true;
        float totalMass;
        Vector2 centerOfMass;
        float tilt;
    #endregion

    #region Constants
        static readonly Vector2 vector2UpRight = Vector2.up + Vector2.right;
        static LayerMask waterMask;
        static WaitForSeconds timeUntilEnabledJump;
    #endregion

    #region FMOD
        private FMOD.Studio.EventInstance woodCreakSFX;
        private FMOD.Studio.EventInstance sailWindUpSFX;
    #endregion

    #region Properties & Shorthands
        float MaxTorque {
            get {
                float keelWeight = rb.mass * ship.keelWeightRatio * Physics2D.gravity.y;
                return tweaks.MaxTorque * keelWeight * ship.keelRelativePos.y * Mathf.Sin(tweaks.MaxTiltAngle * Mathf.Deg2Rad);
            }
        }
        float JumpForce {
            get => -tweaks.JumpForce * rb.mass * (1 + ship.keelWeightRatio) * Physics2D.gravity.y;
        }
        float JumpForceOffset {
            get => tilt * body.size.x * .1f;
        }
        bool hasReleasedTiltInput {
            get => tilt * rb.angularVelocity <= 0 || Mathf.Abs(tilt) < tweaks.GyroBias;
        }
        bool IsTouchingWater {
            get => rb.IsTouchingLayers(waterMask);
        }
    #endregion

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

            waterMask = LayerMask.GetMask("Water");

            Setup();
        }

        private void Update() 
        {
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

        #region On Events
            public void OnThrottleChange(float value)
            {
                sailThrottle = value;

                FMOD.Studio.PLAYBACK_STATE playbackState;    
                sailWindUpSFX.getPlaybackState(out playbackState);
                
                if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED)
                    sailWindUpSFX.start();
            } 
        #endregion

    #endregion

    #region Initialization
        public void SetShip(Ship data) => ship = data;
        public void Setup()
        {
            timeUntilEnabledJump = new WaitForSeconds(tweaks.TimeUntilNextJump);
            
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

            jumpRegion.onBegin += Jump;
            // jumpRegion.onStay += Jump;
            // jumpRegion.OnEnded ;
        }
    #endregion

    #region Actions & Commands
        void Jump() 
        {
            if (jumpIsEnabled && IsTouchingWater)
            {
                wantsToJump = true;
                StartCoroutine(JumpTimer());
            }
        }
    #endregion

    #region Force Application
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
            Vector2 keelPos = center + ((vector2UpRight * size/2) * ship.keelRelativePos).Rotate(rb.rotation);

            // Debug.Log($"Keel Position: {keelPos}");

            rb.AddForceAtPosition(keelWeight, keelPos);
        }

        void ApplyJumpForce()
        {
            // Debug.Log($"IsTouchingWater: {IsTouchingWater}");

            if (wantsToJump && IsTouchingWater && !GameManager.Instance.gameEnded)
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

        void ApplyTilt()
        {
            // print($"Angular Velocity: {rb.angularVelocity}\nTilt Input:{tilt}");
            rb.AddTorque(tilt * MaxTorque * (IsTouchingWater? tweaks.InWaterTorque : 1f) );
        }

    #endregion

    IEnumerator JumpTimer()
    {
        jumpIsEnabled = false;

        #if UNITY_EDITOR
            yield return new WaitForSeconds(tweaks.TimeUntilNextJump);
        #else
            yield return timeUntilEnabledJump;
        #endif

        jumpIsEnabled = true;
    }

    #region Legacy Code
        // void SwitchLane()
        // {
        //     gameObject.layer = gameObject.layer == LayerMask.NameToLayer("Back Entities") ? 
        //         LayerMask.NameToLayer("Default") :
        //         LayerMask.NameToLayer("Back Entities")
        //     ;

        //     foreach (var renderer in renderers)
        //     {
        //         renderer.sortingLayerName = gameObject.layer == LayerMask.NameToLayer("Back Entities") ?
        //             "Back Water" :
        //             "Default"
        //         ;
        //     }
        // }

        // IEnumerator ScaleLerp(float startSpeed, bool scalingToBackLane = true)
        // {
        //     float startScale = scalingToBackLane? 1f : .75f;
        //     float endScale = scalingToBackLane? .75f : 1f;

        //     float currentScale = startScale; 
        //     Vector3 localScale = new Vector3(currentScale, currentScale, 1);
        //     while (rb.velocity.y > 0)
        //     {
        //         float t = (startSpeed - rb.velocity.y) / startSpeed;
        //         currentScale = Mathf.Lerp(startScale, endScale, t);

        //         localScale.x = currentScale;
        //         localScale.y = currentScale;
        //         transform.localScale = localScale;

        //         yield return null;
        //     }
            
        //     localScale.x = endScale;
        //     localScale.y = endScale;
        //     transform.localScale = localScale;

        //     SwitchLane();
        // }
    #endregion
}
