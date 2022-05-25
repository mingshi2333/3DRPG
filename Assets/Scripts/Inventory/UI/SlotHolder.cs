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
            //�����ظ��������磺��Ģ����
            GameManager.Instance.playerStats.RestoreHealth(itemUI.GetItem().useableData.healthPoint);
            //����buff/debuff���磺��Ģ����
            GameManager.Instance.playerStats.AddItemUsedTimer(itemUI.GetItem().useableData);

            //���������Ʒ���½���
            QuestManager.Instance.UpdateQuestProgress(itemUI.GetItem().itemName, -1);

            //��������
            itemUI.Bag.items[itemUI.Index].amountInInventory--;
            //�������Ʒ
            if (itemUI.Bag.items[itemUI.Index].amountInInventory == 0)
                itemUI.Bag.items[itemUI.Index].itemData = null;

            //��Ч
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
                //װ������ �л�����
                if (itemUI.GetItem() != null)
                {
                    GameManager.Instance.playerStats.ChangeWeapon(itemUI.GetItem());
                    //���ظ��ӷ���ͼ��
                    GetComponent<Image>().enabled = false;
                }
                else
                {
                    GameManager.Instance.playerStats.UnEquipWeapon();
                    //��ʾ���ӷ���ͼ��
                    GetComponent<Image>().enabled = true;
                }
                break;
            case SlotType.SHEILD:
                itemUI.Bag = InventoryManager.Instance.equipmentData;
                //װ������
                if (itemUI.GetItem() != null)
                {
                    GameManager.Instance.playerStats.ChangeSheild(itemUI.GetItem());
                    //���ظ��ӷ���ͼ��
                    GetComponent<Image>().enabled = false;
                }
                else
                {
                    GameManager.Instance.playerStats.UnEquipSheild();
                    //�������˫������/�ֹ������ظ��ӷ���ͼ�ꣻ������ʾ
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
