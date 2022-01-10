using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class GUIDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreUI;
    public TextMeshProUGUI moneyUI;
    public TextMeshProUGUI windUI;
    public TextMeshProUGUI tensionUI;

    private int tensionValue;

    // Start is called before the first frame update
    void Start()
    {
        scoreUI.text = "Score: 0";
        moneyUI.text = "0";
        windUI.text = "0 Km/h";
    }

    // Update is called once per frame
    void Update()
    {
        DataManager dataManager = DataManager.Instance;
        GameManager gameManager = GameManager.Instance;

        scoreUI.text = $"Score: {dataManager.score}";
        moneyUI.text = dataManager.money.ToString();
        windUI.text = $"{gameManager.windSpeed} Km/h";
        tensionUI.text = $"{tensionValue}%";
    }

    public void UpdateTension(float appliedForce, float mastStrength)
    {
        tensionValue = (int) ((appliedForce / mastStrength) * 100);
    }
}
