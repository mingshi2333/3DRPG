using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public enum ContinuEffectType { GetDefence , ExpUpRate, DamageChangeRate, DizzyAfterAttack, AttackCDRate }

public class CharacterStats : MonoBehaviour
{
    public event Action<int, int> UpdateHealthBarOnAttack;
    [Tooltip("���������ָ�ʱ��ͷ����ʾ����������������ֵ�Ч��")]
    public bool isPlayer;

    public CharacterData_SO tempCharaData;
    public CharacterData_SO characterData;

    public AttackData_SO tempAtkData;
    public AttackData_SO attackData;
    public RuntimeAnimatorController baseAnimator;

    [Header("Weapon")]
    public Transform weaponSlot;
    public Transform sheildSlot;
    public Transform bowSlot;
    public Transform arrowSlot;


    [HideInInspector]public bool isCritical;

    [HideInInspector] public float lastRegenTime;

    [HideInInspector] public int lastAttackRealDamage;

    [HideInInspector] public bool isHoldShield;
    internal float sheildHitBackWaitTime;
    [HideInInspector] public bool isAim;

    [HideInInspector] public float expUpRate = 1;
    private float damageChangeRate = 1;
    [HideInInspector] float afterAttackDizzyRate = 0;
    [HideInInspector] float attackCDRate = 1;
    [HideInInspector] public bool isInvulnerable;

    //����Ч���ļ�ʱ����Ч������
    [HideInInspector] public List<float> continuEffectTimer;
    [HideInInspector] public List<ContinuEffectType> continuEffectTypes;
    [HideInInspector] public List<float> continuEffectAmount;

    //���ɵ��ӵĳ���Ч��
    [HideInInspector] public List<float> continuEffectTimerNoStack;
    [HideInInspector] public List<ContinuEffectType> continuEffectTypesNoStack;
    [HideInInspector] public List<float> continuEffectAmountNoStack;
    [HideInInspector] public List<string> continuEffectIDNoStack;

    [HideInInspector]public InventoryItem arrowItem;

    [HideInInspector] public float lastSkillTime;
    public bool isShieldSuccess;

    //�������»��ζ����ļ���
    public float HitByCritRate { get { return gameObject.CompareTag("Boss") ? 0.3f : 0.6f; } }

    private void Awake()
    {
        if (tempCharaData != null)
            characterData = Instantiate(tempCharaData);

        if (tempAtkData != null)
            attackData = Instantiate(tempAtkData);

        //���������������ֵ��Χ���������Է�Χ
        //characterData.TestLogFullLevelAttribute();

        baseAnimator = GetComponent<Animator>().runtimeAnimatorController;
    }

    private void Update()
    {
        //�Զ���Ѫ
        HealthRegenerate();
        //����Ч������
        ContinuEffectUpdate();

        lastSkillTime -= Time.deltaTime;
    }

    #region Read from CharacterData_SO
    public int MaxHealth
    {
        get { if (characterData != null) return characterData.maxHealth; else return 0; }
        set { characterData.maxHealth = value; }
    }
    public int CurrentHealth
    {
        get { if (characterData != null) return characterData.currentHealth; else return 0; }
        set { characterData.currentHealth = value; }
    }
    public int BaseDefence
    {
        get { if (characterData != null) return characterData.baseDefence; else return 0; }
        set { characterData.baseDefence = value; }
    }
    public int CurrentDefence
    {
        get { if (characterData != null) return characterData.currentDefence; else return 0; }
        set { characterData.currentDefence = value; }
    }
    public int AttackDodgeVel
    {
        get { if (characterData != null) return characterData.attackDodgeVel; else return 0; }
        set { characterData.attackDodgeVel = value; }
    }
    public int MoveDodgeVel
    {
        get { if (characterData != null) return characterData.moveDodgeVel; else return 0; }
        set { characterData.moveDodgeVel = value; }
    }
    public float MoveDodgeCoolDown
    {
        get { if (characterData != null) return characterData.moveDodgeCoolDown; else return 0; }
        set { characterData.moveDodgeCoolDown = value; }
    }
    public float EnterBushSpeedRate
    {
        get { if (characterData != null) return characterData.enterBushSpeedRate; else return 0; }
        set { characterData.enterBushSpeedRate = value; }
    }
    public float HitRockDamagePerVel
    {
        get { if (characterData != null) return characterData.hitRockDamagePerVel; else return 0; }
        set { characterData.hitRockDamagePerVel = value; }
    }
    public float HitTreeDamagePerVel
    {
        get { if (characterData != null) return characterData.hitTreeDamagePerVel; else return 0; }
        set { characterData.hitTreeDamagePerVel = value; }
    }
    public float HitByPlayerDamagePerVel
    {
        get { if (characterData != null) return characterData.hitByPlayerDamagePerVel; else return 0; }
        set { characterData.hitByPlayerDamagePerVel = value; }
    }
    public float PlayerHurtDamagePerVel
    {
        get { if (characterData != null) return characterData.playerHurtDamagePerVel; else return 0; }
        set { characterData.playerHurtDamagePerVel = value; }
    }
    public float EscapeRate
    {
        get { if (characterData != null) return characterData.escapeRate; else return 0; }
        set { characterData.escapeRate = value; }
    }

