using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Cinemachine;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-500)]
public class GameManager : Singleton<GameManager>
{

    [HideInInspector]public CharacterStats playerStats;
    [HideInInspector]public GameObject player;
    private CinemachineFreeLook followCamera;
    
    [Header("Enemy Spawn")]
    [Tooltip("怪物死亡多久时间后，重新生成同类型怪物（位置随机）")]
    public float rebornMinTime = 300;
    public float rebornMaxTime = 600;
    //[Tooltip("太远的怪物不显示，解决卡顿问题")]
    //public float hideEnemyRadius = 60;
    [Tooltip("怪物加载的边界大小，要和NavMesh的边界相同")]
    public Vector3 hideEnemyBoundsSize = new Vector3(60f, 200f, 60f);
    [Tooltip("加载Boss的边界大小，要比场景物体的边界大")]
    public Vector3 hideBossBoundsSize = new Vector3(150f, 200f, 150f);
    [Tooltip("Prefab和Parent的顺序必须完全对应")]
    public GameObject[] enemyPrefabs;
    private List<Transform> enemyRoots; //Prefab和Parent的顺序必须完全对应

    [Header("Environment")]
    [Tooltip("太远的环境不显示，解决卡顿问题")]
    public Transform environmentRoot;
    [Tooltip("加载场景物体的边界大小，要比怪物加载、生成NavMesh的边界大")]
    public Vector3 hideEnvrionBoundsSize = new Vector3(100f, 200f, 100f);

    [Header("URP_Asset_Renderer透视遮罩")]
    [Tooltip("用于修改玩家的透视遮罩，以适应白天和夜间场景")]
    public RenderObjects playerBehind;
    [Tooltip("用于修改怪物的透视遮罩，以适应白天和夜间场景")]
    public RenderObjects enemyBehind;
    public Material oclussionPlayer;
    public Material oclussionPlayerNight;
    public Material oclussionEnemy;
    public Material oclussionEnemyNight;

    [HideInInspector] public EnemyController[] enemys; //开关active，解决怪物卡顿
    [HideInInspector] public List<Transform> environments; //开关active，解决场景卡顿
    [HideInInspector] public List<Transform> grounds; //开关active，解决场景卡顿

    Bounds lastEnemyBounds;
    Bounds lastEnvrionmentBounds;
    //Bounds lastGroundBounds;

    List<IGameObserver> gameObservers = new List<IGameObserver>();

    private float updateSwitchTime;


    protected override void Awake()
    {
        //注意这里，没注册单例会报错
        base.Awake();
        DontDestroyOnLoad(this);

        if (SceneManager.GetActiveScene().name != "Main")
            NewSceneLoaded();
    }

    //初始场景，或切换新场景时，更新相关内容
    internal void NewSceneLoaded()
    {
        //获得所有怪物(包括隐藏的，修复切换场景时怪物被隐藏的bug)
        enemys = FindObjectsOfType<EnemyController>(true);
        //更新寻找EnemyParent栏目
        if (enemyRoots != null) enemyRoots.Clear();
        enemyRoots = new List<Transform>();
        EnemyRoot[] ers = FindObjectsOfType<EnemyRoot>(true);
        for (int i = 0; i < enemyPrefabs.Length; i++)
        {
            foreach (EnemyRoot er in ers)
            {
                if (er.gameObject.name == enemyPrefabs[i].name)
                {
                    enemyRoots.Add(er.gameObject.transform);
                    break;
                }
            }
        }

        //初始化资源列表
        grounds.Clear();
        environments.Clear();
        environmentRoot = FindObjectOfType<EnvironmentRoot>()?.gameObject.transform;

        //获得所有环境资源的transform
        //环境物体和Ground分开存放
        MeshFilter[] enviMesh = environmentRoot.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < enviMesh.Length; i++)
            if (enviMesh[i].gameObject.CompareTag("Ground"))
                grounds.Add(enviMesh[i].transform);
            else
            if (enviMesh[i].gameObject.CompareTag("Portal"))
            {
                //什么都不做，传送门保持开启
            }
            else
                environments.Add(enviMesh[i].transform);

