using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class Rock : MonoBehaviour
{
    public enum RockState
    {
        HitPlayer, HitEnemy, HitNothing
    }
    public RockState rockState;

    private Rigidbody rb;
    [Header("Basic Setting")]
    public float minHoriVel = 5;
    public float maxHoriVel = 50;
    public GameObject target;
    private Vector3 flyToPos;
    [HideInInspector]public int minDamage;
    [HideInInspector]public int maxDamage;
    private int damage;
    public float hitDmgPerVel = 3f;

    private Vector3 direction;
    private Vector3 horiDir;
    private Vector3 horiVel, vertVel;

    private float minTime;
    private float maxTime;
    private float t;

    public float horiVelFix;
    public float downVelFix;
    [Range(0, 0.888f)] public float catchRange = 0.5f;
    [Tooltip("玩家或怪物靠近石头时受到的斥力，使得石头看起来很重")]
    public float agentMovePush = 5f;
    [Tooltip("玩家或怪物靠近石头时速度的减缓（然后再加上斥力）")]
    public float agentMoveSpeedRate = 0.5f;
    [Range(0, 1)] public float enterTreeSpeedRate = 0.4f;

    public GameObject breakEffect;

    private float disMag;

    private bool catched;
    [HideInInspector] public bool isDestroying;
    private float volumeBias;
    [HideInInspector] public CharacterStats attacker; //是谁扔的石头，用于判断进入战斗状态

    float pitchBias;

    private void Start()
    {
        RandomSize();
        rb = GetComponent<Rigidbody>();
        rb.velocity = Vector3.one; //初始化速度1，防止速度0触发HitNothing
        rb.maxAngularVelocity = 20; //让刚体能够旋转起来！

        rockState = RockState.HitPlayer;
        FlyToTarget();
    }

    private void RandomSize()
    {
        //随机调整石头大小
        float scale = Random.Range(0.666f, 1.333f);
        pitchBias = 0.15f * scale;

        transform.localScale = scale *
            new Vector3(Random.Range(0.9f, 1.1f), Random.Range(0.9f, 1.1f), Random.Range(0.9f, 1.1f));
    }

    private void Update()
    {
        direction = target.transform.position - transform.position;
        horiDir = new Vector3(direction.x, 0, direction.z);
        if (horiDir.magnitude < disMag * 0.5f && !catched)
        {
            catched = true;
            rb.velocity = new Vector3(rb.velocity.x * Random.Range(0f, 0.27f), rb.velocity.y, rb.velocity.z * Random.Range(0f, 0.27f));
            float maxVel = Mathf.Max(horiVel.magnitude, vertVel.magnitude);
            direction = direction.normalized * Random.Range(maxVel, maxHoriVel);
            rb.AddForce(direction, ForceMode.VelocityChange);
            //随机旋转方向
            rb.AddTorque(transform.right * Random.value +
                Vector3.up * Random.Range(10, 20),
                ForceMode.VelocityChange);
        }
    }

    internal void SetDamage(int minDmg, int maxDmg)
    {
        minDamage = minDmg;
        maxDamage = maxDmg;
    }

    private void FixedUpdate()
    {
        if (rb.velocity.sqrMagnitude < 1.5f)
        {
            rockState = RockState.HitNothing;
        }

    }

    public void FlyToTarget()
    {
        if (target == null)
        {
            //没有目标的情况下朝前方随机距离扔出
            flyToPos = attacker.transform.position +
                attacker.transform.forward * Random.Range(5f, 35f);

            //目标设为玩家，从而让石头后半段能够拐弯追击玩家（逼迫玩家闪避操作）
            target = FindObjectOfType<PlayerController>().gameObject;
        }
        else flyToPos = target.transform.position; 

        //随机初始投掷方向
        //direction = target.transform.position - transform.position;
        //direction = (direction.normalized + Vector3.up *Random.Range(0.5f,1.5f)).normalized;
        //多次模拟抛物线，计算命中玩家位置所需初速度，用二分法优化（太麻烦）


        //根据水平最大最小速度算出最小最大时间，然后随机时间
        direction = flyToPos - transform.position;
        horiDir = new Vector3(direction.x, 0, direction.z);
        maxTime = horiDir.magnitude / minHoriVel;
        minTime = horiDir.magnitude / maxHoriVel;
        t = Random.Range(minTime, maxTime);

        //预测玩家一定时间后移动位置，适当考虑玩家转向
        //NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
        //Vector3 dir = target.transform.position +
        //    (agent.velocity.normalized + agent.transform.forward * agent.speed).normalized * agent.velocity.magnitude * t;
        //根据目标位置和时间，修正速度
        //distance = dir - transform.position;
        //horiDis = new Vector3(distance.x, 0, distance.z);

        horiVel = horiDir.normalized * (horiDir.magnitude / t);
        vertVel = Vector3.up * (direction.y / t - Physics.gravity.y * t * 0.5f);

        //速度修正，使得石头更有力
        horiVel += horiVel.normalized * horiVelFix;
        vertVel += Vector3.down * downVelFix;

        //ForceMode.VelocityChange：在此模式下，施加于此刚体的力参数的单位为：距离/时间。
        rb.AddForce(horiVel + vertVel, ForceMode.VelocityChange);
        //随机旋转方向
        rb.AddTorque(transform.right * Random.Range(5, 15) +
            Vector3.up * Random.value,
            ForceMode.VelocityChange);

        //rb.AddForce(ditance * maxForce, ForceMode.VelocityChange);

        //用于判定石头后半段追击玩家
        disMag = direction.magnitude;

        //音效
        float bias = 0.2f * horiVel.magnitude / maxHoriVel;
        AudioManager.Instance.Play3DSoundEffect(SoundName.Rock_Fly,
            AudioManager.Instance.otherSoundDetailList, target.transform, bias, bias);
    }

    private void OnTriggerEnter(Collider other)
    {
        //树木会使石头减速！
        if (other.gameObject.CompareTag("Tree"))
        {
            rb.velocity *= enterTreeSpeedRate;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        EnemyController enemy = null;
        enemy = enemy.GetEnemy(collision.gameObject);

        switch (rockState)
        {
            case RockState.HitPlayer:
                if (collision.gameObject.CompareTag("Player"))
                {
                    NavMeshAgent agent = collision.gameObject.GetComponent<NavMeshAgent>();
                    if(agent.isOnNavMesh)agent.isStopped = true;
                    //给玩家撞击速度增加一些变化
                    Vector3 hitVel = direction.normalized * Random.Range(5, 15)
                        + horiVel.normalized * Random.Range(10, 30);
                    //速度限制，防止撞上低速石头时被诡异弹开
                    float velMag = Mathf.Min(hitVel.magnitude, rb.velocity.magnitude * 3.14f);

                    //附加速度用agent，更丝滑
                    if(agent.isOnNavMesh) agent.isStopped = true;
                    agent.velocity += hitVel.normalized * velMag;
                    collision.gameObject.GetComponent<Animator>().SetTrigger("Dizzy");

                    //计算撞击伤害
                    damage = (int)(maxDamage * (hitVel.magnitude / 40 +
                        rb.velocity.magnitude / maxHoriVel * 2.5f));
                    damage = Mathf.Clamp(damage, minDamage * 2, maxDamage * 2);

                    //Debug.Log("Rock Damage！" + damage);
                    //Debug.Log("hitVel：" + hitVel.magnitude + "  rb：" + rb.velocity.magnitude);
                    var playerStats = collision.gameObject.GetComponent<CharacterStats>();
                    //传递攻击者（石头人），另其进入战斗状态；传递Rock位置，用于玩家格挡
                    playerStats.TakeDamage(damage, transform.position, attacker);

                    rockState = RockState.HitNothing;

                    //岩石撞击玩家的音效
                    SoundName soundName = SoundName.Rock_HitPlayer;
                    SoundDetailList_SO soundList = AudioManager.Instance.otherSoundDetailList;
                    if (playerStats.attackData.hasEquipShield && playerStats.isShieldSuccess)
                    {
                        soundName = playerStats.attackData.isMetalShield ?
                            SoundName.Enemy_MetalShield : SoundName.Enemy_WoodShield;
                        soundList = AudioManager.Instance.golemSoundDetailList;
                    }

                    volumeBias = 0.3f * damage / maxDamage;
                    AudioManager.Instance.Play3DSoundEffect(soundName, soundList,
                        target.transform, 0.3f - pitchBias + volumeBias, -0.5f + volumeBias);
                }
                else if (attacker!=null && collision.gameObject != attacker.gameObject)
                {
                    //岩石撞击其他物体的音效
                    volumeBias = rb.velocity.magnitude / maxHoriVel;
                    AudioManager.Instance.Play3DSoundEffect(SoundName.Rock_HitGround,
                        AudioManager.Instance.otherSoundDetailList, target.transform,
                        -pitchBias + volumeBias, volumeBias);
                }
                break;
            case RockState.HitEnemy:
                if (enemy != null)
                {
                    var enemyStats = enemy.GetComponent<CharacterStats>();
                    float size = transform.localScale.x;

                    //防止多次加经验
                    if (enemyStats.CurrentHealth > 0)
                    {
                        //伤害考虑石头速度和大小
                        damage = (int)(rb.velocity.magnitude * hitDmgPerVel * size * size);
                        //damage = (int)(maxDamage * rb.velocity.magnitude / maxHoriVel * kickDmgRate * size * size);
                        damage = Mathf.Min(damage, maxDamage);
                        //传递攻击者（玩家），另其进入战斗状态
                        enemyStats.TakeDamage(damage, transform.position, attacker);
                    }

                    //怪物被击退
                    NavMeshAgent agent = enemy.gameObject.GetComponent<NavMeshAgent>();
                    //随机速度击退，与伤害有关，不同怪物的调整通过Agent的加速度进行
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    agent.velocity += (enemy.gameObject.transform.position - transform.position)
                        * damage * Random.Range(0.271f, 0.382f);
                    enemy.gameObject.GetComponent<Animator>().SetTrigger("Hit");

                    //Debug.Log("Rock back Damage！" + (damage - enemyStats.CurrentDefence)
                    //    + "  rb.vel：" + rb.velocity.magnitude);


                    if (!isDestroying)
                    {
                        target = enemy.gameObject; //设置敌人为目标，用于生成粒子效果
                        StartCoroutine("LaterDestroy", RockState.HitEnemy);
                        isDestroying = true;

                        //岩石击碎的音效
                        volumeBias = 0.3f * damage / maxDamage;
                        AudioManager.Instance.Play3DSoundEffect(SoundName.Rock_Crack,
                            AudioManager.Instance.otherSoundDetailList, target.transform,
                            -pitchBias + volumeBias, volumeBias);
                    }
                }
                else if (attacker != null && collision.gameObject != attacker.gameObject)
                {
                    //岩石撞击其他物体的音效
                    volumeBias = rb.velocity.magnitude / maxHoriVel;
                    AudioManager.Instance.Play3DSoundEffect(SoundName.Rock_HitGround,
                        AudioManager.Instance.otherSoundDetailList, target.transform,
                        -pitchBias + volumeBias, volumeBias);
                }
                break;
            case RockState.HitNothing:
                if (!isDestroying)
                {
                    StartCoroutine("LaterDestroy", RockState.HitNothing);
                    isDestroying = true;
                }

                if (collision.gameObject.GetComponent<PlayerController>() ||
                    enemy != null)
                {
                    //玩家或怪物推动石头时产生斥力！（不然显得石头很轻）
                    GameObject nearTarget = (enemy != null) ? enemy.gameObject : collision.gameObject;
                    //没必要normalized，因为体积越大越难推动
                    Vector3 dir = nearTarget.transform.position - transform.position;
                    dir = dir * dir.magnitude; //强化难以推动的效果
                    NavMeshAgent agent = nearTarget.GetComponent<NavMeshAgent>();
                    agent.velocity = agent.velocity * agentMoveSpeedRate + dir * agentMovePush;
                }
                break;
        }
    }

    IEnumerator LaterDestroy(RockState rs)
    {
        switch(rs)
        {
            case RockState.HitNothing:
                yield return new WaitForSeconds(Random.Range(10f, 15f));
                break;
            case RockState.HitEnemy:
                //调整粒子大小
                breakEffect.transform.localScale = transform.localScale;

                //设置z轴指向玩家方向
                Vector3 dir = transform.position - target.transform.position;
                dir.y = 0;
                Quaternion rot = Quaternion.FromToRotation(breakEffect.transform.forward, dir);
                Instantiate(breakEffect, transform.position, rot);
                break;
        }    

        Destroy(gameObject);
    }

    //音效
    internal void PlayHitSound(SoundName enemyHitSound)
    {
        float scaleBias = 0.1f / transform.localScale.x;
        float pitchBias = -GameManager.Instance.playerStats.WeaponPitchBias + scaleBias;
        //特殊动作情况
        switch (GameManager.Instance.player.GetComponent<PlayerController>().
            weaponWaveSound)
        {
            case SoundName.TwoHandSword_Rush:
            case SoundName.TwoHandAxe_Rush:
            case SoundName.TwoHandMace_HitBack:
                pitchBias -= 0.25f;
                break;
        }

        //怪物受击的音调，与武器的音调偏移相反
        AudioManager.Instance.Play3DSoundEffect(enemyHitSound, AudioManager.Instance.golemSoundDetailList,
            transform, pitchBias + 0.15f, -scaleBias);
    }
}
