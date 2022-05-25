using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//新增逃离状态：被攻击残血时(血量比玩家少)，有几率逃离
//新增绕后状态：每次攻击后，有几率前往超过玩家的位置，期间移动速度翻倍

public enum EnemyBehaviorState { GUARD, PATROL, CHASE, DEAD, ESCAPE, CROSS_PLAYER_POS }
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(CharacterStats))]
public class EnemyController : MonoBehaviour,IGameObserver
{
    [HideInInspector]public EnemyBehaviorState enemyBehaviorState;
    [HideInInspector]public NavMeshAgent agent;
    protected Animator animator;
    private Collider coll;
    protected Rigidbody rigidBody;

    [HideInInspector]public CharacterStats characterStats;

    [Header("Basic Settings")]
    public string enemyName;
    public bool isNightmare;
    public float sightRadius = 6f;
    [Range(20, 80)] public int halfSightAngle = 45;
    public GameObject eyeBall;
    public bool isGuard;
    [HideInInspector]public float agentSpeed; 
    public float lossTargetTime = 1.5f;

    private bool findAttacker;
    private GameObject attackTarget;
    private bool noFirstLostTarget;
    public float lookAtMinTime = 1.5f;
    public float guardKeepMinTime = 4f;
    [Tooltip("怪物平滑看向玩家的速度比率，默认为1")]
    public float lerpRotationRate = 1;
    private float remainLookAtTime;
    protected float lastAttackTime;
    [HideInInspector]public bool slowAttackRefresh;
    protected float lastSkillTime;
    [Tooltip("是否为乌龟，死亡后变为可击飞对象，保留在场景中一段时间")]
    public bool isTurtleShell;
    public float turtleShellTorqueRate = 5.55f;

    [Header("Patrol State")]
    public float patrolRange = 4f;
    [Tooltip("怪物残血时逃跑的最大速度")]
    public float escapeSpeed;
    [HideInInspector] public float remainChaingTime = 60f;
    [Header("SoundDetail")]
    public SoundDetailList_SO soundDetailList;
    public float maxNoiseTime = 300f;

    protected float stopDistance;
    private Vector3 wayPoint;
    [HideInInspector]public Vector3 guardPos;
    [HideInInspector]public Quaternion guardRotation;
    private Quaternion lookAtRotation;
    private bool noFirstRandomLookAt;

    private float lastPatrolTime; //防止Patrol到达不了目标，从而而一直卡墙的情况

    bool isFoundPlayer;
    bool isWalk;
    bool isChase;
    protected bool isFollow;
    [HideInInspector]public bool isDead;
    bool playerDead;
    [HideInInspector]public bool isTurtleShellDestroying;
    private float TurtleDestroyWaitTime = 20f;

    [HideInInspector] public bool isInsightedByPlayer;
    [HideInInspector] public bool isTargetByPlayer;

    float idleBlend;
    private bool isLosingInsight; //正在丢失透视（协程）

    [HideInInspector] public int numInArray;
    [HideInInspector] public bool isSpawning;
    [HideInInspector] public bool isDestroying;

    private float agentRadius;

    private int stateFrames = 15;
    [HideInInspector] internal float lastStateFrames;
    protected float lerpLookAtTime;

    [HideInInspector] public List<GameObject> attachObjects;

    SoundName[] noises = { SoundName.Enemy_Noise1, SoundName.Enemy_Noise2, SoundName.Enemy_Noise3 };
    float noisePitchBias;
    float lastNoiseTime;
    float currentNoiseTime;

    float NextNoiseTime { get { return Random.Range(0, maxNoiseTime); } }

    Vector3 escapeDirection;
    float escapeDirChangeRate;
    [HideInInspector]public float remainEscapeTime;
    bool isEscaping;

    bool isCrossingPlayerPos;
    NavMeshHit navMeshHit;

    RaycastHit upHighDownHit;
    private Collider[] playerCols;

    public GameObject AttackTarget
    {
        get { return attackTarget; }
        set { attackTarget = value; findAttacker = true; }
    }

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        characterStats = GetComponent<CharacterStats>();
        coll = GetComponent<Collider>();
        if (coll == null) coll = GetComponentInChildren<Collider>();
        rigidBody = GetComponent<Rigidbody>();

        agentSpeed = agent.speed;
        guardPos = transform.position;
        guardRotation = transform.rotation;
        lookAtRotation = guardRotation;
        remainLookAtTime = lookAtMinTime;
        stopDistance = agent.stoppingDistance;
        agentRadius = agent.radius;

        //把怪物AI错开，不在同一帧运行
        lastStateFrames = Random.Range(0, stateFrames);

        //怪物叫声区分
        noisePitchBias = Random.Range(-0.2f, 0.2f);
        lastNoiseTime = NextNoiseTime;

