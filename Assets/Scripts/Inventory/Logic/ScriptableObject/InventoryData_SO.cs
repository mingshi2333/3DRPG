using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="New Inventory",menuName ="Inventory/Inventory Data")]
public class InventoryData_SO : ScriptableObject
{
    public List<InventoryItem> items = new List<InventoryItem>();

    public int AddItem(ItemData_SO newItemData, int amountInPickUp)
    {
        int newAmount;
        //在非空格子寻找可堆叠余量
        if (newItemData.stackableAmount > 1)
        {
            foreach (var item in items)
            {
                if (item.itemData?.itemName == newItemData.itemName
                    && item.amountInInventory < newItemData.stackableAmount)
                {
                    newAmount = Mathf.Min(item.amountInInventory + amountInPickUp, newItemData.stackableAmount);

                    amountInPickUp -= (newAmount - item.amountInInventory);
                    item.amountInInventory = newAmount;

                    if(amountInPickUp<=0) break;
                }
            }
        }

        //如果没有放完，则找空格子
        if (amountInPickUp > 0)
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].itemData == null)
                {
                    items[i].itemData = newItemData;
                    items[i].amountInInventory = Mathf.Min(amountInPickUp, Mathf.Max(1, newItemData.stackableAmount));
                    amountInPickUp -= items[i].amountInInventory;

                    if (amountInPickUp <= 0) break;
                }
            }

        //返回剩下的数量
        return amountInPickUp;
    }
}

[System.Serializable]
public class InventoryItem
{
    public ItemData_SO itemData;
    public int amountInInventory;
}
