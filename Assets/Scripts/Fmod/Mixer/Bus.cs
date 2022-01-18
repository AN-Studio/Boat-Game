using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bus : MonoBehaviour
{
    FMOD.Studio.Bus bus;

    [SerializeField] [Range(-80f, 10f)]
    private float busVolume;

    void Start()
    {
        bus = FMODUnity.RuntimeManager.GetBus("bus:/Bus");
    }

    void Update()
    {
        bus.setVolume(DecibelToLinear(busVolume));
    }

    private float DecibelToLinear(float dB)
    {
        float linear = Mathf.Pow(10.0f, dB / 20f);
        return linear;
    }
}