    public float HealthRegenOnePointTime
    {
        get { if (characterData != null) return characterData.healthRegenOnePointTime; else return 0; }
        set 
        { 
            characterData.healthRegenOnePointTime = value;
            characterData.healthRegenerateIn10Sec = 10f / value;

        }
    }
    public float HealthRegenerateIn10Sec
    {
        get { if (characterData != null) return characterData.healthRegenerateIn10Sec; else return 0; }
        set
        {
            characterData.healthRegenerateIn10Sec = value;
            characterData.healthRegenOnePointTime = 10f / value;
        }
    }
    public float StartRegenTimeAfterCombat
    {
        get { if (characterData != null) return characterData.startRegenTimeAfterCombat; else return 0; }
        set { characterData.startRegenTimeAfterCombat = value; }
    }
    public int CurrentLevel
    {
        get { if (characterData != null) return characterData.currentLevel; else return 0; }
        set { characterData.currentLevel = value; }
    }
    public int LevelNeedExp
    {
        get
        {
            if (characterData == null) return 30;
            else
                return characterData.baseExp - characterData.lastLevelBaseExp;
        }
    }
    public int LevelCurrentExp
    {
        get
        {
            if (characterData == null) return 30;
            else
                return characterData.currentExp - characterData.lastLevelBaseExp;
        }
    }
    public float InSightRadius
    {
        get { if (characterData != null) return characterData.inSightRadius; else return 0; }
        set { characterData.inSightRadius = value; }
    }
    public float OutSightRadius
    {
        get { if (characterData != null) return characterData.outSightRadius; else return 0; }
        set { characterData.outSightRadius = value; }
    }
    public float LongSightRadius
    {
        get { if (characterData != null) return characterData.longSightRadius; else return 0; }
        set { characterData.longSightRadius = value; }
    }
    public float LossInsightTime
    {
        get { if (characterData != null) return characterData.lossInsightTime; else return 0; }
        set { characterData.lossInsightTime = value; }
    }
    #endregion

    #region Read from AttackData_SO
    public float EnemyAttackDuring
    {
        get { return attackData.enemyAtkDuring[attackData.enemyAtkPreLevel]; }
    }

