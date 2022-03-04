using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sail : MonoBehaviour {
    public AudioEventSheet audioSheet;
    public Ship ship;
    private FMOD.Studio.EventInstance mastCrackingSFX;
    private FMOD.Studio.EventInstance mastSnappingSFX;
    public ShipController controller;
    public Rigidbody2D rb;
    public new BoxCollider2D collider;
    public Transform sailTransform;
    Material sailMaterial;

    private float raisedHeight;
    private float loweredHeight;
    private float throttle = 0;
    const float airDensity = 0.001f;
    private bool isBroken = false;
    private Coroutine breakSequence;
    private WaitForSeconds timeUntilBreak = new WaitForSeconds(3f);

    private void Start() 
    {
        mastCrackingSFX = FMODUnity.RuntimeManager.CreateInstance(audioSheet["mastCracking"]);
        mastSnappingSFX = FMODUnity.RuntimeManager.CreateInstance(audioSheet["mastSnapping"]);

        controller = GetComponentInParent<ShipController>();
        collider = GetComponent<BoxCollider2D>();
        sailTransform = transform.GetChild(0);
        SpriteRenderer sailSprite = sailTransform.GetComponentInChildren<SpriteRenderer>();
        sailMaterial = sailSprite.material;
        raisedHeight = sailTransform.localPosition.y;
        loweredHeight = raisedHeight - sailSprite.bounds.size.y;
        
        rb = collider.attachedRigidbody;
        rb.useAutoMass = false;
        rb.mass = 0;
    }
    private void Update() 
    {
        Vector3 position = sailTransform.localPosition;
        position.y = Mathf.Lerp(raisedHeight, loweredHeight, 1-throttle);
        sailMaterial.SetFloat("_AlphaThreshold", throttle);
        sailTransform.localPosition = position;
    }

    private void FixedUpdate() 
    {
        // if (!isBroken) 
            ApplyWindDrag();
    }

    private void ApplyWindDrag()
    {
        GameManager gameManager = GameManager.Instance;
        
        Vector2 relativeVelocity = (gameManager.windSpeed - rb.velocity.x) * Vector2.right;
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
        
        // print($"Drag Force: {dragForce}");
        if (Mathf.Abs(dragForce.x) > ship.mastStrength)
        {
            if (!isBroken && breakSequence == null) 
                breakSequence = StartCoroutine(StartBreakSequence());
        }
        else
        {
            if (breakSequence != null)
            {
                StopCoroutine(breakSequence);
                mastCrackingSFX.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            }
            breakSequence = null;
        }

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

        print("Breaking Mast!");
        mastSnappingSFX.start();

        Destroy(GetComponent<Joint2D>());
        gameObject.layer = transform.parent.gameObject.layer == LayerMask.NameToLayer("Default")?
            LayerMask.NameToLayer("Default") :
            LayerMask.NameToLayer("Back Entities") 
        ;
        isBroken = true;
        rb.useAutoMass = true;
        collider.density = ship.mastDensity;
    }

    // private IEnumerator BreakMast()
    // {
    //     mastCrackingSFX.start();

    //     FMOD.Studio.PLAYBACK_STATE playbackState;    
    //     bool isPlaying = true;

    //     while (isPlaying)
    //     {
    //         yield return null;

    //         mastCrackingSFX.getPlaybackState(out playbackState);
    //         isPlaying = playbackState != FMOD.Studio.PLAYBACK_STATE.STOPPED;
    //     }

    //     print("Breaking Mast!");
    //     mastSnappingSFX.start();

    //     Destroy(GetComponent<Joint2D>());
    //     gameObject.layer = transform.parent.gameObject.layer == LayerMask.NameToLayer("Default")?
    //         LayerMask.NameToLayer("Default") :
    //         LayerMask.NameToLayer("Back Entities") 
    //     ;
    //     isBroken = true;
    // }

    public void SetThrottle(float value) => throttle = Mathf.Clamp01(value);
}