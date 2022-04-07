using UnityEngine;

public class PulseTimer
{
    private float currentTime = 0;
    private float targetTime;

    public delegate void OnPulse();
    public OnPulse onPulse;

    public PulseTimer() => targetTime = 0;
    public PulseTimer(float seconds) => targetTime = seconds;

    public void Tick()
    {
        if (targetTime > 0)
        {
            currentTime = currentTime + Time.deltaTime;
            if (currentTime >= targetTime)
            {
                onPulse?.Invoke();
                currentTime = 0;
            }
        }
    } 

    public void SetPulseTo(float seconds) => targetTime = seconds;
    public void ResetPulseTo(float seconds)
    {
        targetTime = seconds;
        currentTime = 0;
    }

}