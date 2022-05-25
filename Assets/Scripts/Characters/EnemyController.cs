using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//��������״̬����������Ѫʱ(Ѫ���������)���м�������
//�����ƺ�״̬��ÿ�ι������м���ǰ��������ҵ�λ�ã��ڼ��ƶ��ٶȷ���

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
    [Tooltip("����ƽ��������ҵ��ٶȱ��ʣ�Ĭ��Ϊ1")]
    public float lerpRotationRate = 1;
    private float remainLookAtTime;
    protected float lastAttackTime;
    [HideInInspector]public bool slowAttackRefresh;
    protected float lastSkillTime;
    [Tooltip("�Ƿ�Ϊ�ڹ꣬�������Ϊ�ɻ��ɶ��󣬱����ڳ�����һ��ʱ��")]
    public bool isTurtleShell;
    public float turtleShellTorqueRate = 5.55f;

    [Header("Patrol State")]
    public float patrolRange = 4f;
    [Tooltip("�����Ѫʱ���ܵ�����ٶ�")]
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

    private float lastPatrolTime; //��ֹPatrol���ﲻ��Ŀ�꣬�Ӷ���һֱ��ǽ�����

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
    private bool isLosingInsight; //���ڶ�ʧ͸�ӣ�Э�̣�

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

        //�ѹ���AI��������ͬһ֡����
        lastStateFrames = Random.Range(0, stateFrames);

        //�����������
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

    //�л�����ʱ����
    private void OnEnable()
    { 
        //Ϊʲô����û�ж��ĳɹ�����Ϊ���ﱾ���ʹ��ڣ�OnEnable�ȵ�����Awake��ִ��
        //�޸ķ�ʽ������ͨ���޸�Ĭ�ϵĴ���ִ��˳��������
        //�� Project Settings �� �ҵ� Script Execute Order �ֶ������� Manager ����룬
        //����������ֵ����Ϊһ������������-20��ֻҪ��  Default Time ֮��������������ű�ִ�С�

        //���Ĺ۲���
        GameManager.Instance?.AddObserver(this);

        //��õ��������OffMeshLink��������agent
        agent.ActivateCurrentOffMeshLink(true);

        //�����ڹ��
        if (isDead && isTurtleShell)
        {
            TurtleDestroyWaitTime *= 0.618f;
            ResetTurtleDestroy();
        }
    }

    private void OnDisable()
    {
        //��ֹ��Ϸֹͣʱ�ٴε���OnDisable����ȷ��GameManager�Ƿ��ѱ���ǰ����
        if (!GameManager.IsInitialized) return;

        //�Ƴ��۲���
        GameManager.Instance?.RemoveObserver(this);


    }

    protected virtual void Update()
    {
        if (characterStats.CurrentHealth == 0 && !isDead)
        {
            isDead = true;

            //�Ƴ����������ϵ����壨�����ʸ��������һͬ������
            if (attachObjects.Count != 0)
            {
                foreach (var attach in attachObjects)
                {
                    if (attach)
                        attach.transform.SetParent(null);
                }
                attachObjects.Clear();
            }

            //������������
            if (GetComponent<LootSpawner>())
                GetComponent<LootSpawner>().Spawnloot();

            //��һ�ɱ����
            if (QuestManager.IsInitialized)
                QuestManager.Instance.UpdateQuestProgress(enemyName, 1);

            //���������ʷ��ķ���´ν��볡����������ҵȼ�
            if (isNightmare)
                SaveManager.Instance.ResetPlayerLevelInData();
        }

        if (!playerDead)
        {
            if (lastStateFrames < 0)
            {
                //����AI����Ҫÿִ֡�У���Ϊÿ����ִ֡��
                SwitchStates();
                lastStateFrames = stateFrames;
            }

            //����AI��Ϊ���ִ�к�����Ҫƽ��������ҵ�Ч��
            if (lerpLookAtTime > 0 && AttackTarget != null)
                LerpLookAt(AttackTarget.transform);

            SwitchAnimation();
            SwitchInsightLayer();
            CheckNoisePlay();

            //�Ƿ����CDˢ�£���Hit��Dizzy�������
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
            //�뿪OutsightRadius�Ĺ����������ҵ�target���ر�͸��
            if (!isTargetByPlayer &&
                Vector3.Distance(transform.position, player.transform.position) > playerStats.OutSightRadius)
            {
                //��ʧ͸������ʱ
                if (!isLosingInsight)
                {
                    isLosingInsight = true;
                    StartCoroutine("LoseInsightLater");
                }
            }
        }
        else
        {
            //����InsightRadius�ڵĹ������͸��
            //��ҵ�ǰ��attackTarget��LongSightRadius�ڿ���
            //target����ҵĹ��OutSightRadius�ڿ���
            if (isInsightedByPlayer
                || (isTargetByPlayer &&
                Vector3.Distance(transform.position, player.transform.position) <= playerStats.LongSightRadius)
                || (attackTarget == player &&
                Vector3.Distance(transform.position, player.transform.position) <= playerStats.OutSightRadius))

            {
                //����͸����Ҫ�ȹر�Э�̣�������
                StopCoroutine("LoseInsightLater");
                isLosingInsight = false;

                //������øù����͸��
                gameObject.layer = LayerMask.NameToLayer("Enemy Insight");
                Transform[] childs = gameObject.transform.GetComponentsInChildren<Transform>();

                //��������Ҳ����
                foreach (Transform trans in childs)
                    trans.gameObject.layer = LayerMask.NameToLayer("Enemy Insight");

                isInsightedByPlayer = true;
            }
        }
    }

    IEnumerator  LoseInsightLater()
    {
        yield return new WaitForSeconds(GameManager.Instance.playerStats.LossInsightTime);

        //һ��ʱ��󣬶�ʧ�ù����͸��
        gameObject.layer = LayerMask.NameToLayer("Enemy");
        Transform[] childs = gameObject.transform.GetComponentsInChildren<Transform>();

        //��������Ҳ����
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
                //������ľ����٣����κ�ʱ��
                agent.velocity *= characterStats.EnterBushSpeedRate;
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        //�ڹ�ǵ�������
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
                //����������������ң����˺�����ѣ�Σ�����ȷ������Ƿ񱻻��ɣ���Ϊ���isKinematic�رղŻᴥ����

                Vector3 tarPos = collision.gameObject.GetComponent<Transform>().position;
                //�����˺�Ҫ��NavMeshAgent���ٶȣ�
                Vector3 tarVel = collision.gameObject.GetComponent<NavMeshAgent>().velocity;
                //����ҳ�Զ�뷽���򲻻ᴥ��
                if (Vector3.Dot(tarVel, tarPos - transform.position) > 0.618f) break;

                //���ﳯ������򱻻��ˣ�����ң�
                Vector3 addVel = (transform.position - tarPos).normalized * Random.Range(0f, 0.618f) +
                    tarVel.normalized * Random.Range(0.382f, 0.618f);
                addVel *= Mathf.Clamp(tarVel.magnitude, 0, 15f);

                agent.velocity += addVel;

                if (isDead) return; //������������ֻ�ܵ�����

                //����addVel�ܵ��˺�
                int damage = (int)(addVel.magnitude * characterStats.HitByPlayerDamagePerVel);
                //�������ڷ������Ż���HitЧ��
                if(damage> characterStats.CurrentDefence)
                    animator.SetTrigger("Hit");
                characterStats.TakeDamage(damage, collision.transform.position);

                //���Ҳ���ܵ��˺�
                damage = (int)(addVel.magnitude * characterStats.PlayerHurtDamagePerVel);
                CharacterStats playerStats = collision.gameObject.GetComponent<CharacterStats>();
                playerStats.TakeDamage(damage, transform.position);

                //ײ����Ч
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

        //��������target�����ж�
        string tag = target.tag;
        switch (tag)
        {
            //�ڹ��ײ�Ϲ��Boss����ң�������������
            case "Enemy":
            case "Boss":
            case "Player":
                //�����ڹ��Զ�뷽���򲻻ᴥ������agent���ٶȣ�����ʱһЩ��
                if (Vector3.Dot(agent.velocity, transform.position - tarPos) > 0.618f) break;

                Vector3 addTargetVel = (tarPos - transform.position).normalized * Random.Range(1f, 1.5f) +
                    agent.velocity.normalized * Random.Range(2f, 2.5f);
                //�ٶ����ƣ���Ŀ��Ѫ�������й�
                CharacterStats targetStats = target.GetComponent<CharacterStats>();
                addTargetVel *= agent.velocity.magnitude / targetStats.MaxHealth * 15;

                //�����ٶ���agent����˿��
                NavMeshAgent targetAgent = target.GetComponent<NavMeshAgent>();
                if (targetAgent.isOnNavMesh) targetAgent.isStopped = true;
                targetAgent.velocity += addTargetVel;

                if (agent.velocity.magnitude < 0.4399f) return; //�ڹ���ٶ�̫С��û�˺���

                //�����ڹ��(����)�Ƿ���ת�����10��20���˺������ӷ�����
                if (rigidBody.angularVelocity.magnitude < 7f)
                    damage = 15;
                else damage = 30;

                //���ڹ���˺�����
                if (gameObject.tag == "Boss") damage *= 3;

                //ע���Ƕ�Ŀ������˺����ܻ�����
                if (targetStats.CurrentHealth > 0)
                {
                    targetStats.CurrentHealth = Mathf.Max(targetStats.CurrentHealth - damage, 0);
                    //�����ɱĿ�꣬���Update����ֵ��CurrentHealth>0����ֹ��μӾ��飩
                    if (targetStats.CurrentHealth <= 0)
                        FindObjectOfType<PlayerController>().GetComponent<CharacterStats>().
                            characterData.UpdateExp(targetStats.characterData.killPoint);

                    target.GetComponent<Animator>().SetTrigger("Hit");

                    //�������͸�ӡ�Ѫ��UI
                    PlayerController playerCtl = 
                        GameManager.Instance.player.GetComponent<PlayerController>();
                    playerCtl.SwitchLongSightTargets(target);
                    targetStats.UpdateHealthBarAtOnce();
                    //Boss���´�Ѫ��
                    BossHealthUI.Instance.WakeUpBossHealthBar(target);
                }
                //ײ�����巴���������ٶȸ��죨����
                agent.velocity = (transform.position - tarPos).normalized * agent.velocity.magnitude;

                break;
            case"Tree":
            case "Big Stone":
            case "Attackable":
                //ײ�����巴���������ٶȸ��죨����
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
            enemyBehaviorState = EnemyBehaviorState.CHASE; //�������Player���л���CHASE

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
                        //���Ѳ�ߵ�Idle����
                        if (idleBlend == 0f) idleBlend = Random.value;

                        //���ﳯ����������Lerp
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

                //�ж��Ƿ������Ѳ�ߵ�
                if (lastPatrolTime < 0 || Vector3.Distance(wayPoint, transform.position) <= agent.stoppingDistance)
                {
                    isWalk = false;
                    lastPatrolTime = Random.Range(2f, 8f);

                    if (remainLookAtTime > 0)
                    {
                        remainLookAtTime -= Time.deltaTime * stateFrames;
                        //���Ѳ�ߵ�Idle����
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

                    //��ֹNavMesh��̬����ʱ����Ե�����Ѳ�߱���
                    if (!agent.pathPending && agent.destination != wayPoint)
                    {

                        if(agent.isOnNavMesh)
                        {
                            agent.isStopped = false;
                            agent.destination = wayPoint;
                            //��ֹ���￨ס���� 
                            if (agent.velocity.magnitude == 0) agent.velocity = transform.forward;
                        }
                        else //��ֹ���￨ס����
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
                //    Debug.Log("Turtle remain Chasing Time��" + remainChaingTime);

                if (!isFoundPlayer || remainChaingTime < 0)
                {
                    //���ѣ��ص���һ��״̬
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
                            //�趨���ܷ���ΪѲ�ߵ�
                            escapeDirection = guardPos - transform.position;
                            escapeDirection.y = 0;
                            escapeDirection.Normalize();

                            //�л�����״̬������׷���
                            CheckEscape(false, true);
                        }
                        else
                        {
                            //����Ѳ��״̬
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
                        //��ֹ���￨ס���� 
                        if (agent.velocity.magnitude == 0) agent.velocity = transform.forward;
                    }
                    else //��ֹ���￨ס���� 
                        agent.velocity = agent.speed * transform.forward;
                }
                else //�����ٶȾ����ƶ�����
                    isFollow = agent.velocity.magnitude > 0.1f;


                //�ڹ�����Χ���򹥻�
                if (TargetInAttackRange() || TargetInSkillRange())
                {
                    if (lastAttackTime > Mathf.Min(Random.Range(0.5f, 1.5f),
                        characterStats.AttackCoolDown * characterStats.EnemyAttackDuring))
                        lerpLookAtTime = 0.382f; //����ǰҡʱ�ܹ�ƽ���泯���

                    if (lastAttackTime < 0)
                    {
                        //�����ж�
                        characterStats.isCritical = Random.value < characterStats.CriticalChance;
                        Attack();
                    }
                }
                break;

            case EnemyBehaviorState.ESCAPE:
                //����״̬�������ܷ���ȫ������
                isWalk = false;
                isChase = true;
                idleBlend = 0f;
                remainEscapeTime -= Time.deltaTime * stateFrames;
                //����Ѫ��ʣ�࣬�ٶ����޻����������½�
                agent.speed = Mathf.Lerp(1.5f, escapeSpeed,
                    (float)characterStats.CurrentHealth / (characterStats.MaxHealth * 0.3f));

                if (remainEscapeTime<0)
                {
                    //����ʱ�����
                    if(isFoundPlayer)
                    { //����׷��״̬
                        enemyBehaviorState = EnemyBehaviorState.CHASE;
                        lerpLookAtTime = 0.618f;
                    }
                    else
                    { //����Ѳ��״̬
                        if (isGuard)
                            enemyBehaviorState = EnemyBehaviorState.GUARD;
                        else
                            enemyBehaviorState = EnemyBehaviorState.PATROL;
                    }
                    //�����ǣ�
                    isEscaping = false;
                }
                else
                {
                    //��ָ����������
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
                                //����Ҳ���Ŀ��㣬��������ܷ���
                                else CheckEscape(true);
                                //��ֹ���￨ס���� 
                                if (agent.velocity.magnitude == 0) agent.velocity = transform.forward;
                            }
                            else CheckEscape(true);
                        }
                    }
                    else //��ֹ����׷��ʱ��ס����
                        if (agent.velocity.magnitude == 0)
                        agent.velocity = agent.speed * transform.forward;
                }

                break;
            case EnemyBehaviorState.CROSS_PLAYER_POS:
                //�ƺ�״̬����ͼԽ�����λ��
                isWalk = false;
                isChase = true;
                idleBlend = 0f;
                remainEscapeTime -= Time.deltaTime * stateFrames;

                if (remainEscapeTime > 0 && Vector3.Distance(wayPoint, transform.position) > agent.stoppingDistance)
                {
                    isFollow = true;
                    //ǰ���ƺ��Ŀ���
                    if (agent.isOnNavMesh)
                    {
                        agent.isStopped = false;
                        if (!agent.pathPending) agent.destination = wayPoint;

                        //��ֹ���￨ס���� 
                        if (agent.velocity.magnitude == 0) agent.velocity = transform.forward;
                    }
                    else //��ֹ����׷��ʱ��ס����
                        if (agent.velocity.magnitude == 0)
                        agent.velocity = agent.speed * transform.forward;

                    //ֹͣ��ʧĿ�꣬���ֶ���ҵ�Ŀ������
                    noFirstLostTarget = false;
                    StopCoroutine(LoseTarget());
                }
                else
                {
                    //����׷��״̬
                    enemyBehaviorState = EnemyBehaviorState.CHASE;
                    isCrossingPlayerPos = false;
                    lerpLookAtTime = 0.618f;
                    //�����ǣ�
                    isEscaping = false;
                }

                break;
            case EnemyBehaviorState.DEAD:
                if(agent.isOnNavMesh) agent.isStopped = true;

                if (!isTurtleShell)
                {
                    coll.enabled = false;

                    //ֱ�ӹر�agent���ᵼ��StopAgent�޷���ȡNavMashAgent����
                    //agent.enabled = false;
                    //�������������radiusΪ0���Ӷ���ҿ��Դ�������Ŀ��
                    agent.radius = 0;

                    if (!isDestroying)
                    {
                        ////�Ƴ����������ϵ����壨�����ʸ��������һͬ������
                        //foreach (var attach in attachObjects)
                        //{
                        //    if (attach)
                        //        attach.transform.SetParent(null);
                        //}
                        Destroy(gameObject, 2f);
                        //��������������
                        GameManager.Instance.InstantiateEnemy(gameObject, gameObject.transform.position, numInArray);

                        isDestroying = true;
                    }
                }
                else
                {
                    //���ڹ�������⴦�������󲻻�������ʧ���ɻ���ײ������Ŀ��
                    if (!isTurtleShellDestroying)
                    {
                        //�Ŵ���ײ�壬���ڹ�Ǹ���������
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
        //���﹥�����м���Խ�����λ��
        if (attackTarget == null || Random.value > characterStats.EnemyCrossPlayerRate) return;


        Vector3 dir = attackTarget.transform.position - transform.position;
        //ƫ�Ƶķ���
        Vector3 bias = Random.onUnitSphere;
        bias.y = 0;
        bias.Normalize();
        //�����ұ����Լ�����֤Խ����ң�����������λ��
        if(Vector3.Dot(transform.forward,attackTarget.transform.forward)>0)
        {
            if (Vector3.Dot(bias, dir.normalized) < 0) bias = -bias;
            //�������60�ȼн��⣬��һ������
            if (Vector3.Dot(bias, dir.normalized) < 0.5f)
                bias = (dir.normalized + bias).normalized;
        }
        //�������շ�λ����
        dir += bias * characterStats.AttackRange * Random.Range(1f, 3f);

        //����ɹ��ҵ��ƶ��㣬���л�״̬
        if(NavMesh.SamplePosition(transform.position + dir, out navMeshHit, characterStats.AttackRange, 1))
        {
            wayPoint = navMeshHit.position;
            enemyBehaviorState = EnemyBehaviorState.CROSS_PLAYER_POS;
            lerpLookAtTime = -1;
            isCrossingPlayerPos = true;

            //�ƶ��ٶ���ʱ������
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

        //���û���泯��ң��򲻹���
        if (Vector3.Dot(transform.forward, direction.normalized) < characterStats.attackData.enemyAtkCosin)
            return;

        //Skill��Attack�Ĵ����߼���Skill���ȴ���
        if (TargetInSkillRange() && lastSkillTime < 0)
        {
            isFollow = false;
            //���������ȴʱ��
            lastSkillTime = characterStats.SkillCoolDowm * Random.value;
            //���ܹ�������
            animator.SetTrigger("Skill");

            if (agent.isOnNavMesh) agent.isStopped = true;
        }
        else
        if (TargetInAttackRange())
        {
            isFollow = false;
            //���ù���ʱ�䣬���������CD����
            if (characterStats.isCritical)
                lastAttackTime = characterStats.AttackCoolDown * Random.Range(1.618f, 2.718f);
            else
                lastAttackTime = characterStats.AttackCoolDown;
            //����������
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

        //���Խ��ת��Խ��
        float lerpSpeed = 0.06f / Mathf.Max(transform.localScale.x, transform.localScale.y);
        lerpSpeed = Mathf.Max(lerpSpeed, 0.006f) * lerpRotationRate;

        transform.rotation = Quaternion.Lerp(transform.rotation, tarRot, lerpSpeed);
    }

    bool FoundPlayer()
    {
        if (findAttacker || isCrossingPlayerPos)
        {
            //�⵽��ҹ����������������
            findAttacker = false;
            return true;
        }

        //Զ����Ŀ��Ҳ�����⣬��ΪCHASE״̬����Ұ�뾶��������ʵ��Ŀ��ֻ����ң�
        playerCols = Physics.OverlapSphere(transform.position, sightRadius * 2, LayerMask.GetMask("Player"));
        foreach (var player in playerCols)
            if (player.CompareTag("Player"))
            {
                RaycastHit hit;
                Vector3 eyePos = eyeBall.transform.position; //�۾�λ��
                Vector3 eyeFoward = GetEyeForward(eyeBall); //�����۾�����
                //�ж������Ƿ��ڵ�
                Vector3 targetDir1 = player.transform.position - transform.position;
                Vector3 targetDir2 = player.transform.position - eyePos;

                float angle = Vector3.Angle(eyeFoward, targetDir1);
                //Debug.Log(gameObject.ToString() + " angle: " + angle + " |  Radius: " + targetDir1.magnitude);

                //Chase״̬�£���������Ұ�ڣ�����1����Ұ��Χ��
                if (enemyBehaviorState == EnemyBehaviorState.CHASE &&
                    (angle > halfSightAngle && targetDir1.magnitude > sightRadius)) break;
                //��Chase״̬�£���������Ұ�ڣ�����1����Ұ��Χ��
                if (enemyBehaviorState != EnemyBehaviorState.CHASE &&
                    (angle > halfSightAngle || targetDir1.magnitude > sightRadius)) break;

                //�����ڵ�������ң�����׷��Ŀ�ꣻ�ڶ���������ͬ�ж�
                if ((Physics.Raycast(eyePos, targetDir1, out hit, sightRadius) &&
                    hit.transform.gameObject.tag == "Player")
                    || (Physics.Raycast(eyePos, targetDir2, out hit, sightRadius) &&
                    hit.transform.gameObject.tag == "Player"))
                {
                    //���Ŀ�꣬����͸��
                    attackTarget = player.gameObject;
                    SwitchInsightLayer();

                    //ֹͣ��ʧĿ�꣬����noFirstLostTarget
                    noFirstLostTarget = false;
                    StopCoroutine("LoseTarget");

                    return true;
                }

                break; //ö�ٵ�Player���Ϳ���ֹͣ��
            }
        //attackTarget = null;
        if (!noFirstLostTarget)
        {
            //ֻ���ڵ�һ�ζ�ʧĿ���ʱ�򣬻����LossTargetЭ��
            noFirstLostTarget = true;
            StartCoroutine("LoseTarget");
        }

        //Ŀ����δ��ʧ
        if (attackTarget != null) return true;

        //Ŀ���Ѷ�ʧ
        return false;
    }

    internal void CheckEscape(bool forceChangeDir = false, bool forceEscape = false)
    {
        //ǿ�ƽ�������״̬
        isEscaping = isEscaping || forceEscape;

        float healthRate = (float)characterStats.CurrentHealth / characterStats.MaxHealth;
        //Ѫ��1/3����ʱ�������������״̬
        if (!isEscaping && healthRate > 0.3f) return;
        //Ѫ���ָ�����Ѫ���ϣ���������״̬
        if (healthRate > 0.5f && !forceEscape)
        { isEscaping = false; return; }

        //�ܹ���ʱһ���������ܣ�����������״̬����ÿ���ܹ������ᴥ��
        if (!isEscaping && Random.value > characterStats.EscapeRate) return;

        //�����������״̬
        enemyBehaviorState = EnemyBehaviorState.ESCAPE;
        isEscaping = true;
        lerpLookAtTime = -1;

        //�趨���ܷ���
        if (forceChangeDir || Random.value < escapeDirChangeRate)
        {
            escapeDirection = Random.onUnitSphere;
            escapeDirection.y = 0;
            escapeDirection.Normalize();

            //��֪��������ģ���֤Զ����ҷ���forceChangeDir���ڱ���ײǽ��
            if (!forceChangeDir && isFoundPlayer &&
                Vector3.Dot(escapeDirection, GameManager.Instance.player.transform.forward) < 0)
                escapeDirection = -escapeDirection;

            //�´θı䳯��ļ��ʷ����仯
            escapeDirChangeRate = Random.Range(0.2f, 0.8f);
        }

        //����Ѫ��ʣ�࣬�ٶ����޻����������½�
        agent.speed = Mathf.Lerp(2f, escapeSpeed, (float)characterStats.CurrentHealth / (characterStats.MaxHealth * 0.3f));

        //��������״̬�ĳ���ʱ��
        if (!forceChangeDir)
            remainEscapeTime = Random.Range(5f, 15f);

        //�����趨���ܣ����ǳ�������
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
            //��ֹ���￨ס���� 
            if (agent.velocity.magnitude == 0) agent.velocity = transform.forward;
        }
        else agent.velocity = agent.speed * transform.forward;
    }

    IEnumerator LoseTarget()
    {
        yield return new WaitForSeconds(lossTargetTime);

        //����lossTargetTime��Ŀ�궪ʧ������͸�Ӳ�
        attackTarget = null;
        SwitchInsightLayer();

    }
    IEnumerator GuardRandomLookAt()
    {
        //���Guardʱ�䣬guardKeepMinTime��1~3��
        yield return new WaitForSeconds(guardKeepMinTime * (1f + Random.value * 2));

        remainLookAtTime = lookAtMinTime * (1f + Random.value);  //���1~2��LookAtʱ��
        if (idleBlend == 0f) idleBlend = Random.value;
        //Guard״̬�£������´��������
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

        //���lookAtʱ��
        remainLookAtTime = lookAtMinTime * Random.value * 2;

        float randomX = Random.Range(-patrolRange, patrolRange);
        float randomZ = Random.Range(-patrolRange, patrolRange);

        Vector3 randomPoint = new Vector3(guardPos.x + randomX,
            transform.position.y, guardPos.z + randomZ);

        if (Vector3.Distance(randomPoint, transform.position) < 2f)
        {
            //��ֹ�ƶ�����̫�̣��Եúܴ�
            randomPoint = (randomPoint - transform.position).normalized
                * Random.Range(0.5f * patrolRange, 1.5f * patrolRange) + transform.position;
        }

        //���������λ�ã�ʹ����Ground��
        if (randomPoint.IsOnGround(out upHighDownHit))
            randomPoint = upHighDownHit.point;
        else
        {
            //��������������ͼ������ҷ������һ������
            randomPoint =
                (randomPoint + GameManager.Instance.player.transform.position) * 0.6f;
        }

        //��ֹNavMeshײǽ��ס������SamplePosition�޸�
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
            // && transform.CatchedTarget(attackTarget.transform)) //����û��ʹ�ý̳������չ����
        {
            var targetStats = attackTarget.GetComponent<CharacterStats>();

            Vector3 direction = attackTarget.transform.position - transform.position;
            //����Attack��Χ��ʧ
            if (direction.magnitude > characterStats.AttackRange)
            {
                //Debug.Log("miss");
                return false;
            }

            direction.Normalize();
            //�ж�����Զ�����
            if (Vector3.Dot(transform.forward, direction) < characterStats.attackData.enemyAtkCosin)
            {
                //Debug.Log("Dodge����");
                NavMeshAgent targetAgent = attackTarget.GetComponent<NavMeshAgent>();
                Vector3 dodgeDir = -transform.forward + attackTarget.transform.forward * 2;
                if(targetAgent.isOnNavMesh) targetAgent.isStopped = true;
                //������ˮ��˥��
                float waterSlow = Mathf.Lerp(1f, 0.5f, AudioManager.Instance.footStepWaterDeep - transform.position.y);
                targetAgent.velocity = dodgeDir.normalized * targetStats.characterData.attackDodgeVel * waterSlow;
                //��Ч
                attackTarget.GetComponent<PlayerController>().PlayDodgeSound(true);

                return false;
            }
            targetStats.TakeDamage(characterStats, targetStats);

            //���������Ч
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
        //��ʤ����
        //ֹͣ�����ƶ�
        //ֹͣAgent
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
        //�رջ�ʤ����
        animator.SetBool("Win", false);

        //���ö����״̬
        playerDead = false;
        attackTarget = null;

        //����Ѳ��״̬
        if (isGuard)
            enemyBehaviorState = EnemyBehaviorState.GUARD;
        else
            enemyBehaviorState = EnemyBehaviorState.PATROL;
    }

    public virtual Vector3 GetEyeForward(GameObject eyeBall)
    {
        return eyeBall.transform.up * -1f;
    }


    //�ڹ�����ר��
    public void ResetTurtleDestroy()
    {
        StopCoroutine("TurtleLaterDestroy");
        if (gameObject.activeSelf)
            StartCoroutine("TurtleLaterDestroy");
    }
    //�ڹ�����ר��
    IEnumerator TurtleLaterDestroy()
    {
        yield return new WaitForSeconds(Random.Range(TurtleDestroyWaitTime, TurtleDestroyWaitTime * 2));

        coll.enabled = false;

        if (!isDestroying)
        {
            Destroy(gameObject, 2f);
            //��������������
            GameManager.Instance.InstantiateEnemy(gameObject, gameObject.transform.position, numInArray);

            isDestroying = true;
        }
    }

    //������صĶ���������ʱ����agent�뾶Ϊ0
    //event
    void SetAgentZeroRadiusWhenJump()
    {
        agent.radius = 0;
    }
    //������صĶ��������ʱ�ָ�agent�뾶
    //event
    void ResetAgentRadiusWhenLanding()
    {
        agent.radius = agentRadius;
    }

    #region Play Sound
    /// <summary>
    /// Animation Event����ҽŲ���
    /// </summary>
    void FootStepSound()
    {
        if (isDead) return; //�޸�����Ų�����bug

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

    //���ﲥ���ܻ���Ч
    internal void PlayHitSound(SoundName enemyHitSound, float pitchAdd = 0, float volumeAdd = 0)
    {
        float scaleBias = 0.1f / transform.localScale.x;
        float pitchBias = scaleBias + pitchAdd;
        //�Ƿ�Ϊ��ʸ
        if (enemyHitSound != SoundName.Enemy_by_Arrow)
            pitchBias -= GameManager.Instance.playerStats.WeaponPitchBias;

        //���⶯�����
        switch (GameManager.Instance.player.GetComponent<PlayerController>().
            weaponWaveSound)
        {
            case SoundName.TwoHandSword_Rush:
            case SoundName.TwoHandAxe_Rush:
            case SoundName.TwoHandMace_HitBack:
                pitchBias -= 0.25f;
                break;
        }

        //�����ܻ���������������������ƫ���෴
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
    /// ���ﲻͬ�������м��ʴ���
    /// float: ���������ļ���
    /// int: ָ�����������ı�ţ�1~3��0��ʾ�������
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
