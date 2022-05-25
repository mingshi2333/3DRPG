using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ItemUI))]
public class DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    ItemUI currentItemUI;
    SlotHolder currentHolder;
    SlotHolder targetHolder;

    private void Awake()
    {
        currentItemUI = GetComponent<ItemUI>();
        currentHolder = GetComponentInParent<SlotHolder>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        MouseManager.Instance.isDragging = true;

        //记录原始数据
        InventoryManager.Instance.currentDrag = new InventoryManager.DragData();
        InventoryManager.Instance.currentDrag.originalHolder = GetComponentInParent<SlotHolder>();
        InventoryManager.Instance.currentDrag.originalParent = (RectTransform)transform.parent;

        transform.SetParent(InventoryManager.Instance.dragCanvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        //跟随鼠标位置
        transform.position = eventData.position;

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        MouseManager.Instance.isDragging = false;

        //放下物品，交换数据
        //是否指向UI物品
        if (EventSystem.current.IsPointerOverGameObject())
        {
            if(InventoryManager.Instance.CheckInActionUI(eventData.position)
                || InventoryManager.Instance.CheckInEquitmentUI(eventData.position)
                || InventoryManager.Instance.CheckInInventoryUI(eventData.position))
            {
                if (eventData.pointerEnter.gameObject.GetComponent<SlotHolder>())
                    targetHolder = eventData.pointerEnter.gameObject.GetComponent<SlotHolder>();
                else
                    targetHolder = eventData.pointerEnter.gameObject.GetComponentInParent<SlotHolder>();

                //Debug.Log("targetHolder.slotType: " + targetHolder.slotType);
                switch(targetHolder?.slotType)
                {
                    case SlotType.BAG:
                        SwapItem();
                        break;
                    case SlotType.WEAPON:
                        if (currentItemUI.Bag.items[currentItemUI.Index].itemData.itemType == ItemType.Weapon)
                        {
                            SwapItem();

                            //如果是双手武器或持弓，卸下盾牌物品（检查背包多余格子，否则丢弃物品）
                            if (targetHolder.itemUI.GetItem().weaponData.isTwoHand ||
                                targetHolder.itemUI.GetItem().weaponData.isBow)
                                UnequipSheildItem();
                        }
                        break;
                    case SlotType.SHEILD:
                        if (currentItemUI.Bag.items[currentItemUI.Index].itemData.itemType == ItemType.Sheild)
                        {
                            //双手武器时不可装备盾牌
                            if (!GameManager.Instance.playerStats.attackData.isTwoHand)
                                SwapItem();
                        }
                        break;
                    case SlotType.ACION:
                        //只有可使用用物品，才能放进Action栏
                        if (currentItemUI.Bag.items[currentItemUI.Index].itemData.itemType == ItemType.Useable)
                            SwapItem();
                        break;
                }

                currentHolder.UpdateItem();
                targetHolder?.UpdateItem();
            }
        }
        else
        {
            //丢弃该物品
            ThrowItem();
            currentHolder.UpdateItem();
        }

        transform.SetParent(InventoryManager.Instance.currentDrag.originalParent);

        //修正Item Slot的位置
        RectTransform t = transform as RectTransform;
        t.offsetMax = -Vector2.one * 5;
        t.offsetMin = Vector2.one * 5;
    }

    //卸下盾牌物品（检查背包多余格子，否则丢弃物品）
    private void UnequipSheildItem()
    {
        //检查是否装备盾牌
        SlotHolder sheildHolder = InventoryManager.Instance.equipmentUI.slotHolders[1];
        InventoryItem sheildItem = InventoryManager.Instance.equipmentData.items[1];
        //找不到盾牌，返回
        if (sheildItem.amountInInventory == 0) return;

        //尝试把盾牌放入背包
        var lastNum = InventoryManager.Instance.inventoryData.
            AddItem(sheildItem.itemData, sheildItem.amountInInventory);

        //若未能成功放入背包，向世界丢弃盾牌
        if (lastNum>0)
            sheildHolder.itemUI.GetComponent<DragItem>().ThrowItem();
        
        sheildItem.amountInInventory = 0;
        sheildItem.itemData = null;

        //更新盾牌格子UI、背包所有格子UI
        sheildHolder.UpdateItem();
        InventoryManager.Instance.inventoryUI.RefreshUI();
    }

    public void ThrowItem()
    {
        InventoryItem tempItem = currentHolder.itemUI.Bag.items[currentHolder.itemUI.Index];
        if(tempItem.amountInInventory>0)
        {
            //在世界掉落该物品
            var itemObj = Instantiate(tempItem.itemData.objectOnWorld);

            itemObj.transform.SetPositionAndRotation(GameManager.Instance.player.transform.position
                + Vector3.up * 2 + (UnityEngine.Random.value < 0.5f ? transform.right : -transform.right) * 2.5f, 
                UnityEngine.Random.rotation);

            //特殊情况，给箭矢添加PickUp
            if (itemObj.GetComponent<Arrow>())
                itemObj.AddComponent<ItemPickUp>().itemData =
                    itemObj.GetComponent<Arrow>().itemData;
            //新建物品Data，修改拾取数量，修改耐久
            itemObj.GetComponent<ItemPickUp>().itemData = 
                Instantiate(itemObj.GetComponent<ItemPickUp>().itemData);
            itemObj.GetComponent<ItemPickUp>().itemData.amountInPickUp = tempItem.amountInInventory;
            itemObj.GetComponent<ItemPickUp>().itemData.currentDurability = tempItem.itemData.currentDurability;

        }

        //检查任务物品更新进度
        QuestManager.Instance.UpdateQuestProgress(tempItem.itemData.itemName, -tempItem.amountInInventory);
        //背包内清除该物品
        tempItem.amountInInventory = 0;
        tempItem.itemData = null;
    }

    public void SwapItem()
    {
        //相同格则返回，当前格为空则返回
        if (targetHolder == currentHolder) return;

        InventoryItem targetItem = targetHolder.itemUI.Bag.items[targetHolder.itemUI.Index];
        InventoryItem tempItem = currentHolder.itemUI.Bag.items[currentHolder.itemUI.Index];

        if (tempItem.itemData == null) return;

        bool isSameItem = tempItem.itemData?.itemName == targetItem.itemData?.itemName;

        if(isSameItem && targetItem.itemData.stackableAmount>1)
        {
            //堆叠最多到上限
            int newAmount =
                Mathf.Min(targetItem.amountInInventory + tempItem.amountInInventory, targetItem.itemData.stackableAmount);

            tempItem.amountInInventory -= (newAmount - targetItem.amountInInventory);
            if (tempItem.amountInInventory <= 0)
                tempItem.itemData = null;

            targetItem.amountInInventory = newAmount;
        }
        else
        {
            //被交换物品同样需要考虑与当前格匹配！
            if(targetItem.itemData!=null)
                switch (currentHolder?.slotType)
                {
                    case SlotType.BAG:
                        break;
                    case SlotType.WEAPON:
                        if (targetItem.itemData.itemType != ItemType.Weapon)
                            return;
                        //如果交换的是双手武器或持弓，卸下盾牌物品（检查背包多余格子，否则丢弃物品）
                        if (targetItem.itemData.weaponData.isTwoHand || targetItem.itemData.weaponData.isBow)
                            UnequipSheildItem();
                        break;
                    case SlotType.SHEILD:
                        if (targetItem.itemData.itemType != ItemType.Sheild)
                            return;
                        break;
                    case SlotType.ACION:
                        if (targetItem.itemData.itemType != ItemType.Useable)
                            return;
                        break;
                }

            currentHolder.itemUI.Bag.items[currentHolder.itemUI.Index] = targetItem;
            targetHolder.itemUI.Bag.items[targetHolder.itemUI.Index] = tempItem;
        }
    }
}
