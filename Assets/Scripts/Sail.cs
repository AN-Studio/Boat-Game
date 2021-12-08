using UnityEngine;

public class Sail : MonoBehaviour {
    public Rigidbody2D rb;
    public new BoxCollider2D collider;
    public Transform sailTransform;

    private float raisedHeight;
    private float loweredHeight;
    private float throttle = 0;
    public float dragCoefficient = 2.2f;

    private void Start() 
    {
        collider = GetComponent<BoxCollider2D>();
        sailTransform = transform.GetChild(0);
        SpriteRenderer sailSprite = sailTransform.GetComponentInChildren<SpriteRenderer>();
        raisedHeight = sailTransform.localPosition.y;
        loweredHeight = raisedHeight - sailSprite.bounds.size.y;
        
        rb = collider.attachedRigidbody;
    }
    private void Update() 
    {
        Vector3 position = sailTransform.localPosition;
        position.y = Mathf.Lerp(raisedHeight, loweredHeight, 1-throttle);
        sailTransform.localPosition = position;
    }

    private void FixedUpdate() 
    {
        GameManager gameManager = GameManager.Instance;
        
        Vector2 relativeVelocity = (gameManager.windSpeed - rb.velocity.x) * Vector2.right;
        float sailArea = Mathf.Max(collider.size.x*collider.size.x, collider.size.y*collider.size.y);

        Vector2 dragForce = dragCoefficient * relativeVelocity.sqrMagnitude * sailArea * throttle * relativeVelocity.normalized; 

        rb.AddForce(dragForce);
    }

    public void SetThrottle(float value) => throttle = Mathf.Clamp01(value);
}