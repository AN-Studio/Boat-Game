using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parallax : MonoBehaviour
{
    protected Transform cam;
    protected Vector3 origin;
    protected float length;
    [Range(0,1)] public float parallaxFactor;
    protected float height;
    protected float width;

    protected virtual void Awake() 
    {
        cam = Camera.main.transform;
        length = GetComponentInChildren<SpriteRenderer>().bounds.size.x;
        origin = transform.position;

        height = 2 * Mathf.Tan(.5f * Camera.main.fieldOfView) * Mathf.Abs(transform.position.z - cam.position.z);
        width = height * (Screen.width / Screen.height);

    }

    // Update is called once per frame
    protected virtual void FixedUpdate()
    {
        float temp = cam.position.x * (1 - parallaxFactor);
        float distance = cam.position.x * parallaxFactor;

        transform.position = new Vector3(origin.x + distance, origin.y, transform.position.z);

        if (temp > origin.x + length/width) origin.x += length;
        else if (temp < origin.x - length/width) origin.x -= length;
    }
}
