using System.Collections.Generic;
using UnityEngine;

public class GyroInput
{
    public static float GetTilt()
    {
        Vector2 projectionXY = new Vector2(Input.acceleration.x, Input.acceleration.y);
        projectionXY.Normalize();

        float result = Mathf.Clamp(projectionXY.x, -.75f, .75f) / .75f;

        #if UNITY_EDITOR || UNITY_STANDALONE
            if (result == 0 || !UnityEditor.EditorApplication.isRemoteConnected)
            {
                result = Input.GetAxis("Horizontal");
                result = Mathf.Clamp(result, -1, 1);
            }
        #endif

        // Debug.Log(result);
        return result;
    }
}