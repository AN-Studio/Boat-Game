using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveMotion : Parallax
{
    public Vector2 ellipse;
    Vector2 motion;

    // Update is called once per frame
    protected override void FixedUpdate()
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
