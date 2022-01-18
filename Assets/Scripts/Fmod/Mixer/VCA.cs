using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VCA : MonoBehaviour
{
    FMOD.Studio.VCA vca;

    [SerializeField] [Range(-80f, 10f)]
    private float vcaVolume;

    void Start()
    {
        vca = FMODUnity.RuntimeManager.GetVCA("vca:/OnDeck!");
    }

    void Update()
    {
        vca.setVolume(DecibelToLinear(vcaVolume));
    }

    private float DecibelToLinear(float dB)
    {
        float linear = Mathf.Pow(10.0f, dB / 20f);
        return linear;
    }
}
