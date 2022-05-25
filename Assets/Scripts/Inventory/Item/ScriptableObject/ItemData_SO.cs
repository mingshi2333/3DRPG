using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//�����Ͷ������;�ֵ�����ǹ�������������ÿ�ο۳����� = �˺� * 0.01
//��ƷTooltip��ʾ�;öȣ�useable��Ʒ����ʾ

public enum ItemType { Useable, Weapon, Sheild, Arrow, Other }
[CreateAssetMenu(fileName ="New Item", menuName = "Inventory/Item Data")]
public class ItemData_SO : ScriptableObject
{
    [Tooltip("ItemDataĿ¼�µ������ļ���·�������ڶ�������ʱ����Ʒ���ɣ�ĩβ����б��")]
    public string folderPath;
    
    public ItemType itemType;
    public string itemName;
    public Sprite itemIcon;
    public int amountInPickUp = 1; //��Ʒ��ʰȡ״̬�µ����������ǽ��뱳������ʾ������
    public bool randomAmount;
    [TextArea]
    public string description = "";
    [Tooltip("��Ʒ�ɶѵ�������������1�ɶѵ���0��1���ɶѵ�")]
    public int stackableAmount = 1;
    public GameObject objectOnWorld;

    [Header("Useable Item")]
    public UseableItemData_SO useableData;

    [Header("Weapon")]
    public GameObject weaponPrefab;
    public AttackData_SO weaponData;
    public RuntimeAnimatorController weaponAnimator;
    [Tooltip("��������ƵĿ�ʹ�ô��������� * �˺�or�ܷ� * 0.01 = �;öȣ����ǹ�����������")]
    public int durabilityTimes;
    [HideInInspector] public float weaponDurability;
    [HideInInspector] public float currentDurability;

    [Header("Arrow")]
    public int arrowDamageAdd;
    public float arrowSpeedChangeRate = 1f;
    public float arrowPushForceRate = 0.1f;
    public int arrowMaxPushForce = 6;
    public float arrowBrokenRate = 0.5f;
    [Tooltip("�ü�ʸ�����ֳ������Ĺ������룬�������⶯��")]
    public float arrowStingerRange = 2.5f;
    public int arrowStingerDmgAdd = 4;

    public void Awake()
    {
        stackableAmount = Mathf.Max(stackableAmount, 1);

        //��ʼ�������;öȣ�������������DurabilityRandomSet()
        if (weaponPrefab)
        {
            weaponData?.Awake();

            if (itemType == ItemType.Sheild)
                weaponDurability = 0.01f * weaponData.sheildDefence * durabilityTimes;
            else
            if (itemType == ItemType.Weapon)
                weaponDurability = 0.01f * weaponData.initialAverageDmg * durabilityTimes;

            currentDurability = weaponDurability;
            //Debug.Log(weaponPrefab.name + " weaponData.initialAverageDmg:" + weaponData.initialAverageDmg);
        }
    }
}
