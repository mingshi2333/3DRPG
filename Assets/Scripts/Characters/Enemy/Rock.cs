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
    [Tooltip("��һ���￿��ʯͷʱ�ܵ��ĳ�����ʹ��ʯͷ����������")]
    public float agentMovePush = 5f;
    [Tooltip("��һ���￿��ʯͷʱ�ٶȵļ�����Ȼ���ټ��ϳ�����")]
    public float agentMoveSpeedRate = 0.5f;
    [Range(0, 1)] public float enterTreeSpeedRate = 0.4f;

    public GameObject breakEffect;

    private float disMag;

    private bool catched;
    [HideInInspector] public bool isDestroying;
    private float volumeBias;
    [HideInInspector] public CharacterStats attacker; //��˭�ӵ�ʯͷ�������жϽ���ս��״̬

    float pitchBias;

    private void Start()
    {
        RandomSize();
        rb = GetComponent<Rigidbody>();
        rb.velocity = Vector3.one; //��ʼ���ٶ�1����ֹ�ٶ�0����HitNothing
        rb.maxAngularVelocity = 20; //�ø����ܹ���ת������

        rockState = RockState.HitPlayer;
        FlyToTarget();
    }

    private void RandomSize()
    {
        //�������ʯͷ��С
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
            //�����ת����
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
            //û��Ŀ�������³�ǰ����������ӳ�
            flyToPos = attacker.transform.position +
                attacker.transform.forward * Random.Range(5f, 35f);

            //Ŀ����Ϊ��ң��Ӷ���ʯͷ�����ܹ�����׷����ң�����������ܲ�����
            target = FindObjectOfType<PlayerController>().gameObject;
        }
        else flyToPos = target.transform.position; 

        //�����ʼͶ������
        //direction = target.transform.position - transform.position;
        //direction = (direction.normalized + Vector3.up *Random.Range(0.5f,1.5f)).normalized;
        //���ģ�������ߣ������������λ��������ٶȣ��ö��ַ��Ż���̫�鷳��


        //����ˮƽ�����С�ٶ������С���ʱ�䣬Ȼ�����ʱ��
        direction = flyToPos - transform.position;
        horiDir = new Vector3(direction.x, 0, direction.z);
        maxTime = horiDir.magnitude / minHoriVel;
        minTime = horiDir.magnitude / maxHoriVel;
        t = Random.Range(minTime, maxTime);

        //Ԥ�����һ��ʱ����ƶ�λ�ã��ʵ��������ת��
        //NavMeshAgent agent = target.GetComponent<NavMeshAgent>();
        //Vector3 dir = target.transform.position +
        //    (agent.velocity.normalized + agent.transform.forward * agent.speed).normalized * agent.velocity.magnitude * t;
        //����Ŀ��λ�ú�ʱ�䣬�����ٶ�
        //distance = dir - transform.position;
        //horiDis = new Vector3(distance.x, 0, distance.z);

        horiVel = horiDir.normalized * (horiDir.magnitude / t);
        vertVel = Vector3.up * (direction.y / t - Physics.gravity.y * t * 0.5f);

        //�ٶ�������ʹ��ʯͷ������
        horiVel += horiVel.normalized * horiVelFix;
        vertVel += Vector3.down * downVelFix;

        //ForceMode.VelocityChange���ڴ�ģʽ�£�ʩ���ڴ˸�����������ĵ�λΪ������/ʱ�䡣
        rb.AddForce(horiVel + vertVel, ForceMode.VelocityChange);
        //�����ת����
        rb.AddTorque(transform.right * Random.Range(5, 15) +
            Vector3.up * Random.value,
            ForceMode.VelocityChange);

        //rb.AddForce(ditance * maxForce, ForceMode.VelocityChange);

        //�����ж�ʯͷ����׷�����
        disMag = direction.magnitude;

        //��Ч
        float bias = 0.2f * horiVel.magnitude / maxHoriVel;
        AudioManager.Instance.Play3DSoundEffect(SoundName.Rock_Fly,
            AudioManager.Instance.otherSoundDetailList, target.transform, bias, bias);
    }

    private void OnTriggerEnter(Collider other)
    {
        //��ľ��ʹʯͷ���٣�
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
                    //�����ײ���ٶ�����һЩ�仯
                    Vector3 hitVel = direction.normalized * Random.Range(5, 15)
                        + horiVel.normalized * Random.Range(10, 30);
                    //�ٶ����ƣ���ֹײ�ϵ���ʯͷʱ�����쵯��
                    float velMag = Mathf.Min(hitVel.magnitude, rb.velocity.magnitude * 3.14f);

                    //�����ٶ���agent����˿��
                    if(agent.isOnNavMesh) agent.isStopped = true;
                    agent.velocity += hitVel.normalized * velMag;
                    collision.gameObject.GetComponent<Animator>().SetTrigger("Dizzy");

                    //����ײ���˺�
                    damage = (int)(maxDamage * (hitVel.magnitude / 40 +
                        rb.velocity.magnitude / maxHoriVel * 2.5f));
                    damage = Mathf.Clamp(damage, minDamage * 2, maxDamage * 2);

                    //Debug.Log("Rock Damage��" + damage);
                    //Debug.Log("hitVel��" + hitVel.magnitude + "  rb��" + rb.velocity.magnitude);
                    var playerStats = collision.gameObject.GetComponent<CharacterStats>();
                    //���ݹ����ߣ�ʯͷ�ˣ����������ս��״̬������Rockλ�ã�������Ҹ�
                    playerStats.TakeDamage(damage, transform.position, attacker);

                    rockState = RockState.HitNothing;

                    //��ʯײ����ҵ���Ч
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
                    //��ʯײ�������������Ч
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

                    //��ֹ��μӾ���
                    if (enemyStats.CurrentHealth > 0)
                    {
                        //�˺�����ʯͷ�ٶȺʹ�С
                        damage = (int)(rb.velocity.magnitude * hitDmgPerVel * size * size);
                        //damage = (int)(maxDamage * rb.velocity.magnitude / maxHoriVel * kickDmgRate * size * size);
                        damage = Mathf.Min(damage, maxDamage);
                        //���ݹ����ߣ���ң����������ս��״̬
                        enemyStats.TakeDamage(damage, transform.position, attacker);
                    }

                    //���ﱻ����
                    NavMeshAgent agent = enemy.gameObject.GetComponent<NavMeshAgent>();
                    //����ٶȻ��ˣ����˺��йأ���ͬ����ĵ���ͨ��Agent�ļ��ٶȽ���
                    if (agent.isOnNavMesh) agent.isStopped = true;
                    agent.velocity += (enemy.gameObject.transform.position - transform.position)
                        * damage * Random.Range(0.271f, 0.382f);
                    enemy.gameObject.GetComponent<Animator>().SetTrigger("Hit");

                    //Debug.Log("Rock back Damage��" + (damage - enemyStats.CurrentDefence)
                    //    + "  rb.vel��" + rb.velocity.magnitude);


                    if (!isDestroying)
                    {
                        target = enemy.gameObject; //���õ���ΪĿ�꣬������������Ч��
                        StartCoroutine("LaterDestroy", RockState.HitEnemy);
                        isDestroying = true;

                        //��ʯ�������Ч
                        volumeBias = 0.3f * damage / maxDamage;
                        AudioManager.Instance.Play3DSoundEffect(SoundName.Rock_Crack,
                            AudioManager.Instance.otherSoundDetailList, target.transform,
                            -pitchBias + volumeBias, volumeBias);
                    }
                }
                else if (attacker != null && collision.gameObject != attacker.gameObject)
                {
                    //��ʯײ�������������Ч
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
                    //��һ�����ƶ�ʯͷʱ��������������Ȼ�Ե�ʯͷ���ᣩ
                    GameObject nearTarget = (enemy != null) ? enemy.gameObject : collision.gameObject;
                    //û��Ҫnormalized����Ϊ���Խ��Խ���ƶ�
                    Vector3 dir = nearTarget.transform.position - transform.position;
                    dir = dir * dir.magnitude; //ǿ�������ƶ���Ч��
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
                //�������Ӵ�С
                breakEffect.transform.localScale = transform.localScale;

                //����z��ָ����ҷ���
                Vector3 dir = transform.position - target.transform.position;
                dir.y = 0;
                Quaternion rot = Quaternion.FromToRotation(breakEffect.transform.forward, dir);
                Instantiate(breakEffect, transform.position, rot);
                break;
        }    

        Destroy(gameObject);
    }

    //��Ч
    internal void PlayHitSound(SoundName enemyHitSound)
    {
        float scaleBias = 0.1f / transform.localScale.x;
        float pitchBias = -GameManager.Instance.playerStats.WeaponPitchBias + scaleBias;
        //���⶯�����
        switch (GameManager.Instance.player.GetComponent<PlayerController>().
            weaponWaveSound)
        {
            case SoundName.TwoHandSword_Rush:
            case SoundName.TwoHandAxe_Rush:
            case SoundName.TwoHandMace_HitBack:
                pitchBias -= 0.25f;
                break;
        }

        //�����ܻ���������������������ƫ���෴
        AudioManager.Instance.Play3DSoundEffect(enemyHitSound, AudioManager.Instance.golemSoundDetailList,
            transform, pitchBias + 0.15f, -scaleBias);
    }
}
