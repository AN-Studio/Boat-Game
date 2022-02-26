using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveMotion : MonoBehaviour
{
    Transform cam;
    Vector3 origin;
    float length;
    public Vector2 ellipse;
    Vector2 motion;

    void Awake() {
        cam = Camera.main.transform;
        motion = Vector2.zero;
        length = GetComponentInChildren<SpriteRenderer>().bounds.size.x;
        origin = transform.position;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        motion.x = ellipse.x * Mathf.Cos(Time.time);
        motion.y = ellipse.y * Mathf.Sin(Time.time);

        transform.position = new Vector3(origin.x + cam.position.x + motion.x, origin.y + motion.y, transform.position.z);

        if (cam.position.x > origin.x + length) origin.x += length;
        else if (cam.position.x < origin.x - length) origin.x -= length; 
    }
}
