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
    //生命值自动恢复
    public float healthRegenerateIn10Sec;
    [Tooltip("脱战多久恢复生命值")]
    public float startRegenTimeAfterCombat = 8f;


    [Header("Player Only")]
    [Tooltip("对玩家有效，成功闪避攻击后的冲刺")]
    public int attackDodgeVel = 15;
    [Tooltip("对玩家有效，按下空格闪避的冲刺")]
    public int moveDodgeVel = 20;
    [Tooltip("对玩家有效，空格闪避的CD时间")]
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
    [Tooltip("被玩家撞到时，怪物所受伤害（每单位速度）")]
    public float hitByPlayerDamagePerVel = 0.382f;
    [Tooltip("被玩家撞到时，玩家所受伤害（每单位速度）")]
    public float playerHurtDamagePerVel = 0.382f;
    [Tooltip("怪物血量比玩家少时，逃离玩家的最大几率")]
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
    [Tooltip("血量升级随机增加的同时，会有血量恢复升级减少的诅咒（1÷平方根）")]
    public bool isRegenAddSqrtDownWhenHealthExtra = true;

    [HideInInspector]public int lastLevelBaseExp; //用于之前等级总经验值
    private float currentHealthAddRate; //用于血量恢复升级减少
    private int lastNoMultRandExp; //上一等级不参与倍增的随机经验值，用于增加前期恶趣味性
    private int initialDefence;

    private float inSightRadiusLimit;
    private float outSightRadiusLimit;
    private float longSightRadiusLimit;

    [HideInInspector] public float healthRegenOnePointTime;

    [HideInInspector] public CharacterData_SO old;

    public float BaseExpMultiplier
    {
        //每级经验值 = 上一级经验值 * (1 + (等级-1) * 系数 * 随机范围)
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
        //每级血量 += 默认增加值 * (1 + 随机Extra系数)
        get
        {
            currentHealthAddRate = UnityEngine.Random.Range(1f, 1f + healthExtraRandomRange);
            return (int)(healthDefaultAdd * UnityEngine.Random.Range(1f, 1f + healthExtraRandomRange)); 
        }
    }


    private void Awake()
    {
        //只在经验为0的时候运行！
        if (currentExp != 0) return;

        healthRegenOnePointTime = 10f / healthRegenerateIn10Sec;

        baseDefence = isPlayer ? Mathf.Min(baseDefence, 4) : baseDefence;
        initialDefence = baseDefence;
        currentDefence = baseDefence;

        //玩家的话，修正视野
        inSightRadius = Mathf.Max(inSightRadius, 2);
        outSightRadius = Mathf.Max(inSightRadius + 1, outSightRadius);
        longSightRadius = Mathf.Max(outSightRadius + 1, longSightRadius);

        inSightRadiusLimit = inSightRadius * insightUpGradeLimit;
        outSightRadiusLimit = outSightRadius * insightUpGradeLimit;
        longSightRadiusLimit = longSightRadius * insightUpGradeLimit;

        //怪物，初始化逃离几率
        escapeRate = UnityEngine.Random.Range(0.05f, maxEscapeRate);
    }

    public void UpGradeDamageFactor(AttackData_SO atkData)
    {
        //每级攻击 = 默认值 * (1 + 随机Extra系数) + 随机非系数提升
        atkData.damageCurrentLevelFactor += 1 / Mathf.Pow(currentLevel + 0.618f, 1.5f) +
            UnityEngine.Random.Range(0.15f, 0.35f) / (atkData.damageCurrentLevelFactor * atkData.initialAverageDmg);
    }

    public void HealthRegenAdd()
    {
        //血量回复的增加
        float regenAddRate =
            isRegenAddSqrtDownWhenHealthExtra ? (1f / Mathf.Sqrt(currentHealthAddRate)) : 1f;

        healthRegenerateIn10Sec += healthRegen10SecAdd * regenAddRate;
        healthRegenOnePointTime = 10f / healthRegenerateIn10Sec;
    }

    public void TestLogFullLevelAttribute()
    {
        //只对玩家进行Test输出
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
            //攻击测试
            dmgFc += 1 / Mathf.Pow(lv + 0.618f, 1f);
            minDmg = //Mathf.Max(minDmg,
                (int)(dmgFc *
                (atkData.initialAverageDmg - UnityEngine.Random.Range(0, atkData.initialDmgFloatingHalf)));//);
            maxDmg = //Mathf.Max(maxDmg,
                (int)(dmgFc *
                (atkData.initialAverageDmg + UnityEngine.Random.Range(0, atkData.initialDmgFloatingHalf)));//);
            //防御测试
            defence = initialDefence + (int)(((float)lv / maxLevel) * (5.5f - initialDefence));

            //暴击测试
            cp = atkData.initialCriticalProduct * Mathf.Sqrt(1 + (float)lv / maxLevel);
            cm =
                UnityEngine.Random.Range(Mathf.Max(1.234f, cp / 0.88f + 1), Mathf.Min(4.321f, cp / 0.11f + 1));
            cc = Mathf.Clamp(cp / (cm - 1), 0.11f, 0.88f);

            //Debug.Log("Lv" + lv + " >> minDmg:" + (minDmg) + " maxDmg:" + (maxDmg) + //" dmgFc:" + dmgFc + " defence:" + defence);
            //    " critPd:" + cp + " critMul:" + cm + " critChan:" + cc);

            //视野测试
            lSR = UnityEngine.Random.Range(lSR, lSR + (longSightRadiusLimit - lSR) * 0.09f);
            oSR = UnityEngine.Random.Range(oSR, oSR + (outSightRadiusLimit - oSR) * 0.06f);
            iSR = UnityEngine.Random.Range(iSR, iSR + (inSightRadiusLimit - iSR) * 0.03f);
            //修正视野
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

        //击杀小怪时自动保存（保存位置）
        SaveManager.Instance.SavePlayerScene_Pos_Rot();
        SaveManager.Instance.SavePlayerData();
    }

    private void LevelUp()
    {
        //满级满经验的时候，还会再升一次属性
        //怪物maxLevel为0
        if (currentLevel > maxLevel || maxLevel == 0) return;

        AttackData_SO atkData = GameManager.Instance.playerStats.attackData;

        //之前等级的备份（大胆的尝试），注意清除更早的备份，避免堆积
        ///退出重进后就丢失了，看样子并不会递归保存
        old = null;
        old = Instantiate(this);
        atkData.old = null;
        atkData.old = Instantiate(atkData);

        //等级、经验上限提升
        currentLevel = Mathf.Clamp(currentLevel + 1, 1, maxLevel);
        int tempExp = baseExp;
        //经验适当随机，增加恶趣味性
        int tempNoMult = (int)(UnityEngine.Random.Range(0, 20));
        baseExp += (int)((baseExp - lastLevelBaseExp - lastNoMultRandExp) * BaseExpMultiplier) + tempNoMult;
        lastLevelBaseExp = tempExp;
        lastNoMultRandExp = tempNoMult;

        //自定义属性提升
        maxHealth += HealthAddRandom;
        currentHealth = maxHealth;
        HealthRegenAdd();

        //升级后，攻击是有可能减少的！（但基准值一定是逐渐增加的）
        UpGradeDamageFactor(atkData);
        atkData.minDamage =
            (int)(atkData.damageCurrentLevelFactor *
            (atkData.initialAverageDmg - UnityEngine.Random.Range(0, atkData.initialDmgFloatingHalf)));
        atkData.maxDamage =
            (int)(atkData.damageCurrentLevelFactor *
            (atkData.initialAverageDmg + UnityEngine.Random.Range(0, atkData.initialDmgFloatingHalf)));

        //盾牌的防御和攻击
        atkData.sheildDefence = (int)(atkData.damageCurrentLevelFactor * atkData.initialSheildDefence);
        atkData.sheildDamage = (int)(atkData.damageCurrentLevelFactor * atkData.initialSheildDamage);
        atkData.sheildHitBackDefence = (int)(atkData.damageCurrentLevelFactor * atkData.initialSheildHitBackDefence);
        atkData.sheildRushVel = (int)((1 + 0.5f * (float)currentLevel / maxLevel) * atkData.initialSheildRushVel);
        atkData.sheildRushHitForce = (int)((1 + 0.5f * (float)currentLevel / maxLevel) * atkData.initialSheildRushHitForce);

        //自身防御提升（防御不超过史莱姆攻击！）
        baseDefence = initialDefence + (int)(((float)currentLevel / maxLevel) * (7f - initialDefence));
        currentDefence = baseDefence;

        //暴击伤害和暴击率，每次升级随机重置
        //总输出线性增加，Factor = m*c+(1-c)  -->  (m-1)*c = Factor-1
        float critPd = atkData.initialCriticalProduct * Mathf.Sqrt(1 + (float)currentLevel / maxLevel);
        atkData.criticalMultiplier =
            UnityEngine.Random.Range(Mathf.Max(1.234f, critPd / 0.88f + 1), Mathf.Min(4.321f, critPd / 0.11f + 1));
        atkData.criticalChance = Mathf.Clamp(critPd / (atkData.criticalMultiplier - 1), 0.11f, 0.88f);

        //视野也会增加，牛不牛！
        longSightRadius =
            UnityEngine.Random.Range(longSightRadius, longSightRadius + (longSightRadiusLimit - longSightRadius) * 0.06f);
        outSightRadius =
            UnityEngine.Random.Range(outSightRadius, outSightRadius + (outSightRadiusLimit - outSightRadius) * 0.04f);
        inSightRadius =
            UnityEngine.Random.Range(inSightRadius, inSightRadius + (inSightRadiusLimit - inSightRadius) * 0.02f);
        //修正视野
        outSightRadius = Mathf.Max(inSightRadius + 1, outSightRadius);
        longSightRadius = Mathf.Max(outSightRadius + 1, longSightRadius);

        //记录Data等级，用于Debug
        lv++;
        atkData.lv++;

        //更新属性浮窗
        PlayerDataCanvas.Instance.UpdateText();
    }

}
