using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class ActionRegion : MonoBehaviour
{
    public delegate void Action();
    public Action onBegin;
    public Action onEnded;
    public GraphicRaycaster raycaster;
    PointerEventData pointer;
    public EventSystem eventSystem;

    private bool regionTriggered = false;

    // Start is called before the first frame update
    void Start()
    {
        raycaster = GetComponentInParent<GraphicRaycaster>();
        
    }

    // Update is called once per frame
    void Update()
    {
        ReadInput();
    }

    void ReadInput() 
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            pointer = new PointerEventData(eventSystem);
            pointer.position = touch.position;

            List<RaycastResult> results = new List<RaycastResult>();
            raycaster.Raycast(pointer, results);

            // regionTriggered = false;
            foreach(RaycastResult result in results)
            {
                if (result.gameObject == this.gameObject)
                {
                    regionTriggered = true;
                    break;
                }
            }
            
            if (regionTriggered) 
                switch(touch.phase)
                {
                    case TouchPhase.Began:
                        onBegin?.Invoke();
                        break;
                    case TouchPhase.Ended:    
                        onEnded?.Invoke();
                        regionTriggered = false;
                        break;
                    default:
                        break;
                }
            // print("Touch triggered");
        }
        #if UNITY_EDITOR
            else if (!regionTriggered && Input.GetButtonDown("Fire1"))
            {
                pointer = new PointerEventData(eventSystem);
                pointer.position = Input.mousePosition;

                List<RaycastResult> results = new List<RaycastResult>();
                raycaster.Raycast(pointer, results);

                foreach(RaycastResult result in results)
                {
                    if (result.gameObject == this.gameObject)
                    {
                        regionTriggered = true;
                        onBegin?.Invoke();
                        break;
                    }
                }
                // print("Fire1 Down");
            }
            else if (regionTriggered && Input.GetButtonUp("Fire1"))
            {
                pointer = new PointerEventData(eventSystem);
                pointer.position = Input.mousePosition;

                List<RaycastResult> results = new List<RaycastResult>();
                raycaster.Raycast(pointer, results);

                foreach(RaycastResult result in results)
                {
                    if (result.gameObject == this.gameObject)
                    {
                        regionTriggered = false;
                        onEnded?.Invoke();
                        break;
                    }
                }
                // print("Fire1 Up");
            }
        #endif
    }
}
