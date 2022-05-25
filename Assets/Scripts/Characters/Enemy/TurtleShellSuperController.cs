using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurtleShellSuperController : EnemyController
{
    //普通攻击有吸血效果（√）

    //暴击落空增加暴击概率20%并立即清零CD，命中减少概率15%；超过100%且命中，则重置为20%（√）
    //暴击有15%几率立即触发新暴击，需要修改怪物位置并朝向玩家（√）

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

        //获得AttackData后，才可以赋值
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
                //普通攻击吸血
                characterStats.RestoreHealth(suckPoint);
            }
            else
            {
                //暴击命中，减少概率10%
                characterStats.CriticalChance = Mathf.Clamp(characterStats.CriticalChance - chanceDecreaseWhenHitCrit,
                    defalutCritChance, 1f);

                //Debug.Log("CriticalChance Decrease：" + characterStats.CriticalChance);
            }
        }
        else
        {
            if(characterStats.isCritical)
            {
                //暴击落空增加暴击概率20%；超过100%且命中，则重置为初始
                characterStats.CriticalChance += chanceIncreaseWhenMissCrit;
                if (characterStats.CriticalChance > 1f)
                    characterStats.CriticalChance = hit ? defalutCritChance : 1f;

                //Debug.Log("CriticalChance Increase！" + characterStats.CriticalChance);

                //立即清零CD
                lastAttackTime = -0.1f;
            }
        }

        if (characterStats.isCritical && Random.value < conboCritRate)
        {
            //暴击有15%几率立即触发新暴击
            animator.SetTrigger("Combo Critical");

            //修改怪物位置并朝向玩家
            transform.position = comboStartPoint.position;
            transform.LookAt(AttackTarget.transform);
        }

        return hit;
    }
}
