using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeEliteController : EnemyController
{
    //暴击有几率瞬间冲刺，受到伤害有几率后撤
    [Header("Slime Elite")]
    [Range(0, 1)] public float criticalDashRate = 0.66f;
    public float criticalDashVel = 25f;
    [Range(0, 1)] public float getHitDashRate = 0.7f;
    public float getHitDashVel = 15f;

    protected override bool Hit()
    {
        bool hit = base.Hit();

        //暴击有几率瞬间冲刺
        if (characterStats.isCritical && Random.value < criticalDashRate)
        {
            lerpLookAtTime = 0.382f;

            if(agent.isOnNavMesh) agent.isStopped = true;
            agent.velocity = transform.forward * criticalDashVel * Random.Range(1f, 1.3f);
            //音效
            AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_SkillDodge, soundDetailList, transform);
        }

        return hit;
    }

    ///Event
    //受到伤害有几率后撤
    void GetHitDash()
    {
        //刚体动力学开启时可触发，防止鬼畜
        if (Random.value < getHitDashRate && rigidBody.isKinematic)
        {
            if(agent.isOnNavMesh) agent.isStopped = true;

            //暂时关闭刚体动力学
            rigidBody.isKinematic = false;
            transform.LookAt(AttackTarget.transform.position);

            //如果玩家持有弓，前进；否则后撤！
            if (GameManager.Instance.playerStats.attackData.isBow)
                rigidBody.velocity = transform.forward * getHitDashVel + transform.up * 5f;
            else
                rigidBody.velocity = -transform.forward * getHitDashVel + transform.up * 5f;

            animator.SetTrigger("WalkBack");
            //音效
            AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_SkillDodge, soundDetailList, transform);

            //一段时间后开启
            StartCoroutine(OpenKinematicLater());
        }
    }

    IEnumerator OpenKinematicLater()
    {
        yield return new WaitForSeconds(1f);

        rigidBody.isKinematic = true;
    }


    ///Event
    //后撤后有几率左、右撤；Dizzy后马上被Hit也会触发
    void TauntDash()
    {
        if (Random.value < 0.5f)
        {
            //Debug.Log("Right Dash！");

            //刚体动力学应该还未开启，可直接施加力
            rigidBody.velocity = transform.right * getHitDashVel * 0.75f;
            //音效
            AudioManager.Instance.Play3DSoundEffect(SoundName.EnemyAttackDodge, soundDetailList, transform, 0, -0.5f);
        }
        else
        {
            //Debug.Log("Left Dash！");

            //刚体动力学应该还未开启，可直接施加力
            rigidBody.velocity = -transform.right * getHitDashVel * 0.75f;
            //音效
            AudioManager.Instance.Play3DSoundEffect(SoundName.EnemyAttackDodge, soundDetailList, transform, 0, -0.5f);
        }
    }
}
