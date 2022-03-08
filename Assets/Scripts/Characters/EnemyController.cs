using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
public enum EnemyStates {GUARD,PATROL,CHASE,DEAD}
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    // Start is called before the first frame update
    private EnemyStates enemyStates;
    private NavMeshAgent agent;
    private Animator anim;
    [Header("Basic Settings")]
    public float sightRadius;
    public bool isGuard;
    private float speed;
    public float lookAtTime;
    private float remainLookAtTime;
    private GameObject attackTarget;
    private CharacterStats characterStats;
    private float lastAttackTime;
    [Header("Patrol State")]
    private Vector3 wayPoint;
    private Vector3 guardPos;
    public float patrolRange;
    //bool配合动画
    bool isWalk;
    bool isChase;
    bool isFollow;
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();  
        speed = agent.speed;
        anim=GetComponent<Animator>();
        guardPos= transform.position;
        remainLookAtTime=lookAtTime;
        characterStats=GetComponent<CharacterStats>();
    }
    void Start()
    {
        if(isGuard)
        {
            enemyStates = EnemyStates.GUARD;
        }
        else
        {
            enemyStates = EnemyStates.PATROL;
            GetNewWayPoint();
        }
    }

    void Update()
    {
        SwitchStates();
        SwitchAnimation(); 
        lastAttackTime -=Time.deltaTime;
        
    }    
    void SwitchAnimation()
    {
        anim.SetBool("Walk",isWalk);
        anim.SetBool("Chase",isChase);
        anim.SetBool("Follow",isFollow);
        anim.SetBool("Critical",characterStats.isCritical);
    }
    void SwitchStates()
    {
        //如果发现player 切换到CHASE
        if(FoundPlayer())
        {
            enemyStates=EnemyStates.CHASE;
        }
        switch (enemyStates)
        {
            case EnemyStates.GUARD:
                break;
            case EnemyStates.PATROL:
                isChase = false;
                agent.speed = 0.5f*speed;
                //判断是否走到巡逻点
                if(Vector3.Distance(wayPoint,transform.position)<= agent.stoppingDistance)
                {
                    isWalk = false;
                    if(remainLookAtTime>0)
                        remainLookAtTime-=Time.deltaTime;
                    else
                    {
                        GetNewWayPoint();
                    }

                }
                else
                {
                    isWalk = true;
                    agent.destination = wayPoint;
                }
                
                break;
            case EnemyStates.CHASE:
                isWalk = false;
                isChase = true;
                agent.speed = speed;
                //追Player
                if(!FoundPlayer())
                {
                    //TODO:拉脱回到上一个状态
                    isFollow = false;
                    if(remainLookAtTime>0)
                    {
                        agent.destination = transform.position;
                        remainLookAtTime -= Time.deltaTime;
                    }
                    else if(isGuard)
                        enemyStates=EnemyStates.GUARD;
                    else
                        enemyStates=EnemyStates.PATROL; 
                }
                else
                {
                    isFollow=true;
                    agent.isStopped=false;
                    agent.destination=attackTarget.transform.position;
                }

            //TODO:在攻击范围攻击
                if(TargetInAttackRange()||TargetInSkillRange())
                {
                    isFollow = false;
                    agent.isStopped =true;
                    if(lastAttackTime<0)
                    {
                        lastAttackTime=characterStats.attackDate.coolDown;
                        //暴击判断
                        characterStats.isCritical = Random.value<characterStats.attackDate.criticalChance;
                        Attack();
                        //执行攻击
                    }

                }
            //TODO:配合动画
                break;
            case EnemyStates.DEAD:
                break;
        }
    }
    void Attack()
    {
        transform.LookAt(attackTarget.transform);
        if(TargetInAttackRange())
        {
            //进程动画
            anim.SetTrigger("Attack");
        }
        if(TargetInSkillRange())
        {
            //远程动画
            anim.SetTrigger("Skill");
        }
    }
    bool FoundPlayer()
    {
        var colliders = Physics.OverlapSphere(transform.position,sightRadius);
        foreach(var target in colliders)
        {
            if(target.CompareTag("Player"))
            {
                attackTarget=target.gameObject;
                return true;
            }
        }
        attackTarget=null;
        return false;
    }//寻找player
    bool TargetInAttackRange()
    {
        if(attackTarget!=null)
            return Vector3.Distance(attackTarget.transform.position,transform.position)<=characterStats.attackDate.attackRange;
        else 
            return false;
    }
    bool TargetInSkillRange()
    {
        if(attackTarget!=null)
            return Vector3.Distance(attackTarget.transform.position,transform.position)<=characterStats.attackDate.skillRange;
        else 
            return false;
    }
        void GetNewWayPoint()
    {
        remainLookAtTime = lookAtTime;
        float randomX = Random.Range(-patrolRange,patrolRange);
        float randomZ = Random.Range(-patrolRange,patrolRange);
        Vector3 randomPoint = new Vector3(guardPos.x+randomX,transform.position.y,guardPos.z+randomZ);
        NavMeshHit hit;
        wayPoint=NavMesh.SamplePosition(randomPoint,out hit,patrolRange,1)?hit.position:transform.position;//
    }

    void  OnDrawGizmosSelected() 
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position,sightRadius);
    }//圈出巡逻范围
}
