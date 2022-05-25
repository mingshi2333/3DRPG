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

        //��¼ԭʼ����
        InventoryManager.Instance.currentDrag = new InventoryManager.DragData();
        InventoryManager.Instance.currentDrag.originalHolder = GetComponentInParent<SlotHolder>();
        InventoryManager.Instance.currentDrag.originalParent = (RectTransform)transform.parent;

        transform.SetParent(InventoryManager.Instance.dragCanvas.transform, true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        //�������λ��
        transform.position = eventData.position;

    }

    public void OnEndDrag(PointerEventData eventData)
    {
        MouseManager.Instance.isDragging = false;

        //������Ʒ����������
        //�Ƿ�ָ��UI��Ʒ
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

                            //�����˫��������ֹ���ж�¶�����Ʒ����鱳��������ӣ���������Ʒ��
                            if (targetHolder.itemUI.GetItem().weaponData.isTwoHand ||
                                targetHolder.itemUI.GetItem().weaponData.isBow)
                                UnequipSheildItem();
                        }
                        break;
                    case SlotType.SHEILD:
                        if (currentItemUI.Bag.items[currentItemUI.Index].itemData.itemType == ItemType.Sheild)
                        {
                            //˫������ʱ����װ������
                            if (!GameManager.Instance.playerStats.attackData.isTwoHand)
                                SwapItem();
                        }
                        break;
                    case SlotType.ACION:
                        //ֻ�п�ʹ������Ʒ�����ܷŽ�Action��
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
            //��������Ʒ
            ThrowItem();
            currentHolder.UpdateItem();
        }

        transform.SetParent(InventoryManager.Instance.currentDrag.originalParent);

        //����Item Slot��λ��
        RectTransform t = transform as RectTransform;
        t.offsetMax = -Vector2.one * 5;
        t.offsetMin = Vector2.one * 5;
    }

    //ж�¶�����Ʒ����鱳��������ӣ���������Ʒ��
    private void UnequipSheildItem()
    {
        //����Ƿ�װ������
        SlotHolder sheildHolder = InventoryManager.Instance.equipmentUI.slotHolders[1];
        InventoryItem sheildItem = InventoryManager.Instance.equipmentData.items[1];
        //�Ҳ������ƣ�����
        if (sheildItem.amountInInventory == 0) return;

        //���԰Ѷ��Ʒ��뱳��
        var lastNum = InventoryManager.Instance.inventoryData.
            AddItem(sheildItem.itemData, sheildItem.amountInInventory);

        //��δ�ܳɹ����뱳���������綪������
        if (lastNum>0)
            sheildHolder.itemUI.GetComponent<DragItem>().ThrowItem();
        
        sheildItem.amountInInventory = 0;
        sheildItem.itemData = null;

        //���¶��Ƹ���UI���������и���UI
        sheildHolder.UpdateItem();
        InventoryManager.Instance.inventoryUI.RefreshUI();
    }

    public void ThrowItem()
    {
        InventoryItem tempItem = currentHolder.itemUI.Bag.items[currentHolder.itemUI.Index];
        if(tempItem.amountInInventory>0)
        {
            //������������Ʒ
            var itemObj = Instantiate(tempItem.itemData.objectOnWorld);

            itemObj.transform.SetPositionAndRotation(GameManager.Instance.player.transform.position
                + Vector3.up * 2 + (UnityEngine.Random.value < 0.5f ? transform.right : -transform.right) * 2.5f, 
                UnityEngine.Random.rotation);

            //�������������ʸ���PickUp
            if (itemObj.GetComponent<Arrow>())
                itemObj.AddComponent<ItemPickUp>().itemData =
                    itemObj.GetComponent<Arrow>().itemData;
            //�½���ƷData���޸�ʰȡ�������޸��;�
            itemObj.GetComponent<ItemPickUp>().itemData = 
                Instantiate(itemObj.GetComponent<ItemPickUp>().itemData);
            itemObj.GetComponent<ItemPickUp>().itemData.amountInPickUp = tempItem.amountInInventory;
            itemObj.GetComponent<ItemPickUp>().itemData.currentDurability = tempItem.itemData.currentDurability;

        }

        //���������Ʒ���½���
        QuestManager.Instance.UpdateQuestProgress(tempItem.itemData.itemName, -tempItem.amountInInventory);
        //�������������Ʒ
        tempItem.amountInInventory = 0;
        tempItem.itemData = null;
    }

    public void SwapItem()
    {
        //��ͬ���򷵻أ���ǰ��Ϊ���򷵻�
        if (targetHolder == currentHolder) return;

        InventoryItem targetItem = targetHolder.itemUI.Bag.items[targetHolder.itemUI.Index];
        InventoryItem tempItem = currentHolder.itemUI.Bag.items[currentHolder.itemUI.Index];

        if (tempItem.itemData == null) return;

        bool isSameItem = tempItem.itemData?.itemName == targetItem.itemData?.itemName;

        if(isSameItem && targetItem.itemData.stackableAmount>1)
        {
            //�ѵ���ൽ����
            int newAmount =
                Mathf.Min(targetItem.amountInInventory + tempItem.amountInInventory, targetItem.itemData.stackableAmount);

            tempItem.amountInInventory -= (newAmount - targetItem.amountInInventory);
            if (tempItem.amountInInventory <= 0)
                tempItem.itemData = null;

            targetItem.amountInInventory = newAmount;
        }
        else
        {
            //��������Ʒͬ����Ҫ�����뵱ǰ��ƥ�䣡
            if(targetItem.itemData!=null)
                switch (currentHolder?.slotType)
                {
                    case SlotType.BAG:
                        break;
                    case SlotType.WEAPON:
                        if (targetItem.itemData.itemType != ItemType.Weapon)
                            return;
                        //�����������˫��������ֹ���ж�¶�����Ʒ����鱳��������ӣ���������Ʒ��
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
