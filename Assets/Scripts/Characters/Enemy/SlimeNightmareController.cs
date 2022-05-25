using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

//击杀梦魇史莱姆后，会被重置等级为1（会提示玩家）
public class SlimeNightmareController : EnemyController
{
    //想办法让boss在远处显示：设置单独的NavMesh层，不考虑树木、石头遮挡（√）

    //在spine骨骼上添加碰撞体（跟随运动），然后修改相关GetComponent逻辑（Super也需要）
    //起跳相关动画，起跳时设置agent半径为0，落地造成伤害，恢复agent

    //attack01的暴击，有强力击退、强力眩晕

    [Header("Slime Nightmare")]
    public float criticalPushForce = 50f;
    public Transform attack02Center; //attack02做成技能，范围伤害
    public Transform attack02Distance; //attack02的最远距离，用于求范围半径
    public Transform attack02SecondDist; //attack02的二段攻击距离

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

        //确定技能可用距离和范围
        skillMaxRange = (attack02Distance.position - transform.position).magnitude;
        attack02SkillRadius = skillMaxRange - (attack02Center.position - transform.position).magnitude;
        //适当缩小范围，保证命中率
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

        //控制Taunt动画结束，即技能结束
        if(lastTauntAnimTime<0 && isTaunting)
        {
            isTaunting = false;
            animator.SetTrigger("Stop Skill Taunt");
        }
    }

    protected override bool Hit()
    {
        bool hit = base.Hit();

        //attack01的暴击，有强力击退、强力眩晕
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

        //新增Taunt技能，玩家很靠近时才会触发
        if (TargetInTauntRange() && lastTauntTime < 0 && !isTaunting)
        {
            //范围技能，无须看向玩家

            //控制Taunt动画循环时间，到时间手动结束；动画速度为0.7
            lastTauntAnimTime = tauntAnimTime * (int)Random.Range(tauntExitMin, tauntExitMax + 1) / 0.7f;

            //为保证Run状态下Trigger失败时，技能次数不被浪费
            //技能CD重置，放在LandingDamageWhenTaunt()函数中

            animator.SetTrigger("Skill Taunt");
            isTaunting = true;

            if (agent.isOnNavMesh) agent.isStopped = true;
        }

        //Taunt技能中，停止之后判断
        if (isTaunting) return;

        //如果没有面朝玩家，则不攻击（提高这个面朝准确度）
        if (Vector3.Dot(transform.forward, direction) <
            (characterStats.attackData.enemyAtkCosin + 1) * 0.5f)
            return;

        //Skill和Attack的触发逻辑：Skill优先触发
        if (TargetInSkillRange() && lastSkillTime < 0)
        {
            //需要直接看向玩家，保证技能命中
            transform.LookAt(AttackTarget.transform);

            isFollow = false;
            //随机技能冷却时间
            lastSkillTime = characterStats.SkillCoolDowm * Random.Range(1f, 1.618f);
            //技能攻击动画
            animator.SetTrigger("Skill");

            if(agent.isOnNavMesh) agent.isStopped = true;
        }
        else
        if (TargetInAttackRange())
        {
            isFollow = false;
            //随机攻击冷却时间，如果暴击，CD更久
            if (characterStats.isCritical)
                lastAttackTime = characterStats.AttackCoolDown * UnityEngine.Random.Range(1.618f, 2.718f);
            else
                lastAttackTime = characterStats.AttackCoolDown * Random.Range(1f, 1.372f);
            //近身攻击动画
            animator.SetTrigger("Attack");

            if (agent.isOnNavMesh) agent.isStopped = true;
        }
        else
        {
            isFollow = true;
            //无法攻击或技能，则朝玩家移动
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
    //attack02造成目标点为中心的范围伤害和击退，无差别
    void NightmareSkillHit()
    {
        float dmg;
        skillHits =
            Physics.SphereCastAll(attack02Center.position, attack02SkillRadius, transform.forward, 0.1f,
            LayerMask.GetMask("Player", "Enemy", "Enemy Insight"));

        GameObject target = null;
        //对自己以外的所有目标造成伤害
        foreach (RaycastHit hit in skillHits)
        {
            target = target.GetEnemy(hit.collider);
            if (target != gameObject)
            {
                //越中心的目标，受到伤害越多，注意是攻击范围中心
                dmg = characterStats.MinDamage + characterStats.MaxDamage * (1 -
                    Vector3.Distance(attack02Center.position, hit.transform.position) / attack02SkillRadius);
                //伤害为暴击
                dmg *= characterStats.CriticalMultiplier;

                target.GetComponent<CharacterStats>().TakeDamage((int)dmg, attack02Center.position, characterStats);

                //根据伤害决定击退
                target.GetComponent<NavMeshAgent>().velocity =
                    (transform.forward * 3 + (target.transform.position - attack02Center.position).normalized)
                    * dmg * 0.2f;

                if (target.CompareTag("Player"))
                {
                    target.GetComponent<Animator>().SetTrigger("Dizzy");
                    //击中玩家的音效
                    HitPlayerSound(target.GetComponent<CharacterStats>());
                }
                else
                    target.GetComponent<Animator>().SetTrigger("Hit");
            }
        }

        //后撤音效
        AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_SkillDodge, soundDetailList, transform);
    }

    //event
    //attack02复位时的二段伤害
    void NightmareSkillHitSecond()
    {
        //扫描复位路径上的单位
        skillHits = 
            Physics.SphereCastAll(attack02SecondDist.position, attack02SkillRadius, -transform.forward,
            Vector3.Distance(attack02SecondDist.position, transform.position),
            LayerMask.GetMask("Player", "Enemy", "Enemy Insight"));

        GameObject target = null;
        //对自己以外的所有目标造成伤害
        foreach (RaycastHit hit in skillHits)
        {
            target = target.GetEnemy(hit.collider);
            if (target != gameObject)
            {
                target.GetComponent<CharacterStats>().
                    TakeDamage(characterStats.MinDamage, attack02Center.position, characterStats);

                //固定击退速度，起点是二段攻击起点
                target.GetComponent<NavMeshAgent>().velocity =
                    (target.transform.position - attack02Center.position).normalized * attack02SkillRadius;

                target.GetComponent<Animator>().SetTrigger("Hit");
                if (target.CompareTag("Player"))
                {
                    //击中玩家的音效
                    HitPlayerSound(target.GetComponent<CharacterStats>(), -0.1f);
                }
            }

        }

    }

    //RunFWD和Victory动画，每次落地时造成少量伤害和Hit
    //event
    void LandingDamageWhenRun()
    {
        skillHits =
        Physics.SphereCastAll(transform.position, landingDamageRadius, transform.forward, 0.05f,
        LayerMask.GetMask("Player", "Enemy", "Enemy Insight"));

        //对自己以外的所有目标造成伤害
        GameObject target = null;
        foreach (RaycastHit hit in skillHits)
        {
            target = target.GetEnemy(hit.collider);
            if (target != gameObject)
            {
                target.GetComponent<CharacterStats>().TakeDamage(
                    (int)(characterStats.MinDamage * Random.Range(0.382f, 0.618f)), transform.position, characterStats);

                //固定击退速度
                if (target.GetComponent<NavMeshAgent>())
                    target.GetComponent<NavMeshAgent>().velocity =
                        (target.transform.position - transform.position).normalized * landingDamageRadius;

                target.GetComponent<Animator>().SetTrigger("Hit");

                //音效
                if (target.CompareTag("Player"))
                    HitPlayerSound(target.GetComponent<CharacterStats>(), -0.15f);
                else
                    target.GetComponent<EnemyController>().PlayHitSound(SoundName.Unarm_Attack, -0.2f, 0.2f);
            }

        }

    }

    //新增Taunt Skill，动画至少循环3次，即至少六次伤害
    //Taunt动画，每次落地时造成极少伤害和眩晕，持续时间exitTime可随机？
    void LandingDamageWhenTaunt()
    {
        skillHits =
            Physics.SphereCastAll(transform.position, tauntSkillRadius, transform.forward, 0.05f,
            LayerMask.GetMask("Player", "Enemy", "Enemy Insight"));

        //对自己以外的所有目标造成伤害
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
                    //音效
                    if (target.CompareTag("Player"))
                        HitPlayerSound(target.GetComponent<CharacterStats>(), -0.2f);
                }
                else
                {
                    target.GetComponent<Animator>().SetTrigger("Hit");
                    //音效
                    target.GetComponent<EnemyController>().PlayHitSound(SoundName.Unarm_Attack, -0.2f, 0.2f);
                }
            }
        }



        //Debug.Log("Rounds:" + (lastTauntAnimTime / tauntAnimTime * 0.7f) + " lastTauntAnimTime:" + lastTauntAnimTime
        //    + " lastTauntTime:" + lastTauntTime);

        //保证技能触发后，才重置技能冷却时间
        if (lastTauntTime < 0)
            lastTauntTime = tauntCoolDown * Random.Range(0.618f, 1.372f);
    }
}
