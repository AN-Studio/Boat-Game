using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class ActionRegion : MonoBehaviour
{
    public delegate void Action();
    public Action action;
    public GraphicRaycaster raycaster;
    PointerEventData pointer;
    public EventSystem eventSystem;

    // Start is called before the first frame update
    void Start()
    {
        raycaster = GetComponentInParent<GraphicRaycaster>();
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
