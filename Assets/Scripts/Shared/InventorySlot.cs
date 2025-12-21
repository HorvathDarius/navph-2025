using UnityEngine;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour
{
    public Image icon;
    CollectibleItem item;

    public void AddItem(CollectibleItem newItem)
    {
        item = newItem;
        icon.sprite = item.itemIcon;
        icon.gameObject.SetActive(true);
    }

    public void ClearSlot()
    {
        item = null;
        icon.sprite = null;
        icon.enabled = false;
    }
}
