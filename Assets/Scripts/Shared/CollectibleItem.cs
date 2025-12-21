using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class CollectibleItem : ScriptableObject
{
    public string itemName;
    public Sprite itemIcon = null;
}