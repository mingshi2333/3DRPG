using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;//为了使用NavMeshAgent

public class PlayController : MonoBehaviour
{
    private NavMeshAgent agent;
    private GameObject attackTarget;
    private CharacterStats characterStats;
    private float lastAttackTime;
    private Animator anim;
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();//自身变量调用
        anim = GetComponent<Animator>();
        characterStats=GetComponent<CharacterStats>();
    }
    void Start()
    {
        MouseManager.Instance.OnMouseClicked += MoveToTarget;//订阅
        MouseManager.Instance.OnEnemyClicked += EventAttack;
    }



    void Update()
    {
        SwitchAnimation();
        lastAttackTime -= Time.deltaTime;

    }
    private void SwitchAnimation()
    {
        anim.SetFloat("Speed",agent.velocity.sqrMagnitude);
    }
    public void MoveToTarget(Vector3 target)
    {
        StopAllCoroutines();
        agent.isStopped = false;
        agent.destination = target;
    }
    private void EventAttack(GameObject target)
    {
        if(target !=null)
        {
            attackTarget = target;
            StartCoroutine(MoveToAttackTarget());
        }
    }
    IEnumerator MoveToAttackTarget()
    {
        agent.isStopped = false;
        transform.LookAt(attackTarget.transform);//转向
        while(Vector3.Distance(attackTarget.transform.position,transform.position)>characterStats.attackDate.attackRange)//TODO攻击距离之后设置
        {
            agent.destination = attackTarget.transform.position;
            yield return null;
        }//移动
        agent.isStopped = true;//停下
        if(lastAttackTime < 0)
        {
            anim.SetTrigger("Attack");
            lastAttackTime = 0.5f;
        }
    }
}
