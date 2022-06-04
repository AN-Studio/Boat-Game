using UnityEngine;
using UnityEngine.UI;

public class RouteProgressBar : MonoBehaviour 
{
    [SerializeField] Slider slider;

    private void Awake() {
        slider = GetComponentInChildren<Slider>();
    }

    private void Update() {
        slider.value = WorldGenerator.Instance.RouteProgress;
    }
}