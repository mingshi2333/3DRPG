using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cinemachine;

//�ٶܼ��ܣ������Ҽ��ٶܳ��ƶ����ƶ����������泯����Ŀ�ꣻ����ָ���ͨ�ƶ����൱��ȡ���ٶܣ�
///�ڼ�����90�ȷ�������(�����С������֮��*0.444f)�����ǻᱻ����ѣ�Σ����ܵĻ���������ѣ�Σ�
///�ٶ�ʱ���¿ո�ɳ�Ŀ����г�ײ�����(�����С������֮��*0.333f)�Ĺ����˺���
///���Ƴ�ײ��������������ɹ���ѣ�Σ���Boss��Ч��

//�ܷ����ܷ��ɸ񵲱���(�����С������֮��*0.777f)����ʹĿ���ܻ�
///�ܷ��Է�Ӧ��Ҫ��ܸߣ��߷��ո����棻�ܷ��ԷǱ�����Ч

//����˫������������һϵ������

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

    [HideInInspector]public bool isKickingOff; //�Ƿ�kickOff״̬������ײǽ�˺�
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
        //��ҵ��Զ��Ŀ��ʱ��NavMesh����ǰ���ƶ�����Զ����
        playerNavMeshMoveSize =
            FindObjectOfType<LocalNavMeshBuilder>().m_PlayerNavMeshBoundsSize.x * 0.5f;
    }

    void Start()
    {
        //CinemachineFreeLook��ʼ������仯�ӽǣ�Ϊ�˱�֤����ӽǣ���ʼ�ǹرյ�
        ///�Ƶ�RigisterPlayer���棿
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
            //��������ǰ��λ��
            beforeDodgePos = transform.position;
            //������ˮ��˥��
            float waterSlow = Mathf.Lerp(1f, 0.5f, AudioManager.Instance.footStepWaterDeep - transform.position.y);

            //������ҳֶܳ��/�ֹ�����/��ͨ����
            if (agent.isOnNavMesh) agent.isStopped = true;
            if (characterStats.isHoldShield)
            {
                //��ͬ���ƣ���ײ�ٶȲ�һ��
                if (attackTarget != null && agent.updateRotation == false)
                    agent.velocity =
                        (attackTarget.transform.position - transform.position).normalized
                        * characterStats.ShieldRushVel * waterSlow;
                else
                    agent.velocity = transform.forward * characterStats.ShieldRushVel * waterSlow;

                //��ײ�˺���ֻ�Թ���Ŀ�����Ч��
                sheildRushKeepTime = 0.618f;
                //��Ч
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
                    //�ֹ��������ܣ�����������
                    animator.SetTrigger("BackDodge");
                }
                else
                {
                    //�ֹ���ǰ���ܣ�����������
                    animator.SetTrigger("ForwardDodge");
                }
            }
            else 
            {
                //���ƶ���������
                agent.velocity = transform.forward * characterStats.MoveDodgeVel
                    * characterStats.MoveSlowRate * waterSlow;
                //������Ч
                PlayDodgeSound();
            }

            //����CD����װ�������ӳ�
            lastDodgeTime = characterStats.MoveDodgeCoolDown / characterStats.MoveSlowRate;
        }

        if(Input.GetMouseButton(1) && sheildRushKeepTime > 0 && characterStats.lastSkillTime < 0)
        {
            //����ڼ��ٴΰ��¿ո��ͷŸ�����
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
        //���������֪ͨ���й���
        if (isDead)
            GameManager.Instance.NotifyObserversPlayerDead();

        //�ж��������
        if (isWaitingFire && bowPullTime >= characterStats.MinPullBowTime)
            FireArrow();
        //����������ز���
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
        //ǿ������agent����в��bug
        //if (upHighDownHit.point.y > transform.position.y + 1)
        //{
        //    timeKeepOnGround = 5f;
        //    agent.isStopped = true;
        //}

        StopPushTarget();

        //��ⲥ�ź�����
        CheckSeaAmbientMusic();
    }

    //��ֹ�������ƶ�����Ŀ���Agent
    void StopPushTarget()
    {
        if (attackTarget == null || attackTarget.CompareTag("Attackable")) return;

        float nearby = agent.radius * transform.localScale.x +
            attackTarget.GetComponent<NavMeshAgent>().radius * attackTarget.transform.localScale.x;

        Vector3 dir = attackTarget.transform.position - transform.position;
        float dot = Vector3.Dot(dir.normalized, agent.velocity);
        if (dot > 0 && dir.magnitude < nearby + 0.1f)
        {
            //�۳�Ŀ�귽���ϵ��ƶ��ٶȷ���
            agent.velocity -= dir.normalized * dot;
        }

    }

    private void OnAnimatorIK(int layerIndex)
    {
        //���ݵ�ǰ��ʸ�������ٶȷ������ó���ͷ��IK������/����IK��������ʸ��
        if (characterStats.isAim)
        {
            //agent���ؿ�λ�ã�ע�ⲻ��ģ�͵��ؿڣ���
            Vector3 chest = transform.position + new Vector3(0, 0.7f, 0);
            Vector3 focus = chest + arrowVel;
            //����ͷ��LookAt������Ϊ�ؿڿ�ʼ�ؼ�ʸ�����һ�ξ���
            animator.SetLookAtPosition(focus);
            animator.SetLookAtWeight(bowPullRate, 0.382f, 1f, 0, 0.618f);

            //���ּ�����㣨��������λ�ã�
            Vector3 rightHandFocus = chest + transform.right * 0.18f - transform.forward * 0.33f;

            //����
            Vector3 leftHandFocus = rightHandFocus + arrowVel.normalized *
                Vector3.Distance(leftHand.position, rightHandFocus) * 1.5f
                + transform.right * 0.39f;
            animator.SetIKPosition(AvatarIKGoal.LeftHand, leftHandFocus);
            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1);
            //��
            Transform bow_eq = characterStats.bowSlot.GetChild(0);
            bow_eq.LookAt(leftHand.position - (rightHandFocus * 0.7f + chest * 0.3f)
                 + bow_eq.position);
            //���֣�����λ�ã�
            animator.SetIKPosition(AvatarIKGoal.RightHand,
                bow_eq.GetChild(0).GetChild(0).transform.position
                 + transform.right * 0.07f - transform.forward * 0.07f - transform.up * 0.03f);
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, 1);

            //��ʸ
            //Transform arrow_eq = characterStats.arrowSlot.GetChild(0);
            //Vector3 aPos = rightHand.position + (leftHand.position - rightHand.position) * 0.9f
            //    - transform.right * 0.15f;
            //arrow_eq.position = Vector3.Lerp(arrow_eq.position, aPos, bowPullTime);
            ////��������ʱ�������ü�ʸ����һֱ��ת��ʹ��ʸһ��ʼ�ճ���
            //arrow_eq.rotation =
            //    Quaternion.FromToRotation(Vector3.up, arrow_eq.position - rightHand.position) *
            //    Quaternion.LookRotation(arrow_eq.position - rightHand.position, Vector3.up);
            if (bowPullTime > characterStats.MinPullBowTime)
            {
                //�ڹ�������ʾһ���¼�ʸ���������ּ�ʸ���Ӷ��޸���ʸ����
                bow_eq.GetChild(2)?.gameObject.SetActive(true);
                characterStats.arrowSlot.GetChild(0).gameObject.SetActive(false);
            }
        }
        else
        {
            //��ԭIk���á���������
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
            //���㵱ǰ�����µļ�ʸˮƽ�ٶ�
            bowPullAdditionRate = (bowPullTime - characterStats.MinPullBowTime)
                / (characterStats.MaxPullBowTime - characterStats.MinPullBowTime);
            arrowHoriSpeed = characterStats.MinArrowSpeed +
                (characterStats.MaxArrowSpeed - characterStats.MinArrowSpeed) * bowPullAdditionRate;
        }
        //��ǰ��ʸ�ٶȼӳ�
        arrowHoriSpeed *= characterStats.arrowItem.itemData.arrowSpeedChangeRate;

        arrowStartPos = characterStats.arrowSlot.position;
        if (attackTarget != null && !attackTarget.CompareTag("Attackable")
            && !attackTarget.GetComponent<EnemyController>().isDead)
        {
            //�ϴ�ĵ�λCollider���������������
            Vector3 targetCenter; 
            if (attackTarget.GetComponent<Collider>() == null)
                targetCenter = attackTarget.GetComponentInChildren<Collider>().bounds.center;
            else
                targetCenter = attackTarget.GetComponent<Collider>().bounds.center;

            if (Vector3.Distance
            (transform.position, attackTarget.transform.position) < characterStats.LongSightRadius)
            {
                //��LongSightRadius��Χ����Ŀ�꣬������׼�ؿ�
                aimPos = targetCenter;
            }
            else
            {
                //��Χ����Ŀ�꣬��Ҫ������׼�����ٶ���������
                Vector3 aimDir = targetCenter - arrowStartPos;
                Vector3 tempDir = transform.forward * characterStats.MinArrowSpeed;

                aimPos = arrowStartPos + Vector3.Slerp(tempDir, aimDir,
                    Vector3.Dot(tempDir.normalized, aimDir.normalized));
                transform.LookAt(aimPos);
            }
        }
        else //������׼��ʱ����ʸˮƽ�ƶ���λ��
            aimPos = arrowStartPos + transform.forward * characterStats.MinArrowSpeed;

        //��������Ŀ�������ʱ��
        ///��agent�ؿ�Ϊ��㣬��ֹ���￿�������⣨ע�ⲻ��ģ�͵��ؿڣ�
        Vector3 chest = transform.position + new Vector3(0, 0.7f, 0);
        Vector3 direction = aimPos - chest;
        Vector3 horiDir = new Vector3(direction.x, 0, direction.z);
        float t = horiDir.magnitude / arrowHoriSpeed;

        //Ԥ�����һ��ʱ����ƶ�λ�ã��ʵ�����������ǹ���ת��
        if (attackTarget!=null && !attackTarget.CompareTag("Attackable") &&
            !attackTarget.GetComponent<EnemyController>().isDead)
        {
            NavMeshAgent targetAgent = attackTarget.GetComponent<NavMeshAgent>();
            aimPos += targetAgent.velocity * UnityEngine.Random.Range(t * 0.5f, t);
            //��������
            direction = aimPos - chest;
            horiDir = new Vector3(direction.x, 0, direction.z);
            t = horiDir.magnitude / arrowHoriSpeed;
        }

        Vector3 horiVel = horiDir.normalized * arrowHoriSpeed;
        Vector3 vertVel = Vector3.up * (direction.y / t - Physics.gravity.y * t * 0.5f);

        //��ü�ʸ�ٶ�����
        arrowVel = horiVel + vertVel;
    }

    //�����ʸ
    void FireArrow()
    {
        if (characterStats.arrowItem == null) return;

        //�ж�����
        characterStats.isCritical = UnityEngine.Random.value < characterStats.CriticalChance;
        //��ʸ���ɺͷ���
        int damage =  characterStats.FireArrow(arrowVel, bowPullAdditionRate);

        //��ɫ�͹��Ķ���Trigger
        animator.SetTrigger("Fire");
        characterStats.bowSlot.GetChild(0).GetComponent<Animator>().SetTrigger("Fire");

        //�����˺��۳��������;�
        InventoryManager.Instance.ReduceBowDurability(damage, arrowVel);

        //����CD����ز���
        lastAttackTime = characterStats.ArrowReloadCoolDown;
        isWaitingFire = false;
        characterStats.isAim = false;
        bowPullTime = 0;
        agent.speed = agentSpeed * characterStats.MoveSlowRate;
    }


    private void CheckLeftWeapon()
    {
        //����״̬�£����ȡ��������������ʸ����agent��ֹͣ�ƶ�
        if (Input.GetMouseButtonDown(0) && characterStats.isAim)
        {
            characterStats.isAim = false;
            agent.speed = agentSpeed * characterStats.MoveSlowRate;
            isCancelBowPull = true;
        }

        //��ס�Ҽ�ʱ���ٶܣ����������ʹ��������
        if (Input.GetMouseButtonDown(0) && Input.GetMouseButton(1))
        {
            if(characterStats.lastSkillTime<0)
                characterStats.WeaponSkillData?.MainSkill();
        }

        //�ٶ�ȡ���������ʸ
        if (Input.GetMouseButtonUp(1))
        {
            //�ȴ������ʸ��
            if (characterStats.isAim) isWaitingFire = true;

            //��ԭ״̬
            isCancelBowPull = false;
            characterStats.isHoldShield = false;
            agent.speed = agentSpeed * characterStats.MoveSlowRate;
            if(agent.isOnNavMesh)agent.isStopped = true;
        }
        //�ٶܣ��ܷ���ʱ
        if (Input.GetMouseButtonDown(1))
        {
            if (characterStats.attackData.isBow)
            {
                //�����ϼ������ʧ�ܱ�������û�м�ʸ
                if (lastAttackTime < 0 && characterStats.EquipArrow())
                {
                    characterStats.isAim = true;
                    isWaitingFire = false;
                    bowPullTime = 0;
                    agent.speed = agentSpeed * 0.3f * characterStats.MoveSlowRate;
                }
                else //����Ҳ�����ʸ������ر�EventAttack�����ʱ����
                    characterStats.isAim = false;
            }
            else
            {
                //���öܷ��ɴ���ʱ��
                if (!characterStats.isHoldShield)
                    characterStats.sheildHitBackWaitTime = 0.5f;

                characterStats.isHoldShield = true;
                agent.speed = agentSpeed * 0.3f * characterStats.MoveSlowRate;
            }
        }
        //��ֹcd�ڼ����簴������ʧ�ܵ����
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
        //�ٶ�״̬�£��������Ŀ��Ͻ���δ�������򱣳��泯
        if (characterStats.isHoldShield && attackTarget != null
            && dir.magnitude < characterStats.characterData.outSightRadius && !isDead
           && (attackTarget.CompareTag("Attackable") || !attackTarget.GetComponent<EnemyController>().isDead
           || attackTarget.GetComponent<EnemyController>().isTurtleShell))
        {
            agent.updateRotation = false;
            lookAtPos = attackTarget.transform.position;
            //��Ϊagent���رգ��޷��Զ�վֱ���趨LookAt����ˮƽ
            lookAtPos.y = transform.position.y;
            transform.LookAt(lookAtPos);
        }
        else 
        //�ֹ�״̬�£��������Ŀ����longSight����δ�������򱣳��泯
        if(characterStats.isAim && attackTarget!=null
            && dir.magnitude < characterStats.characterData.longSightRadius && !isDead
            && !attackTarget.CompareTag("Attackable") && !attackTarget.GetComponent<EnemyController>().isDead)
        {
            agent.updateRotation = false;
            lookAtPos = attackTarget.transform.position;
            //��Ϊagent���رգ��޷��Զ�վֱ���趨LookAt����ˮƽ
            lookAtPos.y = transform.position.y;
            transform.LookAt(lookAtPos);
        }
        else
            agent.updateRotation = true;

        //�ٶܻ�ֹ�״̬�£����ƺͺ��ƾ��в�ͬ�̶ȵļ���
        if(characterStats.isHoldShield || characterStats.isAim)
        {
            float sideSpeedChange =
                Vector3.Dot(transform.forward, agent.velocity.normalized) * 0.1f;

            agent.speed = agentSpeed * (0.25f + sideSpeedChange) * characterStats.MoveSlowRate;
        }
    }

    internal void SetPlayerOnGround()
    {
        //����yֵ����ֹ��ҳ����ڼв������agent����ʧ��
        if (transform.position.IsOnGround(out upHighDownHit))
        {
            //if (upHighDownHit.point.y < transform.position.y) timeKeepOnGround = 5f;
            if (//timeKeepOnGround > 0 || 
                upHighDownHit.point.y > transform.position.y+0.05f)
                transform.position = new Vector3(transform.position.x,
                    upHighDownHit.point.y, transform.position.z);

            //�����жϣ���ֹ���볡��ʱλ���޷�����
            if (agent.isOnNavMesh) agent.enabled = true;
            else
                transform.position = transform.position - transform.position.normalized * 0.1f;
        }
    }

    private void SwitchInsightTargets()
    {
        //����InsightRadius�ڵĹ���Թ�����isInsightedByPlayer
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
        //ֻ�Թ����Boss�����޸�
        if (newTarget.tag != "Enemy" || newTarget.tag != "Boss") return;

        //attackTarget������Թ�����isLastAttackTarget
        if (lastTarget != null)
            lastTarget.GetComponent<EnemyController>().isTargetByPlayer = false;

        EnemyController newEnemy = null;
        newEnemy = newEnemy.GetEnemy(newTarget);

        newEnemy.isTargetByPlayer = true;

        lastTarget = newEnemy.gameObject;
    }

    private void SwitchKinematic()
    {
        //���ڶ�̬�л�����Ķ���ѧ���أ��Ӷ�ʵ�ֶ�ʯͷ����ľ��Npc����ײ
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
            //�ٶȱ�С��ֹͣ�����ж�
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
                //�����ڼ䣬����ҳ�����ģ������Ч������������˺�
                if (!isKickingOff) return;
                isKickingOff = false;

                int dmg = (int)(agent.velocity.magnitude * characterStats.HitTreeDamagePerVel);
                characterStats.TakeDamage(dmg, other.transform.position);
                //Debug.Log("Hit Tree Damage��" + (dmg - characterStats.CurrentDefence));

                //����Ϊ����ٶȵķ���˥�������ƣ�
                dir = (transform.position - other.gameObject.transform.position).normalized
                    + agent.velocity.normalized * 0.618f;
                agent.velocity = dir * agent.velocity.magnitude * 0.618f;

                //ײ����Ч
                AudioManager.Instance.Play3DSoundEffect(SoundName.Player_HitTree,
                    AudioManager.Instance.otherSoundDetailList, transform);
                break;
            case "Bush":
                //������ľ����٣����κ�ʱ�򣬰������ɣ�
                agent.velocity *= characterStats.EnterBushSpeedRate;
                break;

            case "Portal":
                //��ֹ��Խ�����ź󣬻���Ŀ����۷�
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
                //���رյĴ����ţ�û������
                //����̫����Ҳû����������ֹ����
                if (other.GetComponent<TransitionPoint>().transitionType
                    == TransitionPoint.TransitionType.Off)
                    return;

                dir = other.gameObject.transform.position - transform.position;

                //����ڴ����Ŵ����ܵ�������
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
                //�Ƿ����ڱ������ڼ䣬�ж�ײǽ�˺�
                if (!isKickingOff) return;
                isKickingOff = false;

                int dmg = (int)(agent.velocity.magnitude * characterStats.HitRockDamagePerVel);
                characterStats.TakeDamage(dmg, collision.transform.position);

                //ײ����ʯ��Ч
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
        //�����ײ�Ϲ���ʱ����٣�������������ƿ������С�Ĺ���
        switch (tag)
        {
            case "Enemy":
            case "Boss":
                //���ƶ�״̬�²��жϣ���ֹ����
                if(Vector3.Distance(agent.destination,transform.position)<=0.15f)
                {
                    if(agent.isOnNavMesh) agent.destination = transform.position;
                    break;
                }

                //�����ٶȸ��ݵ������������˥���������ٶȲ���
                dir = collision.gameObject.transform.position - transform.position;
                dot = Vector3.Dot(dir.normalized, agent.velocity);
                if (dot <= 0) break;

                normalVel = dir.normalized * dot;
                tangentVel = agent.velocity - normalVel;
                size = collision.collider.bounds.size;
                normalVel *= (0.2f / Mathf.Max(size.x * size.y * size.z, 0.2f));

                agent.velocity = normalVel + tangentVel;

                //���Ŀ�꣬��֤����ܹ�������Ŀ��
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
        //��ײ�˺���ֻ�Թ���Ŀ�����Ч��
        if (sheildRushKeepTime < 0 || attackTarget == null || isDead) return;

        //���Ǿٶ�ײ�������������룻����Ϊ��������
        float delta = characterStats.attackData.hasEquipShield ? 0.3f : characterStats.AttackRange;

        switch (attackTarget.tag)
        {
            case "Enemy":
            case "Boss":
                targetAgent = attackTarget.GetComponent<NavMeshAgent>();
                //�����Ŀ��ܽӽ�
                if (Vector3.Distance(transform.position, attackTarget.transform.position)
                    < agent.radius + targetAgent.radius * attackTarget.transform.localScale.x + delta
                    && transform.CatchedTarget(attackTarget.transform.position))
                {
                    sheildRushKeepTime = -1; //ֻ�ᴥ��һ��
                    //���û�гֶܣ��򴥷�RushAttack������
                    if(!characterStats.attackData.hasEquipShield)
                    {
                        animator.SetTrigger("RushAttack");
                        //����CD
                        lastAttackTime = characterStats.AttackCoolDown;

                        //���治ִ��
                        break;
                    }

                    EnemyController ec = attackTarget.GetComponent<EnemyController>();
                    Vector3 force;
                    if (ec.isTurtleShell && ec.isDead)
                    {
                        //��ײ�ڹ��
                        //���㱩�����ܳ�ı����ʸ��ߣ�
                        characterStats.isCritical =
                            UnityEngine.Random.value < characterStats.CriticalChance * 1.5f;
                        //������ɵ��������Ǳ�����
                        force = transform.forward *
                            (characterStats.ShieldRushHitForce + UnityEngine.Random.Range(0, agentSpeed));

                        if (characterStats.isCritical) 
                            force *= characterStats.ShieldCritMult;

                        ec.ResetTurtleDestroy();

                        //�رն���ѧ���Ӷ�ģ������Ч������������ת��
                        Rigidbody rb = attackTarget.GetComponent<Rigidbody>();
                        rb.isKinematic = false;
                        //�رս��ٶ�����
                        rb.maxAngularVelocity = 999f;
                        //��Hit()������û��ʩ����ת����

                        //��agent���м��٣��ڹ��Ư�Ƹ�˳��~
                        targetAgent.velocity += force;

                        //���Ƴ�ײ��Ч
                        ShieldRushHitSound();

                        //���治ִ��
                        break;
                    }

                    ///��ײ��ɶ��ƵĹ����˺�
                    int dmg = characterStats.ShieldDamage;
                    //���㱩�����ܳ�ı����ʸ��ߣ�
                    characterStats.isCritical =
                        UnityEngine.Random.value < characterStats.ShieldCritChan;
                    if (characterStats.isCritical)
                        dmg = (int)(dmg * characterStats.ShieldCritMult);

                    var targetStats = attackTarget.GetComponent<CharacterStats>();
                    targetStats.TakeDamage(dmg, transform.position, characterStats);

                    //�۳������;ã����ݹ�������
                    InventoryManager.Instance.ReduceSheildDurability(dmg,
                        targetStats.transform.position - transform.position);

                    //���ﱻ���ƻ��ˣ��̶�1.5��
                    force = transform.forward * characterStats.ShieldRushHitForce;
                    if (characterStats.isCritical) force *= 1.5f;
                    targetAgent.velocity += force;

                    //���Ƴ�ײ��������������ɹ���ѣ��
                    if (attackTarget.tag == "Enemy")
                    {
                        if (characterStats.isCritical)
                            attackTarget.GetComponent<Animator>().SetTrigger("Dizzy");
                        else
                            attackTarget.GetComponent<Animator>().SetTrigger("Hit");
                    }
                    else if (attackTarget.tag == "Boss")
                    {
                        ///��Bossֻ����ͨ�ܻ�
                        if (characterStats.isCritical)
                            attackTarget.GetComponent<Animator>().SetTrigger("Hit");
                    }

                    //���Ƴ�ײ��Ч
                    ShieldRushHitSound();
                }
                break;
            case "Attackable": //��ײʯͷ
                Vector3 size = attackTarget.GetComponent<MeshCollider>().bounds.size;

                if (Vector3.Distance(transform.position, attackTarget.transform.position)
                    < agent.radius + attackTarget.transform.localScale.x * 
                    Mathf.Max(size.x, size.y, size.z) + delta
                    && transform.CatchedTarget(attackTarget.transform.position))
                {
                    sheildRushKeepTime = -1; //ֻ�ᴥ��һ��

                    //���û�гֶܣ��򴥷�RushAttack������
                    if (!characterStats.attackData.hasEquipShield)
                    {
                        animator.SetTrigger("RushAttack");
                        //����CD
                        lastAttackTime = characterStats.AttackCoolDown;

                        //���治ִ��
                        break;
                    }

                    //����ֱ�ӵ��ù����߼���ע����Ч��
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
        //˫���������ƶ�����������β
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
            //��ɫ�͹����л���׼����
            animator.SetBool("IsAim", characterStats.isAim);
            characterStats.bowSlot.GetChild(0).GetComponent<Animator>().
                SetBool("IsAim", characterStats.isAim);

            //ͨ��bowPullRate���ƶ��������ĳ̶�
            if (characterStats.isAim)
            {
                //ֻ����׼������ʱ��Ÿ��£���ֹ���ʱrate˲�����
                bowPullRate = bowPullTime / characterStats.MaxPullBowTime;
            }
            animator.SetFloat("BowPullRate", bowPullRate);
            characterStats.bowSlot.GetChild(0).GetComponent<Animator>().
                SetFloat("BowPullRate", bowPullRate);
        }

        //�ֶܡ�����ʱ���ƶ�����
        if (characterStats.isHoldShield || characterStats.isAim)
        {
            float holdSheildSpeed = agent.velocity.sqrMagnitude;
            //����й���Ŀ�꣬��ʼ���泯��������ܵ���
            if (attackTarget != null &&
                Vector3.Dot(attackTarget.transform.position - transform.position, agent.velocity) < 0)
                holdSheildSpeed = -holdSheildSpeed;

            animator.SetFloat("HoldSheild Speed", holdSheildSpeed);
        }
    }

    //Animation Event
    void HoldBowBackDodge()
    {
        //������ˮ��˥��
        float waterSlow = Mathf.Lerp(1f, 0.5f, AudioManager.Instance.footStepWaterDeep - transform.position.y);
        //�ֹ���������
        agent.velocity = (transform.position - attackTarget.transform.position).normalized
            * characterStats.MoveDodgeVel * characterStats.MoveSlowRate * 1.2f * waterSlow;

        PlayRollDodgeSound();
    }


    //Animation Event
    void HoldBowForwadDodge()
    {
        //������ˮ��˥��
        float waterSlow = Mathf.Lerp(1f, 0.5f, AudioManager.Instance.footStepWaterDeep - transform.position.y);
        //�ֹ���ǰ����
        agent.velocity = transform.forward * characterStats.MoveDodgeVel
            * characterStats.MoveSlowRate * 0.8f * waterSlow;

        PlayRollDodgeSound();
    }
    //Animation Event
    void BackDodgeEnd()
    {
        //����������ܣ��ָ�agent���ٶ�
        backDodging = false;
        agent.angularSpeed = agentAngularSpeed;
    }

    public void MoveToTarget(Vector3 target)
    {
        StopAllCoroutines();
        //����ע�⣬�ָ���ת�ٶ�
        if (!backDodging)
            agent.angularSpeed = agentAngularSpeed;

        //��ֹѰ·���٣���ֹ���˻����ƶ�
        if (agent.pathPending || !agent.isOnNavMesh || isDead) return;

        agent.stoppingDistance = stopDistance;
        if(agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.destination = DestinationInNavMesh(target);
        }
        //���û��й���
        attackNear = 1f;
    }

    private void EventAttack(GameObject target)
    {
        if (isDead || lastAttackTime > 0) return; //��ֹ���˻����ƶ�����ֹ����

        if (target != null)
        {
            //���boss������Ծ�������˶����Ƚϴ�ĵ�λ
            //Collider��������������ϣ�������target��parent�㼶
            if (!target.CompareTag("Attackable")) target = target.GetEnemy();

            attackTarget = target;
            isNearAttackTarget = false;

            characterStats.isCritical =
                UnityEngine.Random.value < characterStats.CriticalChance;
            //�ָ���ת�ٶ�
            agent.angularSpeed = agentAngularSpeed;

            //��ֹ������ٶ�/��������๥��һ��
            if (Input.GetMouseButtonDown(1))
            {
                if (characterStats.attackData.isBow)
                    characterStats.isAim = true;
                else
                    characterStats.isHoldShield = true;
            }

            //�ر�֮ǰ��Э�̣�
            StopCoroutine(MoveToAttackTarget());
            StartCoroutine(MoveToAttackTarget());
        }
    }

    IEnumerator MoveToAttackTarget()
    {
        if (agent.isOnNavMesh) agent.isStopped = false;

        float startDis = Vector3.Distance(attackTarget.transform.position, transform.position);
        //������ʼ���룬������������λ��
        if (attackNear == 1f)
        {
            attackNear = startDis > characterStats.AttackRange * 2 ?
                0.382f : (startDis > characterStats.AttackRange ? 0.618f : 1f);
        }


        float distLimit = 0;
        if (attackTarget.CompareTag("Enemy")|| attackTarget.CompareTag("Boss"))
        {
            //��ֹ��Һ͹���agent�뾶̫�󣬵���������Ź��ﻬ��������
            targetAgent = attackTarget.GetComponent<NavMeshAgent>();
            distLimit = targetAgent.radius * attackTarget.transform.localScale.x +
                agent.radius * transform.localScale.x + 0.05f;

            agent.stoppingDistance = characterStats.AttackRange * attackNear;
            //��ʼ������Χ
            startAtkDistance = Mathf.Max(distLimit + characterStats.AttackRange,
                characterStats.AttackRange * (2f - attackNear));

        }
        else
        {
            //���Attackable�����壬����bound�������
            Vector3 size = attackTarget.GetComponent<Collider>().bounds.size;
            Vector3 scale = attackTarget.GetComponent<Transform>().localScale;
            float targetWidth = Mathf.Max(size.x * scale.x, size.y * scale.y, size.z * scale.z);
            
            agent.stoppingDistance = stopDistance;
            startAtkDistance = Mathf.Max(characterStats.AttackRange, targetWidth);
        }

        //���������޸Ĺ�����Χ����
        //���й�������
        while (attackTarget != null && Vector3.Distance(attackTarget.transform.position, transform.position)
            > startAtkDistance && !isNearAttackTarget)
        {
            if (!agent.pathPending && agent.isOnNavMesh)
                agent.destination = DestinationInNavMesh(attackTarget.transform.position);
            yield return null;
        }

        //����Ǿٶ�/����������ֱ�ӽ���
        if (characterStats.isHoldShield || characterStats.isAim)
        {
            if(agent.isOnNavMesh) agent.isStopped = true;
            attackNear = 1f;
            yield break;
        }

        //Attack
        if (lastAttackTime < 0)
        {
            //��ɫ����Ŀ��
            if (attackTarget != null)
                transform.LookAt(attackTarget.transform);

            animator.SetBool("Critical", characterStats.isCritical);
            animator.SetTrigger("Attack");

            //Debug.Log("isNearAttackTarget:" + isNearAttackTarget +
            //    "  Distance:" + Vector3.Distance(attackTarget.transform.position, transform.position));

            //������ȴʱ�䣬���������CD����
            if (characterStats.isCritical)
                lastAttackTime = characterStats.AttackCoolDown * UnityEngine.Random.Range(1.372f, 1.618f);
            else
                lastAttackTime = characterStats.AttackCoolDown;
        }

        //��������
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
    /// ��ҹ����¼�
    /// Animation Event
    /// Float������dotThreshold��������Χһ���cosֵ��0��ʾ�����90��
    /// Int������middleAngle���ж������������forward֮��ĽǶȣ�˳ʱ��360��
    /// </summary>
    /// <param name="animationEvent"></param>
    void Hit(AnimationEvent animationEvent = null)
    {
        //Ŀ����ܱ�����
        if (attackTarget == null) return;

        if (attackTarget.CompareTag("Attackable"))
        {
            //����ʯͷ��
            if (attackTarget.GetComponent<Rock>())
            {
                Rock rock = attackTarget.GetComponent<Rock>();
                //ֻ�ܷ���HitNothing״̬
                if (rock.rockState != Rock.RockState.HitNothing) return;
                rock.rockState = Rock.RockState.HitEnemy;
                //���ݹ�����
                rock.attacker = characterStats;


                //ֹͣ����ʯͷ
                rock.StopCoroutine("LaterDestroy");
                rock.isDestroying = false;

                Rigidbody rb = attackTarget.GetComponent<Rigidbody>();
                //��ֹ�ٶ�0����HitNothing
                rb.velocity = Vector3.one;

                //������ɵ���������Impulse����ʯͷ���������Ǳ�����
                Vector3 force = transform.forward * UnityEngine.Random.Range(20f, 30f);
                    //+ transform.up * UnityEngine.Random.Range(2f, 3f);
                if (characterStats.isCritical)
                {
                    force *= characterStats.CriticalMultiplier;
                }

                rb.AddForce(force, ForceMode.Impulse);

                //��Ч
                rock.PlayHitSound(enemyHitSound);
            }
        }
        else
        {
            EnemyController ec = attackTarget.GetComponent<EnemyController>();
            if (ec.isTurtleShell && ec.isDead)
            {
                //������ɵ��������Ǳ�����
                Vector3 force = transform.forward * UnityEngine.Random.Range(15f, 20f);
                if (characterStats.isCritical)
                {
                    //Debug.Log("TurtleShell Critical��");
                    force *= characterStats.CriticalMultiplier;
                }
                ec.ResetTurtleDestroy();

                //�رն���ѧ���Ӷ�ģ������Ч������������ת��
                Rigidbody rb = attackTarget.GetComponent<Rigidbody>();
                rb.isKinematic = false;
                //�رս��ٶ�����
                rb.maxAngularVelocity = 999f;
                //���ڹ����ת������
                rb.AddTorque(Vector3.down * force.magnitude * ec.turtleShellTorqueRate, ForceMode.VelocityChange);
                //Debug.Log("AddTorque��" + Mathf.Min(rb.maxAngularVelocity, force.magnitude * ec.turtleShellTorqueRate));

                //��agent���м��٣��ڹ��Ư�Ƹ�˳��~
                attackTarget.GetComponent<NavMeshAgent>().velocity += force;

                attackTarget.GetComponent<EnemyController>().PlayHitSound(enemyHitSound);
            }
            else
            {
                //����ҹ����������ǹ������룬�����ǹ����Ƕ�
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
                //��Ѫ��
                if (characterStats.WeaponSkillData != null)
                    characterStats.WeaponSkillData.WeaponSkillAfterAttack();
                //��Ч
                targetStats.GetComponent<EnemyController>().PlayHitSound(enemyHitSound);

                //˫�������������2/3�����˺�
                if(characterStats.attackData.isTwoHand)
                {
                    dmg = (int)(dmg * 0.666f);

                    slashEnemyCols = Physics.OverlapSphere(transform.position, characterStats.AttackRange,
                        LayerMask.GetMask("Enemy", "Enemy Insight"));
                    foreach (var col in slashEnemyCols)
                    {
                        targetStats = targetStats.GetEnemy(col);

                        //������ҪĿ��
                        if (targetStats.gameObject == attackTarget) continue;

                        targetStats.TakeDamage(dmg, transform.position, characterStats);
                        //��Ч
                        targetStats.GetComponent<EnemyController>().PlayHitSound(enemyHitSound);

                        //�Ƿ����
                        if (characterStats.isCritical && UnityEngine.Random.value < targetStats.HitByCritRate)
                            targetStats.GetComponent<Animator>().SetTrigger("Hit");

                        //��������Ч��
                        if (characterStats.AttackHitForce > 0)
                        {
                            targetStats.GetComponent<NavMeshAgent>().velocity +=
                                (characterStats.isCritical ? 1.5f : 1f) * characterStats.AttackHitForce
                                * (targetStats.transform.position - transform.position).normalized;
                        }
                    }
                }

                //��ʧ�������л���Ч��
                if(characterStats.arrowItem?.itemData!=null && !characterStats.isAim && characterStats.isCritical)
                {
                    attackTarget.GetComponent<NavMeshAgent>().velocity += 0.27f * 
                        characterStats.arrowItem.itemData.arrowMaxPushForce * transform.forward;

                    //��ʸ�𻵼���Ϊ���ʱ��1/3
                    if(UnityEngine.Random.value < characterStats.arrowItem.itemData.arrowBrokenRate *0.3f)
                        InventoryManager.Instance.BrokenEquipArrow(dmg, transform.forward);
                }
            }
        }

        //����֮��ֹͣ����Ŀ�꣡
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

    //���������λ��ΪNavMesh��Χ�ڣ���֤��ҵ��Զ��ʱ�ܹ��ƶ�
    Vector3 DestinationInNavMesh(Vector3 target)
    {
        Vector3 dir = target - transform.position;

        //�������NavMesh��������Ŀ���λ��
        if (dir.magnitude > playerNavMeshMoveSize)
            dir = dir.normalized * playerNavMeshMoveSize;

        return transform.position + dir;
    }

    #region PlaySound
    /// <summary>
    /// Animation Event����ҽŲ���
    /// 0:Walk,  1:Run
    /// </summary>
    void FootStepSound(int walkOrRun)
    {
        //����߶� - 1 = ˮ����û�߶�
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
            //�Ų���
            AudioManager.Instance.Play3DSoundEffect(
                (walkOrRun == 0) ? SoundName.FootStep_Walk_Grass : SoundName.FootStep_Run_Grass,
                AudioManager.Instance.playerFootStepSoundDetailList, transform, -bias1 * 0.5f, -bias1);
            //��ˮ��
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
            //������
            AudioManager.Instance.Play3DSoundEffect(SoundName.Dodge,
                AudioManager.Instance.playerFootStepSoundDetailList, transform,
                    -bias1 * 0.3f + (autoDodge ? -0.2f : 0f), -bias1 * 0.5f);
            //��ˮ��
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
    /// �������ͣ�Unarm, Sword, Axe, TwoHandSword, TwoHandAxe, TwoHandMace, Bow, Arrow
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
                    //��ʧ����
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

    //Animation Agent����
    public void PlayBowPullSound()
    {
        currentWeaponSound = 
        AudioManager.Instance.Play3DSoundEffect(SoundName.Bow_Pull,
            AudioManager.Instance.weaponSoundDetailList, characterStats.bowSlot, characterStats.WeaponPitchBias);
    }

    void CheckSeaAmbientMusic()
    {
        //�ж�������0�㣨�������ģ��ľ��룬�͸߶ȣ��жϺ���������С����֪ͨ��������
        Vector3 horiDir = transform.position;
        float heightToSeaPlane = horiDir.y + 12;
        horiDir.y = 0;
        AudioManager.Instance.SeaAmbientVolume(horiDir.magnitude, heightToSeaPlane); ;
    }
    #endregion
}
