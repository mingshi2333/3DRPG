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
    [Tooltip("�����������ʱ�����������ͬ���͹��λ�������")]
    public float rebornMinTime = 300;
    public float rebornMaxTime = 600;
    //[Tooltip("̫Զ�Ĺ��ﲻ��ʾ�������������")]
    //public float hideEnemyRadius = 60;
    [Tooltip("������صı߽��С��Ҫ��NavMesh�ı߽���ͬ")]
    public Vector3 hideEnemyBoundsSize = new Vector3(60f, 200f, 60f);
    [Tooltip("����Boss�ı߽��С��Ҫ�ȳ�������ı߽��")]
    public Vector3 hideBossBoundsSize = new Vector3(150f, 200f, 150f);
    [Tooltip("Prefab��Parent��˳�������ȫ��Ӧ")]
    public GameObject[] enemyPrefabs;
    private List<Transform> enemyRoots; //Prefab��Parent��˳�������ȫ��Ӧ

    [Header("Environment")]
    [Tooltip("̫Զ�Ļ�������ʾ�������������")]
    public Transform environmentRoot;
    [Tooltip("���س�������ı߽��С��Ҫ�ȹ�����ء�����NavMesh�ı߽��")]
    public Vector3 hideEnvrionBoundsSize = new Vector3(100f, 200f, 100f);

    [Header("URP_Asset_Renderer͸������")]
    [Tooltip("�����޸���ҵ�͸�����֣�����Ӧ�����ҹ�䳡��")]
    public RenderObjects playerBehind;
    [Tooltip("�����޸Ĺ����͸�����֣�����Ӧ�����ҹ�䳡��")]
    public RenderObjects enemyBehind;
    public Material oclussionPlayer;
    public Material oclussionPlayerNight;
    public Material oclussionEnemy;
    public Material oclussionEnemyNight;

    [HideInInspector] public EnemyController[] enemys; //����active��������￨��
    [HideInInspector] public List<Transform> environments; //����active�������������
    [HideInInspector] public List<Transform> grounds; //����active�������������

    Bounds lastEnemyBounds;
    Bounds lastEnvrionmentBounds;
    //Bounds lastGroundBounds;

    List<IGameObserver> gameObservers = new List<IGameObserver>();

    private float updateSwitchTime;


    protected override void Awake()
    {
        //ע�����ûע�ᵥ���ᱨ��
        base.Awake();
        DontDestroyOnLoad(this);

        if (SceneManager.GetActiveScene().name != "Main")
            NewSceneLoaded();
    }

    //��ʼ���������л��³���ʱ�������������
    internal void NewSceneLoaded()
    {
        //������й���(�������صģ��޸��л�����ʱ���ﱻ���ص�bug)
        enemys = FindObjectsOfType<EnemyController>(true);
        //����Ѱ��EnemyParent��Ŀ
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

        //��ʼ����Դ�б�
        grounds.Clear();
        environments.Clear();
        environmentRoot = FindObjectOfType<EnvironmentRoot>()?.gameObject.transform;

        //������л�����Դ��transform
        //���������Ground�ֿ����
        MeshFilter[] enviMesh = environmentRoot.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < enviMesh.Length; i++)
            if (enviMesh[i].gameObject.CompareTag("Ground"))
                grounds.Add(enviMesh[i].transform);
            else
            if (enviMesh[i].gameObject.CompareTag("Portal"))
            {
                //ʲô�������������ű��ֿ���
            }
            else
                environments.Add(enviMesh[i].transform);

        //Debug.Log("enemys.Length:" + enemys.Length + "  grounds.Count:" + grounds.Count
        //     + "  environments.Count:" + environments.Count);

        //��һ��ûע��playerStats���൱�ڹر����й����active
        ///���պý����NavMeshδ���ɣ�Agent��������⣩
        SwitchEnemysActive();
        SwitchEnvironmentActive();

        //֪ͨAutoSaveCanvas����������ɫ
        AutoSaveCanvas.Instance?.SetTextColor();
    }

    internal void LeaveScene()
    {
        //�����Դ�б��������
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
            //��������ҵľ��룬��̬���ù����active���������
            //��һ��ʱ�����һ�μ���
            //
            //:���ÿ�θ���ʱ�Ŀ�������
            if (updateSwitchTime < 0)
            {
                //�ȸ��»������ٸ��¹����ֹ�����������Ȼ��û������
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
                //ֻ�رչ���
                if (enemys[i] && !enemys[i].gameObject.CompareTag("Boss") && !enemys[i].isDead
                    && enemys[i].AttackTarget == null 
                    && enemys[i].enemyBehaviorState!=EnemyBehaviorState.ESCAPE)
                    enemys[i].GetComponent<NavMeshAgent>().enabled = false;
            }
        else
            for (int i = 0; i < enemys.Length; i++)
            {
                //ֻ�ر�Boss
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
            //NavMesh���º�����OnNavMesh�ĵ���Agent����
            if (enemys[i] && !enemys[i].isDead 
                && enemys[i].GetComponent<NavMeshAgent>().isOnNavMesh)
                enemys[i].GetComponent<NavMeshAgent>().enabled = true;
        }
    }

    private void SwitchEnemysActive()
    {
        if (playerStats == null || enemys == null) return;

        //���ع���ı߽緶Χ
        Bounds enemyBounds = QuantizedBound(hideEnemyBoundsSize);
        Bounds bossBounds = QuantizedBound(hideBossBoundsSize);

        RaycastHit upHighDownHit;

        if (enemyBounds != lastEnemyBounds)
        {
            //ö�ٳ������й���
            for (int i = 0; i < enemys.Length; i++)
                if (enemys[i] && !enemys[i].isSpawning)
                {
                    enemys[i].numInArray = i;

                    //Boss��Ground�ڣ���Χ��������ʾ������������С��Χ�ڱ�����ʾ
                    if ((enemys[i].gameObject.CompareTag("Boss") && bossBounds.Contains(enemys[i].transform.position))
                        || enemyBounds.Contains(enemys[i].transform.position))
                    {
                        if (enemys[i].transform.position.IsOnGround(out upHighDownHit))
                        {
                            //��������y������Ground��
                            enemys[i].transform.position = new Vector3(enemys[i].transform.position.x,
                               upHighDownHit.point.y, enemys[i].transform.position.z);
                                enemys[i].gameObject.SetActive(true);
                                enemys[i].GetComponent<NavMeshAgent>().enabled = true;
                        }
                    }
                    else
                    {
                        //����ʷ��ķ������ʾ
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

        //Ϊ�˴������������У��ر�Ground��̬����
        //����Ground�ı߽緶Χ
        //bounds = QuantizedBound(hideGroundBoundsSize);
        //if (bounds != lastGroundBounds)
        //{
        //    //ö�ٳ�������Ground
        //    for (int i = 0; i < grounds.Count; i++)
        //    {
        //        if (bounds.Contains(grounds[i].position))
        //            grounds[i].gameObject.SetActive(true);
        //        else
        //            grounds[i].gameObject.SetActive(false);
        //    }
        //    lastGroundBounds = bounds;
        //}

        //���ػ�������ı߽緶Χ
        bounds = QuantizedBound(hideEnvrionBoundsSize);

        if (bounds != lastEnvrionmentBounds)
        {
            //ö�ٳ������л�������
            for (int i = 0; i < environments.Count; i++)
            {
                if (bounds.Contains(environments[i].position))
                    environments[i].gameObject.SetActive(true);
                else
                    environments[i].gameObject.SetActive(false);
            }
            //�������˸���bounds���ֲ�����ô������
            lastEnvrionmentBounds = bounds;
        }
    }

    internal void SetPlayerOnGround()
    {
        //����yֵ����ֹ��ҳ����ڼв���
        RaycastHit upHighDownHit;
        if (player != null && player.transform.position.IsOnGround(out upHighDownHit))
        {
            player.transform.position = new Vector3(player.transform.position.x,
                upHighDownHit.point.y, player.transform.position.z);
        }
    }

    private Bounds QuantizedBound(Vector3 boundsSize,float quantizeStep = 0.1f)
    {
        //�����߽磬���ڴ�С�仯quantizeStepʱ����
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
        //CinemachineFreeLook��ʼ������仯�ӽǣ�Ϊ�˱�֤����ӽǣ���ʼ�ǹرյ�
        //followCamera.gameObject.SetActive(true);
        
        if (followCamera!=null)
        {
            followCamera.Follow = playerStats.transform.GetChild(2); //TopRigLookAt
            followCamera.LookAt = playerStats.transform.GetChild(2);
            followCamera.GetRig(0).LookAt = playerStats.transform.GetChild(2);
            followCamera.GetRig(1).LookAt = playerStats.transform.GetChild(3); //MiddleRigLookAt
            followCamera.GetRig(2).LookAt = playerStats.transform.GetChild(4); //BottomRigLookAt
        }

        //ҹ�����ľ۹�Ƴ���
        NightSpotLight nightSpotLight = FindObjectOfType<NightSpotLight>();
        if (nightSpotLight != null)
            nightSpotLight.focusPointOnPlayer = playerStats.transform.GetChild(4); //BottomRigLookAt

        //LocalNavMeshBuilder�����ĵ�����
        LocalNavMeshBuilder localNavMeshBuilder = FindObjectOfType<LocalNavMeshBuilder>();
        if (localNavMeshBuilder != null)
            localNavMeshBuilder.m_Tracked = player.transform;

        //���ݵ�ǰ����������͸��Ч������
        if (SceneManager.GetActiveScene().name == "02 Nightmare Scene")
        {
            //����ҹ���͸������
            playerBehind.settings.overrideMaterial = oclussionPlayerNight;
            enemyBehind.settings.overrideMaterial = oclussionEnemyNight;
        }
        else
        {
            //���ð����͸������
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
            //�����֪ͨ�������
            observer.EndNotify();
        }
    }
    public void NotifyObserversGameLoad()
    {
        foreach (var observer in gameObservers)
        {
            //�����֪ͨ��Ҷ���
            observer.LoadNotify();
        }
    }

    //�������������λ������ͬ���͹������һ��ʱ���active
    public void InstantiateEnemy(GameObject enemyType, Vector3 lastPos, int numInArray)
    {
        //ȷ���µĳ�����
        RaycastHit upHighDownHit;
        Vector3 newPos = lastPos;
        float minRange = 30, maxRange = 60;
        for (int i = 0; i < 5; i++)
        {
            Vector2 vec = Random.insideUnitCircle;
            //�������λ��
            newPos = lastPos + (new Vector3(vec.x, 0, vec.y)) * UnityEngine.Random.Range(minRange, maxRange);

            //��֤������Ground�ϣ�ÿ����С��Χ
            if (newPos.IsOnGround(out upHighDownHit))
            {
                //����yֵ
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

        //���ɲ���������
        GameObject enemy = Instantiate(enemyPrefabs[type], newPos, Quaternion.identity, enemyRoots[type]);
        enemys[numInArray] = enemy.GetComponent<EnemyController>();
        enemys[numInArray].isSpawning = true;
        enemys[numInArray].numInArray = numInArray;

        //��ʱ���أ�һ��ʱ������
        enemy.SetActive(false);
        StartCoroutine("LaterActiveEnemy", numInArray);
    }

    IEnumerator LaterActiveEnemy(int numInArray)
    {
        yield return new WaitForSeconds(Random.Range(rebornMinTime, rebornMaxTime));
        //�ж��Ƿ�Ϊ�������Է������л�����bug
        if (enemys != null && numInArray < enemys.Length && enemys[numInArray].isSpawning)
        {
            enemys[numInArray].gameObject.SetActive(true);
            enemys[numInArray].isSpawning = false;
        }
    }

    //���������л��ƺ��ӣ��۲����÷�Χ
    private void OnDrawGizmosSelected()
    {
        if (playerStats == null) return;

        //���ƹ�����ʾ��Χ
        Gizmos.color = Color.red;
        Bounds bounds = QuantizedBound(hideEnemyBoundsSize);
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        //���Ƴ���������ʾ��Χ
        Gizmos.color = Color.green;
        bounds = QuantizedBound(hideEnvrionBoundsSize);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        //Gizmos.DrawWireSphere(playerStats.transform.position, hideEnvironmentRadius);

        //����Ground��ʾ��Χ
        Gizmos.color = Color.yellow;
        bounds = QuantizedBound(hideBossBoundsSize);
        Gizmos.DrawWireCube(bounds.center, bounds.size);
        //Gizmos.DrawWireSphere(playerStats.transform.position, hideGroundRadius);
    }
}
