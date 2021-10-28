using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GUIDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreUI;
    public TextMeshProUGUI moneyUI;

    // Start is called before the first frame update
    void Start()
    {
        scoreUI.text = "Score: 0";
        moneyUI.text = "0";
    }

    // Update is called once per frame
    void Update()
    {
        DataManager dataManager = DataManager.Instance;

        scoreUI.text = $"Score: {dataManager.score}";
        moneyUI.text = dataManager.money.ToString();
    }
}
