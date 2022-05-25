using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GolemController : EnemyController
{
    [Header("Skill")]
    public float kickForce = 25;
    public GameObject rockPrefab;
    public Transform handPos;


    //Animation Event
    public void KickOff()
    {
        if (AttackTarget != null)
        // && transform.CatchedTarget(attackTarget.transform)) //这里没有使用教程里的扩展方法
        {
            var targetStats = AttackTarget.GetComponent<CharacterStats>();

            Vector3 direction = AttackTarget.transform.position - transform.position;
            //超出SkillRange范围丢失
            if (direction.magnitude > characterStats.SkillRange)
                return;

            direction.Normalize();
            NavMeshAgent targetAgent = AttackTarget.GetComponent<NavMeshAgent>();

            //石头人：判定击飞丢失，玩家自动闪避
            if (Vector3.Dot(transform.forward, direction) <
                (characterStats.attackData.enemyAtkCosin + 1f) * 0.5f)
            {
                Vector3 dodgeDir = -transform.forward + AttackTarget.transform.forward * 2;

                if(targetAgent.isOnNavMesh) targetAgent.isStopped = true;
                //闪避涉水的衰减
                float waterSlow = Mathf.Lerp(1f, 0.5f, AudioManager.Instance.footStepWaterDeep - transform.position.y);
                targetAgent.velocity = dodgeDir.normalized * targetStats.AttackDodgeVel * waterSlow;

                //音效
                targetAgent.GetComponent<PlayerController>().PlayDodgeSound(true);
                return;
            }


            //击飞时修正朝向
            lerpLookAtTime = 0.382f;

            //暂时关闭agent旋转
            AttackTarget.GetComponent<PlayerController>().StopAngularSpeedShortTime(1f);

            if(targetAgent.isOnNavMesh)targetAgent.isStopped = true;
            targetAgent.velocity = (characterStats.isCritical ? 1.5f : 1f) * kickForce * direction;
            //播放受击动画
            AttackTarget.GetComponent<Animator>().SetTrigger("Dizzy");

            //石头人击飞对target造成单独伤害，考虑暴击
            float damage = (characterStats.MinDamage + characterStats.MaxDamage) * 0.5f;
            if (characterStats.isCritical)
                damage *= characterStats.CriticalMultiplier;
            targetStats.TakeDamage((int)damage, transform.position, characterStats);
            //命中音效
            HitPlayerSound(targetStats);

            //标记玩家被击飞，有撞墙伤害，按速度衰减，速度低于Speed伤害为0
            AttackTarget.GetComponent<PlayerController>().isKickingOff = true;
        }
    }

    //Animation Event
    public void ThrowRock()
    {
        //生成随机角度的岩石
        Vector3 randRot = new Vector3(Random.value, Random.value, Random.value);
        var rock = Instantiate(rockPrefab, handPos.position, Quaternion.FromToRotation(Vector3.up, randRot));
        rock.GetComponent<Rock>().SetDamage(characterStats.MinDamage, characterStats.MaxDamage);

        if (AttackTarget!=null)
            rock.GetComponent<Rock>().target = AttackTarget;
        
        //传递攻击者
        rock.GetComponent<Rock>().attacker = characterStats;
    }

    public override Vector3 GetEyeForward(GameObject eyeBall)
    {
        return (eyeBall.transform.forward + eyeBall.transform.up * 0.3f).normalized;
    }

    //AttackCoolDown随机，SkillCoolDown固定
    protected override void Attack()
    {
        lerpLookAtTime = 0.382f;

        //Skill和Attack的触发逻辑：Skill优先触发
        if (TargetInSkillRange() && lastSkillTime < 0)
        {
            //固定技能冷却时间，如果暴击，CD更久
            if (characterStats.isCritical)
                lastSkillTime = characterStats.SkillCoolDowm * Random.Range(1.372f, 1.618f);
            else
                lastSkillTime = characterStats.SkillCoolDowm;
            //技能攻击动画
            animator.SetTrigger("Skill");
        }
        else
        if (TargetInAttackRange())
        {
            //随机攻击冷却时间
            lastAttackTime = characterStats.AttackCoolDown * Random.Range(0.3f, 1.7f);
            //近身攻击动画
            animator.SetTrigger("Attack");
            //攻击后才重置stoppingDistance；为了移动的随机性
            agent.stoppingDistance = Random.Range(characterStats.AttackRange, stopDistance);
        }
    }
}
