using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveMotion : MonoBehaviour
{
    Transform cam;
    Vector3 origin;
    float length;
    [Range(0,1)] public float parallaxFactor;
    public Vector2 ellipse;
    Vector2 motion;

    float height;
    float width;
    void Awake() {
        cam = Camera.main.transform;
        motion = Vector2.zero;
        length = GetComponentInChildren<SpriteRenderer>().bounds.size.x;
        origin = transform.position;

        
        height = 2 * Mathf.Tan(.5f * Camera.main.fieldOfView) * Mathf.Abs(transform.position.z - cam.position.z);
        width = height * (Screen.width / Screen.height);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float temp = cam.position.x;
        // float distance = cam.position.x * parallaxFactor;

        motion.x = ellipse.x * Mathf.Cos(Time.time);
        motion.y = ellipse.y * Mathf.Sin(Time.time);

        transform.position = new Vector3(origin.x + motion.x, origin.y + motion.y, transform.position.z);

        if (temp > origin.x + length/width) origin.x += length;
        else if (temp < origin.x - length/width) origin.x -= length; 
    }
}
