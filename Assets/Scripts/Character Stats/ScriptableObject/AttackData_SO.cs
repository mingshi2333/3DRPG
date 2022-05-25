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
    [Tooltip("װ����������������ƶ��ٶ���ɵ�Ӱ��")]
    [Range(0.3f, 1f)] public float moveSlowRate = 1f;
    [Tooltip("�Ƿ�Ϊ˫������������Ƿ����˫������")]
    public bool isTwoHand;
    [Tooltip("�Ƿ�Ϊ��������Ƿ���й�")]
    public bool isBow;
    [Tooltip("�Ƿ�Ϊ����������Ч")]
    public bool isAxe;
    [Tooltip("�Ƿ�Ϊ�����ܣ�������Ч")]
    public bool isMetalShield;
    [Tooltip("��Ч������ƫ��")]
    public float pitchBias;
    [HideInInspector]public float sheildPitchBias;

    [Tooltip("һЩ������Я���ļ��ܣ���ز���������")]
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
    [Tooltip("�����ʸ����С������ٶȣ����˺��������")]
    public float minArrowSpeed;
    public float maxArrowSpeed;
    [Tooltip("�����������С�����ʱ�䣬�ͼ�ʸ�ٶȡ��˺��������")]
    public float minPullBowTime;
    public float maxPullBowTime;
    [Tooltip("�ٴ�װ���ʸ�����ʱ��")]
    public float arrowReloadCoolDown = 0.6f;

    [Header("Critical")]
    public float criticalMultiplier;
    public float criticalChance;
    [HideInInspector] public float initialCriticalProduct;

    [Header("Enemy Only")]
    [Tooltip("�Թ�����Ч0~180�Ƕ�����Ĺ����������ܣ� enemyAtkCosin=0 ��Ӧ180��")]
    [Range(1f, 0f)] public float enemyAtkCosin;

    [Tooltip("���﹥��ǰҡ�ĳ־öȣ�0��ʾ��ȫû��ǰҡ")]
    [Range(0, 4)] public int enemyAtkPreLevel = 2;
    [Tooltip("����ǰҡ�����󣬹�������ʱ��ռ��")]
    public float[] enemyAtkDuring = { 1f, 0.94f, 0.88f, 0.82f, 0.76f };
    [Tooltip("���﹥����ս����Խ�����λ�õ������")]
    [Range(0.05f, 0.5f)] public float enemyCrossPlayerMaxRate;
    [HideInInspector]public float enemyCrossPlayerRate;

    [HideInInspector]public AttackData_SO old;

    //װ������������
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

    //װ�����Ƶ�����
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
        //ֻ�ڵȼ�Ϊ0��ʱ�����У�
        if (lv > 1) return;

        damageCurrentLevelFactor = 1;
        initialAverageDmg = (maxDamage + minDamage) * 0.5f;
        initialDmgFloatingHalf = (maxDamage - minDamage) * 0.5f;

        initialSheildDefence = sheildDefence;
        initialSheildDamage = sheildDamage;
        initialSheildHitBackDefence = sheildHitBackDefence;
        initialSheildRushVel = sheildRushVel;
        initialSheildRushHitForce = sheildRushHitForce;

        //��ʼ����Factor
        //������������ӣ�Factor = m*c+(1-c)  -->  (m-1)*c = Factor-1
        initialCriticalProduct = criticalMultiplier * criticalChance + 1 - criticalChance;

        hasEquipWeapon = false;
        hasEquipShield = false;

        //����
        enemyCrossPlayerRate = Random.Range(0.05f, enemyCrossPlayerMaxRate);
    }

    public void ApplyWeaponData(AttackData_SO weapon)
    {
        hasEquipWeapon = true;
        weaponAttackRange = weapon.attackRange;
        weaponSkillRange = weapon.skillRange;
        weaponAttackCD = weapon.attackCoolDown;
        weaponSkillCD = weapon.skillCoolDown;

        //�˺���������ʵ�ʼ����У���ȡ������ֵ��
        weaponMinDmg = weapon.minDamage;
        weaponMaxDmg = weapon.maxDamage;
        weaponAtkHitForce = weapon.attackHitForce;
        weaponCritMul = weapon.criticalMultiplier;
        weaponCritChan = weapon.criticalChance;

        weaponMoveSlowRate = weapon.moveSlowRate;

        //�Ƿ�Ϊ˫������/��
        isTwoHand = weapon.isTwoHand;
        isBow = weapon.isBow;
        isAxe = weapon.isAxe;
        //��Ч������ƫ��
        pitchBias = weapon.pitchBias;

        //�������ܲ���
        weaponSkillData = weapon.weaponSkillData;

        //���Ĳ���
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
