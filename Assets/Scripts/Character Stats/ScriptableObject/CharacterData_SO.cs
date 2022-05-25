using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Data", menuName = "Character Stats/Data")]
public class CharacterData_SO : ScriptableObject
{
    [HideInInspector] public int lv = 1;

    [Header("Stats Info")]
    public int maxHealth;
    public int currentHealth;
    public int baseDefence;
    public int currentDefence;
    public bool isPlayer;
    [Range(0, 1)] public float enterBushSpeedRate = 0.5f;
    //����ֵ�Զ��ָ�
    public float healthRegenerateIn10Sec;
    [Tooltip("��ս��ûָ�����ֵ")]
    public float startRegenTimeAfterCombat = 8f;


    [Header("Player Only")]
    [Tooltip("�������Ч���ɹ����ܹ�����ĳ��")]
    public int attackDodgeVel = 15;
    [Tooltip("�������Ч�����¿ո����ܵĳ��")]
    public int moveDodgeVel = 20;
    [Tooltip("�������Ч���ո����ܵ�CDʱ��")]
    public float moveDodgeCoolDown;
    public float hitRockDamagePerVel = 0.382f;
    public float hitTreeDamagePerVel = 0.234f;
    [Header("Player Perception")]
    public float inSightRadius = 4f;
    public float outSightRadius = 12f;
    public float longSightRadius = 24f;
    public float insightUpGradeLimit = 2.5f;
    public float lossInsightTime = 15f;



    [Header("Enemy Only")]
    [Tooltip("�����ײ��ʱ�����������˺���ÿ��λ�ٶȣ�")]
    public float hitByPlayerDamagePerVel = 0.382f;
    [Tooltip("�����ײ��ʱ����������˺���ÿ��λ�ٶȣ�")]
    public float playerHurtDamagePerVel = 0.382f;
    [Tooltip("����Ѫ���������ʱ��������ҵ������")]
    [Range(0f, 1f)] public float maxEscapeRate;
    [HideInInspector] public float escapeRate;

    [Header("Enemy Kill")]
    public int killPoint;

    [Header("Player Level")]
    public int currentLevel = 1;
    public int maxLevel = 30;
    public int baseExp = 30;
    public int currentExp = 0;
    [Range(22, 55)] public int baseExpLevelBuff = 30;
    public int healthDefaultAdd;
    [Range(0f, 1f)] public float healthExtraRandomRange = 0.618f;
    public float healthRegen10SecAdd;
    [Tooltip("Ѫ������������ӵ�ͬʱ������Ѫ���ָ��������ٵ����䣨1��ƽ������")]
    public bool isRegenAddSqrtDownWhenHealthExtra = true;

    [HideInInspector]public int lastLevelBaseExp; //����֮ǰ�ȼ��ܾ���ֵ
    private float currentHealthAddRate; //����Ѫ���ָ���������
    private int lastNoMultRandExp; //��һ�ȼ������뱶�����������ֵ����������ǰ�ڶ�Ȥζ��
    private int initialDefence;

    private float inSightRadiusLimit;
    private float outSightRadiusLimit;
    private float longSightRadiusLimit;

    [HideInInspector] public float healthRegenOnePointTime;

    [HideInInspector] public CharacterData_SO old;

    public float BaseExpMultiplier
    {
        //ÿ������ֵ = ��һ������ֵ * (1 + (�ȼ�-1) * ϵ�� * �����Χ)
        get
        {
            float rootMtp = 1f + 
                (baseExpLevelBuff / 1000f) * (currentLevel - 1) 
                * UnityEngine.Random.Range(0.95f, 1.05f);

            return Mathf.Pow(rootMtp, 0.382f);
        }
    }
    public int HealthAddRandom
    {
        //ÿ��Ѫ�� += Ĭ������ֵ * (1 + ���Extraϵ��)
        get
        {
            currentHealthAddRate = UnityEngine.Random.Range(1f, 1f + healthExtraRandomRange);
            return (int)(healthDefaultAdd * UnityEngine.Random.Range(1f, 1f + healthExtraRandomRange)); 
        }
    }


    private void Awake()
    {
        //ֻ�ھ���Ϊ0��ʱ�����У�
        if (currentExp != 0) return;

        healthRegenOnePointTime = 10f / healthRegenerateIn10Sec;

        baseDefence = isPlayer ? Mathf.Min(baseDefence, 4) : baseDefence;
        initialDefence = baseDefence;
        currentDefence = baseDefence;

        //��ҵĻ���������Ұ
        inSightRadius = Mathf.Max(inSightRadius, 2);
        outSightRadius = Mathf.Max(inSightRadius + 1, outSightRadius);
        longSightRadius = Mathf.Max(outSightRadius + 1, longSightRadius);

        inSightRadiusLimit = inSightRadius * insightUpGradeLimit;
        outSightRadiusLimit = outSightRadius * insightUpGradeLimit;
        longSightRadiusLimit = longSightRadius * insightUpGradeLimit;

        //�����ʼ�����뼸��
        escapeRate = UnityEngine.Random.Range(0.05f, maxEscapeRate);
    }

