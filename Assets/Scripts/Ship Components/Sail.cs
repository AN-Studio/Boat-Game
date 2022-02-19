using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sail : MonoBehaviour {
    [FMODUnity.EventRef] public string mastBreakSFX;
    private FMOD.Studio.EventInstance mastBreakSFXInstance;
    public ShipController controller;
    public Rigidbody2D rb;
    public new BoxCollider2D collider;
    public Transform sailTransform;
    Material sailMaterial;

    private float raisedHeight;
    private float loweredHeight;
    private float throttle = 0;
    public float dragCoefficient = 2.2f;
    const float airDensity = 0.001f;
    private bool isBroken = false;
    private Coroutine breakSequence;
    private WaitForSeconds timeUntilBreak = new WaitForSeconds(3f);

    private void Start() 
    {
        mastBreakSFXInstance = FMODUnity.RuntimeManager.CreateInstance(mastBreakSFX);

        controller = GetComponentInParent<ShipController>();
        collider = GetComponent<BoxCollider2D>();
        sailTransform = transform.GetChild(0);
        SpriteRenderer sailSprite = sailTransform.GetComponentInChildren<SpriteRenderer>();
        sailMaterial = sailSprite.material;
        raisedHeight = sailTransform.localPosition.y;
        loweredHeight = raisedHeight - sailSprite.bounds.size.y;
        
        rb = collider.attachedRigidbody;
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
        GameManager gameManager = GameManager.Instance;
        
        Vector2 relativeVelocity = (gameManager.windSpeed - rb.velocity.x) * Vector2.right;
        float sailArea = Mathf.Max(collider.size.x*collider.size.x, collider.size.y*collider.size.y);

        Vector2 dragForce = airDensity * dragCoefficient * relativeVelocity.sqrMagnitude * sailArea * throttle * relativeVelocity.normalized; 
        Vector2 centerOfDrag = transform.TransformPoint(0, -collider.size.y * .5f, 0);

        // rb.AddForce(dragForce);
        rb.AddForceAtPosition(dragForce, centerOfDrag);

        float mastStrength = controller.properties.mastStrength;
        controller.gui.UpdateTension(dragForce.x, mastStrength);
        FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Tension", dragForce.x / mastStrength);
        
        // print($"Drag Force: {dragForce}");
        if (Mathf.Abs(dragForce.x) > mastStrength)
        {
            if (!isBroken && breakSequence == null) 
                breakSequence = StartCoroutine(StartBreakSequence());
        }
        else
        {
            if (breakSequence != null)
                StopCoroutine(breakSequence);
            breakSequence = null;
        }
    }

    private IEnumerator StartBreakSequence()
    {
        yield return timeUntilBreak;

        StartCoroutine(BreakMast());
    }

    private IEnumerator BreakMast()
    {
        mastBreakSFXInstance.start();

        bool isPaused = false;
        while (!isPaused)
        {
            yield return null;

            mastBreakSFXInstance.getPaused(out isPaused);
        }

        Destroy(GetComponent<Joint2D>());
        gameObject.layer = transform.parent.gameObject.layer == LayerMask.NameToLayer("Default")?
            LayerMask.NameToLayer("Default") :
            LayerMask.NameToLayer("Back Entities") 
        ;
    }

    public void SetThrottle(float value) => throttle = Mathf.Clamp01(value);
}