using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    public float heightIncrease = 5f;
    float lerpPercent = 0;
    float start;
    float end;

    private void Awake() 
    {
        start = transform.position.y;
        end = start + heightIncrease;    
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(RaiseAndDestroy());
    }


    protected IEnumerator RaiseAndDestroy()
    {
        Vector3 position = transform.position;
        while (lerpPercent < 1)
        {
            yield return null;

            lerpPercent += 0.1f;
            position.y = Mathf.Lerp(start,end, lerpPercent);

            transform.position = position;
        }

        yield return new WaitForSeconds(.5f);

        Destroy(gameObject);
    }
}
