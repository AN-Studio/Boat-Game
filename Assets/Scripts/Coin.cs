using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coin : Item
{
    [SerializeField] int currencyValue;
    // Start is called before the first frame update
    void Start()
    {
        DataManager.Instance.money += currencyValue;
        GameManager.Instance.IncreaseCoinCombo();

        StartCoroutine(RaiseAndDestroy());
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
