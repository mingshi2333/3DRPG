using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

//����ƶ���Ѫ���ȼ��Ϸ�������������ʾ������뿪��������룻����ʱ�Զ���ʾ

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
            //����ͣ�˵���ѯ�ʷ��������棬��ͣƽ�����롢ƽ���Ƴ�
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
        //������Ч
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
        //������Ч
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
                //ͬ�������ͣ����ж��Ŀ����ƽ�����
                randDoor = (int)Random.Range(0, transitionPoint.destinationTagsOfSameScene.Length);

                //Debug.Log("randDoor:" + transitionPoint.destinationTagsOfSameScene[randDoor]);

                StartCoroutine(Transition(SceneManager.GetActiveScene().name,
                    transitionPoint.destinationTagsOfSameScene[randDoor], true));

                break;
            case TransitionPoint.TransitionType.DifferentScene:

                //�쳡�����ͣ����ж��Ŀ����ƽ�����
                randDoor = (int)Random.Range(0, transitionPoint.destinationTagsOfDiffScene.Length);

                StartCoroutine(Transition(transitionPoint.differetSceneName,
                    transitionPoint.destinationTagsOfDiffScene[randDoor], true));
                break;

            case TransitionPoint.TransitionType.SameSceneRateToDiffScene:
                
                string sceneName = SceneManager.GetActiveScene().name;

                if (Random.value > transitionPoint.rateToDifferentScene)
                {
                    //����ͬ�������ͣ��´δ����������ӣ������������ޣ�
                    transitionPoint.rateToDifferentScene =
                        Mathf.Clamp(transitionPoint.rateToDifferentScene * transitionPoint.rateMultiplyWhenMissToDiff,
                        transitionPoint.startMinRateToDifferentScene, transitionPoint.limitRateToDifferentScene);
                    //�����Ÿ���ҲҪ����
                    PlayerPrefs.SetFloat(sceneName + "_" + transitionPoint.gameObject.name,
                        transitionPoint.rateToDifferentScene);
                    PlayerPrefs.Save();

                    //ͬ�������ͣ����ж��Ŀ����ƽ�����
                    randDoor = (int)Random.Range(0, transitionPoint.destinationTagsOfSameScene.Length);

                    StartCoroutine(Transition(SceneManager.GetActiveScene().name,
                        transitionPoint.destinationTagsOfSameScene[randDoor], true));
                }
                else
                {
                    //�����쳡�����ͣ��������ø���
                    transitionPoint.rateToDifferentScene =
                        Random.Range(transitionPoint.startMinRateToDifferentScene, transitionPoint.startMaxRateToDifferentScene);

                    //�����Ÿ���ҲҪ����
                    PlayerPrefs.SetFloat(sceneName + "_" + transitionPoint.gameObject.name,
                        transitionPoint.rateToDifferentScene);
                    PlayerPrefs.Save();

                    //�쳡�����ͣ����ж��Ŀ����ƽ�����
                    randDoor = (int)Random.Range(0, transitionPoint.destinationTagsOfDiffScene.Length);

                    StartCoroutine(Transition(transitionPoint.differetSceneName,
                        transitionPoint.destinationTagsOfDiffScene[randDoor], true));
                }

                break;
        }
    }

    internal void LoadToScene(string scene)
    {
        //������Ĭ�ϵ�
        Transform start = FindObjectOfType<DefaultStartPoint>().gameObject.transform;

        LoadToScene(scene, start.position, start.position + start.forward);
    }
    internal void LoadToScene(string scene, Vector3 loadPos,Vector3 lookAtPos)
    {
        //�����쳡��
        StartCoroutine(Transition(scene, loadPos, lookAtPos));
    }

    IEnumerator Transition(string scene, Vector3 loadPos, Vector3 lookAtPos)
    {
        //����Ч��
        SceneFader fade = Instantiate(sceneFaderPrefab);
        if (SceneManager.GetActiveScene().name == "02 Nightmare Scene")
            yield return StartCoroutine(fade.FadeOut(0.5f, Color.black));
        else
            yield return StartCoroutine(fade.FadeOut(0.5f, Color.white));

        float fadeInTime = 0.5f;

        if (SceneManager.GetActiveScene().name == scene)
        {
            //�����ͬ������ֻ�޸�λ��
            //�ȴ����ʵ����
            ///����³�����Ҹ��洫���ű仯λ�õ����������Ӻ���Ҳû���⣬ֻ�ǻᱨ�����ѣ�
            while (GameManager.Instance.player == null)
                yield return null;

            player = GameManager.Instance.player;
            playerAgent = player.GetComponent<NavMeshAgent>();

            //�ر����agent��Ȼ�����һ֡
            player.GetComponent<NavMeshAgent>().isStopped = true;
            player.GetComponent<NavMeshAgent>().enabled = false;
            player.GetComponent<PlayerController>().timeKeepOnGround = -1f;
            yield return null;

            player.transform.position = loadPos;
            player.transform.LookAt(lookAtPos);
            //�ٴο���agent������Ϊ��UpdateAllEnviNavMesh�￪������������첽���µ����⣩
            while (!player.GetComponent<NavMeshAgent>().enabled)
            {
                player.GetComponent<NavMeshAgent>().enabled = true;
                yield return null;
            }
            //��ֹ��ҳ����ڼв���
            SetPlayerOnGround();
            player.GetComponent<PlayerController>().timeKeepOnGround = 5f;
        }
        else
        {
            //֪ͨManager���ԭ��������
            GameManager.Instance.LeaveScene();
            //�쳡�����ͣ����س���
            yield return SceneManager.LoadSceneAsync(scene);
            //���ԭ�ȳ��������player
            PlayerController surplusPlayer = FindObjectOfType<PlayerController>();
            if (surplusPlayer != null)
                DestroyImmediate(surplusPlayer.gameObject);

            //�������
            yield return Instantiate(playerPrefab, loadPos, Quaternion.identity);
            //��ֹ��ҳ����ڼв���
            SetPlayerOnGround();
            GameManager.Instance.player.GetComponent<PlayerController>().timeKeepOnGround = 5f;

            //֪ͨManager����
            GameManager.Instance.NewSceneLoaded();
            
            //�����³�������ʼ��԰�ȫ������ʱ������
            fadeInTime = Random.Range(0.5f, 2.5f);
        }

        //���Ż�����
        AudioManager.Instance.OnAfterSceneLoaded();

        //����Ч��
        if (scene == "02 Nightmare Scene")
            yield return StartCoroutine(fade.FadeIn(fadeInTime, Color.black));
        else
            yield return StartCoroutine(fade.FadeIn(fadeInTime, Color.white));
    }

    internal void LoadToDestination(string scene, TransitionDestination.DestinationTag destinationTag)
    {
        //��ȡ�쳡�������͵��������
        StartCoroutine(Transition(scene, destinationTag, false));
    }

    IEnumerator Transition(string scene, TransitionDestination.DestinationTag destinationTag,
        bool isThroughPortal)
    {
        //����Ч��
        SceneFader fade = Instantiate(sceneFaderPrefab);
        if (SceneManager.GetActiveScene().name == "02 Nightmare Scene")
            yield return StartCoroutine(fade.FadeOut(0.5f, Color.black));
        else
            yield return StartCoroutine(fade.FadeOut(0.5f, Color.white));

        float fadeInTime = 0.5f;
        Transform entranceTran;

        if (SceneManager.GetActiveScene().name == scene)
        {
            //ͬ��������
            player = GameManager.Instance.playerStats.gameObject;
            playerAgent = player.GetComponent<NavMeshAgent>();

            if(playerAgent.isOnNavMesh) playerAgent.isStopped = true;

            //����ע����GameObject��Transform����Ȼλ�û����
            entranceTran = GetDestination(destinationTag)?.gameObject.transform;

            //�ر����agent��Ȼ�����һ֡
            player.GetComponent<NavMeshAgent>().isStopped = true;
            player.GetComponent<NavMeshAgent>().enabled = false;
            player.GetComponent<PlayerController>().timeKeepOnGround = -1f;
            yield return null;

            if (entranceTran != null)
                player.transform.SetPositionAndRotation(entranceTran.position, entranceTran.rotation);
            //���һ֡���ԣ�������ͺ������������ܳ��ֵ�NavMesh���²���ʱ
            //Debug.Log("LocalNavMeshBuilder test");
            //FindObjectOfType<LocalNavMeshBuilder>().UpdateAllEnviNavMesh(false);
            //yield return null;

            //�ٴο���agent������Ϊ��UpdateAllEnviNavMesh�￪������������첽���µ����⣩
            while (!player.GetComponent<NavMeshAgent>().enabled)
            {
                player.GetComponent<NavMeshAgent>().enabled = true;
                yield return null;
            }
            //��ֹ��ҳ����ڼв���
            SetPlayerOnGround();
            player.GetComponent<PlayerController>().timeKeepOnGround = 5f;
        }
        else
        {
            //�����ͨ��������ǰ����ͬ�������������ݺ�Ŀ��㣻�����Ƕ���������ֻ������
            if (isThroughPortal)
            {
                SaveManager.Instance.SavePlayerData();
                SaveManager.Instance.SaveDestinationWhenDiffScene(scene, destinationTag);
                //����playerData���ƣ��������ڴ���
                PlayerPrefs.SetString(scene + "_PortalingPlayerData",
                    GameManager.Instance.playerStats.characterData.name);
            }

            //֪ͨManager���ԭ��������
            GameManager.Instance.LeaveScene();

            //�쳡�����ͣ����س���
            yield return SceneManager.LoadSceneAsync(scene);
            //���ԭ�ȳ��������player
            PlayerController surplusPlayer = FindObjectOfType<PlayerController>();
            if (surplusPlayer != null)
                DestroyImmediate(surplusPlayer.gameObject);


            //��ȡ������Ŀ�������
            entranceTran = GetDestination(destinationTag)?.gameObject.transform;
            if(entranceTran ==null)
            {
                //���û�д�����Ŀ��㣬�������Ĭ�ϵ�
                entranceTran = FindObjectOfType<DefaultStartPoint>().gameObject.transform;
            }
            //�������
            yield return Instantiate(playerPrefab, entranceTran.position, entranceTran.rotation);
            //��ֹ��ҳ����ڼв���
            SetPlayerOnGround();
            GameManager.Instance.player.GetComponent<PlayerController>().timeKeepOnGround = 5f;

            //֪ͨManager����
            GameManager.Instance.NewSceneLoaded();

            //�����ͨ�������ţ������������ݣ������Ƕ���������ֻ������
            if (isThroughPortal)
                SaveManager.Instance.StartCoroutine(SaveManager.Instance.LoadPlayerData());

            //�����³�������ʼ��԰�ȫ������ʱ������
            fadeInTime = Random.Range(0.5f, 2.5f);
        }

        //���Ż�����
        AudioManager.Instance.OnAfterSceneLoaded(0f);

        //����Ч��
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
            //�쳡�����ͣ����س���
            yield return SceneManager.LoadSceneAsync(scene);

            //������Ĭ�ϵ�
            Transform start = FindObjectOfType<DefaultStartPoint>().gameObject.transform;
            Vector3 loadPos = start.position;
            Vector3 lookAtPos = loadPos + start.forward;
            //���ԭ�ȳ��������player
            PlayerController surplusPlayer = FindObjectOfType<PlayerController>();
            if (surplusPlayer != null)
                DestroyImmediate(surplusPlayer.gameObject);

            //�������
            yield return Instantiate(playerPrefab, loadPos, Quaternion.identity);
            //��ҳ���
            GameManager.Instance.player.transform.LookAt(lookAtPos);
            //����GameManager���£���Ҫ��
            GameManager.Instance.NewSceneLoaded();

            //��ȡ������ڳ���������
            SaveManager.Instance.StartCoroutine(SaveManager.Instance.LoadPlayerScene_Pos_Rot());
            //��ȡ�������
            SaveManager.Instance.StartCoroutine(SaveManager.Instance.LoadPlayerData());

            //���Ż�����
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
            //�쳡�����ͣ����س���
            yield return SceneManager.LoadSceneAsync(scene);

            //������Ĭ�ϵ�
            Transform start = FindObjectOfType<DefaultStartPoint>().transform;
            Vector3 loadPos = start.position;
            Vector3 lookAtPos = loadPos + start.forward;
            //���ԭ�ȳ��������player
            PlayerController surplusPlayer = FindObjectOfType<PlayerController>();
            if (surplusPlayer != null)
                DestroyImmediate(surplusPlayer.gameObject);

            //�������
            yield return Instantiate(playerPrefab, loadPos, Quaternion.identity);
            //��ҳ���
            GameManager.Instance.player.transform.LookAt(lookAtPos);
            //����GameManager���£���Ҫ��
            GameManager.Instance.NewSceneLoaded();

            //��ֹ��ҳ����ڼв���
            SetPlayerOnGround();
            GameManager.Instance.player.GetComponent<PlayerController>().timeKeepOnGround = 5f;

            //����������ڳ��������ꡢ����
            SaveManager.Instance.SavePlayerScene_Pos_Rot();
            //�����������
            SaveManager.Instance.SavePlayerData();

            //���Ż�����
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
        //����Ҫ�����ȴ�����
        FindObjectOfType<MainMenuCamera>().faderWaitClosed = true;
        fadeNotified = false;

        //���Ż�����
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
        //���ﲻ��Ҫ��ʵ��
    }
}
