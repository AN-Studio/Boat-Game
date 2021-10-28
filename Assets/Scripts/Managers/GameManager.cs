using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    #region Singleton
        static GameManager instance;
        public static GameManager Instance {get => instance;}
    #endregion

    #region Switches
        public bool gameStarted = false;
        public bool gameEnded = false;
    #endregion

    void Awake() 
    {
        #region Singleton
            if (instance == null)
            {
                instance = this;
            } 
            else
            {
                Debug.LogWarning("Tried to create another instance of GameManager");
                Destroy(this.gameObject);
            }
        #endregion
    
        DontDestroyOnLoad(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
