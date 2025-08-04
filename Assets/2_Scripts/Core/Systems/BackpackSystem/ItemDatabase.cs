using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [System.Serializable]
    public class ItemConfig
    {
        public string ItemID;
        public string DisplayName;
        [TextArea] public string Description;
        public bool IsInspectable = true;
        public GameObject Prefab3D;
        public Sprite Icon;
    }

    public List<ItemConfig> Items = new();

    public ItemConfig GetItem(string itemID)
    {
        return Items.Find(item => item.ItemID == itemID);
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Default Items")]
    private void GenerateDefaultItems()
    {
        Items = new List<ItemConfig>
        {
            new() {
                ItemID = "compass",
                DisplayName = "Brass Compass",
                Description = "A precision instrument for navigation. The needle trembles slightly.",
                IsInspectable = true
            },
            new() {
                ItemID = "kerma_tablet",
                DisplayName = "Kerma Stone Tablet",
                Description = "Ancient stone etched with mysterious glyphs.",
                IsInspectable = true
            },
            new() {
                ItemID = "journal",
                DisplayName = "Field Journal",
                Description = "Eleanor's personal observations and sketches.",
                IsInspectable = false
            }
        };
    }
#endif
}