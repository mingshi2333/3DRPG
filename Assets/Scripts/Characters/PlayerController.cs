using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cinemachine;

//举盾技能，按下右键举盾朝移动点移动，并保持面朝攻击目标；左键恢复普通移动（相当于取消举盾）
///期间正面90度防御提升(最大最小攻击力之和*0.444f)，但是会被暴击眩晕（技能的话本来就有眩晕）
///举盾时按下空格可朝目标进行冲撞，造成(最大最小攻击力之和*0.333f)的攻击伤害；
///盾牌冲撞触发暴击，可造成怪物眩晕（对Boss无效）

//盾反，盾反可格挡暴击(最大最小攻击力之和*0.777f)，并使目标受击
///盾反对反应力要求很高，高风险高收益；盾反对非暴击无效

//制作双持武器动画，一系列武器

public class PlayerController : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private CharacterStats characterStats;
    private CapsuleCollider col;
    [HideInInspector] public Rigidbody playerRigidBody;

    [HideInInspector]public GameObject attackTarget;
    [HideInInspector]public GameObject lastTarget;
    private float lastAttackTime;
    private float attackNear;
    [HideInInspector] public bool isDead;
    private float stopDistance;

    private NavMeshAgent targetAgent;
    private float startAtkDistance;
    private float playerNavMeshMoveSize;

    [HideInInspector]public bool isKickingOff; //是否kickOff状态，用于撞墙伤害
    private float lastDodgeTime;
    private float sheildRushKeepTime;
    [HideInInspector] public Vector3 beforeDodgePos;

    [HideInInspector] public float agentAngularSpeed;

    RaycastHit upHighDownHit;
    [HideInInspector] public float timeKeepOnGround;

    [HideInInspector]public float agentSpeed;

    float bowPullTime;
    float bowPullRate;
    private float bowPullAdditionRate;
    private float arrowHoriSpeed;
    Vector3 arrowStartPos;
    Vector3 aimPos;
    private Vector3 arrowVel;
    private bool backDodging;
    private bool isWaitingFire;
    private bool isCancelBowPull;

    public Transform leftHand;
    public Transform rightHand;
    [HideInInspector]public Sound currentWeaponSound;

    [HideInInspector] public SoundName weaponWaveSound;
    SoundName enemyHitSound;
    private bool isNearAttackTarget;
    private Collider[] slashEnemyCols;
    private Collider[] insightEnemyCols;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        characterStats = GetComponent<CharacterStats>();
        col = GetComponent<CapsuleCollider>();
        playerRigidBody = GetComponent<Rigidbody>();

        stopDistance = agent.stoppingDistance;
        agentSpeed = agent.speed;
        agentAngularSpeed = agent.angularSpeed;
        //玩家点击远处目标时，NavMesh更新前可移动的最远距离
        playerNavMeshMoveSize =
            FindObjectOfType<LocalNavMeshBuilder>().m_PlayerNavMeshBoundsSize.x * 0.5f;
    }

    void Start()
    {
        //CinemachineFreeLook初始会随意变化视角，为了保证相机视角，初始是关闭的
        ///移到RigisterPlayer里面？
        FindObjectOfType<CinemachineFreeLook>(true).gameObject.SetActive(true);
    }

    private void OnEnable()
    {
        MouseManager.Instance.OnMouseClicked += MoveToTarget;
        MouseManager.Instance.OnEnemyClicked += EventAttack;
        MouseManager.Instance.OnPlayerKeyInput += SpaceDodge;

        GameManager.Instance.RigisterPlayer(characterStats);
    }

    private void OnDisable()
    {
        if (MouseManager.Instance != null)
        {
            MouseManager.Instance.OnMouseClicked -= MoveToTarget;
            MouseManager.Instance.OnEnemyClicked -= EventAttack;
            MouseManager.Instance.OnPlayerKeyInput -= SpaceDodge;
        }
    }

    private void SpaceDodge()
    {
        if (isDead) return;

        if (lastDodgeTime < 0)
        {
            //保存闪避前的位置
            beforeDodgePos = transform.position;
            //闪避涉水的衰减
            float waterSlow = Mathf.Lerp(1f, 0.5f, AudioManager.Instance.footStepWaterDeep - transform.position.y);

            //计算玩家持盾冲锋/持弓后闪/普通闪避
            if (agent.isOnNavMesh) agent.isStopped = true;
            if (characterStats.isHoldShield)
            {
                //不同盾牌，冲撞速度不一样
                if (attackTarget != null && agent.updateRotation == false)
                    agent.velocity =
                        (attackTarget.transform.position - transform.position).normalized
                        * characterStats.ShieldRushVel * waterSlow;
                else
                    agent.velocity = transform.forward * characterStats.ShieldRushVel * waterSlow;

                //冲撞伤害，只对攻击目标产生效果
                sheildRushKeepTime = 0.618f;
                //音效
                PlayrShieldDodgeSound();
            }
            else
            if (characterStats.isAim)
            {
                if (attackTarget != null && (transform.position - attackTarget.transform.position).magnitude
                    < characterStats.characterData.outSightRadius)
                {
                    agent.angularSpeed = 0;
                    backDodging = true;
                    //持弓朝后闪避（动画触发）
                    animator.SetTrigger("BackDodge");
                }
                else
                {
                    //持弓朝前闪避（动画触发）
                    animator.SetTrigger("ForwardDodge");
                }
            }
            else 
            {
                //朝移动方向闪避
                agent.velocity = transform.forward * characterStats.MoveDodgeVel
                    * characterStats.MoveSlowRate * waterSlow;
                //闪避音效
                PlayDodgeSound();
            }

            //闪避CD会随装备重量延长
            lastDodgeTime = characterStats.MoveDodgeCoolDown / characterStats.MoveSlowRate;
        }

        if(Input.GetMouseButton(1) && sheildRushKeepTime > 0 && characterStats.lastSkillTime < 0)
        {
            //冲锋期间再次按下空格，释放副技能
            if (characterStats.WeaponSkillData && characterStats.WeaponSkillData.CanSecondSkill())
            {
                characterStats.WeaponSkillData?.SecondSkill();
                sheildRushKeepTime = -1;
            }
        }
    }

    private void Update()
    {
        isDead = characterStats.CurrentHealth == 0;
        //玩家死亡，通知所有怪物
        if (isDead)
            GameManager.Instance.NotifyObserversPlayerDead();

        //判定立即射击
        if (isWaitingFire && bowPullTime >= characterStats.MinPullBowTime)
            FireArrow();
        //拉弓蓄力相关参数
        if (characterStats.isAim)
            CheckArrowVel();

        CheckLeftWeapon();
        SwitchAnimation();
        SwitchKinematic();
        SwitchInsightTargets();
        SheildRushCheck();

        lastDodgeTime -= Time.deltaTime;
        lastAttackTime -= Time.deltaTime;


        SetPlayerOnGround();
        timeKeepOnGround -= Time.deltaTime;
        //强行修正agent进入夹层的bug
        //if (upHighDownHit.point.y > transform.position.y + 1)
        //{
        //    timeKeepOnGround = 5f;
        //    agent.isStopped = true;
        //}

        StopPushTarget();

        //检测播放海浪声
        CheckSeaAmbientMusic();
    }

    //防止很容易推动攻击目标的Agent
    void StopPushTarget()
    {
        if (attackTarget == null || attackTarget.CompareTag("Attackable")) return;

        float nearby = agent.radius * transform.localScale.x +
            attackTarget.GetComponent<NavMeshAgent>().radius * attackTarget.transform.localScale.x;

        Vector3 dir = attackTarget.transform.position - transform.position;
        float dot = Vector3.Dot(dir.normalized, agent.velocity);
        if (dot > 0 && dir.magnitude < nearby + 0.1f)
        {
            //扣除目标方向上的移动速度分量
            agent.velocity -= dir.normalized * dot;
        }

    }

    private void OnAnimatorIK(int layerIndex)
    {
        //根据当前箭矢蓄力的速度方向，设置朝向（头部IK、右手/左手IK、弓、箭矢）
        if (characterStats.isAim)
        {
            //agent的胸口位置（注意不是模型的胸口！）
            Vector3 chest = transform.position + new Vector3(0, 0.7f, 0);
            Vector3 focus = chest + arrowVel;
            //设置头部LookAt，焦点为胸口开始沿箭矢方向的一段距离
            animator.SetLookAtPosition(focus);
            animator.SetLookAtWeight(bowPullRate, 0.382f, 1f, 0, 0.618f);

            //右手计算基点（而非最终位置）
            Vector3 rightHandFocus = chest + transform.right * 0.18f - transform.forward * 0.33f;

            //左手
            Vector3 leftHandFocus = rightHandFocus + arrowVel.normalized *
                Vector3.Distance(leftHand.position, rightHandFocus) * 1.5f
                + transform.right * 0.39f;
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandFocus);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            //弓
            Transform bow_eq = characterStats.bowSlot.GetChild(0);
            bow_eq.LookAt(leftHand.position - (rightHandFocus * 0.7f + chest * 0.3f)
                 + bow_eq.position);
            //右手（最终位置）
            animator.SetIKPosition(AvatarIKGoal.RightHand,
                bow_eq.GetChild(0).GetChild(0).transform.position
                 + transform.right * 0.07f - transform.forward * 0.07f - transform.up * 0.03f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);

            //箭矢
            //Transform arrow_eq = characterStats.arrowSlot.GetChild(0);
            //Vector3 aPos = rightHand.position + (leftHand.position - rightHand.position) * 0.9f
            //    - transform.right * 0.15f;
            //arrow_eq.position = Vector3.Lerp(arrow_eq.position, aPos, bowPullTime);
            ////调整朝向时，还需让箭矢不会一直旋转，使箭矢一面始终朝上
            //arrow_eq.rotation =
            //    Quaternion.FromToRotation(Vector3.up, arrow_eq.position - rightHand.position) *
            //    Quaternion.LookRotation(arrow_eq.position - rightHand.position, Vector3.up);
            if (bowPullTime > characterStats.MinPullBowTime)
            {
                //在弓上面显示一个新箭矢，隐藏左手箭矢，从而修复箭矢抖动
                bow_eq.GetChild(2)?.gameObject.SetActive(true);
                characterStats.arrowSlot.GetChild(0).gameObject.SetActive(false);
            }
        }
        else
        {
            //还原Ik设置、弓箭设置
            animator.SetLookAtWeight(0);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 0);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0);
            if (characterStats.bowSlot.childCount > 0)
            {
                characterStats.bowSlot.GetChild(0).localRotation = Quaternion.identity;
                characterStats.bowSlot.GetChild(0).GetChild(2)?.gameObject.SetActive(false);
            }
            if (characterStats.arrowSlot.childCount > 0)
            {
                characterStats.arrowSlot.GetChild(0).localPosition = Vector3.zero;
                characterStats.arrowSlot.GetChild(0).localRotation = Quaternion.identity;
                characterStats.arrowSlot.GetChild(0).gameObject.SetActive(true);
            }
        }
    }

    void OnDrawGizmos()
    {
        //if(animator!=null)
        //{
        //    Gizmos.color = Color.blue;
        //    Gizmos.DrawWireSphere(leftHand.position, 0.1f);
        //    Gizmos.DrawWireSphere(rightHand.position, 0.1f);
        //    Gizmos.color = Color.yellow;
        //    Vector3 chest = transform.position + new Vector3(0, 0.7f, 0);
        //    Gizmos.DrawWireSphere(chest, 0.1f);
        //}

        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + col.center, col.height * 0.618f);
        }
    }

    private void CheckArrowVel()
    {
        if (characterStats.arrowItem?.itemData == null) return;
        if(attackTarget && attackTarget.CompareTag("Attackable")) return;

        bowPullTime = Mathf.Clamp(bowPullTime + Time.deltaTime, 0, characterStats.MaxPullBowTime);

        if (bowPullTime <= characterStats.MinPullBowTime)
        {
            bowPullAdditionRate = 0;
            arrowHoriSpeed = characterStats.MinArrowSpeed;
        }
        else
        {
            //计算当前蓄力下的箭矢水平速度
            bowPullAdditionRate = (bowPullTime - characterStats.MinPullBowTime)
                / (characterStats.MaxPullBowTime - characterStats.MinPullBowTime);
            arrowHoriSpeed = characterStats.MinArrowSpeed +
                (characterStats.MaxArrowSpeed - characterStats.MinArrowSpeed) * bowPullAdditionRate;
        }
        //当前箭矢速度加成
        arrowHoriSpeed *= characterStats.arrowItem.itemData.arrowSpeedChangeRate;

        arrowStartPos = characterStats.arrowSlot.position;
        if (attackTarget != null && !attackTarget.CompareTag("Attackable")
            && !attackTarget.GetComponent<EnemyController>().isDead)
        {
            //较大的单位Collider绑定在子物体骨骼上
            Vector3 targetCenter; 
            if (attackTarget.GetComponent<Collider>() == null)
                targetCenter = attackTarget.GetComponentInChildren<Collider>().bounds.center;
            else
                targetCenter = attackTarget.GetComponent<Collider>().bounds.center;

            if (Vector3.Distance
            (transform.position, attackTarget.transform.position) < characterStats.LongSightRadius)
            {
                //如LongSightRadius范围内有目标，快速瞄准胸口
                aimPos = targetCenter;
            }
            else
            {
                //范围外有目标，需要缓慢瞄准，但速度足以射中
                Vector3 aimDir = targetCenter - arrowStartPos;
                Vector3 tempDir = transform.forward * characterStats.MinArrowSpeed;

                aimPos = arrowStartPos + Vector3.Slerp(tempDir, aimDir,
                    Vector3.Dot(tempDir.normalized, aimDir.normalized));
                transform.LookAt(aimPos);
            }
        }
        else //否则瞄准短时间后箭矢水平移动的位置
            aimPos = arrowStartPos + transform.forward * characterStats.MinArrowSpeed;

        //计算射中目标所需的时间
        ///以agent胸口为起点，防止怪物靠近出问题（注意不是模型的胸口）
        Vector3 chest = transform.position + new Vector3(0, 0.7f, 0);
        Vector3 direction = aimPos - chest;
        Vector3 horiDir = new Vector3(direction.x, 0, direction.z);
        float t = horiDir.magnitude / arrowHoriSpeed;

        //预测怪物一定时间后移动位置，适当随机，不考虑怪物转向
        if (attackTarget!=null && !attackTarget.CompareTag("Attackable") &&
            !attackTarget.GetComponent<EnemyController>().isDead)
        {
            NavMeshAgent targetAgent = attackTarget.GetComponent<NavMeshAgent>();
            aimPos += targetAgent.velocity * UnityEngine.Random.Range(t * 0.5f, t);
            //修正参数
            direction = aimPos - chest;
            horiDir = new Vector3(direction.x, 0, direction.z);
            t = horiDir.magnitude / arrowHoriSpeed;
        }

        Vector3 horiVel = horiDir.normalized * arrowHoriSpeed;
        Vector3 vertVel = Vector3.up * (direction.y / t - Physics.gravity.y * t * 0.5f);

        //求得箭矢速度向量
        arrowVel = horiVel + vertVel;
    }

    //发射箭矢
    void FireArrow()
    {
        if (characterStats.arrowItem == null) return;

        //判定暴击
        characterStats.isCritical = UnityEngine.Random.value < characterStats.CriticalChance;
        //箭矢生成和发射
        int damage =  characterStats.FireArrow(arrowVel, bowPullAdditionRate);

        //角色和弓的动画Trigger
        animator.SetTrigger("Fire");
        characterStats.bowSlot.GetChild(0).GetComponent<Animator>().SetTrigger("Fire");

        //根据伤害扣除武器弓耐久
        InventoryManager.Instance.ReduceBowDurability(damage, arrowVel);

        //重置CD、相关参数
        lastAttackTime = characterStats.ArrowReloadCoolDown;
        isWaitingFire = false;
        characterStats.isAim = false;
        bowPullTime = 0;
        agent.speed = agentSpeed * characterStats.MoveSlowRate;
    }


    private void CheckLeftWeapon()
    {
        //蓄力状态下，左键取消拉弓（保留箭矢），agent不停止移动
        if (Input.GetMouseButtonDown(0) && characterStats.isAim)
        {
            characterStats.isAim = false;
            agent.speed = agentSpeed * characterStats.MoveSlowRate;
            isCancelBowPull = true;
        }

        //按住右键时（举盾）按下左键，使用主技能
        if (Input.GetMouseButtonDown(0) && Input.GetMouseButton(1))
        {
            if(characterStats.lastSkillTime<0)
                characterStats.WeaponSkillData?.MainSkill();
        }

        //举盾取消；射击箭矢
        if (Input.GetMouseButtonUp(1))
        {
            //等待射击箭矢！
            if (characterStats.isAim) isWaitingFire = true;

            //还原状态
            isCancelBowPull = false;
            characterStats.isHoldShield = false;
            agent.speed = agentSpeed * characterStats.MoveSlowRate;
            if(agent.isOnNavMesh)agent.isStopped = true;
        }
        //举盾，盾反计时
        if (Input.GetMouseButtonDown(1))
        {
            if (characterStats.attackData.isBow)
            {
                //拉弓上箭，如果失败表明背包没有箭矢
                if (lastAttackTime < 0 && characterStats.EquipArrow())
                {
                    characterStats.isAim = true;
                    isWaitingFire = false;
                    bowPullTime = 0;
                    agent.speed = agentSpeed * 0.3f * characterStats.MoveSlowRate;
                }
                else //如果找不到箭矢，这里关闭EventAttack里的临时开启
                    characterStats.isAim = false;
            }
            else
            {
                //设置盾反可触发时间
                if (!characterStats.isHoldShield)
                    characterStats.sheildHitBackWaitTime = 0.5f;

                characterStats.isHoldShield = true;
                agent.speed = agentSpeed * 0.3f * characterStats.MoveSlowRate;
            }
        }
        //防止cd期间提早按下拉弓失败的情况
        if (Input.GetMouseButton(1) && !isCancelBowPull)
        {
            if (characterStats.attackData.isBow)
            {
                if (!characterStats.isAim && lastAttackTime < 0 && characterStats.EquipArrow())
                {
                    characterStats.isAim = true;
                    isWaitingFire = false;
                    bowPullTime = 0;
                    agent.speed = agentSpeed * 0.3f * characterStats.MoveSlowRate;
                }
            }
        }

        characterStats.sheildHitBackWaitTime -= Time.deltaTime;

        Vector3 dir = (attackTarget == null) ? Vector3.zero
            : attackTarget.transform.position - transform.position;
        Vector3 lookAtPos;
        //举盾状态下，如果攻击目标较近且未死亡，则保持面朝
        if (characterStats.isHoldShield && attackTarget != null
            && dir.magnitude < characterStats.characterData.outSightRadius && !isDead
           && (attackTarget.CompareTag("Attackable") || !attackTarget.GetComponent<EnemyController>().isDead
           || attackTarget.GetComponent<EnemyController>().isTurtleShell))
        {
            agent.updateRotation = false;
            lookAtPos = attackTarget.transform.position;
            //因为agent被关闭，无法自动站直；设定LookAt保持水平
            lookAtPos.y = transform.position.y;
            transform.LookAt(lookAtPos);
        }
        else 
        //持弓状态下，如果攻击目标在longSight内且未死亡，则保持面朝
        if(characterStats.isAim && attackTarget!=null
            && dir.magnitude < characterStats.characterData.longSightRadius && !isDead
            && !attackTarget.CompareTag("Attackable") && !attackTarget.GetComponent<EnemyController>().isDead)
        {
            agent.updateRotation = false;
            lookAtPos = attackTarget.transform.position;
            //因为agent被关闭，无法自动站直；设定LookAt保持水平
            lookAtPos.y = transform.position.y;
            transform.LookAt(lookAtPos);
        }
        else
            agent.updateRotation = true;

        //举盾或持弓状态下，侧移和后移均有不同程度的减速
        if(characterStats.isHoldShield || characterStats.isAim)
        {
            float sideSpeedChange =
                Vector3.Dot(transform.forward, agent.velocity.normalized) * 0.1f;

            agent.speed = agentSpeed * (0.25f + sideSpeedChange) * characterStats.MoveSlowRate;
        }
    }

    internal void SetPlayerOnGround()
    {
        //重置y值，防止玩家出生在夹层里，或者agent开启失败
        if (transform.position.IsOnGround(out upHighDownHit))
        {
            //if (upHighDownHit.point.y < transform.position.y) timeKeepOnGround = 5f;
            if (//timeKeepOnGround > 0 || 
                upHighDownHit.point.y > transform.position.y+0.05f)
                transform.position = new Vector3(transform.position.x,
                    upHighDownHit.point.y, transform.position.z);

            //加上判断，防止进入场景时位置无法读档
            if (agent.isOnNavMesh) agent.enabled = true;
            else
                transform.position = transform.position - transform.position.normalized * 0.1f;
        }
    }

    private void SwitchInsightTargets()
    {
        //进入InsightRadius内的怪物，对怪物标记isInsightedByPlayer
        insightEnemyCols = Physics.OverlapSphere(transform.position + col.center, 
            characterStats.InSightRadius, LayerMask.GetMask("Enemy"));

        EnemyController enemy =null;
        foreach (var col in insightEnemyCols)
        {
            enemy = enemy.GetEnemy(col);

            enemy.isInsightedByPlayer = true;
        }
    }

    public void SwitchLongSightTargets(GameObject newTarget)
    {
        //只对怪物和Boss进行修改
        if (newTarget.tag != "Enemy" || newTarget.tag != "Boss") return;

        //attackTarget变更，对怪物标记isLastAttackTarget
        if (lastTarget != null)
            lastTarget.GetComponent<EnemyController>().isTargetByPlayer = false;

        EnemyController newEnemy = null;
        newEnemy = newEnemy.GetEnemy(newTarget);

        newEnemy.isTargetByPlayer = true;

        lastTarget = newEnemy.gameObject;
    }

    private void SwitchKinematic()
    {
        //用于动态切换刚体的动力学开关，从而实现对石头、树木、Npc的碰撞
        if (isKickingOff ||
            Physics.CheckSphere(transform.position + col.center, col.height * 0.618f,
            LayerMask.GetMask("Tree", "Stone", "Enemy", "Enemy Insight"), QueryTriggerInteraction.Collide)
            || Physics.CheckSphere(transform.position + col.center, col.height * 0.382f,
            LayerMask.GetMask("Npc"), QueryTriggerInteraction.Ignore))
        {
            playerRigidBody.isKinematic = false;
        }
        else
        {
            playerRigidBody.isKinematic = true;
        }
    }

    private void FixedUpdate()
    {
        if (isKickingOff)
        {
            //速度变小，停止击飞判定
            if (agent.velocity.magnitude != 0 && agent.velocity.magnitude < agent.speed)
            {
                isKickingOff = false;
            }

        }
    }

    private void OnTriggerEnter(Collider other)
    {
        string tag = other.gameObject.tag;
        Vector3 dir;
        switch (tag)
        {
            case "Tree":
                //击飞期间，给玩家斥力（模拟物理效果），并造成伤害
                if (!isKickingOff) return;
                isKickingOff = false;

                int dmg = (int)(agent.velocity.magnitude * characterStats.HitTreeDamagePerVel);
                characterStats.TakeDamage(dmg, other.transform.position);
                //Debug.Log("Hit Tree Damage！" + (dmg - characterStats.CurrentDefence));

                //斥力为玩家速度的反弹衰减（近似）
                dir = (transform.position - other.gameObject.transform.position).normalized
                    + agent.velocity.normalized * 0.618f;
                agent.velocity = dir * agent.velocity.magnitude * 0.618f;

                //撞树音效
                AudioManager.Instance.Play3DSoundEffect(SoundName.Player_HitTree,
                    AudioManager.Instance.otherSoundDetailList, transform);
                break;
            case "Bush":
                //穿过灌木会减速！（任何时候，包括击飞）
                agent.velocity *= characterStats.EnterBushSpeedRate;
                break;

            case "Portal":
                //防止穿越传送门后，还往目标点折返
                if(agent.isOnNavMesh)agent.isStopped = true;
                break;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        string tag = other.gameObject.tag;
        Vector3 dir;

        switch (tag)
        {
            case "Portal":
                //被关闭的传送门，没有引力
                //距离太近，也没有引力（防止鬼畜）
                if (other.GetComponent<TransitionPoint>().transitionType
                    == TransitionPoint.TransitionType.Off)
                    return;

                dir = other.gameObject.transform.position - transform.position;

                //玩家在传送门处感受到引力！
                agent.velocity += dir.normalized * (8 - dir.magnitude) * 0.1f;

                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        string tag = collision.gameObject.tag;

        switch (tag)
        {
            case "Attackable":
            case "Big Stone":
                //是否正在被击飞期间，判定撞墙伤害
                if (!isKickingOff) return;
                isKickingOff = false;

                int dmg = (int)(agent.velocity.magnitude * characterStats.HitRockDamagePerVel);
                characterStats.TakeDamage(dmg, collision.transform.position);

                //撞击岩石音效
                AudioManager.Instance.Play3DSoundEffect(SoundName.Player_HitStone,
                    AudioManager.Instance.otherSoundDetailList, transform);
                break;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        string tag = collision.gameObject.tag;

        Vector3 dir;
        Vector3 normalVel, tangentVel, size;
        float dot;
        //让玩家撞上怪物时会减速，而不会很轻松推开任意大小的怪物
        switch (tag)
        {
            case "Enemy":
            case "Boss":
                //非移动状态下不判断，防止鬼畜
                if(Vector3.Distance(agent.destination,transform.position)<=0.15f)
                {
                    if(agent.isOnNavMesh) agent.destination = transform.position;
                    break;
                }

                //法线速度根据敌人体积和质量衰减，切线速度不变
                dir = collision.gameObject.transform.position - transform.position;
                dot = Vector3.Dot(dir.normalized, agent.velocity);
                if (dot <= 0) break;

                normalVel = dir.normalized * dot;
                tangentVel = agent.velocity - normalVel;
                size = collision.collider.bounds.size;
                normalVel *= (0.2f / Mathf.Max(size.x * size.y * size.z, 0.2f));

                agent.velocity = normalVel + tangentVel;

                //标记目标，保证玩家能够攻击到目标
                if (collision.gameObject == attackTarget)
                    isNearAttackTarget = true;
                break;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        string tag = collision.gameObject.tag;
        switch (tag)
        {
            case "Enemy":
            case "Boss":
                if (collision.gameObject == attackTarget)
                    isNearAttackTarget = false;
                break;
        }
    }


    void SheildRushCheck()
    {
        //冲撞伤害，只对攻击目标产生效果
        if (sheildRushKeepTime < 0 || attackTarget == null || isDead) return;

        //若是举盾撞击，需贴近距离；否则为攻击距离
        float delta = characterStats.attackData.hasEquipShield ? 0.3f : characterStats.AttackRange;

        switch (attackTarget.tag)
        {
            case "Enemy":
            case "Boss":
                targetAgent = attackTarget.GetComponent<NavMeshAgent>();
                //必须和目标很接近
                if (Vector3.Distance(transform.position, attackTarget.transform.position)
                    < agent.radius + targetAgent.radius * attackTarget.transform.localScale.x + delta
                    && transform.CatchedTarget(attackTarget.transform.position))
                {
                    sheildRushKeepTime = -1; //只会触发一次
                    //如果没有持盾，则触发RushAttack动画！
                    if(!characterStats.attackData.hasEquipShield)
                    {
                        animator.SetTrigger("RushAttack");
                        //攻击CD
                        lastAttackTime = characterStats.AttackCoolDown;

                        //后面不执行
                        break;
                    }

                    EnemyController ec = attackTarget.GetComponent<EnemyController>();
                    Vector3 force;
                    if (ec.isTurtleShell && ec.isDead)
                    {
                        //冲撞乌龟壳
                        //计算暴击（盾冲的暴击率更高）
                        characterStats.isCritical =
                            UnityEngine.Random.value < characterStats.CriticalChance * 1.5f;
                        //反击造成的力，考虑暴击！
                        force = transform.forward *
                            (characterStats.ShieldRushHitForce + UnityEngine.Random.Range(0, agentSpeed));

                        if (characterStats.isCritical) 
                            force *= characterStats.ShieldCritMult;

                        ec.ResetTurtleDestroy();

                        //关闭动力学，从而模拟物理效果（重力、旋转）
                        Rigidbody rb = attackTarget.GetComponent<Rigidbody>();
                        rb.isKinematic = false;
                        //关闭角速度限制
                        rb.maxAngularVelocity = 999f;
                        //和Hit()的区别：没有施加旋转力矩

                        //用agent进行加速，乌龟壳漂移更顺滑~
                        targetAgent.velocity += force;

                        //盾牌冲撞音效
                        ShieldRushHitSound();

                        //后面不执行
                        break;
                    }

                    ///冲撞造成盾牌的攻击伤害
                    int dmg = characterStats.ShieldDamage;
                    //计算暴击（盾冲的暴击率更高）
                    characterStats.isCritical =
                        UnityEngine.Random.value < characterStats.ShieldCritChan;
                    if (characterStats.isCritical)
                        dmg = (int)(dmg * characterStats.ShieldCritMult);

                    var targetStats = attackTarget.GetComponent<CharacterStats>();
                    targetStats.TakeDamage(dmg, transform.position, characterStats);

                    //扣除盾牌耐久，传递攻击方向
                    InventoryManager.Instance.ReduceSheildDurability(dmg,
                        targetStats.transform.position - transform.position);

                    //怪物被盾牌击退，固定1.5倍
                    force = transform.forward * characterStats.ShieldRushHitForce;
                    if (characterStats.isCritical) force *= 1.5f;
                    targetAgent.velocity += force;

                    //盾牌冲撞触发暴击，可造成怪物眩晕
                    if (attackTarget.tag == "Enemy")
                    {
                        if (characterStats.isCritical)
                            attackTarget.GetComponent<Animator>().SetTrigger("Dizzy");
                        else
                            attackTarget.GetComponent<Animator>().SetTrigger("Hit");
                    }
                    else if (attackTarget.tag == "Boss")
                    {
                        ///对Boss只有普通受击
                        if (characterStats.isCritical)
                            attackTarget.GetComponent<Animator>().SetTrigger("Hit");
                    }

                    //盾牌冲撞音效
                    ShieldRushHitSound();
                }
                break;
            case "Attackable": //冲撞石头
                Vector3 size = attackTarget.GetComponent<MeshCollider>().bounds.size;

                if (Vector3.Distance(transform.position, attackTarget.transform.position)
                    < agent.radius + attackTarget.transform.localScale.x * 
                    Mathf.Max(size.x, size.y, size.z) + delta
                    && transform.CatchedTarget(attackTarget.transform.position))
                {
                    sheildRushKeepTime = -1; //只会触发一次

                    //如果没有持盾，则触发RushAttack动画！
                    if (!characterStats.attackData.hasEquipShield)
                    {
                        animator.SetTrigger("RushAttack");
                        //攻击CD
                        lastAttackTime = characterStats.AttackCoolDown;

                        //后面不执行
                        break;
                    }

                    //否则直接调用攻击逻辑（注意音效）
                    enemyHitSound = characterStats.attackData.isMetalShield ?
                        SoundName.Enemy_by_MetalShieldRushHit : SoundName.Enemy_by_WoodShieldRushHit;
                    Hit(); 
                }
                break;
        }

        sheildRushKeepTime -= Time.deltaTime;
    }

    private void ShieldRushHitSound()
    {
        SoundName soundName = characterStats.attackData.isMetalShield ?
            SoundName.Enemy_by_MetalShieldRushHit : SoundName.Enemy_by_WoodShieldRushHit;
        AudioManager.Instance.Play3DSoundEffect(soundName,
        attackTarget.GetComponent<EnemyController>().soundDetailList, transform);
    }

    private void SwitchAnimation()
    {
        //双手武器，移动结束缓慢收尾
        float speed;
        if (characterStats.attackData.isTwoHand)
        {
            if(animator.GetFloat("Speed") < 1)
                speed = Mathf.Lerp(animator.GetFloat("Speed"), agent.velocity.sqrMagnitude, Time.deltaTime);
            else
                speed = Mathf.Lerp(animator.GetFloat("Speed"), agent.velocity.sqrMagnitude, 0.2f);
        }
        else
            speed = agent.velocity.sqrMagnitude;

        animator.SetFloat("Speed", speed);
        animator.SetBool("Death", isDead);

        if (!characterStats.attackData.isBow)
            animator.SetBool("IsHoldSheild", characterStats.isHoldShield);
        else
        {
            //角色和弓，切换瞄准动画
            animator.SetBool("IsAim", characterStats.isAim);
            characterStats.bowSlot.GetChild(0).GetComponent<Animator>().
                SetBool("IsAim", characterStats.isAim);

            //通过bowPullRate控制动画拉弓的程度
            if (characterStats.isAim)
            {
                //只有瞄准蓄力的时候才更新，防止射击时rate瞬间归零
                bowPullRate = bowPullTime / characterStats.MaxPullBowTime;
            }
            animator.SetFloat("BowPullRate", bowPullRate);
            characterStats.bowSlot.GetChild(0).GetComponent<Animator>().
                SetFloat("BowPullRate", bowPullRate);
        }

        //持盾、拉弓时的移动动画
        if (characterStats.isHoldShield || characterStats.isAim)
        {
            float holdSheildSpeed = agent.velocity.sqrMagnitude;
            //如果有攻击目标，则始终面朝；因而可能倒退
            if (attackTarget != null &&
                Vector3.Dot(attackTarget.transform.position - transform.position, agent.velocity) < 0)
                holdSheildSpeed = -holdSheildSpeed;

            animator.SetFloat("HoldSheild Speed", holdSheildSpeed);
        }
    }

    //Animation Event
    void HoldBowBackDodge()
    {
        //闪避涉水的衰减
        float waterSlow = Mathf.Lerp(1f, 0.5f, AudioManager.Instance.footStepWaterDeep - transform.position.y);
        //持弓朝后闪避
        agent.velocity = (transform.position - attackTarget.transform.position).normalized
            * characterStats.MoveDodgeVel * characterStats.MoveSlowRate * 1.2f * waterSlow;

        PlayRollDodgeSound();
    }


    //Animation Event
    void HoldBowForwadDodge()
    {
        //闪避涉水的衰减
        float waterSlow = Mathf.Lerp(1f, 0.5f, AudioManager.Instance.footStepWaterDeep - transform.position.y);
        //持弓朝前闪避
        agent.velocity = transform.forward * characterStats.MoveDodgeVel
            * characterStats.MoveSlowRate * 0.8f * waterSlow;

        PlayRollDodgeSound();
    }
    //Animation Event
    void BackDodgeEnd()
    {
        //结束向后闪避，恢复agent角速度
        backDodging = false;
        agent.angularSpeed = agentAngularSpeed;
    }

    public void MoveToTarget(Vector3 target)
    {
        StopAllCoroutines();
        //这里注意，恢复旋转速度
        if (!backDodging)
            agent.angularSpeed = agentAngularSpeed;

        //防止寻路卡顿，防止死了还能移动
        if (agent.pathPending || !agent.isOnNavMesh || isDead) return;

        agent.stoppingDistance = stopDistance;
        if(agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.destination = DestinationInNavMesh(target);
        }
        //重置滑行攻击
        attackNear = 1f;
    }

    private void EventAttack(GameObject target)
    {
        if (isDead || lastAttackTime > 0) return; //防止死了还能移动，防止鬼畜

        if (target != null)
        {
            //针对boss等有跳跃动画或运动幅度较大的单位
            //Collider绑定在子物体骨骼上，真正的target在parent层级
            if (!target.CompareTag("Attackable")) target = target.GetEnemy();

            attackTarget = target;
            isNearAttackTarget = false;

            characterStats.isCritical =
                UnityEngine.Random.value < characterStats.CriticalChance;
            //恢复旋转速度
            agent.angularSpeed = agentAngularSpeed;

            //防止近距离举盾/拉弓，会多攻击一下
            if (Input.GetMouseButtonDown(1))
            {
                if (characterStats.attackData.isBow)
                    characterStats.isAim = true;
                else
                    characterStats.isHoldShield = true;
            }

            //关闭之前的协程！
            StopCoroutine(MoveToAttackTarget());
            StartCoroutine(MoveToAttackTarget());
        }
    }

    IEnumerator MoveToAttackTarget()
    {
        if (agent.isOnNavMesh) agent.isStopped = false;

        float startDis = Vector3.Distance(attackTarget.transform.position, transform.position);
        //根据起始距离，决定攻击滑行位置
        if (attackNear == 1f)
        {
            attackNear = startDis > characterStats.AttackRange * 2 ?
                0.382f : (startDis > characterStats.AttackRange ? 0.618f : 1f);
        }


        float distLimit = 0;
        if (attackTarget.CompareTag("Enemy")|| attackTarget.CompareTag("Boss"))
        {
            //防止玩家和怪物agent半径太大，导致玩家绕着怪物滑动不攻击
            targetAgent = attackTarget.GetComponent<NavMeshAgent>();
            distLimit = targetAgent.radius * attackTarget.transform.localScale.x +
                agent.radius * transform.localScale.x + 0.05f;

            agent.stoppingDistance = characterStats.AttackRange * attackNear;
            //起始攻击范围
            startAtkDistance = Mathf.Max(distLimit + characterStats.AttackRange,
                characterStats.AttackRange * (2f - attackNear));

        }
        else
        {
            //针对Attackable等物体，计算bound的最大宽度
            Vector3 size = attackTarget.GetComponent<Collider>().bounds.size;
            Vector3 scale = attackTarget.GetComponent<Transform>().localScale;
            float targetWidth = Mathf.Max(size.x * scale.x, size.y * scale.y, size.z * scale.z);
            
            agent.stoppingDistance = stopDistance;
            startAtkDistance = Mathf.Max(characterStats.AttackRange, targetWidth);
        }

        //根据武器修改攻击范围参数
        //滑行攻击起跳
        while (attackTarget != null && Vector3.Distance(attackTarget.transform.position, transform.position)
            > startAtkDistance && !isNearAttackTarget)
        {
            if (!agent.pathPending && agent.isOnNavMesh)
                agent.destination = DestinationInNavMesh(attackTarget.transform.position);
            yield return null;
        }

        //如果是举盾/拉弓，这里直接结束
        if (characterStats.isHoldShield || characterStats.isAim)
        {
            if(agent.isOnNavMesh) agent.isStopped = true;
            attackNear = 1f;
            yield break;
        }

        //Attack
        if (lastAttackTime < 0)
        {
            //角色看向目标
            if (attackTarget != null)
                transform.LookAt(attackTarget.transform);

            animator.SetBool("Critical", characterStats.isCritical);
            animator.SetTrigger("Attack");

            //Debug.Log("isNearAttackTarget:" + isNearAttackTarget +
            //    "  Distance:" + Vector3.Distance(attackTarget.transform.position, transform.position));

            //重置冷却时间，如果暴击，CD更久
            if (characterStats.isCritical)
                lastAttackTime = characterStats.AttackCoolDown * UnityEngine.Random.Range(1.372f, 1.618f);
            else
                lastAttackTime = characterStats.AttackCoolDown;
        }

        //攻击后滑行
        while (attackTarget != null && Vector3.Distance(attackTarget.transform.position, transform.position)
            > distLimit && !isNearAttackTarget)
        {
            if (!agent.pathPending && agent.isOnNavMesh)
                agent.destination = DestinationInNavMesh(attackTarget.transform.position);
            yield return null;
        }

        if(agent.isOnNavMesh)agent.isStopped = true;
        attackNear = 1f;
    }

    /// <summary>
    /// 玩家攻击事件
    /// Animation Event
    /// Float：传递dotThreshold，攻击范围一半的cos值，0表示两侧各90度
    /// Int：传递middleAngle，判定中心线与玩家forward之间的角度，顺时针360度
    /// </summary>
    /// <param name="animationEvent"></param>
    void Hit(AnimationEvent animationEvent = null)
    {
        //目标可能被销毁
        if (attackTarget == null) return;

        if (attackTarget.CompareTag("Attackable"))
        {
            //反击石头！
            if (attackTarget.GetComponent<Rock>())
            {
                Rock rock = attackTarget.GetComponent<Rock>();
                //只能反击HitNothing状态
                if (rock.rockState != Rock.RockState.HitNothing) return;
                rock.rockState = Rock.RockState.HitEnemy;
                //传递攻击者
                rock.attacker = characterStats;


                //停止销毁石头
                rock.StopCoroutine("LaterDestroy");
                rock.isDestroying = false;

                Rigidbody rb = attackTarget.GetComponent<Rigidbody>();
                //防止速度0触发HitNothing
                rb.velocity = Vector3.one;

                //反击造成的力，采用Impulse考虑石头质量，考虑暴击！
                Vector3 force = transform.forward * UnityEngine.Random.Range(20f, 30f);
                    //+ transform.up * UnityEngine.Random.Range(2f, 3f);
                if (characterStats.isCritical)
                {
                    force *= characterStats.CriticalMultiplier;
                }

                rb.AddForce(force, ForceMode.Impulse);

                //音效
                rock.PlayHitSound(enemyHitSound);
            }
        }
        else
        {
            EnemyController ec = attackTarget.GetComponent<EnemyController>();
            if (ec.isTurtleShell && ec.isDead)
            {
                //反击造成的力，考虑暴击！
                Vector3 force = transform.forward * UnityEngine.Random.Range(15f, 20f);
                if (characterStats.isCritical)
                {
                    //Debug.Log("TurtleShell Critical！");
                    force *= characterStats.CriticalMultiplier;
                }
                ec.ResetTurtleDestroy();

                //关闭动力学，从而模拟物理效果（重力、旋转）
                Rigidbody rb = attackTarget.GetComponent<Rigidbody>();
                rb.isKinematic = false;
                //关闭角速度限制
                rb.maxAngularVelocity = 999f;
                //让乌龟壳旋转起来！
                rb.AddTorque(Vector3.down * force.magnitude * ec.turtleShellTorqueRate, ForceMode.VelocityChange);
                //Debug.Log("AddTorque！" + Mathf.Min(rb.maxAngularVelocity, force.magnitude * ec.turtleShellTorqueRate));

                //用agent进行加速，乌龟壳漂移更顺滑~
                attackTarget.GetComponent<NavMeshAgent>().velocity += force;

                attackTarget.GetComponent<EnemyController>().PlayHitSound(enemyHitSound);
            }
            else
            {
                //简化玩家攻击，不考虑攻击距离，但考虑攻击角度
                if(animationEvent == null)
                {
                    if (!transform.CatchedTarget(attackTarget.transform.position, 0)) return;
                }
                else
                {
                    if (!transform.CatchedTarget(animationEvent.intParameter,
                        attackTarget.transform.position, animationEvent.floatParameter)) return;
                }


                CharacterStats targetStats = attackTarget.GetComponent<CharacterStats>();
                int dmg = targetStats.TakeDamage(characterStats, targetStats);
                //鲜血剑
                if (characterStats.WeaponSkillData != null)
                    characterStats.WeaponSkillData.WeaponSkillAfterAttack();
                //音效
                targetStats.GetComponent<EnemyController>().PlayHitSound(enemyHitSound);

                //双手武器可以造成2/3溅射伤害
                if(characterStats.attackData.isTwoHand)
                {
                    dmg = (int)(dmg * 0.666f);

                    slashEnemyCols = Physics.OverlapSphere(transform.position, characterStats.AttackRange,
                        LayerMask.GetMask("Enemy", "Enemy Insight"));
                    foreach (var col in slashEnemyCols)
                    {
                        targetStats = targetStats.GetEnemy(col);

                        //跳过主要目标
                        if (targetStats.gameObject == attackTarget) continue;

                        targetStats.TakeDamage(dmg, transform.position, characterStats);
                        //音效
                        targetStats.GetComponent<EnemyController>().PlayHitSound(enemyHitSound);

                        //是否击晕
                        if (characterStats.isCritical && UnityEngine.Random.value < targetStats.HitByCritRate)
                            targetStats.GetComponent<Animator>().SetTrigger("Hit");

                        //攻击击退效果
                        if (characterStats.AttackHitForce > 0)
                        {
                            targetStats.GetComponent<NavMeshAgent>().velocity +=
                                (characterStats.isCritical ? 1.5f : 1f) * characterStats.AttackHitForce
                                * (targetStats.transform.position - transform.position).normalized;
                        }
                    }
                }

                //持失暴击，有击退效果
                if(characterStats.arrowItem?.itemData!=null && !characterStats.isAim && characterStats.isCritical)
                {
                    attackTarget.GetComponent<NavMeshAgent>().velocity += 0.27f * 
                        characterStats.arrowItem.itemData.arrowMaxPushForce * transform.forward;

                    //箭矢损坏几率为射击时的1/3
                    if(UnityEngine.Random.value < characterStats.arrowItem.itemData.arrowBrokenRate *0.3f)
                        InventoryManager.Instance.BrokenEquipArrow(dmg, transform.forward);
                }
            }
        }

        //攻击之后停止跟随目标！
        if(agent.isOnNavMesh) agent.destination = transform.position;
    }

    public void StopAngularSpeedShortTime(float stopTime = 0.5f)
    {
        StartCoroutine("SuspendAngular", stopTime);
    }

    IEnumerator SuspendAngular(float suspendTime)
    {
        agent.angularSpeed = 0;
        yield return new WaitForSeconds(suspendTime);

        agent.angularSpeed = agentAngularSpeed;
    }

    //修正点击的位置为NavMesh范围内，保证玩家点击远处时能够移动
    Vector3 DestinationInNavMesh(Vector3 target)
    {
        Vector3 dir = target - transform.position;

        //如果超出NavMesh，则修正目标点位置
        if (dir.magnitude > playerNavMeshMoveSize)
            dir = dir.normalized * playerNavMeshMoveSize;

        return transform.position + dir;
    }

    #region PlaySound
    /// <summary>
    /// Animation Event：玩家脚步声
    /// 0:Walk,  1:Run
    /// </summary>
    void FootStepSound(int walkOrRun)
    {
        //地面高度 - 1 = 水面淹没高度
        if (transform.position.y> AudioManager.Instance.footStepWaterDeep)
        {
            AudioManager.Instance.Play3DSoundEffect(
                (walkOrRun == 0) ? SoundName.FootStep_Walk_Grass : SoundName.FootStep_Run_Grass,
                AudioManager.Instance.playerFootStepSoundDetailList, transform);
        }
        else if(transform.position.y< AudioManager.Instance.footStepWaterDeep- 1f)
        {
            AudioManager.Instance.Play3DSoundEffect(
                (walkOrRun == 0) ? SoundName.FootStep_Walk_Water : SoundName.FootStep_Run_Water,
                AudioManager.Instance.playerFootStepSoundDetailList, transform);
        }
        else
        {
            float bias1 = AudioManager.Instance.footStepWaterDeep - transform.position.y;
            float bias2 = transform.position.y - AudioManager.Instance.footStepWaterDeep + 1f;
            //脚步声
            AudioManager.Instance.Play3DSoundEffect(
                (walkOrRun == 0) ? SoundName.FootStep_Walk_Grass : SoundName.FootStep_Run_Grass,
                AudioManager.Instance.playerFootStepSoundDetailList, transform, -bias1 * 0.5f, -bias1);
            //涉水声
            AudioManager.Instance.Play3DSoundEffect(
                (walkOrRun == 0) ? SoundName.FootStep_Walk_Water : SoundName.FootStep_Run_Water,
                AudioManager.Instance.playerFootStepSoundDetailList, transform, -bias2 * 0.5f, -bias2);
        }
    }

    void PlayrShieldDodgeSound()
    {
        AudioManager.Instance.Play3DSoundEffect(SoundName.SheildDodge,
            AudioManager.Instance.playerFootStepSoundDetailList, transform);
    }
    public void PlayDodgeSound(bool autoDodge = false)
    {
        if (transform.position.y > AudioManager.Instance.footStepWaterDeep)
            AudioManager.Instance.Play3DSoundEffect(SoundName.Dodge,
                AudioManager.Instance.playerFootStepSoundDetailList, transform, autoDodge ? -0.2f : 0f);
        else if (transform.position.y < AudioManager.Instance.footStepWaterDeep-1f)
        {
            AudioManager.Instance.Play3DSoundEffect(SoundName.Dodge,
                AudioManager.Instance.playerFootStepSoundDetailList, transform, 
                -0.3f + (autoDodge ? -0.2f : 0f), -0.5f);
            AudioManager.Instance.Play3DSoundEffect(SoundName.FootStep_Run_Water,
                AudioManager.Instance.playerFootStepSoundDetailList, transform,
                -0.3f + (autoDodge ? -0.2f : 0f), 0.5f);
        }
        else
        {
            float bias1 = AudioManager.Instance.footStepWaterDeep - transform.position.y;
            float bias2 = transform.position.y - AudioManager.Instance.footStepWaterDeep + 1f;
            //闪避声
            AudioManager.Instance.Play3DSoundEffect(SoundName.Dodge,
                AudioManager.Instance.playerFootStepSoundDetailList, transform,
                    -bias1 * 0.3f + (autoDodge ? -0.2f : 0f), -bias1 * 0.5f);
            //涉水声
            AudioManager.Instance.Play3DSoundEffect(SoundName.FootStep_Run_Water,
                AudioManager.Instance.playerFootStepSoundDetailList, transform,
                -bias2 * 0.3f + (autoDodge ? -0.2f : 0f), 0.5f - bias2);
        }
    }

    void PlayRollDodgeSound()
    {
        AudioManager.Instance.Play3DSoundEffect(SoundName.RollDodge,
            AudioManager.Instance.playerFootStepSoundDetailList, transform);
    }

    //Animation Event
    void PlayrRollSound()
    {
        AudioManager.Instance.Play3DSoundEffect(SoundName.Roll,
            AudioManager.Instance.playerFootStepSoundDetailList, transform);
    }

    /// <summary>
    /// Animation Event
    /// 武器类型：Unarm, Sword, Axe, TwoHandSword, TwoHandAxe, TwoHandMace, Bow, Arrow
    /// </summary>
    /// <param name="weaponTye"></param>
    void PlayWeaponSound(string soundString)
    {
        weaponWaveSound = SoundName.None;

        switch (soundString)
        {
            case "Unarm":
                if(characterStats.arrowItem?.itemData != null && !characterStats.isAim && characterStats.isCritical)
                {
                    //持失暴击
                    if (characterStats.isCritical)
                    {
                        weaponWaveSound = SoundName.Sword_Critical;
                        enemyHitSound = SoundName.Enemy_by_SwordCrit;
                    }
                    else
                    {
                        weaponWaveSound = SoundName.Sword_Attack;
                        enemyHitSound = SoundName.Enemy_by_Sword;
                    }
                }
                else
                if (characterStats.isCritical)
                {
                    weaponWaveSound = SoundName.Unarm_Critical;
                    enemyHitSound = SoundName.Enemy_by_UnarmCrit;
                }
                else
                {
                    weaponWaveSound = SoundName.Unarm_Attack;
                    enemyHitSound = SoundName.Enemy_by_Unarm;
                }
                break;
            case "Sword":
                if (characterStats.attackData.isAxe)
                {
                    if (characterStats.isCritical)
                    {
                        weaponWaveSound = SoundName.Axe_Critical;
                        enemyHitSound = SoundName.Enemy_by_AxeCrit;
                    }
                    else
                    {
                        weaponWaveSound = SoundName.Axe_Attack;
                        enemyHitSound = SoundName.Enemy_by_Axe;
                    }
                }
                else if(characterStats.attackData.hasEquipWeapon)
                {
                    if (characterStats.isCritical)
                    {
                        weaponWaveSound = SoundName.Sword_Critical;
                        enemyHitSound = SoundName.Enemy_by_SwordCrit;
                    }
                    else
                    {
                        weaponWaveSound = SoundName.Sword_Attack;
                        enemyHitSound = SoundName.Enemy_by_Sword;
                    }
                }
                else
                {
                    if (characterStats.isCritical)
                    {
                        weaponWaveSound = SoundName.Unarm_Critical;
                        enemyHitSound = SoundName.Enemy_by_UnarmCrit;
                    }
                    else
                    {
                        weaponWaveSound = SoundName.Unarm_Attack;
                        enemyHitSound = SoundName.Enemy_by_Unarm;
                    }
                }
                break;
            case "TwoHandSword":
                if (characterStats.isCritical)
                {
                    weaponWaveSound = SoundName.TwoHandSword_Critical;
                    enemyHitSound = SoundName.Enemy_by_TwoHandSwordCrit;
                }
                else
                {
                    weaponWaveSound = SoundName.TwoHandSword_Attack;
                    enemyHitSound = SoundName.Enemy_by_TwoHandSword;
                }
                break;
            case "TwoHandSword_Rush":
                weaponWaveSound = SoundName.TwoHandSword_Rush;
                enemyHitSound = characterStats.isCritical ? 
                    SoundName.Enemy_by_TwoHandSwordCrit : SoundName.Enemy_by_TwoHandSword;
                break;
            case "TwoHandSword_HitBack":
                weaponWaveSound = SoundName.TwoHandSword_HitBack;
                enemyHitSound = characterStats.isCritical ? 
                    SoundName.Enemy_by_TwoHandSwordCrit : SoundName.Enemy_by_TwoHandSword;
                break;
            case "TwoHandAxe":
                if (characterStats.isCritical)
                {
                    weaponWaveSound = SoundName.TwoHandAxe_Critical;
                    enemyHitSound = SoundName.Enemy_by_TwoHandAxeCrit;
                }
                else
                {
                    weaponWaveSound = SoundName.TwoHandAxe_Attack;
                    enemyHitSound = SoundName.Enemy_by_TwoHandAxe;
                }
                break;
            case "TwoHandAxe_Rush":
                weaponWaveSound = SoundName.TwoHandAxe_Rush;
                enemyHitSound = characterStats.isCritical ?
                    SoundName.Enemy_by_TwoHandAxeCrit : SoundName.Enemy_by_TwoHandAxe;
                break;
            case "TwoHandAxe_HitBack":
                weaponWaveSound = SoundName.TwoHandAxe_HitBack;
                enemyHitSound = characterStats.isCritical ?
                    SoundName.Enemy_by_TwoHandAxeCrit : SoundName.Enemy_by_TwoHandAxe;
                break;
            case "TwoHandMace":
                if (characterStats.isCritical)
                {
                    weaponWaveSound = SoundName.TwoHandMace_Critical;
                    enemyHitSound = SoundName.Enemy_by_TwoHandMaceCrit;
                }
                else
                {
                    weaponWaveSound = SoundName.TwoHandMace_Attack;
                    enemyHitSound = SoundName.Enemy_by_TwoHandMace;
                }break;
            case "TwoHandMace_Rush":
                weaponWaveSound = SoundName.TwoHandMace_Rush;
                enemyHitSound = characterStats.isCritical ?
                    SoundName.Enemy_by_TwoHandMaceCrit : SoundName.Enemy_by_TwoHandMace;
                break;
            case "TwoHandMace_HitBack":
                weaponWaveSound = SoundName.TwoHandMace_HitBack;
                enemyHitSound = characterStats.isCritical ?
                    SoundName.Enemy_by_TwoHandMaceCrit : SoundName.Enemy_by_TwoHandMace;
                break;
        }

        Transform weaponPos = characterStats.weaponSlot.childCount == 2 ?
            characterStats.weaponSlot.GetChild(1) : characterStats.weaponSlot;

        if (weaponWaveSound != SoundName.None)
        {
            currentWeaponSound =
            AudioManager.Instance.Play3DSoundEffect(weaponWaveSound,
                AudioManager.Instance.weaponSoundDetailList, weaponPos, characterStats.WeaponPitchBias);
        }
    }
    //Animation Event
    void ShieldHitBackSound()
    {
        AudioManager.Instance.Play3DSoundEffect(SoundName.Shield_HitBack,
            AudioManager.Instance.weaponSoundDetailList, characterStats.sheildSlot, characterStats.ShieldPitchBias);
    }

    //Animation Agent调用
    public void PlayBowPullSound()
    {
        currentWeaponSound = 
        AudioManager.Instance.Play3DSoundEffect(SoundName.Bow_Pull,
            AudioManager.Instance.weaponSoundDetailList, characterStats.bowSlot, characterStats.WeaponPitchBias);
    }

    void CheckSeaAmbientMusic()
    {
        //判断与世界0点（岛屿中心）的距离，和高度，判断海浪音量大小，并通知调整音量
        Vector3 horiDir = transform.position;
        float heightToSeaPlane = horiDir.y + 12;
        horiDir.y = 0;
        AudioManager.Instance.SeaAmbientVolume(horiDir.magnitude, heightToSeaPlane); ;
    }
    #endregion
}
