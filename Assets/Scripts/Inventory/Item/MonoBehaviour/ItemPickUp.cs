using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickUp : MonoBehaviour
{
    public ItemData_SO itemData;
    [Tooltip("��Ʒ�Ƿ��ˢ��")]
    public bool canRefresh = false;
    [Tooltip("��Ʒˢ�µ��������")]
    public int refreshMaxAmount = 0;

    RaycastHit upHighDownHit;

    private void OnEnable()
    {
        if (itemData == null) return;

        //�Ƿ�Ϊ���������
        if (itemData.amountInPickUp > 0 && itemData.randomAmount)
        {
            itemData.amountInPickUp = Random.Range(1, itemData.amountInPickUp);
        }

        //�Ƿ�Ϊ��Ʒˢ��
        if (itemData.amountInPickUp == 0 && canRefresh)
        {
            itemData.amountInPickUp = Random.Range(0, refreshMaxAmount);
            if(itemData.amountInPickUp>0)
            {
                //ˢ����ʾ��Ʒ
                GetComponent<MeshRenderer>().enabled = true;
                GetComponent<Collider>().enabled = true;
            }
        }

        //�����������������һ��������itemData
        if (itemData.weaponPrefab)
            itemData = Instantiate(itemData);
    }

    public void DurabilityRandomSet()
    {
        if (itemData.itemType != ItemType.Weapon && itemData.itemType != ItemType.Sheild) return;

        //����ǹ�����䣬������;�
        itemData.currentDurability = Random.value * itemData.weaponDurability;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var pickAmount = itemData.amountInPickUp;
            //����Ʒ��������ӵ�����
            var lastAmount = InventoryManager.Instance.inventoryData.AddItem(itemData, itemData.amountInPickUp);

            InventoryManager.Instance.inventoryUI.RefreshUI();

            pickAmount -= lastAmount;
            //����Ƿ�������
            QuestManager.Instance.UpdateQuestProgress(itemData.itemName, pickAmount);

            //��ƷҪô��ʣ�£�Ҫô���û�destroy
            if (lastAmount <= 0)
            {
                if (canRefresh)
                {
                    //�ʵ����ˢ��λ��
                    Vector2 vec = Random.insideUnitCircle;
                    Vector3 newPos = transform.position +
                        (new Vector3(vec.x, 0, vec.y)) * UnityEngine.Random.Range(0, 5f);
                    if (!newPos.IsOnGround(out upHighDownHit))
                        newPos = transform.position;
                    //����yֵ
                    newPos.y = upHighDownHit.point.y;
                    transform.position = newPos;

                    //�����ˢ�£�����ʱ������Ʒ��������
                    GetComponent<MeshRenderer>().enabled = false;
                    GetComponent<Collider>().enabled = false;
                }
                else
                    Destroy(gameObject);
            }
            else
                itemData.amountInPickUp = lastAmount; //����ʣ�µĲ���

            //���ʰȡ������/����/��ʸ���Զ����棨����Ƶ�����棩
            if (itemData.itemType !=ItemType.Useable && itemData.itemType != ItemType.Other)
            {
                SaveManager.Instance.SavePlayerScene_Pos_Rot();
                SaveManager.Instance.SavePlayerData();
            }
        }
    }
}