        escapeSpeed = Mathf.Max(escapeSpeed, agentSpeed * 1.3f);
    }

    protected virtual void Start()
    {
        if(isGuard)
        {
            enemyBehaviorState = EnemyBehaviorState.GUARD;
        }
        else
        {
            enemyBehaviorState = EnemyBehaviorState.PATROL;
            GetPatrolWayPoint();
        }
    }

    //切换场景时启用
    private void OnEnable()
    { 
        //为什么怪物没有订阅成功？因为怪物本来就存在，OnEnable比单例的Awake先执行
        //修改方式：可以通过修改默认的代码执行顺序来满足
        //在 Project Settings 中 找到 Script Execute Order 手动添加你的 Manager 类代码，
        //并将它的数值设置为一个负数，例如-20。只要在  Default Time 之上则会优先其他脚本执行。

        //订阅观察者
        GameManager.Instance?.AddObserver(this);

        //获得导航网络的OffMeshLink，并开启agent
        agent.ActivateCurrentOffMeshLink(true);

        //重置乌龟壳
        if (isDead && isTurtleShell)
        {
            TurtleDestroyWaitTime *= 0.618f;
            ResetTurtleDestroy();
        }
    }

    private void OnDisable()
    {
        //防止游戏停止时再次调用OnDisable报错，确认GameManager是否已被提前销毁
        if (!GameManager.IsInitialized) return;

        //移除观察者
        GameManager.Instance?.RemoveObserver(this);


    }

    protected virtual void Update()
    {
        if (characterStats.CurrentHealth == 0 && !isDead)
        {
            isDead = true;

            //移除附加在身上的物体（比如箭矢），以免一同被销毁
            if (attachObjects.Count != 0)
            {
                foreach (var attach in attachObjects)
                {
                    if (attach)
                        attach.transform.SetParent(null);
                }
                attachObjects.Clear();
            }

            //怪物死亡掉落
            if (GetComponent<LootSpawner>())
                GetComponent<LootSpawner>().Spawnloot();

            //玩家击杀奖励
            if (QuestManager.IsInitialized)
                QuestManager.Instance.UpdateQuestProgress(enemyName, 1);

            //如果是梦魇史莱姆：下次进入场景后，重置玩家等级
            if (isNightmare)
                SaveManager.Instance.ResetPlayerLevelInData();
        }

        if (!playerDead)
        {
            if (lastStateFrames < 0)
            {
                //怪物AI不需要每帧执行，改为每隔几帧执行
                SwitchStates();
                lastStateFrames = stateFrames;
            }

            //怪物AI改为间隔执行后，仍需要平滑看向玩家的效果
            if (lerpLookAtTime > 0 && AttackTarget != null)
                LerpLookAt(AttackTarget.transform);

            SwitchAnimation();
            SwitchInsightLayer();
            CheckNoisePlay();

            //是否减缓CD刷新（被Hit和Dizzy的情况）
            if (!slowAttackRefresh)
            {
                lastAttackTime -= Time.deltaTime;
                lastSkillTime -= Time.deltaTime;
            }
            else
            {
                lastAttackTime -= Time.deltaTime * 0.3f;
                lastSkillTime -= Time.deltaTime * 0.5f;
            }
            
            lastPatrolTime -= Time.deltaTime;

            lastStateFrames--;
        }
    }

    private void SwitchInsightLayer()
    {
        GameObject player = GameManager.Instance?.player;
        CharacterStats playerStats = GameManager.Instance?.playerStats;

        if (player == null || playerStats == null) return;

        if (gameObject.layer == LayerMask.NameToLayer("Enemy Insight"))
        {
            //离开OutsightRadius的怪物，若不是玩家的target，关闭透视
            if (!isTargetByPlayer &&
                Vector3.Distance(transform.position, player.transform.position) > playerStats.OutSightRadius)
            {
                //丢失透视用延时
                if (!isLosingInsight)
                {
                    isLosingInsight = true;
                    StartCoroutine("LoseInsightLater");
                }
            }
        }
        else
        {
            //进入InsightRadius内的怪物，开启透视
            //玩家当前的attackTarget，LongSightRadius内开启
            //target是玩家的怪物，OutSightRadius内开启
            if (isInsightedByPlayer
                || (isTargetByPlayer &&
                Vector3.Distance(transform.position, player.transform.position) <= playerStats.LongSightRadius)
                || (attackTarget == player &&
                Vector3.Distance(transform.position, player.transform.position) <= playerStats.OutSightRadius))

            {
                //开启透视需要先关闭协程，待测试
                StopCoroutine("LoseInsightLater");
                isLosingInsight = false;

                //立即获得该怪物的透视
                gameObject.layer = LayerMask.NameToLayer("Enemy Insight");
                Transform[] childs = gameObject.transform.GetComponentsInChildren<Transform>();

                //对子物体也更新
                foreach (Transform trans in childs)
                    trans.gameObject.layer = LayerMask.NameToLayer("Enemy Insight");

                isInsightedByPlayer = true;
            }
        }
    }

    IEnumerator  LoseInsightLater()
    {
        yield return new WaitForSeconds(GameManager.Instance.playerStats.LossInsightTime);

        //一段时间后，丢失该怪物的透视
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        Transform[] childs = gameObject.transform.GetComponentsInChildren<Transform>();

        //对子物体也更新
        foreach(Transform trans in childs)
            trans.gameObject.layer = LayerMask.NameToLayer("Enemy");

        isInsightedByPlayer = false;

    }

    private void OnTriggerEnter(Collider other)
    {
        string tag = other.gameObject.tag;
        switch (tag)
        {
            case "Bush":
                //穿过灌木会减速！（任何时候）
                agent.velocity *= characterStats.EnterBushSpeedRate;
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        //乌龟壳单独处理
        if(isTurtleShell &&isDead)
        {
            DeadShellOnCollisionEnter(collision);
            return;
        }

        string tag = collision.gameObject.tag;
        switch (tag)
        {
            case "Player":
                if (!collision.gameObject.GetComponent<PlayerController>().isKickingOff) return;
                //怪物遇到被击飞玩家，低伤害，有眩晕（无须确认玩家是否被击飞，因为玩家isKinematic关闭才会触发）

                Vector3 tarPos = collision.gameObject.GetComponent<Transform>().position;
                //计算伤害要用NavMeshAgent的速度！
                Vector3 tarVel = collision.gameObject.GetComponent<NavMeshAgent>().velocity;
                //若玩家朝远离方向，则不会触发
                if (Vector3.Dot(tarVel, tarPos - transform.position) > 0.618f) break;

                //怪物朝随机方向被击退（被玩家）
                Vector3 addVel = (transform.position - tarPos).normalized * Random.Range(0f, 0.618f) +
                    tarVel.normalized * Random.Range(0.382f, 0.618f);
                addVel *= Mathf.Clamp(tarVel.magnitude, 0, 15f);

                agent.velocity += addVel;

                if (isDead) return; //怪物死亡，则只受到击退

                //根据addVel受到伤害
                int damage = (int)(addVel.magnitude * characterStats.HitByPlayerDamagePerVel);
                //攻击大于防御，才会有Hit效果
                if(damage> characterStats.CurrentDefence)
                    animator.SetTrigger("Hit");
                characterStats.TakeDamage(damage, collision.transform.position);

                //玩家也会受到伤害
                damage = (int)(addVel.magnitude * characterStats.PlayerHurtDamagePerVel);
                CharacterStats playerStats = collision.gameObject.GetComponent<CharacterStats>();
                playerStats.TakeDamage(damage, transform.position);

                //撞击音效
                AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_by_Unarm, soundDetailList, transform, -0.15f);
                break;
        }
    }

    private void DeadShellOnCollisionEnter(Collision collision)
    {
        EnemyController enemy = null;
        enemy = enemy.GetEnemy(collision.gameObject);

        GameObject target = (enemy != null) ? enemy.gameObject : collision.gameObject;

        Vector3 tarPos = target.GetComponent<Transform>().position;
        int damage;

        //对真正的target进行判断
        string tag = target.tag;
        switch (tag)
        {
            //乌龟壳撞上怪物、Boss或玩家，朝随机方向击退
            case "Enemy":
            case "Boss":
            case "Player":
                //若朝乌龟壳远离方向，则不会触发（用agent的速度，更及时一些）
                if (Vector3.Dot(agent.velocity, transform.position - tarPos) > 0.618f) break;

                Vector3 addTargetVel = (tarPos - transform.position).normalized * Random.Range(1f, 1.5f) +
                    agent.velocity.normalized * Random.Range(2f, 2.5f);
                //速度限制，与目标血量上限有关
                CharacterStats targetStats = target.GetComponent<CharacterStats>();
                addTargetVel *= agent.velocity.magnitude / targetStats.MaxHealth * 15;

                //附加速度用agent，更丝滑
                NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();
                if (targetAgent.isOnNavMesh) targetAgent.isStopped = true;
                targetAgent.velocity += addTargetVel;

                if (agent.velocity.magnitude < 0.4399f) return; //乌龟壳速度太小就没伤害了

                //根据乌龟壳(自身)是否旋转，造成10或20点伤害，无视防御！
                if (rigidBody.angularVelocity.magnitude < 7f)
                    damage = 15;
                else damage = 30;

                //大乌龟壳伤害增加
                if (gameObject.tag == "Boss") damage *= 3;

                //注意是对目标造成伤害和受击动画
                if (targetStats.CurrentHealth > 0)
                {
                    targetStats.CurrentHealth = Mathf.Max(targetStats.CurrentHealth - damage, 0);
                    //如果击杀目标，玩家Update经验值（CurrentHealth>0，防止多次加经验）
                    if (targetStats.CurrentHealth <= 0)
                        FindObjectOfType<PlayerController>().GetComponent<CharacterStats>().
                            characterData.UpdateExp(targetStats.characterData.killPoint);

                    target.GetComponent<Animator>().SetTrigger("Hit");

                    //怪物更新透视、血条UI
                    PlayerController playerCtl = 
                        GameManager.Instance.player.GetComponent<PlayerController>();
                    playerCtl.SwitchLongSightTargets(target);
                    targetStats.UpdateHealthBarAtOnce();
                    //Boss更新大血条
                    BossHealthUI.Instance.WakeUpBossHealthBar(target);
                }
                //撞到物体反弹，并且速度更快（？）
                agent.velocity = (transform.position - tarPos).normalized * agent.velocity.magnitude;

                break;
            case"Tree":
            case "Big Stone":
            case "Attackable":
                //撞到物体反弹，并且速度更快（？）
                agent.velocity = (transform.position - tarPos).normalized * agent.velocity.magnitude;

                break;
        }
    }

    void SwitchAnimation()
    {
        animator.SetBool("Walk", isWalk);
        animator.SetBool("Chase", isChase);
        animator.SetBool("Follow", isFollow);
        animator.SetBool("Critical", characterStats.isCritical);
        animator.SetBool("Death", isDead);

        animator.SetFloat("IdleBlend", idleBlend);
    }

    void SwitchStates()
    {
        isFoundPlayer = FoundPlayer();

        if (isDead)
            enemyBehaviorState = EnemyBehaviorState.DEAD;
        else
        if (isFoundPlayer && !isEscaping && !isCrossingPlayerPos)
            enemyBehaviorState = EnemyBehaviorState.CHASE; //如果发现Player，切换到CHASE

        switch (enemyBehaviorState)
        {
            case EnemyBehaviorState.GUARD:
                if (!isGuard)
                {
                    enemyBehaviorState = EnemyBehaviorState.PATROL;
                    return;
                }
                isChase = false;
                agent.speed = agentSpeed * 0.5f;

                if (lastPatrolTime < 0 || Vector3.Magnitude(guardPos - transform.position) > agent.stoppingDistance)
                {
                    isWalk = true;
                    lastPatrolTime = Random.Range(2f, 8f);

                    if (!agent.pathPending && agent.isOnNavMesh && agent.destination != guardPos)
                    {
                        agent.isStopped = false;
                        agent.stoppingDistance = 0.5f;

                        if (NavMesh.SamplePosition(guardPos, out navMeshHit, patrolRange, 1))
                            agent.destination = guardPos;
                    }
                }
                else
                {
                    isWalk = false;
                    agent.stoppingDistance = stopDistance;

                    if (remainLookAtTime > 0)
                    {
                        remainLookAtTime -= Time.deltaTime * stateFrames;
                        //随机巡逻点Idle动画
                        if (idleBlend == 0f) idleBlend = Random.value;

                        //怪物朝随机方向进行Lerp
                        transform.rotation = Quaternion.Lerp(transform.rotation, lookAtRotation, 0.0618f);

                        noFirstRandomLookAt = false;
                    }
                    else
                    {
                        if (!noFirstRandomLookAt)
                            StartCoroutine("GuardRandomLookAt");
                        noFirstRandomLookAt = true;
                        idleBlend = 0f;
                    }
                }
                break;
            case EnemyBehaviorState.PATROL:
                if (isGuard)
                {
                    enemyBehaviorState = EnemyBehaviorState.GUARD;
                    return;
                }
                isChase = false;
                agent.speed = agentSpeed * 0.5f;
                agent.stoppingDistance = 0.5f;

                //判断是否到了随机巡逻点
                if (lastPatrolTime < 0 || Vector3.Distance(wayPoint, transform.position) <= agent.stoppingDistance)
                {
                    isWalk = false;
                    lastPatrolTime = Random.Range(2f, 8f);

                    if (remainLookAtTime > 0)
                    {
                        remainLookAtTime -= Time.deltaTime * stateFrames;
                        //随机巡逻点Idle动画
                        if (idleBlend == 0f && remainLookAtTime > lookAtMinTime)
                            idleBlend = Random.value;

                    }
                    else
                    {
                        idleBlend = 0f;
                        GetPatrolWayPoint();
                    }
                }
                else
                {
                    isWalk = true;

                    //防止NavMesh动态加载时，边缘怪物的巡逻报错
                    if (!agent.pathPending && agent.destination != wayPoint)
                    {

                        if(agent.isOnNavMesh)
                        {
                            agent.isStopped = false;
                            agent.destination = wayPoint;
                            //防止怪物卡住不动 
                            if (agent.velocity.magnitude == 0) agent.velocity = transform.forward;
                        }
                        else //防止怪物卡住不动
                        agent.velocity = transform.forward * agent.speed;
                    }
                }
                break;
            case EnemyBehaviorState.CHASE:

                isWalk = false;
                isChase = true;
                agent.speed = agentSpeed;
                idleBlend = 0f;
                agent.stoppingDistance = stopDistance;

                remainChaingTime -= Time.deltaTime * stateFrames;
                //if (gameObject.name == "TurtleShell")
                //    Debug.Log("Turtle remain Chasing Time：" + remainChaingTime);

                if (!isFoundPlayer || remainChaingTime < 0)
                {
                    //拉脱，回到上一个状态
                    isFollow = false;
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    lerpLookAtTime = -1;
                    attackTarget = null;

                    if (remainLookAtTime > 0)
                    {
                        remainLookAtTime = - Time.deltaTime * stateFrames;
                    }
                    else
                    {
                        if(isFoundPlayer)
                        {
                            //Debug.Log(gameObject.name+ " Stop Chasing -> CheckEscape()");
                            //设定逃跑方向为巡逻点
                            escapeDirection = guardPos - transform.position;
                            escapeDirection.y = 0;
                            escapeDirection.Normalize();

                            //切换逃跑状态，不再追玩家
                            CheckEscape(false, true);
                        }
                        else
                        {
                            //返回巡逻状态
                            //Debug.Log(gameObject.name + " Stop Chasing -> GUARD or PATROL");
                            if (isGuard)
                                enemyBehaviorState = EnemyBehaviorState.GUARD;
                            else
                                enemyBehaviorState = EnemyBehaviorState.PATROL;
                        }

                        remainChaingTime = Random.Range(10f, 60f);
                    }

                }
                else if (attackTarget != null && 
                    Vector3.Distance(attackTarget.transform.position, transform.position) > agent.stoppingDistance)
                {
                    isFollow = true;
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        if (!agent.pathPending)
                            agent.destination = attackTarget.transform.position;
                        //防止怪物卡住不动 
                        if (agent.velocity.magnitude == 0) agent.velocity = transform.forward;
                    }
                    else //防止怪物卡住不动 
                        agent.velocity = agent.speed * transform.forward;
                }
                else //根据速度决定移动动画
                    isFollow = agent.velocity.magnitude > 0.1f;


                //在攻击范围内则攻击
                if (TargetInAttackRange() || TargetInSkillRange())
                {
                    if (lastAttackTime > Mathf.Min(Random.Range(0.5f, 1.5f),
                        characterStats.AttackCoolDown * characterStats.EnemyAttackDuring))
                        lerpLookAtTime = 0.382f; //攻击前摇时能够平滑面朝玩家

                    if (lastAttackTime < 0)
                    {
                        //暴击判断
                        characterStats.isCritical = Random.value < characterStats.CriticalChance;
                        Attack();
                    }
                }
                break;

            case EnemyBehaviorState.ESCAPE:
                //逃跑状态，朝逃跑方向全力逃离
                isWalk = false;
                isChase = true;
                idleBlend = 0f;
                remainEscapeTime -= Time.deltaTime * stateFrames;
                //根据血量剩余，速度上限会提升或者下降
                agent.speed = Mathf.Lerp(1.5f, escapeSpeed,
                    (float)characterStats.CurrentHealth / (characterStats.MaxHealth * 0.3f));

                if (remainEscapeTime<0)
                {
                    //逃离时间结束
                    if(isFoundPlayer)
                    { //返回追击状态
                        enemyBehaviorState = EnemyBehaviorState.CHASE;
                        lerpLookAtTime = 0.618f;
                    }
                    else
                    { //返回巡逻状态
                        if (isGuard)
                            enemyBehaviorState = EnemyBehaviorState.GUARD;
                        else
                            enemyBehaviorState = EnemyBehaviorState.PATROL;
                    }
                    //清除标记！
                    isEscaping = false;
                }
                else
                {
                    //朝指定方向逃离
                    isFollow = true;
                    if(agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        if (!agent.pathPending)
                        {
                            Vector3 destination = transform.position + escapeDirection * 15f;
                            if(destination.IsOnGround(out upHighDownHit))
                            {
                                destination.y = upHighDownHit.point.y;
                                if (NavMesh.SamplePosition(destination, out navMeshHit, patrolRange, 1))
                                    agent.destination = navMeshHit.position;
                                //如果找不到目标点，则更改逃跑方向
                                else CheckEscape(true);
                                //防止怪物卡住不动 
                                if (agent.velocity.magnitude == 0) agent.velocity = transform.forward;
                            }
                            else CheckEscape(true);
                        }
                    }
                    else //防止怪物追逐时卡住不动
                        if (agent.velocity.magnitude == 0)
                        agent.velocity = agent.speed * transform.forward;
                }

                break;
            case EnemyBehaviorState.CROSS_PLAYER_POS:
                //绕后状态，试图越过玩家位置
                isWalk = false;
                isChase = true;
                idleBlend = 0f;
                remainEscapeTime -= Time.deltaTime * stateFrames;

                if (remainEscapeTime > 0 && Vector3.Distance(wayPoint, transform.position) > agent.stoppingDistance)
                {
                    isFollow = true;
                    //前往绕后的目标点
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        if (!agent.pathPending) agent.destination = wayPoint;

                        //防止怪物卡住不动 
                        if (agent.velocity.magnitude == 0) agent.velocity = transform.forward;
                    }
                    else //防止怪物追逐时卡住不动
                        if (agent.velocity.magnitude == 0)
                        agent.velocity = agent.speed * transform.forward;

                    //停止丢失目标，保持对玩家的目标锁定
                    noFirstLostTarget = false;
                    StopCoroutine(LoseTarget());
                }
                else
                {
                    //返回追击状态
                    enemyBehaviorState = EnemyBehaviorState.CHASE;
                    isCrossingPlayerPos = false;
                    lerpLookAtTime = 0.618f;
                    //清除标记！
                    isEscaping = false;
                }

                break;
            case EnemyBehaviorState.DEAD:
                if(agent.isOnNavMesh) agent.isStopped = true;

                if (!isTurtleShell)
                {
                    coll.enabled = false;

                    //直接关闭agent，会导致StopAgent无法获取NavMashAgent报错
                    //agent.enabled = false;
                    //解决方法：设置radius为0，从而玩家可以穿过死亡目标
                    agent.radius = 0;

                    if (!isDestroying)
                    {
                        ////移除附加在身上的物体（比如箭矢），以免一同被销毁
                        //foreach (var attach in attachObjects)
                        //{
                        //    if (attach)
                        //        attach.transform.SetParent(null);
                        //}
                        Destroy(gameObject, 2f);
                        //死亡后重新生成
                        GameManager.Instance.InstantiateEnemy(gameObject, gameObject.transform.position, numInArray);

                        isDestroying = true;
                    }
                }
                else
                {
                    //对乌龟进行特殊处理，死亡后不会立即消失，可击打撞击其他目标
                    if (!isTurtleShellDestroying)
                    {
                        //放大碰撞体，让乌龟壳更容易命中
                        BoxCollider boxCol = (BoxCollider)coll;
                        boxCol.size *= 1.618f;
                        boxCol.center = new Vector3(boxCol.center.x, boxCol.center.y * 1.618f, boxCol.center.z);
                        
                        ResetTurtleDestroy();
                        isTurtleShellDestroying = true;
                    }
                }
                break;
        }
    }

    internal void CheckCrossPlayer()
    {
        //怪物攻击后，有几率越过玩家位置
        if (attackTarget == null || Random.value > characterStats.EnemyCrossPlayerRate) return;


        Vector3 dir = attackTarget.transform.position - transform.position;
        //偏移的方向
        Vector3 bias = Random.onUnitSphere;
        bias.y = 0;
        bias.Normalize();
        //如果玩家背朝自己，则保证越过玩家（否则任意走位）
        if(Vector3.Dot(transform.forward,attackTarget.transform.forward)>0)
        {
            if (Vector3.Dot(bias, dir.normalized) < 0) bias = -bias;
            //如果仍在60度夹角外，进一步修正
            if (Vector3.Dot(bias, dir.normalized) < 0.5f)
                bias = (dir.normalized + bias).normalized;
        }
        //决定最终方位距离
        dir += bias * characterStats.AttackRange * Random.Range(1f, 3f);

        //如果成功找到移动点，则切换状态
        if(NavMesh.SamplePosition(transform.position + dir, out navMeshHit, characterStats.AttackRange, 1))
        {
            wayPoint = navMeshHit.position;
            enemyBehaviorState = EnemyBehaviorState.CROSS_PLAYER_POS;
            lerpLookAtTime = -1;
            isCrossingPlayerPos = true;

            //移动速度暂时提升！
            agent.speed = agentSpeed * 2f;

            remainEscapeTime = Random.Range(2f, 4f);
        }    
    }

    protected virtual void Attack()
    {
        lerpLookAtTime = 0.382f;
        //transform.LookAt(attackTarget.transform);

        Vector3 direction = attackTarget.transform.position - transform.position;
        float distance = direction.magnitude;

        //如果没有面朝玩家，则不攻击
        if (Vector3.Dot(transform.forward, direction.normalized) < characterStats.attackData.enemyAtkCosin)
            return;

        //Skill和Attack的触发逻辑：Skill优先触发
        if (TargetInSkillRange() && lastSkillTime < 0)
        {
            isFollow = false;
            //随机技能冷却时间
            lastSkillTime = characterStats.SkillCoolDowm * Random.value;
            //技能攻击动画
            animator.SetTrigger("Skill");

            if (agent.isOnNavMesh) agent.isStopped = true;
        }
        else
        if (TargetInAttackRange())
        {
            isFollow = false;
            //重置攻击时间，如果暴击，CD更久
            if (characterStats.isCritical)
                lastAttackTime = characterStats.AttackCoolDown * Random.Range(1.618f, 2.718f);
            else
                lastAttackTime = characterStats.AttackCoolDown;
            //近身攻击动画
            animator.SetTrigger("Attack");

            if (agent.isOnNavMesh) agent.isStopped = true;
        }
    }

    private void LerpLookAt(Transform targetTransform)
    {
        if (isDead) return;

        Vector3 tarDir = targetTransform.position - transform.position;
        tarDir.y = 0;
        Quaternion tarRot = Quaternion.FromToRotation(Vector3.forward, tarDir);

        //体积越大，转身越慢
        float lerpSpeed = 0.06f / Mathf.Max(transform.localScale.x, transform.localScale.y);
        lerpSpeed = Mathf.Max(lerpSpeed, 0.006f) * lerpRotationRate;

        transform.rotation = Quaternion.Lerp(transform.rotation, tarRot, lerpSpeed);
    }

    bool FoundPlayer()
    {
        if (findAttacker || isCrossingPlayerPos)
        {
            //遭到玩家攻击后，立即发现玩家
            findAttacker = false;
            return true;
        }

        //远距离目标也加入检测，因为CHASE状态下视野半径翻倍（事实上目标只有玩家）
        playerCols = Physics.OverlapSphere(transform.position, sightRadius * 2, LayerMask.GetMask("Player"));
        foreach (var player in playerCols)
            if (player.CompareTag("Player"))
            {
                RaycastHit hit;
                Vector3 eyePos = eyeBall.transform.position; //眼睛位置
                Vector3 eyeFoward = GetEyeForward(eyeBall); //怪物眼睛朝向
                //判断视线是否被遮挡
                Vector3 targetDir1 = player.transform.position - transform.position;
                Vector3 targetDir2 = player.transform.position - eyePos;

                float angle = Vector3.Angle(eyeFoward, targetDir1);
                //Debug.Log(gameObject.ToString() + " angle: " + angle + " |  Radius: " + targetDir1.magnitude);

                //Chase状态下：若不在视野内，且在1倍视野范围外
                if (enemyBehaviorState == EnemyBehaviorState.CHASE &&
                    (angle > halfSightAngle && targetDir1.magnitude > sightRadius)) break;
                //非Chase状态下：若不在视野内，或在1倍视野范围外
                if (enemyBehaviorState != EnemyBehaviorState.CHASE &&
                    (angle > halfSightAngle || targetDir1.magnitude > sightRadius)) break;

                //若无遮挡看见玩家，设置追击目标；第二条射线相同判断
                if ((Physics.Raycast(eyePos, targetDir1, out hit, sightRadius) &&
                    hit.transform.gameObject.tag == "Player")
                    || (Physics.Raycast(eyePos, targetDir2, out hit, sightRadius) &&
                    hit.transform.gameObject.tag == "Player"))
                {
                    //获得目标，更新透视
                    attackTarget = player.gameObject;
                    SwitchInsightLayer();

                    //停止丢失目标，重置noFirstLostTarget
                    noFirstLostTarget = false;
                    StopCoroutine("LoseTarget");

                    return true;
                }

                break; //枚举到Player，就可以停止了
            }
        //attackTarget = null;
        if (!noFirstLostTarget)
        {
            //只有在第一次丢失目标的时候，会调用LossTarget协程
            noFirstLostTarget = true;
            StartCoroutine("LoseTarget");
        }

        //目标尚未丢失
        if (attackTarget != null) return true;

        //目标已丢失
        return false;
    }

    internal void CheckEscape(bool forceChangeDir = false, bool forceEscape = false)
    {
        //强制进入逃跑状态
        isEscaping = isEscaping || forceEscape;

        float healthRate = (float)characterStats.CurrentHealth / characterStats.MaxHealth;
        //血量1/3以上时，不会进入逃跑状态
        if (!isEscaping && healthRate > 0.3f) return;
        //血量恢复至半血以上，结束逃跑状态
        if (healthRate > 0.5f && !forceEscape)
        { isEscaping = false; return; }

        //受攻击时一定几率逃跑；若进入逃跑状态，则每次受攻击都会触发
        if (!isEscaping && Random.value > characterStats.EscapeRate) return;

        //怪物进入逃跑状态
        enemyBehaviorState = EnemyBehaviorState.ESCAPE;
        isEscaping = true;
        lerpLookAtTime = -1;

        //设定逃跑方向
        if (forceChangeDir || Random.value < escapeDirChangeRate)
        {
            escapeDirection = Random.onUnitSphere;
            escapeDirection.y = 0;
            escapeDirection.Normalize();

            //若知道玩家在哪，则保证远离玩家方向（forceChangeDir用于避免撞墙）
            if (!forceChangeDir && isFoundPlayer &&
                Vector3.Dot(escapeDirection, GameManager.Instance.player.transform.forward) < 0)
                escapeDirection = -escapeDirection;

            //下次改变朝向的几率发生变化
            escapeDirChangeRate = Random.Range(0.2f, 0.8f);
        }

        //根据血量剩余，速度上限会提升或者下降
        agent.speed = Mathf.Lerp(2f, escapeSpeed, (float)characterStats.CurrentHealth / (characterStats.MaxHealth * 0.3f));

        //重置逃离状态的持续时间
        if (!forceChangeDir)
            remainEscapeTime = Random.Range(5f, 15f);

        //初次设定逃跑，考虑常见问题
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            Vector3 destination = transform.position + escapeDirection * 5f;
            if (destination.IsOnGround(out upHighDownHit))
            {
                destination.y = upHighDownHit.point.y;
                if (NavMesh.SamplePosition(destination, out navMeshHit, patrolRange, 1))
                    agent.destination = navMeshHit.position;
            }
            else
            {
                escapeDirection = -escapeDirection;
                agent.destination = GameManager.Instance.player.transform.position + escapeDirection * 5f;
            }
            //防止怪物卡住不动 
            if (agent.velocity.magnitude == 0) agent.velocity = transform.forward;
        }
        else agent.velocity = agent.speed * transform.forward;
    }

    IEnumerator LoseTarget()
    {
        yield return new WaitForSeconds(lossTargetTime);

        //经过lossTargetTime后，目标丢失；更新透视层
        attackTarget = null;
        SwitchInsightLayer();

    }
    IEnumerator GuardRandomLookAt()
    {
        //随机Guard时间，guardKeepMinTime的1~3倍
        yield return new WaitForSeconds(guardKeepMinTime * (1f + Random.value * 2));

        remainLookAtTime = lookAtMinTime * (1f + Random.value);  //随机1~2倍LookAt时间
        if (idleBlend == 0f) idleBlend = Random.value;
        //Guard状态下，设置下次随机朝向
        float yRotNextLookAt = Random.Range(-1.0f, 1.0f) * (90 - halfSightAngle);
        lookAtRotation = Quaternion.AngleAxis(yRotNextLookAt, Vector3.up) * guardRotation;
    }

    protected bool TargetInAttackRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position)
                <= characterStats.AttackRange;
        else
            return false;
    }

    protected virtual bool TargetInSkillRange()
    {
        if (attackTarget != null)
            return Vector3.Distance(attackTarget.transform.position, transform.position)
                <= characterStats.SkillRange;
        else
            return false;
    }

    void GetPatrolWayPoint()
    {
        if (!GameManager.Instance.player) return;

        //随机lookAt时间
        remainLookAtTime = lookAtMinTime * Random.value * 2;

        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);

        Vector3 randomPoint = new Vector3(guardPos.x + randomX,
            transform.position.y, guardPos.z + randomZ);

        if (Vector3.Distance(randomPoint, transform.position) < 2f)
        {
            //防止移动距离太短，显得很呆
            randomPoint = (randomPoint - transform.position).normalized
                * Random.Range(0.5f * patrolRange, 1.5f * patrolRange) + transform.position;
        }

        //修正随机点位置，使其在Ground上
        if (randomPoint.IsOnGround(out upHighDownHit))
            randomPoint = upHighDownHit.point;
        else
        {
            //随机点如果超出地图，朝玩家方向进行一次修正
            randomPoint =
                (randomPoint + GameManager.Instance.player.transform.position) * 0.6f;
        }

        //防止NavMesh撞墙卡住，调用SamplePosition修复
        //NavMesh.GetAreaFromName("Walkable") == 1
        wayPoint = NavMesh.SamplePosition(randomPoint, out navMeshHit, patrolRange, 1) ?
            navMeshHit.position : randomPoint +
            (GameManager.Instance.player.transform.position - randomPoint) 
            * Random.Range(-0.3f, 0.3f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, sightRadius);
        Vector3 eyePos = eyeBall.transform.position;
        Gizmos.DrawLine(eyePos, eyePos + GetEyeForward(eyeBall) * sightRadius * 2);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, patrolRange);
    }

    //Animation Event
    protected virtual bool Hit()
    {
        if (attackTarget != null)
            // && transform.CatchedTarget(attackTarget.transform)) //这里没有使用教程里的扩展方法
        {
            var targetStats = attackTarget.GetComponent<CharacterStats>();

            Vector3 direction = attackTarget.transform.position - transform.position;
            //超出Attack范围丢失
            if (direction.magnitude > characterStats.AttackRange)
            {
                //Debug.Log("miss");
                return false;
            }

            direction.Normalize();
            //判定玩家自动闪避
            if (Vector3.Dot(transform.forward, direction) < characterStats.attackData.enemyAtkCosin)
            {
                //Debug.Log("Dodge！！");
                NavMeshAgent targetAgent = attackTarget.GetComponent<NavMeshAgent>();
                Vector3 dodgeDir = -transform.forward + attackTarget.transform.forward * 2;
                if(targetAgent.isOnNavMesh) targetAgent.isStopped = true;
                //闪避涉水的衰减
                float waterSlow = Mathf.Lerp(1f, 0.5f, AudioManager.Instance.footStepWaterDeep - transform.position.y);
                targetAgent.velocity = dodgeDir.normalized * targetStats.characterData.attackDodgeVel * waterSlow;
                //音效
                attackTarget.GetComponent<PlayerController>().PlayDodgeSound(true);

                return false;
            }
            targetStats.TakeDamage(characterStats, targetStats);

            //击中玩家音效
            HitPlayerSound(targetStats);

            return true;
        }

        return false;
    }

    public void HitPlayerSound(CharacterStats targetStats, float bias  =0)
    {
        SoundName soundName = characterStats.isCritical ? SoundName.Enemy_CritBody : SoundName.Enemy_AttackBody;
        if (targetStats.attackData.hasEquipShield && targetStats.isShieldSuccess)
        {
            soundName = targetStats.attackData.isMetalShield ?
                (characterStats.isCritical ? SoundName.Enemy_CritMetalShield : SoundName.Enemy_MetalShield)
               : (characterStats.isCritical ? SoundName.Enemy_CritWoodShield : SoundName.Enemy_WoodShield);
        }
        AudioManager.Instance.Play3DSoundEffect(soundName, soundDetailList, targetStats.transform, bias, bias);
    }

    public void EndNotify()
    {
        //获胜动画
        //停止所有移动
        //停止Agent
        if (Vector3.Distance(transform.position, GameManager.Instance.player.transform.position) 
            < patrolRange + sightRadius)
            animator.SetBool("Win", true);

        playerDead = true;
        isChase = false;
        isWalk = false;
        attackTarget = null;
    }

    public void LoadNotify()
    {
        //关闭获胜动画
        animator.SetBool("Win", false);

        //重置对玩家状态
        playerDead = false;
        attackTarget = null;

        //重置巡逻状态
        if (isGuard)
            enemyBehaviorState = EnemyBehaviorState.GUARD;
        else
            enemyBehaviorState = EnemyBehaviorState.PATROL;
    }

    public virtual Vector3 GetEyeForward(GameObject eyeBall)
    {
        return eyeBall.transform.up * -1f;
    }


    //乌龟死亡专用
    public void ResetTurtleDestroy()
    {
        StopCoroutine("TurtleLaterDestroy");
        if (gameObject.activeSelf)
            StartCoroutine("TurtleLaterDestroy");
    }
    //乌龟死亡专用
    IEnumerator TurtleLaterDestroy()
    {
        yield return new WaitForSeconds(Random.Range(TurtleDestroyWaitTime, TurtleDestroyWaitTime * 2));

        coll.enabled = false;

        if (!isDestroying)
        {
            Destroy(gameObject, 2f);
            //死亡后重新生成
            GameManager.Instance.InstantiateEnemy(gameObject, gameObject.transform.position, numInArray);

            isDestroying = true;
        }
    }

    //起跳相关的动画，起跳时设置agent半径为0
    //event
    void SetAgentZeroRadiusWhenJump()
    {
        agent.radius = 0;
    }
    //起跳相关的动画，落地时恢复agent半径
    //event
    void ResetAgentRadiusWhenLanding()
    {
        agent.radius = agentRadius;
    }

    #region Play Sound
    /// <summary>
    /// Animation Event：玩家脚步声
    /// </summary>
    void FootStepSound()
    {
        if (isDead) return; //修复死后脚步声的bug

        float scaleBias = 0.1f / transform.localScale.x;
        if (enemyBehaviorState == EnemyBehaviorState.CHASE)
            AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_Run, soundDetailList, transform, scaleBias, -scaleBias);
        else
            AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_Walk, soundDetailList, transform, scaleBias, -scaleBias);
    }
    public void SlimeGrassFastSound(float volumeBiasRate)
    {
        float scaleBias = 0.1f / transform.localScale.x;
        AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_Walk, soundDetailList, transform,
            0.4f + scaleBias, volumeBiasRate - scaleBias);
    }
    void SlimeCriticalStepSound()
    {
        float scaleBias = 0.1f / transform.localScale.x;
        AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_Run, soundDetailList, transform,
            -0.4f + scaleBias, 0.2f - scaleBias);
    }
    //Animation Event
    void SlimeTauntStepSound(float volumeBiasRate)
    {
        float scaleBias = 0.1f / transform.localScale.x;
        AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_Run, soundDetailList, transform,
            0.2f + scaleBias, -0.1f - scaleBias + volumeBiasRate);
    }
    void SlimeVictoryStepSound()
    {
        float scaleBias = 0.1f / transform.localScale.x;
        AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_Run, soundDetailList, transform,
            -0.2f + scaleBias, 0.1f - scaleBias);
    }

    //Animation Event
    public void AttackDodgeSound()
    {
        float scaleBias = 0.1f / transform.localScale.x;
        AudioManager.Instance.Play3DSoundEffect(SoundName.EnemyAttackDodge, soundDetailList, transform,
            scaleBias, - scaleBias);
    }

    //怪物播放受击音效
    internal void PlayHitSound(SoundName enemyHitSound, float pitchAdd = 0, float volumeAdd = 0)
    {
        float scaleBias = 0.1f / transform.localScale.x;
        float pitchBias = scaleBias + pitchAdd;
        //是否为箭矢
        if (enemyHitSound != SoundName.Enemy_by_Arrow)
            pitchBias -= GameManager.Instance.playerStats.WeaponPitchBias;

        //特殊动作情况
        switch (GameManager.Instance.player.GetComponent<PlayerController>().
            weaponWaveSound)
        {
            case SoundName.TwoHandSword_Rush:
            case SoundName.TwoHandAxe_Rush:
            case SoundName.TwoHandMace_HitBack:
                pitchBias -= 0.25f;
                break;
        }

        //怪物受击的音调，与武器的音调偏移相反
        AudioManager.Instance.Play3DSoundEffect(enemyHitSound, soundDetailList, transform,
            pitchBias, -scaleBias + volumeAdd);
    }

    void AttackWaveSound()
    {
        float scaleBias = 0.1f / transform.localScale.x;
        SoundName soundName = characterStats.isCritical ? SoundName.Enemy_CritWave : SoundName.Enemy_AttackWave;
        AudioManager.Instance.Play3DSoundEffect(soundName, soundDetailList, transform, scaleBias, -scaleBias);
    }

    void CheckNoisePlay(bool forcePlay = false, int noiseID = 0)
    {
        if (isDead) return;

        if ((forcePlay || lastNoiseTime < 0) && currentNoiseTime < 0)
        {
            Sound sound = null;
            if (noiseID == 0)
                sound = AudioManager.Instance.Play3DSoundEffect(noises[Random.Range(0, noises.Length)],
                    soundDetailList, transform, noisePitchBias);
            else
                sound = AudioManager.Instance.Play3DSoundEffect(noises[noiseID-1],
                    soundDetailList, transform, noisePitchBias);

            lastNoiseTime = NextNoiseTime;
            currentNoiseTime = sound.audioSource.clip.length * 1.5f;
        }

        lastNoiseTime -= Time.deltaTime;
        currentNoiseTime -= Time.deltaTime;
    }

    /// <summary>
    /// Animation Event
    /// 怪物不同动画，有几率触发
    /// float: 叫声触发的几率
    /// int: 指定叫声触发的编号，1~3；0表示随机触发
    /// </summary>
    /// <param name="animationEvent"></param>
    public void PlayNoise(AnimationEvent animationEvent)
    {
        if (Random.value < animationEvent.floatParameter)
            CheckNoisePlay(true, animationEvent.intParameter);
    }
    public void RandomPlayNoise(float rate)
    {
        if (Random.value < rate) 
            StartCoroutine(LaterPlayNoise());
    }

    IEnumerator LaterPlayNoise()
    {
        yield return new WaitForSeconds(Random.value);
        CheckNoisePlay(true);
    }
    #endregion
}
