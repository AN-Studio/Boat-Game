using UnityEngine;
// using TMPro;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class DataManager : MonoBehaviour
{
    #region Singleton
        DataManager instance;
        public DataManager Instance {get => instance;}
    #endregion

    [System.Flags]
    public enum Boat {
        SailBoat = 0x01,
        LongBoat = 0x02,
        ModernFisher = 0x04

    }

    // #region References
    //     [Header("References")]
    //     public TextMeshProUGUI scoreUI;
    //     public TextMeshProUGUI moneyUI; 
    // #endregion

    #region Data
        [Header("Game Data")]
        public int highScore = 0;
        public int totalMoney = 0;
        public int score = 0;
        public int money = 0;
        public Boat ownedBoats = Boat.SailBoat;
    #endregion

    #region MonoBehaviour Functions
        void Awake() 
        {
            #region Singleton
                if (instance == null)
                {
                    instance = this;
                } 
                else
                {
                    Debug.LogWarning("Tried to create another instance of DataManager");
                    Destroy(this.gameObject);
                }
            #endregion
        
            DontDestroyOnLoad(gameObject);
        }

        // Start is called before the first frame update
        void Start()
        {
            LoadData();   
        }

        void OnDestroy() 
        {
            SaveData();    
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    #endregion

    #region Public Functions
        public void SaveData() 
        {
            BinaryFormatter formatter = new BinaryFormatter();
            
            string path = Application.persistentDataPath + "/gamedata.dat";
            FileStream stream = new FileStream(path, FileMode.Create);

            GameData data = new GameData(this);

            formatter.Serialize(stream, data);
            stream.Close();
        }

        public void LoadData()
        {
            string path = Application.persistentDataPath + "/gamedata.dat";
            if (File.Exists(path))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                FileStream stream = new FileStream(path, FileMode.Open);

                GameData data = formatter.Deserialize(stream) as GameData;
                stream.Close();

                highScore = data.highScore;
                totalMoney = data.totalMoney;
                ownedBoats = (Boat) data.ownedBoats;        
            }
        }
    #endregion
}
