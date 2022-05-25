using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

//鼠标移动到血条等级上方，会有属性提示，鼠标离开后持续几秒；升级时自动显示

public class SceneController : Singleton<SceneController>, IGameObserver
{
    public GameObject playerPrefab;
    public SceneFader sceneFaderPrefab;
    public PauseMenu pauseMenuPrefab;
    PauseMenu pauseMenu;

    bool fadeNotified;

    GameObject player;
    NavMeshAgent playerAgent;
    internal string roomID;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }

    private void Start()
    {
        GameManager.Instance.AddObserver(this);
    }

    private void Update()
    {
        CheckPauseOrContinue();
    }

    void CheckPauseOrContinue()
    {
        if (SceneManager.GetActiveScene().name != "Main")
        {
            //打开暂停菜单，询问返回主界面，暂停平滑进入、平滑推出
            if (Input.GetKeyDown(KeyCode.Escape) && !InventoryManager.Instance.isOpen
                && !DialogueUI.Instance.layoutControl.activeSelf
                && !QuestUI.Instance.isOpen)
            {
                if (Time.timeScale > 0)
                    StartCoroutine(SmoothPause());
                else
                    StartCoroutine(SmoothContinue());
            }
        }
    }

    IEnumerator SmoothPause()
    {
        //播放音效
        AudioManager.Instance.Play2DSoundEffect(SoundName.Menu_ButtonClick,
            AudioManager.Instance.menuSoundDetailList, transform);

        StopCoroutine(SmoothContinue());
        float delta = 0.1f;
        while (Time.timeScale != 0)
        {
            Time.timeScale = Mathf.Clamp(Time.timeScale - delta, 0, 1);
            yield return null;
        }
        if (pauseMenu != null) Destroy(pauseMenu.gameObject);

        pauseMenu = Instantiate(pauseMenuPrefab);
        yield break;
    }

    IEnumerator SmoothContinue()
    {
        //播放音效
        AudioManager.Instance.Play2DSoundEffect(SoundName.Menu_ButtonSelect,
            AudioManager.Instance.menuSoundDetailList, transform);

        StopCoroutine(SmoothPause());
        if (pauseMenu == null) yield break;

        pauseMenu?.gameObject?.SetActive(false);
        if (pauseMenu != null) Destroy(pauseMenu.gameObject);
        float delta = 0.1f;
        while (Time.timeScale != 1)
        {
            Time.timeScale = Mathf.Clamp(Time.timeScale + delta, 0, 1);
            yield return null;
        }
        yield break;
    }

    public void TransitionToDestination(TransitionPoint transitionPoint)
    {
        int randDoor;
        switch (transitionPoint.transitionType)
        {
            case TransitionPoint.TransitionType.SameScene:
                //同场景传送，若有多个目标则平均随机
                randDoor = (int)Random.Range(0, transitionPoint.destinationTagsOfSameScene.Length);

                //Debug.Log("randDoor:" + transitionPoint.destinationTagsOfSameScene[randDoor]);

                StartCoroutine(Transition(SceneManager.GetActiveScene().name,
                    transitionPoint.destinationTagsOfSameScene[randDoor], true));

                break;
            case TransitionPoint.TransitionType.DifferentScene:

                //异场景传送，若有多个目标则平均随机
                randDoor = (int)Random.Range(0, transitionPoint.destinationTagsOfDiffScene.Length);

                StartCoroutine(Transition(transitionPoint.differetSceneName,
                    transitionPoint.destinationTagsOfDiffScene[randDoor], true));
                break;

            case TransitionPoint.TransitionType.SameSceneRateToDiffScene:
                
                string sceneName = SceneManager.GetActiveScene().name;

                if (Random.value > transitionPoint.rateToDifferentScene)
                {
                    //触发同场景传送；下次触发概率增加（但不超过上限）
                    transitionPoint.rateToDifferentScene =
                        Mathf.Clamp(transitionPoint.rateToDifferentScene * transitionPoint.rateMultiplyWhenMissToDiff,
                        transitionPoint.startMinRateToDifferentScene, transitionPoint.limitRateToDifferentScene);
                    //传送门概率也要保存
                    PlayerPrefs.SetFloat(sceneName + "_" + transitionPoint.gameObject.name,
                        transitionPoint.rateToDifferentScene);
                    PlayerPrefs.Save();

                    //同场景传送，若有多个目标则平均随机
                    randDoor = (int)Random.Range(0, transitionPoint.destinationTagsOfSameScene.Length);

                    StartCoroutine(Transition(SceneManager.GetActiveScene().name,
                        transitionPoint.destinationTagsOfSameScene[randDoor], true));
                }
                else
                {
                    //触发异场景传送，首先重置概率
                    transitionPoint.rateToDifferentScene =
                        Random.Range(transitionPoint.startMinRateToDifferentScene, transitionPoint.startMaxRateToDifferentScene);

                    //传送门概率也要保存
                    PlayerPrefs.SetFloat(sceneName + "_" + transitionPoint.gameObject.name,
                        transitionPoint.rateToDifferentScene);
                    PlayerPrefs.Save();

                    //异场景传送，若有多个目标则平均随机
                    randDoor = (int)Random.Range(0, transitionPoint.destinationTagsOfDiffScene.Length);

                    StartCoroutine(Transition(transitionPoint.differetSceneName,
                        transitionPoint.destinationTagsOfDiffScene[randDoor], true));
                }

                break;
        }
    }

    internal void LoadToScene(string scene)
    {
        //出生在默认点
        Transform start = FindObjectOfType<DefaultStartPoint>().gameObject.transform;

        LoadToScene(scene, start.position, start.position + start.forward);
    }
    internal void LoadToScene(string scene, Vector3 loadPos,Vector3 lookAtPos)
    {
        //加载异场景
        StartCoroutine(Transition(scene, loadPos, lookAtPos));
    }

    IEnumerator Transition(string scene, Vector3 loadPos, Vector3 lookAtPos)
    {
        //淡出效果
        SceneFader fade = Instantiate(sceneFaderPrefab);
        if (SceneManager.GetActiveScene().name == "02 Nightmare Scene")
            yield return StartCoroutine(fade.FadeOut(0.5f, Color.black));
        else
            yield return StartCoroutine(fade.FadeOut(0.5f, Color.white));

        float fadeInTime = 0.5f;

        if (SceneManager.GetActiveScene().name == scene)
        {
            //如果是同场景，只修改位置
            //等待玩家实例化
            ///针对新场景玩家跟随传送门变化位置的情况（不添加好像也没问题，只是会报错提醒）
            while (GameManager.Instance.player == null)
                yield return null;

            player = GameManager.Instance.player;
            playerAgent = player.GetComponent<NavMeshAgent>();

            //关闭玩家agent，然后等下一帧
            player.GetComponent<NavMeshAgent>().isStopped = true;
            player.GetComponent<NavMeshAgent>().enabled = false;
            player.GetComponent<PlayerController>().timeKeepOnGround = -1f;
            yield return null;

            player.transform.position = loadPos;
            player.transform.LookAt(lookAtPos);
            //再次开启agent（若改为在UpdateAllEnviNavMesh里开启，好像会有异步导致的问题）
            while (!player.GetComponent<NavMeshAgent>().enabled)
            {
                player.GetComponent<NavMeshAgent>().enabled = true;
                yield return null;
            }
            //防止玩家出生在夹层里
            SetPlayerOnGround();
            player.GetComponent<PlayerController>().timeKeepOnGround = 5f;
        }
        else
        {
            //通知Manager清除原场景数据
            GameManager.Instance.LeaveScene();
            //异场景传送，加载场景
            yield return SceneManager.LoadSceneAsync(scene);
            //清除原先场景多余的player
            PlayerController surplusPlayer = FindObjectOfType<PlayerController>();
            if (surplusPlayer != null)
                DestroyImmediate(surplusPlayer.gameObject);

            //生成玩家
            yield return Instantiate(playerPrefab, loadPos, Quaternion.identity);
            //防止玩家出生在夹层里
            SetPlayerOnGround();
            GameManager.Instance.player.GetComponent<PlayerController>().timeKeepOnGround = 5f;

            //通知Manager更新
            GameManager.Instance.NewSceneLoaded();
            
            //进入新场景，开始相对安全，淡入时间可随机
            fadeInTime = Random.Range(0.5f, 2.5f);
        }

        //播放环境音
        AudioManager.Instance.OnAfterSceneLoaded();

        //淡入效果
        if (scene == "02 Nightmare Scene")
            yield return StartCoroutine(fade.FadeIn(fadeInTime, Color.black));
        else
            yield return StartCoroutine(fade.FadeIn(fadeInTime, Color.white));
    }

    internal void LoadToDestination(string scene, TransitionDestination.DestinationTag destinationTag)
    {
        //读取异场景，传送点生成玩家
        StartCoroutine(Transition(scene, destinationTag, false));
    }

    IEnumerator Transition(string scene, TransitionDestination.DestinationTag destinationTag,
        bool isThroughPortal)
    {
        //淡出效果
        SceneFader fade = Instantiate(sceneFaderPrefab);
        if (SceneManager.GetActiveScene().name == "02 Nightmare Scene")
            yield return StartCoroutine(fade.FadeOut(0.5f, Color.black));
        else
            yield return StartCoroutine(fade.FadeOut(0.5f, Color.white));

        float fadeInTime = 0.5f;
        Transform entranceTran;

        if (SceneManager.GetActiveScene().name == scene)
        {
            //同场景传送
            player = GameManager.Instance.playerStats.gameObject;
            playerAgent = player.GetComponent<NavMeshAgent>();

            if(playerAgent.isOnNavMesh) playerAgent.isStopped = true;

            //这里注意是GameObject的Transform，不然位置会出错
            entranceTran = GetDestination(destinationTag)?.gameObject.transform;

            //关闭玩家agent，然后等下一帧
            player.GetComponent<NavMeshAgent>().isStopped = true;
            player.GetComponent<NavMeshAgent>().enabled = false;
            player.GetComponent<PlayerController>().timeKeepOnGround = -1f;
            yield return null;

            if (entranceTran != null)
                player.transform.SetPositionAndRotation(entranceTran.position, entranceTran.rotation);
            //多等一帧试试，解决传送后立即读档可能出现的NavMesh更新不及时
            //Debug.Log("LocalNavMeshBuilder test");
            //FindObjectOfType<LocalNavMeshBuilder>().UpdateAllEnviNavMesh(false);
            //yield return null;

            //再次开启agent（若改为在UpdateAllEnviNavMesh里开启，好像会有异步导致的问题）
            while (!player.GetComponent<NavMeshAgent>().enabled)
            {
                player.GetComponent<NavMeshAgent>().enabled = true;
                yield return null;
            }
            //防止玩家出生在夹层里
            SetPlayerOnGround();
            player.GetComponent<PlayerController>().timeKeepOnGround = 5f;
        }
        else
        {
            //如果是通过传送门前往不同场景，保存数据和目标点；否则是读档，这里只负责传送
            if (isThroughPortal)
            {
                SaveManager.Instance.SavePlayerData();
                SaveManager.Instance.SaveDestinationWhenDiffScene(scene, destinationTag);
                //保存playerData名称，声明正在传送
                PlayerPrefs.SetString(scene + "_PortalingPlayerData",
                    GameManager.Instance.playerStats.characterData.name);
            }

            //通知Manager清除原场景数据
            GameManager.Instance.LeaveScene();

            //异场景传送，加载场景
            yield return SceneManager.LoadSceneAsync(scene);
            //清除原先场景多余的player
            PlayerController surplusPlayer = FindObjectOfType<PlayerController>();
            if (surplusPlayer != null)
                DestroyImmediate(surplusPlayer.gameObject);


            //获取传送门目标点坐标
            entranceTran = GetDestination(destinationTag)?.gameObject.transform;
            if(entranceTran ==null)
            {
                //如果没有传送门目标点，则出生在默认点
                entranceTran = FindObjectOfType<DefaultStartPoint>().gameObject.transform;
            }
            //生成玩家
            yield return Instantiate(playerPrefab, entranceTran.position, entranceTran.rotation);
            //防止玩家出生在夹层里
            SetPlayerOnGround();
            GameManager.Instance.player.GetComponent<PlayerController>().timeKeepOnGround = 5f;

            //通知Manager更新
            GameManager.Instance.NewSceneLoaded();

            //如果是通过传送门，则加载玩家数据；否则是读档，这里只负责传送
            if (isThroughPortal)
                SaveManager.Instance.StartCoroutine(SaveManager.Instance.LoadPlayerData());

            //进入新场景，开始相对安全，淡入时间可随机
            fadeInTime = Random.Range(0.5f, 2.5f);
        }

        //播放环境音
        AudioManager.Instance.OnAfterSceneLoaded(0f);

        //淡入效果
        if (scene == "02 Nightmare Scene")
            yield return StartCoroutine(fade.FadeIn(fadeInTime, Color.black));
        else
            yield return StartCoroutine(fade.FadeIn(fadeInTime, Color.white));
    }

    private void SetPlayerOnGround()
    {
        GameManager.Instance.SetPlayerOnGround();
    }

    private TransitionDestination GetDestination(TransitionDestination.DestinationTag destinationTag)
    {
        var entrances = FindObjectsOfType<TransitionDestination>();

        foreach (var e in entrances)
        {
            if (e.destinationTag == destinationTag)
                return e;
        }

        return null;
    }

    public void TransitionToLoadLevel()
    {
        StartCoroutine(LoadSavedGame(SaveManager.Instance.SceneName));
    }

    IEnumerator LoadSavedGame(string scene)
    {
        SceneFader fade = Instantiate(sceneFaderPrefab);
        if (scene != null)
        {
            yield return StartCoroutine(fade.FadeOut(Random.Range(0.5f, 2.5f), Color.white, true));
            //异场景传送，加载场景
            yield return SceneManager.LoadSceneAsync(scene);

            //出生在默认点
            Transform start = FindObjectOfType<DefaultStartPoint>().gameObject.transform;
            Vector3 loadPos = start.position;
            Vector3 lookAtPos = loadPos + start.forward;
            //清除原先场景多余的player
            PlayerController surplusPlayer = FindObjectOfType<PlayerController>();
            if (surplusPlayer != null)
                DestroyImmediate(surplusPlayer.gameObject);

            //生成玩家
            yield return Instantiate(playerPrefab, loadPos, Quaternion.identity);
            //玩家朝向
            GameManager.Instance.player.transform.LookAt(lookAtPos);
            //提醒GameManager更新（重要）
            GameManager.Instance.NewSceneLoaded();

            //读取玩家所在场景和坐标
            SaveManager.Instance.StartCoroutine(SaveManager.Instance.LoadPlayerScene_Pos_Rot());
            //读取玩家数据
            SaveManager.Instance.StartCoroutine(SaveManager.Instance.LoadPlayerData());

            //播放环境音
            AudioManager.Instance.OnAfterSceneLoaded();

            if (scene == "02 Nightmare Scene")
                yield return StartCoroutine(fade.FadeIn(Random.Range(0.5f, 2.5f), Color.black));
            else
                yield return StartCoroutine(fade.FadeIn(Random.Range(0.5f, 2.5f), Color.white));
        }

        yield break;
    }

    public void TransitionToFirstLevel()
    {
        StartCoroutine(LoadNewGame("01 Novice Scene"));
    }

    IEnumerator LoadNewGame(string scene)
    {
        SceneFader fade = Instantiate(sceneFaderPrefab);
        if (scene != null)
        {
            yield return StartCoroutine(fade.FadeOut(Random.Range(0.5f, 2.5f), Color.white, true));
            //异场景传送，加载场景
            yield return SceneManager.LoadSceneAsync(scene);

            //出生在默认点
            Transform start = FindObjectOfType<DefaultStartPoint>().transform;
            Vector3 loadPos = start.position;
            Vector3 lookAtPos = loadPos + start.forward;
            //清除原先场景多余的player
            PlayerController surplusPlayer = FindObjectOfType<PlayerController>();
            if (surplusPlayer != null)
                DestroyImmediate(surplusPlayer.gameObject);

            //生成玩家
            yield return Instantiate(playerPrefab, loadPos, Quaternion.identity);
            //玩家朝向
            GameManager.Instance.player.transform.LookAt(lookAtPos);
            //提醒GameManager更新（重要）
            GameManager.Instance.NewSceneLoaded();

            //防止玩家出生在夹层里
            SetPlayerOnGround();
            GameManager.Instance.player.GetComponent<PlayerController>().timeKeepOnGround = 5f;

            //保存玩家所在场景、坐标、朝向
            SaveManager.Instance.SavePlayerScene_Pos_Rot();
            //保存玩家数据
            SaveManager.Instance.SavePlayerData();

            //播放环境音
            AudioManager.Instance.OnAfterSceneLoaded();

            yield return StartCoroutine(fade.FadeIn(Random.Range(0.5f, 2.5f), Color.white));
        }

        yield break;
    }

    public void TransitionToMain()
    {
        StartCoroutine(LoadMain());
    }

    IEnumerator LoadMain()
    {
        SceneFader fade = Instantiate(sceneFaderPrefab);
        if (SceneManager.GetActiveScene().name == "02 Nightmare Scene")
            yield return StartCoroutine(fade.FadeOut(Random.Range(0.5f, 2.5f), Color.black));
        else
            yield return StartCoroutine(fade.FadeOut(Random.Range(0.5f, 2.5f), Color.white));

        yield return SceneManager.LoadSceneAsync("Main");
        //不需要白屏等待加载
        FindObjectOfType<MainMenuCamera>().faderWaitClosed = true;
        fadeNotified = false;

        //播放环境音
        AudioManager.Instance.OnAfterSceneLoaded();

        yield return StartCoroutine(fade.FadeIn(Random.Range(0.5f, 2.5f), Color.white));
    }

    public void EndNotify()
    {
        if (!fadeNotified)
        {
            fadeNotified = true;

            StartCoroutine(LaterLoadMain());
        }
    }

    IEnumerator LaterLoadMain()
    {
        yield return new WaitForSeconds(Random.Range(3f, 6f));
        StartCoroutine(LoadMain());
    }

    public void LoadNotify()
    {
        //这里不需要不实现
    }
}
