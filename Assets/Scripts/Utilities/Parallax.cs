using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    public Transform cam;
    public float parallaxFactor;
    
    protected float length; 
    protected float scaledLength;
    protected Vector2 origin;

    void Awake() 
    {
        cam = Camera.main.transform;
        length = GetComponentInChildren<SpriteRenderer>().bounds.size.x;
        scaledLength = length * transform.lossyScale.x;
        origin = transform.position;

    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float temp = cam.position.x * (1 - parallaxFactor);
        float distance = cam.position.x * parallaxFactor;

        transform.position = new Vector3(origin.x + distance, origin.y, transform.position.z);

        if (temp > origin.x + scaledLength) origin.x += length;
        else if (temp < origin.x - scaledLength) origin.x -= length; 
    }
}