        //Debug.Log("enemys.Length:" + enemys.Length + "  grounds.Count:" + grounds.Count
        //     + "  environments.Count:" + environments.Count);

        //第一遍没注册playerStats，相当于关闭所有怪物的active
        ///（刚好解决了NavMesh未生成，Agent警告的问题）
        SwitchEnemysActive();
        SwitchEnvironmentActive();

        //通知AutoSaveCanvas更改字体颜色
        AutoSaveCanvas.Instance?.SetTextColor();
    }

    internal void LeaveScene()
    {
        //清空资源列表、相关引用
        grounds.Clear();
        environments.Clear();

        enemys = null;
        player = null;
        playerStats = null;

        lastEnemyBounds = new Bounds();
        lastEnvrionmentBounds = new Bounds();

        AudioManager.Instance.StopAllAmbient();
    }

    IEnumerator Start()
    {
        while (true)
        {
            //根据与玩家的距离，动态设置怪物的active，解决卡顿
            //隔一段时间更新一次即可
            //
            //:解决每次更新时的卡顿问题
            if (updateSwitchTime < 0)
            {
                //先更新环境、再更新怪物，防止意外情况（虽然还没遇到）
                SwitchEnvironmentActive();
                SwitchEnemysActive();
                updateSwitchTime = 2f;
            }
            else
                updateSwitchTime -= Time.deltaTime;

            yield return new WaitForFixedUpdate();
        }
    }

    internal void CloseNoAttackingEnemysAgent(bool isBoss)
    {
        if (enemys == null) return;

        if (!isBoss)
            for (int i = 0; i < enemys.Length; i++)
            {
                //只关闭怪物
                if (enemys[i] && !enemys[i].gameObject.CompareTag("Boss") && !enemys[i].isDead
                    && enemys[i].AttackTarget == null 
                    && enemys[i].enemyBehaviorState!=EnemyBehaviorState.ESCAPE)
                    enemys[i].GetComponent<NavMeshAgent>().enabled = false;
            }
        else
            for (int i = 0; i < enemys.Length; i++)
            {
                //只关闭Boss
                if (enemys[i] && enemys[i].gameObject.CompareTag("Boss") && !enemys[i].isDead
                    && enemys[i].AttackTarget == null
                    && enemys[i].enemyBehaviorState != EnemyBehaviorState.ESCAPE)
                    enemys[i].GetComponent<NavMeshAgent>().enabled = false;
            }

    }

    internal void OpenOnNavMeshEnemysAgent()
    {
        if (enemys == null) return;

        for (int i = 0; i < enemys.Length; i++)
        {
            //NavMesh更新后，所有OnNavMesh的敌人Agent开启
            if (enemys[i] && !enemys[i].isDead 
                && enemys[i].GetComponent<NavMeshAgent>().isOnNavMesh)
                enemys[i].GetComponent<NavMeshAgent>().enabled = true;
        }
    }

    private void SwitchEnemysActive()
    {
        if (playerStats == null || enemys == null) return;

        //加载怪物的边界范围
        Bounds enemyBounds = QuantizedBound(hideEnemyBoundsSize);
        Bounds bossBounds = QuantizedBound(hideBossBoundsSize);

        RaycastHit upHighDownHit;

        if (enemyBounds != lastEnemyBounds)
        {
            //枚举场上所有怪物
            for (int i = 0; i < enemys.Length; i++)
                if (enemys[i] && !enemys[i].isSpawning)
                {
                    enemys[i].numInArray = i;

                    //Boss在Ground内（大范围）保持显示，其他怪物在小范围内保持显示
                    if ((enemys[i].gameObject.CompareTag("Boss") && bossBounds.Contains(enemys[i].transform.position))
                        || enemyBounds.Contains(enemys[i].transform.position))
                    {
                        if (enemys[i].transform.position.IsOnGround(out upHighDownHit))
                        {
                            //修正怪物y坐标在Ground上
                            enemys[i].transform.position = new Vector3(enemys[i].transform.position.x,
                               upHighDownHit.point.y, enemys[i].transform.position.z);
                                enemys[i].gameObject.SetActive(true);
                                enemys[i].GetComponent<NavMeshAgent>().enabled = true;
                        }
                    }
                    else
                    {
                        //梦魇史莱姆保持显示
                        if (enemys[i].isNightmare) continue;

                        enemys[i].GetComponent<NavMeshAgent>().enabled = false;

                        enemys[i].gameObject.SetActive(false);
                    }
                }

            lastEnemyBounds = enemyBounds;
        }
    }


    public void SwitchEnvironmentActive()
    {
        if (playerStats == null || environments == null) return;

        Bounds bounds;

        //为了传送门正常运行，关闭Ground动态开关
        //加载Ground的边界范围
        //bounds = QuantizedBound(hideGroundBoundsSize);
        //if (bounds != lastGroundBounds)
        //{
        //    //枚举场上所有Ground
        //    for (int i = 0; i < grounds.Count; i++)
        //    {
        //        if (bounds.Contains(grounds[i].position))
        //            grounds[i].gameObject.SetActive(true);
        //        else
        //            grounds[i].gameObject.SetActive(false);
        //    }
        //    lastGroundBounds = bounds;
        //}

        //加载环境物体的边界范围
        bounds = QuantizedBound(hideEnvrionBoundsSize);

        if (bounds != lastEnvrionmentBounds)
        {
            //枚举场上所有环境物体
            for (int i = 0; i < environments.Count; i++)
            {
                if (bounds.Contains(environments[i].position))
                    environments[i].gameObject.SetActive(true);
                else
                    environments[i].gameObject.SetActive(false);
            }
            //这里忘了更新bounds，怪不得那么卡。。
            lastEnvrionmentBounds = bounds;
        }
    }

    internal void SetPlayerOnGround()
    {
        //重置y值，防止玩家出生在夹层里
        RaycastHit upHighDownHit;
        if (player != null && player.transform.position.IsOnGround(out upHighDownHit))
        {
            player.transform.position = new Vector3(player.transform.position.x,
                upHighDownHit.point.y, player.transform.position.z);
        }
    }

    private Bounds QuantizedBound(Vector3 boundsSize,float quantizeStep = 0.1f)
    {
        //量化边界，仅在大小变化quantizeStep时更新
        return new Bounds(Quantize(playerStats.transform.position, quantizeStep * boundsSize), boundsSize);
    }

    static Vector3 Quantize(Vector3 center, Vector3 quant)
    {
        float x = quant.x * Mathf.Floor(center.x / quant.x);
        float y = quant.y * Mathf.Floor(center.y / quant.y);
        float z = quant.z * Mathf.Floor(center.z / quant.z);

        return new Vector3(x, y, z);
    }

    public void RigisterPlayer(CharacterStats pStats)
    {
        playerStats = pStats;
        player = FindObjectOfType<PlayerController>().gameObject;

        followCamera = FindObjectOfType<CinemachineFreeLook>(true);
        //CinemachineFreeLook初始会随意变化视角，为了保证相机视角，初始是关闭的
        //followCamera.gameObject.SetActive(true);
        
        if (followCamera!=null)
        {
            followCamera.Follow = playerStats.transform.GetChild(2); //TopRigLookAt
            followCamera.LookAt = playerStats.transform.GetChild(2);
            followCamera.GetRig(0).LookAt = playerStats.transform.GetChild(2);
            followCamera.GetRig(1).LookAt = playerStats.transform.GetChild(3); //MiddleRigLookAt
            followCamera.GetRig(2).LookAt = playerStats.transform.GetChild(4); //BottomRigLookAt
        }

        //夜晚场景的聚光灯朝向
        NightSpotLight nightSpotLight = FindObjectOfType<NightSpotLight>();
        if (nightSpotLight != null)
            nightSpotLight.focusPointOnPlayer = playerStats.transform.GetChild(4); //BottomRigLookAt

        //LocalNavMeshBuilder的中心点设置
        LocalNavMeshBuilder localNavMeshBuilder = FindObjectOfType<LocalNavMeshBuilder>();
        if (localNavMeshBuilder != null)
            localNavMeshBuilder.m_Tracked = player.transform;

        //根据当前场景，设置透视效果遮罩
        if (SceneManager.GetActiveScene().name == "02 Nightmare Scene")
        {
            //设置夜晚的透视遮罩
            playerBehind.settings.overrideMaterial = oclussionPlayerNight;
            enemyBehind.settings.overrideMaterial = oclussionEnemyNight;
        }
        else
        {
            //设置白天的透视遮罩
            playerBehind.settings.overrideMaterial = oclussionPlayer;
            enemyBehind.settings.overrideMaterial = oclussionEnemy;
        }
    }

    public void AddObserver(IGameObserver observer)
    {
        gameObservers.Add(observer);
    }

    public void RemoveObserver(IGameObserver observer) 
    {
        gameObservers.Remove(observer);
    }

    public void NotifyObserversPlayerDead()
    {
        foreach(var observer in gameObservers)
        {
            //向怪物通知玩家死亡
            observer.EndNotify();
        }
    }
    public void NotifyObserversGameLoad()
    {
        foreach (var observer in gameObservers)
        {
            //向怪物通知玩家读档
            observer.LoadNotify();
        }
    }

    //怪物死亡后，随机位置生成同类型怪物，并在一段时间后active
    public void InstantiateEnemy(GameObject enemyType, Vector3 lastPos, int numInArray)
    {
        //确认新的出生点
        RaycastHit upHighDownHit;
        Vector3 newPos = lastPos;
        float minRange = 30, maxRange = 60;
        for (int i = 0; i < 5; i++)
        {
            Vector2 vec = Random.insideUnitCircle;
            //随机怪物位置
            newPos = lastPos + (new Vector3(vec.x, 0, vec.y)) * UnityEngine.Random.Range(minRange, maxRange);

            //保证怪物在Ground上，每次缩小范围
            if (newPos.IsOnGround(out upHighDownHit))
            {
                //修正y值
                newPos.y = upHighDownHit.point.y;
                break;
            }
            else
            {
                minRange *= 0.5f;
                maxRange *= 0.5f;
            }
        }

        int type = 0;
        for (int i = 0; i < enemyPrefabs.Length; i++)
            if (enemyPrefabs[i].name == enemyType.name)
            {
                type = i;
                break;
            }

        //生成并加入数组
        GameObject enemy = Instantiate(enemyPrefabs[type], newPos, Quaternion.identity, enemyRoots[type]);
        enemys[numInArray] = enemy.GetComponent<EnemyController>();
        enemys[numInArray].isSpawning = true;
        enemys[numInArray].numInArray = numInArray;

        //暂时隐藏，一段时间后出现
        enemy.SetActive(false);
        StartCoroutine("LaterActiveEnemy", numInArray);
    }

    IEnumerator LaterActiveEnemy(int numInArray)
    {
        yield return new WaitForSeconds(Random.Range(rebornMinTime, rebornMaxTime));
        //判定是否为重生，以防场景切换导致bug
        if (enemys != null && numInArray < enemys.Length && enemys[numInArray].isSpawning)
        {
            enemys[numInArray].gameObject.SetActive(true);
            enemys[numInArray].isSpawning = false;
        }
    }

    //场景窗口中绘制盒子，观察作用范围
    private void OnDrawGizmosSelected()
    {
        if (playerStats == null) return;

        //绘制怪物显示范围
        Gizmos.color = Color.red;
        Bounds bounds = QuantizedBound(hideEnemyBoundsSize);
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        //绘制场景物体显示范围
        Gizmos.color = Color.green;
        bounds = QuantizedBound(hideEnvrionBoundsSize);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        //Gizmos.DrawWireSphere(playerStats.transform.position, hideEnvironmentRadius);

        //绘制Ground显示范围
        Gizmos.color = Color.yellow;
        bounds = QuantizedBound(hideBossBoundsSize);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        //Gizmos.DrawWireSphere(playerStats.transform.position, hideGroundRadius);
    }
}
