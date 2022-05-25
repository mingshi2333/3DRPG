using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//��ɱ����ʷ��ķ�󣬻ᱻ���õȼ�Ϊ1������ʾ��ң�
public class SlimeNightmareController : EnemyController
{
    //��취��boss��Զ����ʾ�����õ�����NavMesh�㣬��������ľ��ʯͷ�ڵ����̣�

    //��spine�����������ײ�壨�����˶�����Ȼ���޸����GetComponent�߼���SuperҲ��Ҫ��
    //������ض���������ʱ����agent�뾶Ϊ0���������˺����ָ�agent

    //attack01�ı�������ǿ�����ˡ�ǿ��ѣ��

    [Header("Slime Nightmare")]
    public float criticalPushForce = 50f;
    public Transform attack02Center; //attack02���ɼ��ܣ���Χ�˺�
    public Transform attack02Distance; //attack02����Զ���룬������Χ�뾶
    public Transform attack02SecondDist; //attack02�Ķ��ι�������

    [Header("Nightmare Taunt Skill")]
    public float tauntCoolDown = 30f;
    public int tauntExitMin = 3;
    public int tauntExitMax = 5;
    public AnimationClip tauntAnimation;

    float skillMaxRange;
    float skillMinRange;
    float attack02SkillRadius;
    float landingDamageRadius;
    float tauntSkillRadius;

    private GameObject spineObject;
    private float lastTauntTime;
    private float tauntAnimTime;
    private float lastTauntAnimTime;
    private bool isTaunting;
    private RaycastHit[] skillHits;

    protected override void Awake()
    {
        base.Awake();

        //ȷ�����ܿ��þ���ͷ�Χ
        skillMaxRange = (attack02Distance.position - transform.position).magnitude;
        attack02SkillRadius = skillMaxRange - (attack02Center.position - transform.position).magnitude;
        //�ʵ���С��Χ����֤������
        skillMaxRange -= 1.5f;
        skillMinRange = skillMaxRange - (attack02SkillRadius - 1.5f) * 2;

        landingDamageRadius = attack02SkillRadius - 1.5f;
        tauntSkillRadius = attack02SkillRadius + 3f;

        spineObject = GetComponentInChildren<BoxCollider>().gameObject;

        tauntAnimTime = tauntAnimation.length;

    }

    protected override void Start()
    {
        base.Start();

        characterStats.SkillRange = skillMaxRange;
    }

    protected override void Update()
    {
        base.Update();

        UpdateTauntTime();
    }

    private void UpdateTauntTime()
    {
        lastTauntTime -= Time.deltaTime;
        lastTauntAnimTime -= Time.deltaTime;

        //����Taunt���������������ܽ���
        if(lastTauntAnimTime<0 && isTaunting)
        {
            isTaunting = false;
            animator.SetTrigger("Stop Skill Taunt");
        }
    }

    protected override bool Hit()
    {
        bool hit = base.Hit();

        //attack01�ı�������ǿ�����ˡ�ǿ��ѣ��
        if (hit && characterStats.isCritical)
        {
            AttackTarget.GetComponent<NavMeshAgent>().isStopped = true;

            AttackTarget.GetComponent<NavMeshAgent>().velocity =
                transform.forward * criticalPushForce * Random.Range(1, 1.372f);

            AttackTarget.GetComponent<Animator>().SetTrigger("Strong Dizzy");
        }

        return hit;
    }

