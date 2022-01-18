using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CustomSlider : MonoBehaviour
{
    public enum Direction {
        LeftToRight,
        RightToLeft,
        BottomToTop,
        TopToBottom
    }

    #region References
        [Header("References")]
        public RectTransform fillRect;
        public RectTransform handleRect;
        GraphicRaycaster raycaster;
        PointerEventData pointerEventData;
        EventSystem eventSystem;
        Camera cam;
    #endregion
    
    #region Settings
        [Header("Settings")]
        public Direction direction = Direction.LeftToRight;
        public float minValue = 0f;
        public float maxValue = 1f;
        public float value = 0f;
    #endregion

    bool isTouchingHandle = false;

    public delegate void OnValueChanged(float value);
    public OnValueChanged onValueChanged;

    // Start is called before the first frame update
    void Start()
    {
        raycaster = GetComponentInParent<GraphicRaycaster>();
        eventSystem = FindObjectOfType<EventSystem>();
        cam = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        CheckHandleTouch();

        if (isTouchingHandle) UpdateHandle();
    }

    void CheckHandleTouch()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                pointerEventData = new PointerEventData(eventSystem);
                pointerEventData.position = touch.position;

                List<RaycastResult> results = new List<RaycastResult>();

                raycaster.Raycast(pointerEventData, results);

                foreach(var result in results)
                {
                    if (result.gameObject == handleRect.gameObject) 
                    {
                        isTouchingHandle = true;
                    }
                }

            }
            else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
            {
                isTouchingHandle = false;
            } 
        }
    }

    void UpdateHandle()
    {
        Touch touch = Input.GetTouch(0);

        Vector3[] corners = new Vector3[4];
        fillRect.GetLocalCorners(corners);

        float xMin = corners[0].x;
        float xMax = corners[2].x;
        float yMin = corners[0].y;
        float yMax = corners[2].y;

        Vector3 position = handleRect.localPosition;
        Vector3 touchPosition = cam.ScreenToWorldPoint(touch.position);
        touchPosition = fillRect.InverseTransformPoint(touch.position);
        float delta = 1;


        if (direction == Direction.LeftToRight || direction == Direction.RightToLeft)
        {
            // print($"xMin {xMin} xMax {xMax}");
            position.x = Mathf.Clamp(touchPosition.x, xMin, xMax);
            delta = Mathf.Abs(xMax - xMin);
        }
        else if (direction == Direction.BottomToTop || direction == Direction.TopToBottom)
        {
            // print($"yMin {yMin} yMax {yMax}");
            position.y = Mathf.Clamp(touchPosition.y, yMin, yMax);
            delta = Mathf.Abs(yMax - yMin);
        }

        handleRect.localPosition = position;

        switch(direction)
        {
            case Direction.LeftToRight:
                value = Mathf.Abs((position.x - xMin) / delta);
                break;
            case Direction.RightToLeft:
                value = Mathf.Abs((position.x - xMax) / delta);
                break;
            case Direction.BottomToTop:
                value = Mathf.Abs((position.y - yMin) / delta);
                break;
            case Direction.TopToBottom:
                value = Mathf.Abs((position.y - yMax) / delta);
                break;
            default:
                break;
        }

        value = (maxValue - minValue) * value + minValue;

        if (onValueChanged != null) onValueChanged(value);
    }
}
