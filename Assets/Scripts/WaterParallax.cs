using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterParallax : Parallax
{
    public Vector2 ellipse;
    Vector2 motion;

    void Start() {
        motion = Vector2.zero;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float temp = cam.position.x * (1 - parallaxFactor);
        float distance = cam.position.x * parallaxFactor;

        motion.x = ellipse.x * Mathf.Cos(Time.time) / parallaxFactor;
        motion.y = ellipse.y * Mathf.Sin(Time.time) / parallaxFactor;

        transform.position = new Vector3(origin.x + distance + motion.x, origin.y + motion.y, transform.position.z);

        if (temp > origin.x + length) origin.x += length;
        else if (temp < origin.x - length) origin.x -= length; 
    }
}
