using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

public enum SlotType { BAG, WEAPON, SHEILD, ACION }
public class SlotHolder : MonoBehaviour, IPointerClickHandler,IPointerEnterHandler,IPointerExitHandler
{
    public SlotType slotType;
    public ItemUI itemUI;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount % 2 == 0)
        {
            UseItem();
        }
    }

    public void UseItem()
    {
        if (itemUI.GetItem() == null
            || GameManager.Instance.player.GetComponent<PlayerController>().isDead)
            return;

        if (itemUI.GetItem().itemType == ItemType.Useable
            && itemUI.Bag.items[itemUI.Index].amountInInventory > 0)
        {
            //立即回复生命（如：红蘑菇）
            GameManager.Instance.playerStats.RestoreHealth(itemUI.GetItem().useableData.healthPoint);
            //叠加buff/debuff（如：黄蘑菇）
            GameManager.Instance.playerStats.AddItemUsedTimer(itemUI.GetItem().useableData);

            //检查任务物品更新进度
            QuestManager.Instance.UpdateQuestProgress(itemUI.GetItem().itemName, -1);

            //数量减少
            itemUI.Bag.items[itemUI.Index].amountInInventory--;
            //清除空物品
            if (itemUI.Bag.items[itemUI.Index].amountInInventory == 0)
                itemUI.Bag.items[itemUI.Index].itemData = null;

            //音效
            AudioManager.Instance.Play2DSoundEffect(SoundName.UseItem,
                AudioManager.Instance.otherSoundDetailList, GameManager.Instance.player.transform);
        }
        UpdateItem();
    }

    public void UpdateItem()
    {
        switch (slotType)
        {
            case SlotType.BAG:
                itemUI.Bag = InventoryManager.Instance.inventoryData;
                break;
            case SlotType.WEAPON:
                itemUI.Bag = InventoryManager.Instance.equipmentData;
                //装备武器 切换武器
                if (itemUI.GetItem() != null)
                {
                    GameManager.Instance.playerStats.ChangeWeapon(itemUI.GetItem());
                    //隐藏格子符号图标
                    GetComponent<Image>().enabled = false;
                }
                else
                {
                    GameManager.Instance.playerStats.UnEquipWeapon();
                    //显示格子符号图标
                    GetComponent<Image>().enabled = true;
                }
                break;
            case SlotType.SHEILD:
                itemUI.Bag = InventoryManager.Instance.equipmentData;
                //装备盾牌
                if (itemUI.GetItem() != null)
                {
                    GameManager.Instance.playerStats.ChangeSheild(itemUI.GetItem());
                    //隐藏格子符号图标
                    GetComponent<Image>().enabled = false;
                }
                else
                {
                    GameManager.Instance.playerStats.UnEquipSheild();
                    //如果持有双手武器/持弓，隐藏格子符号图标；否则显示
                    if (GameManager.Instance.playerStats.attackData.isTwoHand ||
                        GameManager.Instance.playerStats.attackData.isBow)
                        GetComponent<Image>().enabled = false;
                    else
                        GetComponent<Image>().enabled = true;
                }
                break;
            case SlotType.ACION:
                itemUI.Bag = InventoryManager.Instance.actionData;
                break;
        }

        //Debug.Log("itemUI.Index:" + itemUI.Index);
        InventoryItem item = itemUI.Bag.items[itemUI.Index];
        itemUI.SetupItemUI(item.itemData, item.amountInInventory);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemUI.GetItem())
        {
            InventoryManager.Instance.tooltip.SetupToolTip(itemUI.GetItem());
            InventoryManager.Instance.tooltip.gameObject.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        InventoryManager.Instance.tooltip.gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        InventoryManager.Instance.tooltip.gameObject.SetActive(false);
    }
}
