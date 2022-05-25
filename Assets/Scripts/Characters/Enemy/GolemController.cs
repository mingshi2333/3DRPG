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
        // && transform.CatchedTarget(attackTarget.transform)) //����û��ʹ�ý̳������չ����
        {
            var targetStats = AttackTarget.GetComponent<CharacterStats>();

            Vector3 direction = AttackTarget.transform.position - transform.position;
            //����SkillRange��Χ��ʧ
            if (direction.magnitude > characterStats.SkillRange)
                return;

            direction.Normalize();
            NavMeshAgent targetAgent = AttackTarget.GetComponent<NavMeshAgent>();

            //ʯͷ�ˣ��ж����ɶ�ʧ������Զ�����
            if (Vector3.Dot(transform.forward, direction) <
                (characterStats.attackData.enemyAtkCosin + 1f) * 0.5f)
            {
                Vector3 dodgeDir = -transform.forward + AttackTarget.transform.forward * 2;

                if(targetAgent.isOnNavMesh) targetAgent.isStopped = true;
                //������ˮ��˥��
                float waterSlow = Mathf.Lerp(1f, 0.5f, AudioManager.Instance.footStepWaterDeep - transform.position.y);
                targetAgent.velocity = dodgeDir.normalized * targetStats.AttackDodgeVel * waterSlow;

                //��Ч
                targetAgent.GetComponent<PlayerController>().PlayDodgeSound(true);
                return;
            }


            //����ʱ��������
            lerpLookAtTime = 0.382f;

            //��ʱ�ر�agent��ת
            AttackTarget.GetComponent<PlayerController>().StopAngularSpeedShortTime(1f);

            if(targetAgent.isOnNavMesh)targetAgent.isStopped = true;
            targetAgent.velocity = (characterStats.isCritical ? 1.5f : 1f) * kickForce * direction;
            //�����ܻ�����
            AttackTarget.GetComponent<Animator>().SetTrigger("Dizzy");

            //ʯͷ�˻��ɶ�target��ɵ����˺������Ǳ���
            float damage = (characterStats.MinDamage + characterStats.MaxDamage) * 0.5f;
            if (characterStats.isCritical)
                damage *= characterStats.CriticalMultiplier;
            targetStats.TakeDamage((int)damage, transform.position, characterStats);
            //������Ч
            HitPlayerSound(targetStats);

            //�����ұ����ɣ���ײǽ�˺������ٶ�˥�����ٶȵ���Speed�˺�Ϊ0
            AttackTarget.GetComponent<PlayerController>().isKickingOff = true;
        }
    }

    //Animation Event
    public void ThrowRock()
    {
        //��������Ƕȵ���ʯ
        Vector3 randRot = new Vector3(Random.value, Random.value, Random.value);
        var rock = Instantiate(rockPrefab, handPos.position, Quaternion.FromToRotation(Vector3.up, randRot));
        rock.GetComponent<Rock>().SetDamage(characterStats.MinDamage, characterStats.MaxDamage);

        if (AttackTarget!=null)
            rock.GetComponent<Rock>().target = AttackTarget;
        
        //���ݹ�����
        rock.GetComponent<Rock>().attacker = characterStats;
    }

    public override Vector3 GetEyeForward(GameObject eyeBall)
    {
        return (eyeBall.transform.forward + eyeBall.transform.up * 0.3f).normalized;
    }

    //AttackCoolDown�����SkillCoolDown�̶�
    protected override void Attack()
    {
        lerpLookAtTime = 0.382f;

        //Skill��Attack�Ĵ����߼���Skill���ȴ���
        if (TargetInSkillRange() && lastSkillTime < 0)
        {
            //�̶�������ȴʱ�䣬���������CD����
            if (characterStats.isCritical)
                lastSkillTime = characterStats.SkillCoolDowm * Random.Range(1.372f, 1.618f);
            else
                lastSkillTime = characterStats.SkillCoolDowm;
            //���ܹ�������
            animator.SetTrigger("Skill");
        }
        else
        if (TargetInAttackRange())
        {
            //���������ȴʱ��
            lastAttackTime = characterStats.AttackCoolDown * Random.Range(0.3f, 1.7f);
            //����������
            animator.SetTrigger("Attack");
            //�����������stoppingDistance��Ϊ���ƶ��������
            agent.stoppingDistance = Random.Range(characterStats.AttackRange, stopDistance);
        }
    }
}
