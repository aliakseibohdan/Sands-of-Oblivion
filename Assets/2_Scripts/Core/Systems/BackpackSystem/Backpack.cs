using System.Collections.Generic;
using UnityEngine;

public class Backpack : MonoBehaviour
{
    private List<string> items = new();
    private ItemDatabase itemDatabase;

    private void Awake()
    {
        itemDatabase = Resources.Load<ItemDatabase>("ItemDatabase");
    }

    public void AddItem(string itemID)
    {
        if (!items.Contains(itemID))
        {
            items.Add(itemID);
        }
    }

    public void RemoveItem(string itemID)
    {
        if (items.Contains(itemID))
        {
            items.Remove(itemID);
        }
    }

    public bool ContainsItem(string itemID)
    {
        return items.Contains(itemID);
    }

    public List<string> GetItems()
    {
        return new List<string>(items);
    }
}