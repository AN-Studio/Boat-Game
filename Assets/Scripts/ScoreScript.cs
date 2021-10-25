using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreScript : MonoBehaviour
{
    public Text highScore;
    public Text score;
    public static int scoreInt = 0;

    private void Start()
    {
        highScore.text = PlayerPrefs.GetInt("HighScore", 0).ToString();
    }
    private void Update()
    {
        score.text = scoreInt.ToString();
        if (scoreInt > PlayerPrefs.GetInt("HighScore", 0))
        {
            highScore.text = scoreInt.ToString();
        }
        if (Input.GetKey(KeyCode.Space))
        {
            scoreInt += 1;
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetScore();
        }
    }
    private void OnDestroy()
    {
        SaveScore();
    }
    public void SaveScore()
    {
        if (scoreInt > PlayerPrefs.GetInt("HighScore", 0))
        {
            PlayerPrefs.SetInt("HighScore", scoreInt);
        }
    }
    public void ResetScore()
    {
        PlayerPrefs.DeleteKey("HighScore");
        highScore.text = "0";
    }
}
