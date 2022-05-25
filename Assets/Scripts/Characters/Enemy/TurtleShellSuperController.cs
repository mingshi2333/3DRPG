using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurtleShellSuperController : EnemyController
{
    //��ͨ��������ѪЧ�����̣�

    //����������ӱ�������20%����������CD�����м��ٸ���15%������100%�����У�������Ϊ20%���̣�
    //������15%�������������±�������Ҫ�޸Ĺ���λ�ò�������ң��̣�

    [Header("TurtleShell Super")]
    public float chanceIncreaseWhenMissCrit = 0.2f;
    public float chanceDecreaseWhenHitCrit = 0.15f;
    public float conboCritRate = 0.15f;
    public float normalAtkSuckBloodRate = 0.3f;
    public Transform comboStartPoint;

    private float defalutCritChance;

    protected override void Start()
    {
        base.Start();

        //���AttackData�󣬲ſ��Ը�ֵ
        StartCoroutine("SaveDefalutCritChance");
    }

    IEnumerator SaveDefalutCritChance()
    {
        yield return new WaitForSeconds(1f);

        defalutCritChance = characterStats.CriticalChance;
    }

    protected override bool Hit()
    {
        bool hit = base.Hit();
        if(hit)
        {
            if (!characterStats.isCritical)
            {
                int suckPoint = (int)(characterStats.lastAttackRealDamage * normalAtkSuckBloodRate);
                //��ͨ������Ѫ
                characterStats.RestoreHealth(suckPoint);
            }
            else
            {
                //�������У����ٸ���10%
                characterStats.CriticalChance = Mathf.Clamp(characterStats.CriticalChance - chanceDecreaseWhenHitCrit,
                    defalutCritChance, 1f);

                //Debug.Log("CriticalChance Decrease��" + characterStats.CriticalChance);
            }
        }
        else
        {
            if(characterStats.isCritical)
            {
                //����������ӱ�������20%������100%�����У�������Ϊ��ʼ
                characterStats.CriticalChance += chanceIncreaseWhenMissCrit;
                if (characterStats.CriticalChance > 1f)
                    characterStats.CriticalChance = hit ? defalutCritChance : 1f;

                //Debug.Log("CriticalChance Increase��" + characterStats.CriticalChance);

                //��������CD
                lastAttackTime = -0.1f;
            }
        }

        if (characterStats.isCritical && Random.value < conboCritRate)
        {
            //������15%�������������±���
            animator.SetTrigger("Combo Critical");

            //�޸Ĺ���λ�ò��������
            transform.position = comboStartPoint.position;
            transform.LookAt(AttackTarget.transform);
        }

        return hit;
    }
}
