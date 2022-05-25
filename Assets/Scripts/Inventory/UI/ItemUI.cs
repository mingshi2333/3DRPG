using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemUI : MonoBehaviour
{
    public Image icon = null;
    public Text amountBack = null;
    public Text amount = null;

    public ItemData_SO currentItemData;
    public InventoryData_SO Bag { set; get; }
    public int Index { set; get; } = -1;

    public void SetupItemUI(ItemData_SO item, int amountInInventory)
    {
        //小于0表示为任务物品扣除，不显示
        if (amountInInventory < 0) item = null;
        
        if (item != null)
        {
            currentItemData = item;
            icon.sprite = item.itemIcon;
            if (amountInInventory > 1)
            {
                amountBack.text = amountInInventory.ToString();
                amount.text = amountInInventory.ToString();
            }
            else
            {
                amountBack.text = "";
                amount.text = "";
            }

            icon.gameObject.SetActive(true);
        }
        else
            icon.gameObject.SetActive(false);
    }

    public ItemData_SO GetItem()
    {
        //检查数据错误
        if (Bag.items[Index].amountInInventory <= 0)
        {
            Bag.items[Index].itemData = null;
            Bag.items[Index].amountInInventory = 0;
        }

        //返回物品数据
        return Bag.items[Index].itemData;
    }
}
