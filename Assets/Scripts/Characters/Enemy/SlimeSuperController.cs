using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SlimeSuperController : EnemyController
{
    [Header("Slime Super")]
    public float criticalPushForce = 25f;
    public float superDashSpeed = 60f;
    public float superDashAccel = 300f;
    public float superDashDuringTime = 1.5f;

    private float acceleration;

    protected override void Awake()
    {
        base.Awake();

        acceleration = agent.acceleration;
    }

    //击退+短暂眩晕，非暴击效果减半
    protected override bool Hit()
    {
        bool hit = base.Hit();
        if (hit && AttackTarget != null)
        {
            if (characterStats.isCritical)
            {
                AttackTarget.GetComponent<PlayerController>().StopAngularSpeedShortTime(1f);

                AttackTarget.GetComponent<NavMeshAgent>().velocity = 
                    criticalPushForce * Random.Range(1, 1.372f) * transform.forward;
                AttackTarget.GetComponent<Animator>().SetTrigger("Dizzy");
            }
            else
            {
                AttackTarget.GetComponent<PlayerController>().StopAngularSpeedShortTime(0.5f);

                AttackTarget.GetComponent<NavMeshAgent>().velocity = 
                    criticalPushForce * Random.Range(0.382f, 0.618f) * transform.forward;
                AttackTarget.GetComponent<Animator>().SetTrigger("Hit");
            }
        }

        return hit;
    }

    //有远距离朝玩家冲刺的技能
    //Animation Event
    void SlimeSuperDash()
    {
        StartCoroutine(ChangeSpeedDuringDash());
    }

    IEnumerator ChangeSpeedDuringDash()
    {
        if (AttackTarget != null)
            transform.LookAt(AttackTarget.transform);

        //稍微停顿一会儿，心理准备时间
        yield return new WaitForSeconds(0.382f);

        //音效
        AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_SkillDodge, soundDetailList, transform);

        agent.speed = superDashSpeed;
        agent.acceleration = superDashAccel;

        yield return new WaitForSeconds(superDashDuringTime);
         
        agent.speed = agentSpeed;
        agent.acceleration = acceleration;
    }

    //Event
    void FastHit()
    {
        bool hit = base.Hit();
        if (AttackTarget == null) return;

        //冲刺后攻击有击退效果
        AttackTarget.GetComponent<PlayerController>().StopAngularSpeedShortTime(0.5f);

        if (Vector3.Distance(transform.position, AttackTarget.transform.position) < 7f)
        {
            AttackTarget.GetComponent<NavMeshAgent>().velocity = transform.forward * criticalPushForce;
            AttackTarget.GetComponent<Animator>().SetTrigger("Dizzy");
        }
    }
}
