using UnityEngine;

[CreateAssetMenu(fileName = "New Route", menuName = "Scriptable Objects/Route", order = 0)]
public class Route : ScriptableObject 
{
    public int dangerLevel = 1;
    public int length = 50;   
}