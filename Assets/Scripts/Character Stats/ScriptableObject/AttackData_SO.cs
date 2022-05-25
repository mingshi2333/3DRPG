using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Attack", menuName = "Attack/Attack Data")]
public class AttackData_SO : ScriptableObject
{
    [HideInInspector] public int lv = 1;

    public float attackRange;
    public float skillRange;
    public float attackCoolDown;
    public float skillCoolDown;
    public int minDamage;
    public int maxDamage;
    public float attackHitForce;
    [Tooltip("装备该武器，对玩家移动速度造成的影响")]
    [Range(0.3f, 1f)] public float moveSlowRate = 1f;
    [Tooltip("是否为双手武器；玩家是否持有双手武器")]
    public bool isTwoHand;
    [Tooltip("是否为弓；玩家是否持有弓")]
    public bool isBow;
    [Tooltip("是否为斧，用于音效")]
    public bool isAxe;
    [Tooltip("是否为金属盾，用于音效")]
    public bool isMetalShield;
    [Tooltip("音效的音调偏移")]
    public float pitchBias;
    [HideInInspector]public float sheildPitchBias;

    [Tooltip("一些武器所携带的技能，相关参数另外存放")]
    public WeaponSkillData_SO weaponSkillData;

    [Header("Sheild")]
    public int sheildDefence;
    public int sheildDamage;
    public int sheildHitBackDefence;
    public float sheildRushVel = 16;
    public float sheildRushHitForce = 16;

    [HideInInspector] public float damageCurrentLevelFactor;
    [HideInInspector] public float initialAverageDmg;
    [HideInInspector] public float initialDmgFloatingHalf;
    [HideInInspector] public int initialSheildDefence;
    [HideInInspector] public int initialSheildDamage;
    [HideInInspector] public int initialSheildHitBackDefence;
    [HideInInspector] public float initialSheildRushVel;
    [HideInInspector] public float initialSheildRushHitForce;


    [Header("Bow")]
    [Tooltip("射出箭矢的最小、最大速度，和伤害线性相关")]
    public float minArrowSpeed;
    public float maxArrowSpeed;
    [Tooltip("拉弓所需的最小、最大时间，和箭矢速度、伤害线性相关")]
    public float minPullBowTime;
    public float maxPullBowTime;
    [Tooltip("再次装填箭矢所需的时间")]
    public float arrowReloadCoolDown = 0.6f;

    [Header("Critical")]
    public float criticalMultiplier;
    public float criticalChance;
    [HideInInspector] public float initialCriticalProduct;

    [Header("Enemy Only")]
    [Tooltip("对怪物有效0~180角度以外的攻击可以闪避， enemyAtkCosin=0 对应180度")]
    [Range(1f, 0f)] public float enemyAtkCosin;

    [Tooltip("怪物攻击前摇的持久度，0表示完全没有前摇")]
    [Range(0, 4)] public int enemyAtkPreLevel = 2;
    [Tooltip("怪物前摇结束后，攻击持续时间占比")]
    public float[] enemyAtkDuring = { 1f, 0.94f, 0.88f, 0.82f, 0.76f };
    [Tooltip("怪物攻击后，战术性越过玩家位置的最大几率")]
    [Range(0.05f, 0.5f)] public float enemyCrossPlayerMaxRate;
    [HideInInspector]public float enemyCrossPlayerRate;

    [HideInInspector]public AttackData_SO old;

    //装备武器的属性
    [HideInInspector] public bool hasEquipWeapon;
    [HideInInspector] public float weaponAttackRange;
    [HideInInspector] public float weaponSkillRange;
    [HideInInspector] public float weaponAttackCD;
    [HideInInspector] public float weaponSkillCD;
    [HideInInspector] public int weaponMinDmg;
    [HideInInspector] public int weaponMaxDmg;
    [HideInInspector] public float weaponAtkHitForce;
    [HideInInspector] public float weaponCritMul;
    [HideInInspector] public float weaponCritChan;
    [HideInInspector] public float weaponMoveSlowRate;

    //装备盾牌的属性
    [HideInInspector] public bool hasEquipShield;
    [HideInInspector] public int equipShieldDefence;
    [HideInInspector] public int equipShieldDamage;
    [HideInInspector] public int equipShieldHitBackDfc;
    [HideInInspector] public float equipShieldCritMult;
    [HideInInspector] public float equipShieldCritChan;
    [HideInInspector] public float equipShieldRushVel;
    [HideInInspector] public float equipShieldRushHitForce;
    [HideInInspector] public float equipShieldMoveSlowRate;


    internal void Awake()
    {
        //只在等级为0的时候运行！
        if (lv > 1) return;

        damageCurrentLevelFactor = 1;
        initialAverageDmg = (maxDamage + minDamage) * 0.5f;
        initialDmgFloatingHalf = (maxDamage - minDamage) * 0.5f;

        initialSheildDefence = sheildDefence;
        initialSheildDamage = sheildDamage;
        initialSheildHitBackDefence = sheildHitBackDefence;
        initialSheildRushVel = sheildRushVel;
        initialSheildRushHitForce = sheildRushHitForce;

        //起始暴击Factor
        //总输出线性增加，Factor = m*c+(1-c)  -->  (m-1)*c = Factor-1
        initialCriticalProduct = criticalMultiplier * criticalChance + 1 - criticalChance;

        hasEquipWeapon = false;
        hasEquipShield = false;

        //怪物
        enemyCrossPlayerRate = Random.Range(0.05f, enemyCrossPlayerMaxRate);
    }

    public void ApplyWeaponData(AttackData_SO weapon)
    {
        hasEquipWeapon = true;
        weaponAttackRange = weapon.attackRange;
        weaponSkillRange = weapon.skillRange;
        weaponAttackCD = weapon.attackCoolDown;
        weaponSkillCD = weapon.skillCoolDown;

        //伤害、暴击（实际计算中，采取折中数值）
        weaponMinDmg = weapon.minDamage;
        weaponMaxDmg = weapon.maxDamage;
        weaponAtkHitForce = weapon.attackHitForce;
        weaponCritMul = weapon.criticalMultiplier;
        weaponCritChan = weapon.criticalChance;

        weaponMoveSlowRate = weapon.moveSlowRate;

        //是否为双手武器/弓
        isTwoHand = weapon.isTwoHand;
        isBow = weapon.isBow;
        isAxe = weapon.isAxe;
        //音效的音调偏移
        pitchBias = weapon.pitchBias;

        //武器技能参数
        weaponSkillData = weapon.weaponSkillData;

        //弓的参数
        minArrowSpeed = weapon.minArrowSpeed;
        maxArrowSpeed = weapon.maxArrowSpeed;
        minPullBowTime = weapon.minPullBowTime;
        maxPullBowTime = weapon.maxPullBowTime;
        arrowReloadCoolDown = weapon.arrowReloadCoolDown;
    }

    public void ApplySheildData(AttackData_SO shield)
    {
        hasEquipShield = true;
        equipShieldDefence = shield.sheildDefence;
        equipShieldDamage = shield.sheildDamage;
        equipShieldHitBackDfc = shield.sheildHitBackDefence;

        equipShieldCritMult = shield.criticalMultiplier;
        equipShieldCritChan = shield.criticalChance;

        equipShieldRushVel = shield.sheildRushVel;
        equipShieldRushHitForce = shield.sheildRushHitForce;

        equipShieldMoveSlowRate = shield.moveSlowRate;

        isMetalShield = shield.isMetalShield;
        sheildPitchBias = shield.pitchBias;
    }
}