    protected override void Attack()
    {
        lerpLookAtTime = 0.382f;

        Vector3 direction = AttackTarget.transform.position - transform.position;
        direction.Normalize();

        //����Taunt���ܣ���Һܿ���ʱ�Żᴥ��
        if (TargetInTauntRange() && lastTauntTime < 0 && !isTaunting)
        {
            //��Χ���ܣ����뿴�����

            //����Taunt����ѭ��ʱ�䣬��ʱ���ֶ������������ٶ�Ϊ0.7
            lastTauntAnimTime = tauntAnimTime * (int)Random.Range(tauntExitMin, tauntExitMax + 1) / 0.7f;

            //Ϊ��֤Run״̬��Triggerʧ��ʱ�����ܴ��������˷�
            //����CD���ã�����LandingDamageWhenTaunt()������

            animator.SetTrigger("Skill Taunt");
            isTaunting = true;

            if (agent.isOnNavMesh) agent.isStopped = true;
        }

        //Taunt�����У�ֹ֮ͣ���ж�
        if (isTaunting) return;

        //���û���泯��ң��򲻹������������泯׼ȷ�ȣ�
        if (Vector3.Dot(transform.forward, direction) <
            (characterStats.attackData.enemyAtkCosin + 1) * 0.5f)
            return;

        //Skill��Attack�Ĵ����߼���Skill���ȴ���
        if (TargetInSkillRange() && lastSkillTime < 0)
        {
            //��Ҫֱ�ӿ�����ң���֤��������
            transform.LookAt(AttackTarget.transform);

            isFollow = false;
            //���������ȴʱ��
            lastSkillTime = characterStats.SkillCoolDowm * Random.Range(1f, 1.618f);
            //���ܹ�������
            animator.SetTrigger("Skill");

            if(agent.isOnNavMesh) agent.isStopped = true;
        }
        else
        if (TargetInAttackRange())
        {
            isFollow = false;
            //���������ȴʱ�䣬���������CD����
            if (characterStats.isCritical)
                lastAttackTime = characterStats.AttackCoolDown * UnityEngine.Random.Range(1.618f, 2.718f);
            else
                lastAttackTime = characterStats.AttackCoolDown * Random.Range(1f, 1.372f);
            //����������
            animator.SetTrigger("Attack");

            if (agent.isOnNavMesh) agent.isStopped = true;
        }
        else
        {
            isFollow = true;
            //�޷��������ܣ�������ƶ�
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.destination = AttackTarget.transform.position;
            }
        }
    }

    private bool TargetInTauntRange()
    {
        if (AttackTarget != null)
            return Vector3.Distance(AttackTarget.transform.position, transform.position) < tauntSkillRadius - 1.5f;
        else
            return false;
    }

    protected override bool TargetInSkillRange()
    {
        if (AttackTarget != null)
        {
            float dist = Vector3.Distance(AttackTarget.transform.position, transform.position);
            return (dist > skillMinRange && dist < skillMaxRange-1f);
        }
        else
            return false;
    }

    //event
    //attack02���Ŀ���Ϊ���ĵķ�Χ�˺��ͻ��ˣ��޲��
    void NightmareSkillHit()
    {
        float dmg;
        skillHits =
            Physics.SphereCastAll(attack02Center.position, attack02SkillRadius, transform.forward, 0.1f,
            LayerMask.GetMask("Player", "Enemy", "Enemy Insight"));

        GameObject target = null;
        //���Լ����������Ŀ������˺�
        foreach (RaycastHit hit in skillHits)
        {
            target = target.GetEnemy(hit.collider);
            if (target != gameObject)
            {
                //Խ���ĵ�Ŀ�꣬�ܵ��˺�Խ�࣬ע���ǹ�����Χ����
                dmg = characterStats.MinDamage + characterStats.MaxDamage * (1 -
                    Vector3.Distance(attack02Center.position, hit.transform.position) / attack02SkillRadius);
                //�˺�Ϊ����
                dmg *= characterStats.CriticalMultiplier;

                target.GetComponent<CharacterStats>().TakeDamage((int)dmg, attack02Center.position, characterStats);

                //�����˺���������
                target.GetComponent<NavMeshAgent>().velocity =
                    (transform.forward * 3 + (target.transform.position - attack02Center.position).normalized)
                    * dmg * 0.2f;

                if (target.CompareTag("Player"))
                {
                    target.GetComponent<Animator>().SetTrigger("Dizzy");
                    //������ҵ���Ч
                    HitPlayerSound(target.GetComponent<CharacterStats>());
                }
                else
                    target.GetComponent<Animator>().SetTrigger("Hit");
            }
        }

        //����Ч
        AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_SkillDodge, soundDetailList, transform);
    }

    //event
    //attack02��λʱ�Ķ����˺�
    void NightmareSkillHitSecond()
    {
        //ɨ�踴λ·���ϵĵ�λ
        skillHits = 
            Physics.SphereCastAll(attack02SecondDist.position, attack02SkillRadius, -transform.forward,
            Vector3.Distance(attack02SecondDist.position, transform.position),
            LayerMask.GetMask("Player", "Enemy", "Enemy Insight"));

        GameObject target = null;
        //���Լ����������Ŀ������˺�
        foreach (RaycastHit hit in skillHits)
        {
            target = target.GetEnemy(hit.collider);
            if (target != gameObject)
            {
                target.GetComponent<CharacterStats>().
                    TakeDamage(characterStats.MinDamage, attack02Center.position, characterStats);

                //�̶������ٶȣ�����Ƕ��ι������
                target.GetComponent<NavMeshAgent>().velocity =
                    (target.transform.position - attack02Center.position).normalized * attack02SkillRadius;

                target.GetComponent<Animator>().SetTrigger("Hit");
                if (target.CompareTag("Player"))
                {
                    //������ҵ���Ч
                    HitPlayerSound(target.GetComponent<CharacterStats>(), -0.1f);
                }
            }

        }

    }

    //RunFWD��Victory������ÿ�����ʱ��������˺���Hit
    //event
    void LandingDamageWhenRun()
    {
        skillHits =
        Physics.SphereCastAll(transform.position, landingDamageRadius, transform.forward, 0.05f,
        LayerMask.GetMask("Player", "Enemy", "Enemy Insight"));

        //���Լ����������Ŀ������˺�
        GameObject target = null;
        foreach (RaycastHit hit in skillHits)
        {
            target = target.GetEnemy(hit.collider);
            if (target != gameObject)
            {
                target.GetComponent<CharacterStats>().TakeDamage(
                    (int)(characterStats.MinDamage * Random.Range(0.382f, 0.618f)), transform.position, characterStats);

                //�̶������ٶ�
                if (target.GetComponent<NavMeshAgent>())
                    target.GetComponent<NavMeshAgent>().velocity =
                        (target.transform.position - transform.position).normalized * landingDamageRadius;

                target.GetComponent<Animator>().SetTrigger("Hit");

                //��Ч
                if (target.CompareTag("Player"))
                    HitPlayerSound(target.GetComponent<CharacterStats>(), -0.15f);
                else
                    target.GetComponent<EnemyController>().PlayHitSound(SoundName.Unarm_Attack, -0.2f, 0.2f);
            }

        }

    }

    //����Taunt Skill����������ѭ��3�Σ������������˺�
    //Taunt������ÿ�����ʱ��ɼ����˺���ѣ�Σ�����ʱ��exitTime�������
    void LandingDamageWhenTaunt()
    {
        skillHits =
            Physics.SphereCastAll(transform.position, tauntSkillRadius, transform.forward, 0.05f,
            LayerMask.GetMask("Player", "Enemy", "Enemy Insight"));

        //���Լ����������Ŀ������˺�
        GameObject target = null;
        foreach (RaycastHit hit in skillHits)
        {
            target = target.GetEnemy(hit.collider);
            if (target != gameObject)
            {
                target.GetComponent<CharacterStats>().TakeDamage(
                    (int)(characterStats.MinDamage * Random.Range(0.230f, 0.382f)), transform.position, characterStats);

                if (target.CompareTag("Player"))
                {
                    target.GetComponent<Animator>().SetTrigger("Dizzy");
                    //��Ч
                    if (target.CompareTag("Player"))
                        HitPlayerSound(target.GetComponent<CharacterStats>(), -0.2f);
                }
                else
                {
                    target.GetComponent<Animator>().SetTrigger("Hit");
                    //��Ч
                    target.GetComponent<EnemyController>().PlayHitSound(SoundName.Unarm_Attack, -0.2f, 0.2f);
                }
            }
        }



        //Debug.Log("Rounds:" + (lastTauntAnimTime / tauntAnimTime * 0.7f) + " lastTauntAnimTime:" + lastTauntAnimTime
        //    + " lastTauntTime:" + lastTauntTime);

        //��֤���ܴ����󣬲����ü�����ȴʱ��
        if (lastTauntTime < 0)
            lastTauntTime = tauntCoolDown * Random.Range(0.618f, 1.372f);
    }
}
