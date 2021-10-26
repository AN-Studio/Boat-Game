using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ScoreScript : MonoBehaviour
{
    public TextMeshProUGUI highScoreTMP;
    public TextMeshProUGUI scoreTMP;
    public int score = 0;

    private void Start()
    {
        highScoreTMP.text = "Highscore: " + PlayerPrefs.GetString("HighScore", "0");
    }
    private void Update()
    {
        scoreTMP.text = $"Score: {score}";
        if (score > PlayerPrefs.GetInt("HighScore", 0))
        {
            highScoreTMP.text = $"Highscore: {score}";
        }

        // #if (UNITY_STANDALONE || UNITY_EDITOR)
            if (Input.GetKey(KeyCode.Space))
            {
                score += 1;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                ResetScore();
            }
        // #endif
    }
    private void OnDestroy()
    {
        SaveScore();
    }
    public void SaveScore()
    {
        if (score > PlayerPrefs.GetInt("HighScore", 0))
        {
            PlayerPrefs.SetInt("HighScore", score);
        }
    }
    public void ResetScore()
    {
        // PlayerPrefs.DeleteKey("HighScore");
        // highScoreTMP.text = "0";
        score = 0;
    }
    public void ResetHighScore() 
    {
        PlayerPrefs.SetInt("HighScore", 0);
        // PlayerPrefs.DeleteKey("HighScore");
        highScoreTMP.text = "0";
    } 
}
