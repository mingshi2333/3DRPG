using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickUp : MonoBehaviour
{
    public ItemData_SO itemData;
    [Tooltip("物品是否可刷新")]
    public bool canRefresh = false;
    [Tooltip("物品刷新的最大数量")]
    public int refreshMaxAmount = 0;

    RaycastHit upHighDownHit;

    private void OnEnable()
    {
        if (itemData == null) return;

        //是否为随机掉落量
        if (itemData.amountInPickUp > 0 && itemData.randomAmount)
        {
            itemData.amountInPickUp = Random.Range(1, itemData.amountInPickUp);
        }

        //是否为物品刷新
        if (itemData.amountInPickUp == 0 && canRefresh)
        {
            itemData.amountInPickUp = Random.Range(0, refreshMaxAmount);
            if(itemData.amountInPickUp>0)
            {
                //刷新显示物品
                GetComponent<MeshRenderer>().enabled = true;
                GetComponent<Collider>().enabled = true;
            }
        }

        //如果是武器，新生成一个独立的itemData
        if (itemData.weaponPrefab)
            itemData = Instantiate(itemData);
    }

    public void DurabilityRandomSet()
    {
        if (itemData.itemType != ItemType.Weapon && itemData.itemType != ItemType.Sheild) return;

        //如果是怪物掉落，则随机耐久
        itemData.currentDurability = Random.value * itemData.weaponDurability;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var pickAmount = itemData.amountInPickUp;
            //将物品尽可能添加到背包
            var lastAmount = InventoryManager.Instance.inventoryData.AddItem(itemData, itemData.amountInPickUp);

            InventoryManager.Instance.inventoryUI.RefreshUI();

            pickAmount -= lastAmount;
            //检查是否有任务
            QuestManager.Instance.UpdateQuestProgress(itemData.itemName, pickAmount);

            //物品要么还剩下，要么重置或destroy
            if (lastAmount <= 0)
            {
                if (canRefresh)
                {
                    //适当随机刷新位置
                    Vector2 vec = Random.insideUnitCircle;
                    Vector3 newPos = transform.position +
                        (new Vector3(vec.x, 0, vec.y)) * UnityEngine.Random.Range(0, 5f);
                    if (!newPos.IsOnGround(out upHighDownHit))
                        newPos = transform.position;
                    //修正y值
                    newPos.y = upHighDownHit.point.y;
                    transform.position = newPos;

                    //如果可刷新，则暂时隐藏物品，不销毁
                    GetComponent<MeshRenderer>().enabled = false;
                    GetComponent<Collider>().enabled = false;
                }
                else
                    Destroy(gameObject);
            }
            else
                itemData.amountInPickUp = lastAmount; //保留剩下的部分

            //如果拾取是武器/盾牌/箭矢，自动保存（避免频繁保存）
            if (itemData.itemType !=ItemType.Useable && itemData.itemType != ItemType.Other)
            {
                SaveManager.Instance.SavePlayerScene_Pos_Rot();
                SaveManager.Instance.SavePlayerData();
            }
        }
    }
}
