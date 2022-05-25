using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GruntController : EnemyController
{
    [Header("Skill")]
    public float kickForce = 10;

    private Vector3 direction;

    //Attack2技能攻击的前摇击飞
    public void KickOff()
    {
        if (AttackTarget != null)
        {
            direction = AttackTarget.transform.position - transform.position;
            //超出Skill范围丢失
            if (direction.magnitude > characterStats.SkillRange) return;

            direction.Normalize();

            CharacterStats targetStats = AttackTarget.GetComponent<CharacterStats>();
            //判定击飞丢失（666恶魔之爪，Dot修正.666f）
            if (Vector3.Dot(transform.forward, direction) <
                characterStats.attackData.enemyAtkCosin * 0.666f)
            {
                //Debug.Log("Dodge Grunt KickOff~~");
                return;
            }

            //击飞时修正朝向
            lerpLookAtTime = 0.382f;

            //兽人士兵特性：若本次暴击，则不判定击飞
            if (characterStats.isCritical)
            {
                //技能立即可用
                lastSkillTime = 0f;
                return;
            }

            NavMeshAgent targetAgent = AttackTarget.GetComponent<NavMeshAgent>();
            if(agent.isOnNavMesh) targetAgent.isStopped = true;
            targetAgent.velocity = direction * kickForce;
            //播放受击动画
            AttackTarget.GetComponent<Animator>().SetTrigger("Dizzy");
            //标记玩家被击飞，有撞墙伤害，按速度衰减，速度低于Speed伤害为0
            AttackTarget.GetComponent<PlayerController>().isKickingOff = true;

            //击飞对target造成额外伤害，无视防御
            int damage = Mathf.Max(characterStats.MinDamage, 0);
            targetStats.CurrentHealth = Mathf.Max(targetStats.CurrentHealth - damage, 0);

            //追击效果
            StartCoroutine("SecondPursuit");
        }
    }

    public override Vector3 GetEyeForward(GameObject eyeBall)
    {
        return (eyeBall.transform.forward - eyeBall.transform.right * 0.9f).normalized;
    }

    IEnumerator SecondPursuit()
    {
        yield return new WaitForSeconds(0.123f);
        agent.velocity = direction * kickForce * 1.23f;
        yield return new WaitForSeconds(0.123f);
        agent.velocity = Vector3.zero;
    }
}
