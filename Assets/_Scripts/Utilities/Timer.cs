using UnityEngine;

public class Timer
{
    private float currentTime = 0;
    private float targetTime;

    public void Update()
    {
        currentTime = currentTime + Time.deltaTime;
        
    } 
}