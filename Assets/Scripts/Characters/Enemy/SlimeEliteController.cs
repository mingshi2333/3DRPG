using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeEliteController : EnemyController
{
    //�����м���˲���̣��ܵ��˺��м��ʺ�
    [Header("Slime Elite")]
    [Range(0, 1)] public float criticalDashRate = 0.66f;
    public float criticalDashVel = 25f;
    [Range(0, 1)] public float getHitDashRate = 0.7f;
    public float getHitDashVel = 15f;

    protected override bool Hit()
    {
        bool hit = base.Hit();

        //�����м���˲����
        if (characterStats.isCritical && Random.value < criticalDashRate)
        {
            lerpLookAtTime = 0.382f;

            if(agent.isOnNavMesh) agent.isStopped = true;
            agent.velocity = transform.forward * criticalDashVel * Random.Range(1f, 1.3f);
            //��Ч
            AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_SkillDodge, soundDetailList, transform);
        }

        return hit;
    }

    ///Event
    //�ܵ��˺��м��ʺ�
    void GetHitDash()
    {
        //���嶯��ѧ����ʱ�ɴ�������ֹ����
        if (Random.value < getHitDashRate && rigidBody.isKinematic)
        {
            if(agent.isOnNavMesh) agent.isStopped = true;

            //��ʱ�رո��嶯��ѧ
            rigidBody.isKinematic = false;
            transform.LookAt(AttackTarget.transform.position);

            //�����ҳ��й���ǰ��������󳷣�
            if (GameManager.Instance.playerStats.attackData.isBow)
                rigidBody.velocity = transform.forward * getHitDashVel + transform.up * 5f;
            else
                rigidBody.velocity = -transform.forward * getHitDashVel + transform.up * 5f;

            animator.SetTrigger("WalkBack");
            //��Ч
            AudioManager.Instance.Play3DSoundEffect(SoundName.Enemy_SkillDodge, soundDetailList, transform);

            //һ��ʱ�����
            StartCoroutine(OpenKinematicLater());
        }
    }

    IEnumerator OpenKinematicLater()
    {
        yield return new WaitForSeconds(1f);

        rigidBody.isKinematic = true;
    }


    ///Event
    //�󳷺��м������ҳ���Dizzy�����ϱ�HitҲ�ᴥ��
    void TauntDash()
    {
        if (Random.value < 0.5f)
        {
            //Debug.Log("Right Dash��");

            //���嶯��ѧӦ�û�δ��������ֱ��ʩ����
            rigidBody.velocity = transform.right * getHitDashVel * 0.75f;
            //��Ч
            AudioManager.Instance.Play3DSoundEffect(SoundName.EnemyAttackDodge, soundDetailList, transform, 0, -0.5f);
        }
        else
        {
            //Debug.Log("Left Dash��");

            //���嶯��ѧӦ�û�δ��������ֱ��ʩ����
            rigidBody.velocity = -transform.right * getHitDashVel * 0.75f;
            //��Ч
            AudioManager.Instance.Play3DSoundEffect(SoundName.EnemyAttackDodge, soundDetailList, transform, 0, -0.5f);
        }
    }
}
