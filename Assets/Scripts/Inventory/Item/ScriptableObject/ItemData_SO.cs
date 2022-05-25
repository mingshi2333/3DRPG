using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//武器和盾牌有耐久值，若是怪物掉落则随机；每次扣除点数 = 伤害 * 0.01
//物品Tooltip显示耐久度，useable物品不显示

public enum ItemType { Useable, Weapon, Sheild, Arrow, Other }
[CreateAssetMenu(fileName ="New Item", menuName = "Inventory/Item Data")]
public class ItemData_SO : ScriptableObject
{
    [Tooltip("ItemData目录下的所属文件夹路径，用于读档背包时的物品生成；末尾无需斜杠")]
    public string folderPath;
    
    public ItemType itemType;
    public string itemName;
    public Sprite itemIcon;
    public int amountInPickUp = 1; //物品可拾取状态下的数量，而非进入背包后显示的数量
    public bool randomAmount;
    [TextArea]
    public string description = "";
    [Tooltip("物品可堆叠的数量，大于1可堆叠，0或1不可堆叠")]
    public int stackableAmount = 1;
    public GameObject objectOnWorld;

    [Header("Useable Item")]
    public UseableItemData_SO useableData;

    [Header("Weapon")]
    public GameObject weaponPrefab;
    public AttackData_SO weaponData;
    public RuntimeAnimatorController weaponAnimator;
    [Tooltip("武器或盾牌的可使用次数，次数 * 伤害or盾防 * 0.01 = 耐久度；若是怪物掉落则随机")]
    public int durabilityTimes;
    [HideInInspector] public float weaponDurability;
    [HideInInspector] public float currentDurability;

    [Header("Arrow")]
    public int arrowDamageAdd;
    public float arrowSpeedChangeRate = 1f;
    public float arrowPushForceRate = 0.1f;
    public int arrowMaxPushForce = 6;
    public float arrowBrokenRate = 0.5f;
    [Tooltip("用箭矢当成手持武器的攻击距离，仅限特殊动作")]
    public float arrowStingerRange = 2.5f;
    public int arrowStingerDmgAdd = 4;

    public void Awake()
    {
        stackableAmount = Mathf.Max(stackableAmount, 1);

        //初始化武器耐久度；怪物掉落随机见DurabilityRandomSet()
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
