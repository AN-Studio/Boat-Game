
[System.Serializable]
public class GameData
{
    public int highScore;
    public int totalMoney;
    public int ownedBoats;  

    public GameData(DataManager manager)
    {
        highScore = manager.highScore;
        totalMoney = manager.totalMoney;
        ownedBoats = (int) manager.ownedBoats;
    }
}
