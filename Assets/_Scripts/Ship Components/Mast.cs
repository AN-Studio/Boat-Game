using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Mast : MonoBehaviour {

    struct Sail {
        public Transform transform;
        public SpriteRenderer renderer;
        public Material material;
        public float raisedHeight;
        public float loweredHeight;
    }

    public AudioEventSheet audioSheet;
    public Ship ship;
    private FMOD.Studio.EventInstance mastCrackingSFX;
    private FMOD.Studio.EventInstance mastSnappingSFX;
    public ShipController controller;
    public Rigidbody2D rb;
    public new BoxCollider2D collider;
    List<Sail> sails;

    private float throttle = 0;
    const float airDensity = 0.001f;
    private bool isBroken = false;
    private Coroutine breakSequence;
    private WaitForSeconds timeUntilBreak = new WaitForSeconds(3f);

    bool LandedWithMast {
        get => 
            collider.IsTouchingLayers(ShipController.waterMask) && 
            !controller.body.IsTouchingLayers(ShipController.waterMask)
        ;
    }

    private void OnCollisionEnter2D(Collision2D other) 
    {
        if (!this.isBroken)
            BreakMast();
    }

    private void OnTriggerEnter2D(Collider2D other) 
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Water") && 
        !this.isBroken && !controller.body.IsTouchingLayers(ShipController.waterMask))
            BreakMast();
    }

    private void Start() 
    {
        mastCrackingSFX = FMODUnity.RuntimeManager.CreateInstance(audioSheet["mastCracking"]);
        mastSnappingSFX = FMODUnity.RuntimeManager.CreateInstance(audioSheet["mastSnapping"]);

        controller = GetComponentInParent<ShipController>();
        collider = GetComponent<BoxCollider2D>();
        rb = GetComponent<Rigidbody2D>();
        // print(rb);
        
        rb.useAutoMass = false;
        rb.mass = 0;

        sails = new List<Sail>();
        foreach (Transform child in transform)
        {
            Sail sail = new Sail();
            sail.transform = child;
            sail.renderer = child.GetComponent<SpriteRenderer>();
            sail.material = sail.renderer.material;
            sail.raisedHeight = child.localPosition.y;
            sail.loweredHeight = sail.raisedHeight - sail.renderer.sprite.bounds.size.y;

            print(sail.renderer.sprite.bounds.size.y);

            sails.Add(sail);
        }
    }
    private void Update() 
    {
        foreach(Sail sail in sails)
        {
            Vector3 position = sail.transform.localPosition;
            position.y = Mathf.Lerp(sail.raisedHeight, sail.loweredHeight, 1-throttle);
            sail.transform.localPosition = position;
            sail.material.SetFloat("_AlphaThreshold", throttle);
        }

    }

    private void FixedUpdate() 
    {
        if (!isBroken) 
            ApplyWindDrag();
    }

    private void ApplyWindDrag()
    {
        GameManager gameManager = GameManager.Instance;
        
        Vector2 relativeVelocity = (gameManager.WindSpeed - rb.velocity.x) * Vector2.right;
        float sailArea = Mathf.Max(collider.size.x*collider.size.x, collider.size.y*collider.size.y);

        Vector2 dragForce = airDensity * ship.averageSailDrag * relativeVelocity.sqrMagnitude * sailArea * throttle * relativeVelocity.normalized; 
        Vector2 centerOfDrag = isBroken?
            transform.position :
            transform.TransformPoint(0, -collider.size.y * .5f, 0)
        ;

        // rb.AddForce(dragForce);
        rb.AddForceAtPosition(dragForce, centerOfDrag);

        controller.gui.UpdateTension(dragForce.x, ship.mastStrength);
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Tension", Mathf.Clamp01(dragForce.x / ship.mastStrength));
        
        // // Query mast tension to start or halt the mast break sequence
        // if (Mathf.Abs(dragForce.x) > ship.mastStrength)
        // {
        //     if (!isBroken && breakSequence == null) 
        //         breakSequence = StartCoroutine(StartBreakSequence());
        // }
        // else
        // {
        //     if (breakSequence != null)
        //     {
        //         StopCoroutine(breakSequence);
        //         mastCrackingSFX.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        //     }
        //     breakSequence = null;
        // }

    }

    private void CheckIntegrity()
    {
        if (!isBroken && LandedWithMast) BreakMast();

    }

    private IEnumerator StartBreakSequence()
    {
        yield return timeUntilBreak;

        // StartCoroutine(BreakMast());

        mastCrackingSFX.start();

        FMOD.Studio.PLAYBACK_STATE playbackState;    
        bool isPlaying = true;

        while (isPlaying)
        {
            yield return null;

            mastCrackingSFX.getPlaybackState(out playbackState);
            isPlaying = playbackState != FMOD.Studio.PLAYBACK_STATE.STOPPED;
        }

        BreakMast();
    }

    public void BreakMast()
    {
        print("Breaking Mast!");
        mastSnappingSFX.start();

        Destroy(GetComponent<Joint2D>());
        
        gameObject.layer = LayerMask.NameToLayer("Default");
        isBroken = true;
        rb.useAutoMass = true;
        collider.density = ship.mastDensity;

        GameManager.Instance.LoseGame();
    }

    public void SetThrottle(float value) => throttle = Mathf.Clamp01(value);
}