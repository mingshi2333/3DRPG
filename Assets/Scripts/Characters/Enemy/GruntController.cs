using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GruntController : EnemyController
{
    [Header("Skill")]
    public float kickForce = 10;

    private Vector3 direction;

    //Attack2���ܹ�����ǰҡ����
    public void KickOff()
    {
        if (AttackTarget != null)
        {
            direction = AttackTarget.transform.position - transform.position;
            //����Skill��Χ��ʧ
            if (direction.magnitude > characterStats.SkillRange) return;

            direction.Normalize();

            CharacterStats targetStats = AttackTarget.GetComponent<CharacterStats>();
            //�ж����ɶ�ʧ��666��ħ֮צ��Dot����.666f��
            if (Vector3.Dot(transform.forward, direction) <
                characterStats.attackData.enemyAtkCosin * 0.666f)
            {
                //Debug.Log("Dodge Grunt KickOff~~");
                return;
            }

            //����ʱ��������
            lerpLookAtTime = 0.382f;

            //����ʿ�����ԣ������α��������ж�����
            if (characterStats.isCritical)
            {
                //������������
                lastSkillTime = 0f;
                return;
            }

            NavMeshAgent targetAgent = AttackTarget.GetComponent<NavMeshAgent>();
            if(agent.isOnNavMesh) targetAgent.isStopped = true;
            targetAgent.velocity = direction * kickForce;
            //�����ܻ�����
            AttackTarget.GetComponent<Animator>().SetTrigger("Dizzy");
            //�����ұ����ɣ���ײǽ�˺������ٶ�˥�����ٶȵ���Speed�˺�Ϊ0
            AttackTarget.GetComponent<PlayerController>().isKickingOff = true;

            //���ɶ�target��ɶ����˺������ӷ���
            int damage = Mathf.Max(characterStats.MinDamage, 0);
            targetStats.CurrentHealth = Mathf.Max(targetStats.CurrentHealth - damage, 0);

            //׷��Ч��
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