    public void UpGradeDamageFactor(AttackData_SO atkData)
    {
        //ÿ������ = Ĭ��ֵ * (1 + ���Extraϵ��) + �����ϵ������
        atkData.damageCurrentLevelFactor += 1 / Mathf.Pow(currentLevel + 0.618f, 1.5f) +
            UnityEngine.Random.Range(0.15f, 0.35f) / (atkData.damageCurrentLevelFactor * atkData.initialAverageDmg);
    }

    public void HealthRegenAdd()
    {
        //Ѫ���ظ�������
        float regenAddRate =
            isRegenAddSqrtDownWhenHealthExtra ? (1f / Mathf.Sqrt(currentHealthAddRate)) : 1f;

        healthRegenerateIn10Sec += healthRegen10SecAdd * regenAddRate;
        healthRegenOnePointTime = 10f / healthRegenerateIn10Sec;
    }

    public void TestLogFullLevelAttribute()
    {
        //ֻ����ҽ���Test���
        if (baseExp == 0) return;
        AttackData_SO atkData = GameManager.Instance.playerStats.attackData;
        if (atkData == null) return;

        int minExp = baseExp, maxExp = baseExp;
        int lastLvMinEx = 0, lastLvMaxEx = 0, tempMin = 0, tempMax = 0, lastNoMult = 0;
        float expMul;

        int minMH = maxHealth, maxMH = maxHealth;
        float minReg10Sec = healthRegenerateIn10Sec, maxReg10Sec = healthRegenerateIn10Sec;

        float dmgFc = 1, minDmg = atkData.minDamage, maxDmg = atkData.maxDamage;

        int defence;

        float cp, cm, cc;

        float iSR = inSightRadius, oSR = outSightRadius, lSR = longSightRadius;

        for (int lv = 2; lv <= maxLevel; lv++)
        {
            tempMin = minExp;
            expMul = Mathf.Pow(1f + (baseExpLevelBuff / 1000f) * (lv - 1) * 0.95f, 0.382f);
            minExp += (int)((minExp - lastLvMinEx) * expMul);
            lastLvMinEx = tempMin;

            tempMax = maxExp;
            expMul = Mathf.Pow(1f + (baseExpLevelBuff / 1000f) * (lv - 1) * 1.05f, 0.382f);
            maxExp += (int)((maxExp - lastLvMaxEx - lastNoMult) * expMul) + 20;
            lastLvMaxEx = tempMax;
            lastNoMult = 20;
            //Debug.Log("Lv" + lv + " >> minExpNeed:" + (minExp - lastLvMinEx) + " maxExpNeed:" + (maxExp - lastLvMaxEx)
            //    + " lastNoMult:" + 20);

            minMH += healthDefaultAdd;
            maxMH += (int)(healthDefaultAdd * (1f + healthExtraRandomRange));

            minReg10Sec += healthRegen10SecAdd  / Mathf.Sqrt(1f + healthExtraRandomRange);
            maxReg10Sec += healthRegen10SecAdd;
            //��������
            dmgFc += 1 / Mathf.Pow(lv + 0.618f, 1f);
            minDmg = //Mathf.Max(minDmg,
                (int)(dmgFc *
                (atkData.initialAverageDmg - UnityEngine.Random.Range(0, atkData.initialDmgFloatingHalf)));//);
            maxDmg = //Mathf.Max(maxDmg,
                (int)(dmgFc *
                (atkData.initialAverageDmg + UnityEngine.Random.Range(0, atkData.initialDmgFloatingHalf)));//);
            //��������
            defence = initialDefence + (int)(((float)lv / maxLevel) * (5.5f - initialDefence));

            //��������
            cp = atkData.initialCriticalProduct * Mathf.Sqrt(1 + (float)lv / maxLevel);
            cm =
                UnityEngine.Random.Range(Mathf.Max(1.234f, cp / 0.88f + 1), Mathf.Min(4.321f, cp / 0.11f + 1));
            cc = Mathf.Clamp(cp / (cm - 1), 0.11f, 0.88f);

            //Debug.Log("Lv" + lv + " >> minDmg:" + (minDmg) + " maxDmg:" + (maxDmg) + //" dmgFc:" + dmgFc + " defence:" + defence);
            //    " critPd:" + cp + " critMul:" + cm + " critChan:" + cc);

            //��Ұ����
            lSR = UnityEngine.Random.Range(lSR, lSR + (longSightRadiusLimit - lSR) * 0.09f);
            oSR = UnityEngine.Random.Range(oSR, oSR + (outSightRadiusLimit - oSR) * 0.06f);
            iSR = UnityEngine.Random.Range(iSR, iSR + (inSightRadiusLimit - iSR) * 0.03f);
            //������Ұ
            oSR = Mathf.Max(iSR + 1, oSR);
            lSR = Mathf.Max(oSR + 1, lSR);
            //Debug.Log("Lv" + lv + " >> iSR:" + iSR + " oSR:" + oSR + " lSR:" + lSR);


        }

        //Debug.Log("Test Lv" + maxLevel + " >> minExp:" + minExp + " maxExp:" + maxExp
        //    + " >> minHealth:" + minMH + " maxHealth:" + maxMH);
        //Debug.Log(" >> minReg10Sec:" + minReg10Sec + " maxReg10Sec:" + maxReg10Sec
        //    + " >> slowRegFullTime:" + maxMH / minReg10Sec * 10 + " fastRegFullTime:" + minMH / maxReg10Sec * 10);
    }

