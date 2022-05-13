using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public partial class RandomizedList<T>
{

    [SerializeField] List<WeightedItem<T>> list;

    public int Count {
        get => list.Count;
    }

    public RandomizedList()
    {
        list = new List<WeightedItem<T>>();
    }

    public RandomizedList(IEnumerable<WeightedItem<T>> list)
    {
        this.list = new List<WeightedItem<T>>(list);
    }

    public void Add(T item, int weight) => list.Add(new WeightedItem<T>(item, weight));
    public T GetRandom()
    {
        int totalWeight = 0;
        foreach (var obj in list) 
            totalWeight += obj.weight;

        float random = Random.value * totalWeight;

        int index = 0;
        int w = list[index].weight;
        while (w < random)
        {
            index++;
            w += list[index].weight;
        }

        return list[index].item;
    }

}