    public float AttackRange
    {
        get 
        {
            //�����ʸ��������Χ���˺������ӣ�
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon && !attackData.isBow)
                    return attackData.weaponAttackRange;
                else
                if (attackData.hasEquipWeapon && !isAim && arrowItem?.itemData != null && isCritical)
                    return arrowItem.itemData.arrowStingerRange;
                else //û�����������ж��ƣ������������
                if (attackData.hasEquipShield)
                    return attackData.attackRange - 0.5f;
                else
                    return attackData.attackRange;
            }
            else
                return 0; 
        }
        set { attackData.attackRange = value; }
    }
    public float SkillRange
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon)
                    return attackData.weaponSkillRange;
                else
                    return attackData.skillRange;
            }
            else
                return 0;
        }
        set { attackData.skillRange = value; }
    }

    public float AttackCoolDown
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon)
                    return attackData.weaponAttackCD * attackCDRate;
                else
                    return attackData.attackCoolDown * attackCDRate;
            }
            else
                return 0;
        }
        set { attackData.attackCoolDown = value; }
    }
    public float ArrowReloadCoolDown
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon && attackData.isBow)
                    return attackData.arrowReloadCoolDown;
                else
                    return 0;
            }
            else
                return 0;
        }
        set { attackData.arrowReloadCoolDown = value; }
    }

    public float SkillCoolDowm
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon)
                    return attackData.weaponSkillCD;
                else
                    return attackData.skillCoolDown;
            }
            else
                return 0;
        }
        set { attackData.skillCoolDown = value; }
    }

    public int MinDamage
    {
        get
        {
            //�����ʸ��������Χ���˺������ӣ�
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon && (!attackData.isBow || isAim))
                    return (int)((attackData.minDamage * 0.3f + attackData.weaponMinDmg) * damageChangeRate);
                else
                if (attackData.hasEquipWeapon && !isAim && arrowItem?.itemData != null && isCritical)
                    return (int)((attackData.minDamage + arrowItem.itemData.arrowStingerDmgAdd)
                        * damageChangeRate);
                else
                    return (int)(attackData.minDamage * damageChangeRate);
            }
            else
                return 0;
        }
        set { attackData.minDamage = value; }
    }
    public int MaxDamage
    {
        get
        {
            //�����ʸ��������Χ���˺������ӣ�
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon && (!attackData.isBow || isAim))
                    return (int)((attackData.maxDamage * 0.3f + attackData.weaponMaxDmg) * damageChangeRate);
                else
                if (attackData.hasEquipWeapon && !isAim && arrowItem?.itemData != null && isCritical)
                    return (int)((attackData.maxDamage + arrowItem.itemData.arrowStingerDmgAdd)
                        * damageChangeRate);
                else
                    return (int)(attackData.maxDamage * damageChangeRate);
            }
            else
                return 0;
        }
        set { attackData.maxDamage = value; }
    }

    public float CriticalMultiplier
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon && (!attackData.isBow || isAim))
                    return (attackData.criticalMultiplier * 0.5f + attackData.weaponCritMul * 0.5f);
                else
                    return attackData.criticalMultiplier;
            }
            else
                return 0;
        }
        set { attackData.criticalMultiplier = value; }
    }
    public float CriticalChance
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon && (!attackData.isBow || isAim))
                    return (attackData.criticalChance * 0.5f + attackData.weaponCritChan * 0.5f);
                else
                    return attackData.criticalChance;
            }
            else
                return 0;
        }
        set { attackData.criticalChance = value; }
    }

    public float AttackHitForce
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon && (!attackData.isBow || isAim))
                    return attackData.weaponAtkHitForce * (0.5f + (float)CurrentLevel / characterData.maxLevel);
                else
                    return attackData.attackHitForce * (0.5f + (float)CurrentLevel / characterData.maxLevel);
            }
            else
                return 0;
        }
        set { attackData.attackHitForce = value; }
    }
    public float MoveSlowRate
    {
        get
        {
            if (attackData != null)
            {
                float msr = attackData.moveSlowRate;
                if (attackData.hasEquipWeapon) msr *= attackData.weaponMoveSlowRate;
                if (attackData.hasEquipShield) msr *= attackData.equipShieldMoveSlowRate;

                return msr;
            }
            else
                return 1;
        }
        set { attackData.moveSlowRate = value; }
    }

    public int SheildDefence
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipShield)
                    return (int)(attackData.sheildDefence * 0.3f + (float)attackData.equipShieldDefence);
                else
                    return attackData.sheildDefence;
            }
            else
                return 0;
        }
        set { attackData.sheildDefence = value; }
    }
    public int ShieldDamage
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipShield)
                    return (int)(attackData.sheildDamage * 0.3f + (float)attackData.equipShieldDamage);
                else
                    return attackData.sheildDamage;
            }
            else
                return 0;
        }
        set { attackData.sheildDamage = value; }
    }
    public int ShieldHitBackDefence
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipShield)
                    return (int)(attackData.sheildHitBackDefence * 0.3f + (float)attackData.equipShieldHitBackDfc);
                else
                    return attackData.sheildHitBackDefence;
            }
            else
                return 0;
        }
        set { attackData.sheildHitBackDefence = value; }
    }
    public float ShieldCritMult
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipShield)
                    return (attackData.criticalMultiplier * 0.5f + attackData.equipShieldCritMult * 0.5f);
                else
                    return attackData.criticalMultiplier;
            }
            else
                return 0;
        }
        set { attackData.criticalMultiplier = value; }
    }
    public float ShieldCritChan
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipShield) //ע���Ƕܱ��ʣ�0.5*1.5 = 0.75
                    return (attackData.criticalChance * 0.75f + attackData.equipShieldCritChan * 0.5f);
                else
                    return attackData.criticalChance * 1.5f;
            }
            else
                return 0;
        }
        set { attackData.criticalChance = value; }
    }
    public float ShieldRushVel
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipShield)
                    return (attackData.sheildRushVel * 0.3f + attackData.equipShieldRushVel * 0.8f);
                else
                    return attackData.sheildRushVel;
            }
            else
                return 0;
        }
        set { attackData.sheildRushVel = value; }
    }
    public float ShieldRushHitForce
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipShield)
                    return (attackData.sheildRushHitForce * 0.3f + attackData.equipShieldRushHitForce * 0.8f);
                else
                    return attackData.sheildRushHitForce;
            }
            else
                return 0;
        }
        set { attackData.sheildRushHitForce = value; }
    }
    public float MinArrowSpeed
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon && attackData.isBow)
                    return attackData.minArrowSpeed;
                else return 0;
            }
            else return 0;
        }
        set { attackData.minArrowSpeed = value; }
    }
    public float MaxArrowSpeed
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon && attackData.isBow)
                    return attackData.maxArrowSpeed;
                else return 0;
            }
            else return 0;
        }
        set { attackData.maxArrowSpeed = value; }
    }

    public float MaxPullBowTime
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon && attackData.isBow)
                    return attackData.maxPullBowTime;
                else return 0;
            }
            else return 0;
        }
        set { attackData.maxPullBowTime = value; }
    }

    public float MinPullBowTime
    {
        get
        {
            if (attackData != null)
            {
                if (attackData.hasEquipWeapon && attackData.isBow)
                    return attackData.minPullBowTime;
                else return 0;
            }
            else return 0;
        }
        set { attackData.minPullBowTime = value; }
    }

    public WeaponSkillData_SO WeaponSkillData
    {
        get { if (attackData != null) return attackData.weaponSkillData; else return null; }
    }

    public float WeaponPitchBias
    {
        get { if (attackData != null) return attackData.pitchBias; else return 0; }
    }
    public float ShieldPitchBias
    {
        get { if (attackData != null) return attackData.sheildPitchBias; else return 0; }
    }

    public float EnemyCrossPlayerRate
    {
        get { if (attackData != null) return attackData.enemyCrossPlayerRate; else return 0; }
    }
    #endregion

    #region Character Health
    private void HealthRegenerate()
    {
        //�����Զ��ָ�������ʱֹͣ��
        if (lastRegenTime < 0 && CurrentHealth < MaxHealth && CurrentHealth > 0)
        {
            lastRegenTime = HealthRegenOnePointTime;
            CurrentHealth++;
            //�������ң�Update Ѫ��UI
            if (isPlayer)
                UpdateHealthBarOnAttack?.Invoke(CurrentHealth, MaxHealth);
        }

        lastRegenTime -= Time.deltaTime;
    }
    public void EnterCombatStatus()
    {
        //ֹͣ�Զ���Ѫ
        lastRegenTime = StartRegenTimeAfterCombat;
    }

    public void RestoreHealth(int point)
    {
        if (point == 0) return;
        //�ظ�ָ������������ֵ���磺���ڹ����Ѫ����Ч����
        if(CurrentHealth < MaxHealth && CurrentHealth > 0)
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth + point, 0, MaxHealth);
            //Update Ѫ��UI
            UpdateHealthBarOnAttack?.Invoke(CurrentHealth, MaxHealth);
        }
    }
    internal void UpdateHealthBarAtOnce()
    {
        //��������£����ڹ������ض��˺������ã���������Ѫ��UI
        UpdateHealthBarOnAttack?.Invoke(CurrentHealth, MaxHealth);
    }

    #endregion

    #region Character Combat

    public int TakeDamage(CharacterStats attacker, CharacterStats defener)
    {
        //��ֹ��μӾ���
        if (defener.CurrentHealth <= 0 || defener.isInvulnerable) return 0;

        int damage = attacker.CurrentDamage();
        if (attacker.attackData.hasEquipWeapon)
        {   //�۳������;ã����ݹ������򣻳ֹ���ս�������;�
            if (!attacker.attackData.isBow)
                InventoryManager.Instance.ReduceWeaponDurability(damage,
                    defener.transform.position - attacker.transform.position);
        }

        int realDmg = Mathf.Max(damage - defener.CurrentDefence, 0);
        //���defener�ľٶܷ���Ч��
        if (defener.isHoldShield
            && defener.transform.CatchedTarget(attacker.transform.position))
        {
            //����Ƕܷ���ֻ�Ա�������
            if (defener.sheildHitBackWaitTime > 0 && attacker.isCritical)
            {
                realDmg = Mathf.Max(damage - defener.ShieldHitBackDefence, 0);

                //�ܷ��̶��˺�
                attacker.TakeDamage(defener.ShieldDamage, defener.transform.position, defener);

                //��Ҳ��Ŷܷ�����
                defener.GetComponent<Animator>().SetTrigger("SheildHitBack");
                //���ﲥ���ܻ�����
                attacker.GetComponent<Animator>().SetTrigger("Hit");
            }
            else
                realDmg = Mathf.Max((damage - defener.SheildDefence), 0);

            //�۳������;ã����ݹ�������
            if (defener.attackData.hasEquipShield)
                InventoryManager.Instance.ReduceSheildDurability(damage,
                    defener.transform.position - attacker.transform.position);

            isShieldSuccess = true;
        }
        else
            isShieldSuccess = false;

        realDmg = Mathf.Min(defener.CurrentHealth, realDmg);
        defener.CurrentHealth -= realDmg;

        if (attacker.isCritical)
        {
            //��ҳֶܻᱻ���ﱩ��ѣ��
            if (defener.isHoldShield && defener.sheildHitBackWaitTime < 0)
                defener.GetComponent<Animator>().SetTrigger("Dizzy");
            else
            //����Ϊ��ͨ�ܻ������ʱ�����
            if (!defener.isHoldShield && UnityEngine.Random.value < HitByCritRate)
                defener.GetComponent<Animator>().SetTrigger("Hit");

            //�ܷ����ᱻ���κ��ܻ�
        }
        else
        {
            //û���ܻ������������������ֶ��������
            if (defener.CurrentHealth > 0)
                defener.GetComponent<EnemyController>()?.RandomPlayNoise(0.2f);
        }

        //��������Ч��
        if (attacker.AttackHitForce > 0)
        {
            defener.GetComponent<NavMeshAgent>().velocity +=
                (attacker.isCritical ? 1.5f : 1f) * attacker.AttackHitForce * attacker.transform.forward;
        }

        //Update Ѫ��UI
        defener.UpdateHealthBarOnAttack?.Invoke(CurrentHealth, MaxHealth);
        if (!defener.gameObject.CompareTag("Player"))
        {
            //��Ҹ��¹���͸�ӣ�Boss���´�Ѫ��
            GameManager.Instance.player.GetComponent<PlayerController>().
                SwitchLongSightTargets(defener.gameObject);
            BossHealthUI.Instance.WakeUpBossHealthBar(defener.gameObject);
        }

        //����Update
        if (CurrentHealth <= 0)
            attacker.characterData.UpdateExp(characterData.killPoint);

        //ʵ������˺����ݸ������ߣ����ڹ�����ѪЧ��
        attacker.lastAttackRealDamage = realDmg;

        sheildHitBackWaitTime = -1; //�ܷ�ֻ�ᴥ��һ�Σ����۳ɹ����

        if(defener.GetComponent<EnemyController>()!=null)
        {
            var enemy = defener.GetComponent<EnemyController>();
            //�����������ܻ�ս���ƶ��ĳ���ʱ��
            if (enemy.remainEscapeTime > 0) enemy.remainEscapeTime *= 0.38f;
            if (enemy.AttackTarget == null)
            {
                enemy.lastStateFrames = -1;
                enemy.remainEscapeTime = -1;
            }
            //���﷢����ң�
            enemy.AttackTarget = attacker.gameObject;
            //�ӳ�׷��ʱ��
            if (enemy.remainChaingTime < 10f) enemy.remainChaingTime += 5f;
            //�����м�������
            if (defener.CurrentHealth < attacker.CurrentHealth) enemy.CheckEscape();
        }
        else
        {
            //���﹥�����м���Խ�����λ��
            attacker.GetComponent<EnemyController>().CheckCrossPlayer();
        }

        //˫������ս��״̬�����û�Ѫ��
        attacker.EnterCombatStatus();
        defener.EnterCombatStatus();

        //�����߿����ܵ�DizzyAfterAttackЧ��
        if (attacker.afterAttackDizzyRate > 0 && UnityEngine.Random.value < attacker.afterAttackDizzyRate)
            attacker.GetComponent<Animator>().SetTrigger("Dizzy");

        //����ԭʼ�˺�������ʵ���˺���
        return damage;
    }

    public void TakeDamage(int damage, Vector3 attackFrom, CharacterStats attacker = null)
    {
        //��ֹ��μӾ���
        if (CurrentHealth <= 0 || isInvulnerable) return;

        int realDmg = Mathf.Max(damage - CurrentDefence, 0);
        //���defener�ľٶܷ���Ч��
        if (isHoldShield && transform.CatchedTarget(attackFrom))
        {
            realDmg = Mathf.Max((damage - SheildDefence), 0);

            //�۳������;ã����ݹ�������
            if (attackData.hasEquipShield)
                InventoryManager.Instance.
                    ReduceSheildDurability(damage, transform.position - attackFrom);

            isShieldSuccess = true;
        }
        else
            isShieldSuccess = false;

        realDmg = Mathf.Min(CurrentHealth, realDmg);
        CurrentHealth = CurrentHealth - realDmg;

        //û���ܻ������������������ֶ��������
        if (CurrentHealth > 0) 
            GetComponent<EnemyController>()?.RandomPlayNoise(0.2f);

        //Update Ѫ��UI
        UpdateHealthBarOnAttack?.Invoke(CurrentHealth, MaxHealth);
        if (!gameObject.CompareTag("Player"))
        {
            //��Ҹ��¹���͸�ӣ�Boss���´�Ѫ��
            GameManager.Instance.player.GetComponent<PlayerController>().
                SwitchLongSightTargets(gameObject);
            BossHealthUI.Instance.WakeUpBossHealthBar(gameObject);
        }

        if (attacker != null)
        {
            //����Update
            if (CurrentHealth <= 0)
                attacker.characterData.UpdateExp(characterData.killPoint);

            //��ʵ����˺����ݸ������ߣ����ڹ�����ѪЧ��
            attacker.lastAttackRealDamage = realDmg;

            //����������ս��״̬
            attacker.EnterCombatStatus();
        }

        if (GetComponent<EnemyController>() != null && attacker != null)
        {
            var enemy = GetComponent<EnemyController>();
            //�����������ܻ�ս���ƶ��ĳ���ʱ��
            if (enemy.remainEscapeTime > 0) enemy.remainEscapeTime *= 0.38f;
            if (enemy.AttackTarget == null)
            {
                enemy.lastStateFrames = -1;
                enemy.remainEscapeTime = -1;
            }
            //���﷢����ң�
            enemy.AttackTarget = attacker.gameObject;
            //�ӳ�׷��ʱ��
            if (enemy.remainChaingTime < 10f) enemy.remainChaingTime += 5f;
            //�����м�������
            if (CurrentHealth < attacker.CurrentHealth) enemy.CheckEscape();
        }

        //�������ս��״̬
        EnterCombatStatus();
    }

    public int CurrentDamage()
    {
        float coreDamage = UnityEngine.Random.Range(MinDamage, MaxDamage);

        if(isCritical)
        {
            coreDamage *= CriticalMultiplier;
        }

        return (int)coreDamage;
    }

    internal int FireArrow(Vector3 arrowVel, float bowPullAdditionRate)
    {
        //���ɼ�ʸ������λ��
        Transform arrowOnBow = bowSlot.GetChild(0).GetChild(2).GetChild(0);
        var arrowOnWorld = Instantiate(arrowItem.itemData.objectOnWorld);
        arrowOnWorld.transform.position =
            (transform.position + new Vector3(0, 0.7f, 0) + arrowOnBow.transform.position) * 0.5f;
        arrowOnWorld.transform.rotation = arrowOnBow.transform.rotation;

        //�����˺�����ʸ����
        int arrowDamage = MinDamage + (int)((MaxDamage - MinDamage) * bowPullAdditionRate);
        float brokenRate = arrowItem.itemData.arrowBrokenRate * arrowDamage / MaxDamage;
        //�˺����Ǳ����ͼӳ�
        if (isCritical)
            arrowDamage = (int)(arrowDamage * CriticalMultiplier);
        arrowDamage += arrowItem.itemData.arrowDamageAdd;

        //�����ʸ��
        ///��ʸ���ʵļ���Ƚϸ���
        arrowOnWorld.GetComponent<Arrow>().Fire(arrowVel, arrowDamage,
            isCritical || bowPullAdditionRate > 0.6f,
            arrowItem.itemData.arrowPushForceRate,
            arrowItem.itemData.arrowMaxPushForce,
            brokenRate);

        //��ʸ�������٣�ˢ�±�����
        arrowItem.amountInInventory--;
        if (arrowItem.amountInInventory == 0) arrowItem.itemData = null;
        InventoryManager.Instance.inventoryUI.RefreshUI();

        //ж��slot�ϵļ�ʸ
        UnEquipArrow();
        
        return arrowDamage;
    }
    #endregion

    #region Equip Weapon
    public void ChangeWeapon(ItemData_SO weapon)
    {
        UnEquipWeapon();
        UnEquipArrow();
        EquipWeapon(weapon);
    }

    public void EquipWeapon(ItemData_SO weapon)
    {
        if(weapon.weaponPrefab!=null)
        {
            //˫�������򹭣���ֹװ�����ƣ����ض���Icon
            if (weapon.weaponData.isTwoHand || weapon.weaponData.isBow)
                UnEquipSheild();

            //��������
            attackData.ApplyWeaponData(weapon.weaponData);

            //���¶��Ʋ�Icon
            var sheildHolder = InventoryManager.Instance.equipmentUI.slotHolders[1];
            if (sheildHolder.itemUI.Index >= 0) sheildHolder.UpdateItem();

            GameObject weaponObj;
            //�ڶ�ӦSlot��������Weapon
            if (weapon.weaponData.isBow)
                weaponObj = Instantiate(weapon.weaponPrefab, bowSlot);
            else
                weaponObj = Instantiate(weapon.weaponPrefab, weaponSlot);

            //�л�����
            GetComponent<Animator>().runtimeAnimatorController = weapon.weaponAnimator;

            //����agent�ٶ�
            GetComponent<NavMeshAgent>().speed =
                GetComponent<PlayerController>().agentSpeed * MoveSlowRate;
        }
    }

    public void UnEquipWeapon()
    {
        if (weaponSlot.childCount != 1)
        {
            for (int i = 0; i < weaponSlot.childCount; i++)
            {
                //������ʸ��
                if (weaponSlot.GetChild(i).transform != arrowSlot)
                    Destroy(weaponSlot.GetChild(i).gameObject);
            }
        }
        if (bowSlot.transform.childCount != 0)
        {
            for (int i = 0; i < bowSlot.childCount; i++)
            {
                Destroy(bowSlot.transform.GetChild(i).gameObject);
            }
        }

        //ж����������ԭ����
        attackData.hasEquipWeapon = false;
        attackData.isTwoHand = false;
        attackData.isBow = false;

        //���¶��Ʋ�Icon
        var sheildHolder = InventoryManager.Instance.equipmentUI.slotHolders[1];
        if(sheildHolder.itemUI.Index>=0) sheildHolder.UpdateItem();

        //�л�����
        if (!attackData.hasEquipWeapon && !attackData.hasEquipShield)
            GetComponent<Animator>().runtimeAnimatorController = baseAnimator;

        //����agent�ٶ�
        GetComponent<NavMeshAgent>().speed =
            GetComponent<PlayerController>().agentSpeed * MoveSlowRate;
    }

    public void ChangeSheild(ItemData_SO sheild)
    {
        UnEquipSheild();
        UnEquipArrow();
        EquipSheild(sheild);
    }

    public void EquipSheild(ItemData_SO sheild)
    {
        if (sheild.weaponPrefab != null)
        {
            //˫��������ֹ�ʱ������װ������
            if (attackData.isTwoHand || attackData.isBow) return;

            //�ڶ�ӦSlot��������Weapon
            Instantiate(sheild.weaponPrefab, sheildSlot);

            //��������
            attackData.ApplySheildData(sheild.weaponData);

            //�л�����
            GetComponent<Animator>().runtimeAnimatorController = sheild.weaponAnimator;

            //����agent�ٶ�
            GetComponent<NavMeshAgent>().speed =
                GetComponent<PlayerController>().agentSpeed * MoveSlowRate;
        }
    }

    public void UnEquipSheild()
    {
        if (sheildSlot.childCount != 0)
        {
            for (int i = 0; i < sheildSlot.childCount; i++)
            {
                Destroy(sheildSlot.GetChild(i).gameObject);
            }
        }
        //ж�¶��ƣ���ԭ����
        attackData.hasEquipShield = false;
        isHoldShield = false;

        //�л�����
        if (!attackData.hasEquipWeapon && !attackData.hasEquipShield)
            GetComponent<Animator>().runtimeAnimatorController = baseAnimator;

        //����agent�ٶ�
        GetComponent<NavMeshAgent>().speed =
            GetComponent<PlayerController>().agentSpeed * MoveSlowRate;
    }

    internal bool EquipArrow()
    {
        //���֮ǰ��ʸ
        UnEquipArrow();
        //�Ȳ��ұ�������û�м�ʸ
        arrowItem = InventoryManager.Instance.FindArrow();
        //����ǻ�Ӱ�������ɻ�Ӱ��ʸ
        if (arrowItem == null && WeaponSkillData != null && WeaponSkillData.GetBullet()!= null)
        {
            arrowItem = new InventoryItem 
            { itemData = WeaponSkillData.GetBullet(), amountInInventory = 99 };
        }

        if(arrowItem!=null)
        {
            //�������ɼ�ʸ
            Instantiate(arrowItem.itemData.weaponPrefab, arrowSlot);
            //��������Slot���ɼ�ʸ
            var arrowOnBow = Instantiate(arrowItem.itemData.weaponPrefab,
                bowSlot.GetChild(0).GetChild(2));
            return true;
        }

        return false;
    }
    internal void UnEquipArrow()
    {
        //ɾ�������ϵļ�ʸ
        if (arrowSlot.childCount != 0)
        {
            for (int i = 0; i < arrowSlot.childCount; i++)
            {
                Destroy(arrowSlot.GetChild(i).gameObject);
            }
        }
        //ɾ����������Slot�ļ�ʸ
        if (bowSlot.childCount != 0 && bowSlot.GetChild(0).GetChild(2).childCount != 0)
            Destroy(bowSlot.GetChild(0).GetChild(2).GetChild(0).gameObject);

        arrowItem = null;
    }
    #endregion

    #region Apply Data Change
    public void ContinuEffectUpdate()
    {
        CurrentDefence = BaseDefence;
        expUpRate = 1;
        damageChangeRate = 1;
        afterAttackDizzyRate = 1; ///����DizzyRate����������
        attackCDRate = 1;

        //���г���Ч����ʱ����ˢ�£��Ӻ���ǰö��
        for (int i = continuEffectTimer.Count-1; i >= 0; i--)
        {
            continuEffectTimer[i] -= Time.deltaTime;
            if(continuEffectTimer[i]<0)
            {
                //ʱ�䵽��ɾ����ʱ���Ͷ�Ӧ���Ա仯
                continuEffectTimer.RemoveAt(i);
                continuEffectTypes.RemoveAt(i);
                continuEffectAmount.RemoveAt(i);
            }
            else
            {
                //���Ӹü�ʱ���Ķ�ӦЧ��
                switch (continuEffectTypes[i])
                {
                    case ContinuEffectType.GetDefence:
                        CurrentDefence += (int)continuEffectAmount[i];
                        break;
                    case ContinuEffectType.ExpUpRate:
                        expUpRate *= continuEffectAmount[i];
                        break;
                    case ContinuEffectType.DamageChangeRate:
                        damageChangeRate *= continuEffectAmount[i];
                        break;
                    case ContinuEffectType.DizzyAfterAttack:
                        afterAttackDizzyRate *= (1 - continuEffectAmount[i]);
                        break;
                    case ContinuEffectType.AttackCDRate:
                        attackCDRate *= continuEffectAmount[i];
                        break;
                }
            }
        }

        //NoStack�ĳ���Ч����ʱ����ˢ�£��Ӻ���ǰö��
        for (int i = continuEffectTimerNoStack.Count - 1; i >= 0; i--)
        {
            continuEffectTimerNoStack[i] -= Time.deltaTime;
            if (continuEffectTimerNoStack[i] < 0)
            {
                //ʱ�䵽��ɾ��NoStack��ʱ���Ͷ�Ӧ���Ա仯
                continuEffectTimerNoStack.RemoveAt(i);
                continuEffectTypesNoStack.RemoveAt(i);
                continuEffectAmountNoStack.RemoveAt(i);
                continuEffectIDNoStack.RemoveAt(i);
            }
            else
            {
                //����NoStack��ʱ���Ķ�ӦЧ��
                switch (continuEffectTypesNoStack[i])
                {
                    case ContinuEffectType.GetDefence:
                        CurrentDefence += (int)continuEffectAmountNoStack[i];
                        break;
                    case ContinuEffectType.ExpUpRate:
                        expUpRate *= continuEffectAmountNoStack[i];
                        break;
                    case ContinuEffectType.DamageChangeRate:
                        damageChangeRate *= continuEffectAmountNoStack[i];
                        break;
                    case ContinuEffectType.DizzyAfterAttack:
                        afterAttackDizzyRate *= (1 - continuEffectAmountNoStack[i]);
                        break;
                    case ContinuEffectType.AttackCDRate:
                        attackCDRate *= continuEffectAmountNoStack[i];
                        break;
                }
            }
        }

        afterAttackDizzyRate = 1 - afterAttackDizzyRate;
    }
    public void AddItemUsedTimer(UseableItemData_SO item)
    {
        if (item.getDefence != 0)
        {
            continuEffectTimer.Add(item.keepTime);
            continuEffectTypes.Add(ContinuEffectType.GetDefence);
            continuEffectAmount.Add(item.getDefence);
        }
        if (item.expUpRate != 1)
        {
            continuEffectTimer.Add(item.keepTime);
            continuEffectTypes.Add(ContinuEffectType.ExpUpRate);
            continuEffectAmount.Add(item.expUpRate);
        }
    }

    public void AddEffectTimer(ContinuEffectType effectType, float amount, float keepTime, 
        bool canStack =true, string noStackID = "")
    {
        if(canStack)
        {
            //���Buff���Ե���
            continuEffectTimer.Add(keepTime);
            continuEffectTypes.Add(effectType);
            continuEffectAmount.Add(amount);
        }
        else
        {//���Buff�����Ե��ӣ�����NoStack�ļ�ʱ��
            //�����Ƿ����ID
            int num = -1;
            for (int i = 0; i < continuEffectIDNoStack.Count; i++)
            {
                if(continuEffectIDNoStack[i] == noStackID)
                {
                    num = i;
                    break;
                }
            }

            if (num == -1)
            {
                //�����������List
                continuEffectTimerNoStack.Add(keepTime);
                continuEffectTypesNoStack.Add(effectType);
                continuEffectAmountNoStack.Add(amount);
                continuEffectIDNoStack.Add(noStackID);
            }
            else
            {
                //�����򸲸�
                continuEffectTimerNoStack[num] = keepTime;
                continuEffectTypesNoStack[num] = effectType;
                continuEffectAmountNoStack[num] = amount;
            }
        }
    }

    #endregion
}