    public void UpdateExp(int point)
    {
        currentExp += point;

        if (currentExp >= baseExp)
            LevelUp();

        //��ɱС��ʱ�Զ����棨����λ�ã�
        SaveManager.Instance.SavePlayerScene_Pos_Rot();
        SaveManager.Instance.SavePlayerData();
    }

    private void LevelUp()
    {
        //�����������ʱ�򣬻�������һ������
        //����maxLevelΪ0
        if (currentLevel > maxLevel || maxLevel == 0) return;

        AttackData_SO atkData = GameManager.Instance.playerStats.attackData;

        //֮ǰ�ȼ��ı��ݣ��󵨵ĳ��ԣ���ע���������ı��ݣ�����ѻ�
        ///�˳��ؽ���Ͷ�ʧ�ˣ������Ӳ�����ݹ鱣��
        old = null;
        old = Instantiate(this);
        atkData.old = null;
        atkData.old = Instantiate(atkData);

        //�ȼ���������������
        currentLevel = Mathf.Clamp(currentLevel + 1, 1, maxLevel);
        int tempExp = baseExp;
        //�����ʵ���������Ӷ�Ȥζ��
        int tempNoMult = (int)(UnityEngine.Random.Range(0, 20));
        baseExp += (int)((baseExp - lastLevelBaseExp - lastNoMultRandExp) * BaseExpMultiplier) + tempNoMult;
        lastLevelBaseExp = tempExp;
        lastNoMultRandExp = tempNoMult;

        //�Զ�����������
        maxHealth += HealthAddRandom;
        currentHealth = maxHealth;
        HealthRegenAdd();

        //�����󣬹������п��ܼ��ٵģ�������׼ֵһ���������ӵģ�
        UpGradeDamageFactor(atkData);
        atkData.minDamage =
            (int)(atkData.damageCurrentLevelFactor *
            (atkData.initialAverageDmg - UnityEngine.Random.Range(0, atkData.initialDmgFloatingHalf)));
        atkData.maxDamage =
            (int)(atkData.damageCurrentLevelFactor *
            (atkData.initialAverageDmg + UnityEngine.Random.Range(0, atkData.initialDmgFloatingHalf)));

        //���Ƶķ����͹���
        atkData.sheildDefence = (int)(atkData.damageCurrentLevelFactor * atkData.initialSheildDefence);
        atkData.sheildDamage = (int)(atkData.damageCurrentLevelFactor * atkData.initialSheildDamage);
        atkData.sheildHitBackDefence = (int)(atkData.damageCurrentLevelFactor * atkData.initialSheildHitBackDefence);
        atkData.sheildRushVel = (int)((1 + 0.5f * (float)currentLevel / maxLevel) * atkData.initialSheildRushVel);
        atkData.sheildRushHitForce = (int)((1 + 0.5f * (float)currentLevel / maxLevel) * atkData.initialSheildRushHitForce);

        //�����������������������ʷ��ķ��������
        baseDefence = initialDefence + (int)(((float)currentLevel / maxLevel) * (7f - initialDefence));
        currentDefence = baseDefence;

        //�����˺��ͱ����ʣ�ÿ�������������
        //������������ӣ�Factor = m*c+(1-c)  -->  (m-1)*c = Factor-1
        float critPd = atkData.initialCriticalProduct * Mathf.Sqrt(1 + (float)currentLevel / maxLevel);
        atkData.criticalMultiplier =
            UnityEngine.Random.Range(Mathf.Max(1.234f, critPd / 0.88f + 1), Mathf.Min(4.321f, critPd / 0.11f + 1));
        atkData.criticalChance = Mathf.Clamp(critPd / (atkData.criticalMultiplier - 1), 0.11f, 0.88f);

        //��ҰҲ�����ӣ�ţ��ţ��
        longSightRadius =
            UnityEngine.Random.Range(longSightRadius, longSightRadius + (longSightRadiusLimit - longSightRadius) * 0.06f);
        outSightRadius =
            UnityEngine.Random.Range(outSightRadius, outSightRadius + (outSightRadiusLimit - outSightRadius) * 0.04f);
        inSightRadius =
            UnityEngine.Random.Range(inSightRadius, inSightRadius + (inSightRadiusLimit - inSightRadius) * 0.02f);
        //������Ұ
        outSightRadius = Mathf.Max(inSightRadius + 1, outSightRadius);
        longSightRadius = Mathf.Max(outSightRadius + 1, longSightRadius);

        //��¼Data�ȼ�������Debug
        lv++;
        atkData.lv++;

        //�������Ը���
        PlayerDataCanvas.Instance.UpdateText();
    }

}
