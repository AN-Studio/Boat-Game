
[System.Serializable]
public struct WeightedItem<I> 
{
    public I item;
    public int weight;
    public WeightedItem(I item, int weight)
    {
        this.item = item;
        this.weight = weight;
    }
}
