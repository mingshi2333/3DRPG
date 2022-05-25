using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

//�ɱ������ڳ�������ȡ�浵ʱ�����Ӧ������λ��
//��ɱС��ʱ���Զ����棨��������λ�ã�
//���벻ͬ�������Զ��浵���޷�ͨ����������ԭ�ȳ���


public class SaveManager : Singleton<SaveManager>
{
    string sceneName = "ContinueScene";
    AutoSaveCanvas autoSaveCanvas;

    public string SceneName { get { return PlayerPrefs.GetString(sceneName,""); } }

    protected override void Awake()
    {
        base.Awake();
        autoSaveCanvas = FindObjectOfType<AutoSaveCanvas>();

        DontDestroyOnLoad(this);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "Main")
        {
            if (Input.GetKeyDown(KeyCode.Backspace))
            {
                SceneController.Instance.TransitionToMain();
            }

            //if (Input.GetKeyDown(KeyCode.S))
            //{
            //    //����������ڳ��������ꡢ����
            //    SavePlayerScene_Pos_Rot();
            //    //�����������
            //    SavePlayerData();
            //}

            //if (Input.GetKeyDown(KeyCode.L))
            //{
            //    //��ȡ������ڳ���������
            //    StartCoroutine(LoadPlayerScene_Pos_Rot());

            //    //��ȡ�������
            //    StartCoroutine(LoadPlayerData());
            //}
        }
    }
    public void SavePlayerScene_Pos_Rot()
    {
        //�Դ����жϣ��Ƿ������˵����룻����Ϊ�������ԣ�������
        if (autoSaveCanvas == null) return;

        GameObject player = GameManager.Instance.player;
        Vector3 pos = player.transform.position;
        CharacterData_SO playerData = GameManager.Instance.playerStats.characterData;

        //������ҳ�����
        PlayerPrefs.SetString(SaveManager.Instance.sceneName, SceneManager.GetActiveScene().name);

        //�����������
        PlayerPrefs.SetFloat(playerData.name + "_Position_X", pos.x);
        PlayerPrefs.SetFloat(playerData.name + "_Position_Y", pos.y);
        PlayerPrefs.SetFloat(playerData.name + "_Position_Z", pos.z);
        //������͵�tag
        PlayerPrefs.DeleteKey(playerData.name + "_DestinationTag");

        pos += player.transform.forward;
        //������ҳ���ʹ��LookAt�����ٱ���һ��������
        PlayerPrefs.SetFloat(playerData.name + "_LookAt_X", pos.x);
        PlayerPrefs.SetFloat(playerData.name + "_LookAt_Y", pos.y);
        PlayerPrefs.SetFloat(playerData.name + "_LookAt_Z", pos.z);


        PlayerPrefs.Save();
    }

    //ͨ��������ǰ����ͬ����ʱ������������ҵ�Ŀ�괫�͵㣬��Ϊ�´εĳ���λ��
    internal void SaveDestinationWhenDiffScene(string sceneName, TransitionDestination.DestinationTag destinationTag)
    {
        CharacterData_SO playerData = GameManager.Instance.playerStats.characterData;
        //�������Ŀ�곡����
        PlayerPrefs.SetString(SaveManager.Instance.sceneName, sceneName);

        //����Ŀ�괫�͵�tag
        PlayerPrefs.SetInt(playerData.name + "_DestinationTag", (int)destinationTag);

        //����������
        PlayerPrefs.DeleteKey(playerData.name + "_Position_X");
        PlayerPrefs.DeleteKey(playerData.name + "_Position_Y");
        PlayerPrefs.DeleteKey(playerData.name + "_Position_Z");

        PlayerPrefs.Save();
    }

    public IEnumerator LoadPlayerScene_Pos_Rot()
    {
        CharacterData_SO playerData = GameManager.Instance.playerStats.characterData;
        float x = 0, y = 0, z = 0;
        bool hasPos = false, hasTag = false;
        TransitionDestination.DestinationTag destinationTag = TransitionDestination.DestinationTag.Enter;

        if (PlayerPrefs.HasKey(playerData.name + "_DestinationTag"))
        {
            //����д��͵�tag�����ȡ����ת���ɶ�Ӧenum����
            destinationTag = (TransitionDestination.DestinationTag)
                PlayerPrefs.GetInt(playerData.name + "_DestinationTag");

            hasTag = true;
        }
       else
        if (PlayerPrefs.HasKey(playerData.name + "_Position_X"))
        {
            //�����ȡ�������
            x = PlayerPrefs.GetFloat(playerData.name + "_Position_X");
            y = PlayerPrefs.GetFloat(playerData.name + "_Position_Y");
            z = PlayerPrefs.GetFloat(playerData.name + "_Position_Z");
            hasPos = true;
        }

        string sceneName = SaveManager.Instance.SceneName;
        if (sceneName != SceneManager.GetActiveScene().name)
        {

            //Debug.Log("SceneManager.GetActiveScene().name: "+ SceneManager.GetActiveScene().name);
            //���������ͬ����������ؽ����쳡�������������
            if (hasTag)
                SceneController.Instance.LoadToDestination(sceneName, destinationTag);
            if (hasPos)
            {
                float lx = x, ly = y, lz = z + 1; //Ĭ��forward��z+1

                if (PlayerPrefs.HasKey(playerData.name + "_LookAt_X"))
                {
                    lx = PlayerPrefs.GetFloat(playerData.name + "_LookAt_X");
                    ly = PlayerPrefs.GetFloat(playerData.name + "_LookAt_Y");
                    lz = PlayerPrefs.GetFloat(playerData.name + "_LookAt_Z");
                }
                SceneController.Instance.LoadToScene(sceneName, new Vector3(x, y, z), new Vector3(lx, ly, lz));
            }
            else
                SceneController.Instance.LoadToScene(sceneName);
        }
        else
        {
            //��ͬ���������������ǰ��destinationTag��������λ��
            if (hasTag)
                SceneController.Instance.LoadToDestination(SceneManager.GetActiveScene().name, destinationTag);
            else if (hasPos)
            {
                GameObject player = GameManager.Instance.player;

                //�ر����agent��Ȼ�����һ֡
                player.GetComponent<NavMeshAgent>().enabled = false;
                player.GetComponent<PlayerController>().timeKeepOnGround = -1f;
                yield return null;

                //��ͬ��������ֵ�������
                player.transform.position = new Vector3(x, y, z);
                //��ֵ��ҳ���
                if (PlayerPrefs.HasKey(playerData.name + "_LookAt_X"))
                {
                    x = PlayerPrefs.GetFloat(playerData.name + "_LookAt_X");
                    y = PlayerPrefs.GetFloat(playerData.name + "_LookAt_Y");
                    z = PlayerPrefs.GetFloat(playerData.name + "_LookAt_Z");

                    player.transform.LookAt(new Vector3(x, y, z));
                }

                //��ֹ��ҳ����ڼв���
                GameManager.Instance.SetPlayerOnGround();

                //�ٴο���agent������Ϊ��UpdateAllEnviNavMesh�￪������������첽���µ����⣩
                while (!player.GetComponent<NavMeshAgent>().enabled)
                {
                    player.GetComponent<NavMeshAgent>().enabled = true;
                    yield return null;
                }
                player.GetComponent<PlayerController>().timeKeepOnGround = 5f;
            }

            //ͬ��������ʱ�ĵ�������Ч��
            SceneFader fade = Instantiate(SceneController.Instance.sceneFaderPrefab);
            if (sceneName == "02 Nightmare Scene")
                yield return StartCoroutine(fade.FadeOutIn(Color.black, Color.black, 0f, 0.5f));
            else
                yield return StartCoroutine(fade.FadeOutIn(Color.white, Color.white, 0f, 0.5f));
        }

        yield break;
    }

    internal void ResetPlayerLevelInData()
    {
        //��������ʷ��ķ��������ҵȼ����Ӷ��øô浵���м�����ս�Ŀռ䣩
        ///������������岻����
        if(!PlayerPrefs.HasKey("ResetPlayerLevelNectEnter"))
        {
            PlayerPrefs.SetInt("ResetPlayerLevelNectEnter", 1);
            //�������Ժ͹���
            Save(GameManager.Instance.playerStats.tempCharaData, GameManager.Instance.playerStats.characterData.name);
            Save(GameManager.Instance.playerStats.tempAtkData, GameManager.Instance.playerStats.attackData.name);

            //��ʾ����ѱ���
            if (autoSaveCanvas != null)
            {
                autoSaveCanvas.StopCoroutine(autoSaveCanvas.FadeOutIn());
                autoSaveCanvas.StartCoroutine(autoSaveCanvas.FadeOutIn());
            }

            //��ʾ��ҽ����õȼ�
            ResetLevelCanvas.Instance?.StartCoroutine(ResetLevelCanvas.Instance.FadeOut());
        }
    }

    public void SavePlayerData()
    {
        //�Դ����жϣ��Ƿ������˵����룻����Ϊ�������ԣ�������
        if (autoSaveCanvas == null) return;

        //�ж��Ƿ��´����õȼ�����������ʷ��ķ�������ǲ��ܱ���
        if (!PlayerPrefs.HasKey("ResetPlayerLevelNectEnter"))
        {
            //�����������
            Save(GameManager.Instance.playerStats.characterData,
                GameManager.Instance.playerStats.characterData.name);
            //�����˱��湥�����ݣ�
            Save(GameManager.Instance.playerStats.attackData,
                GameManager.Instance.playerStats.attackData.name);
        }

        //���汳��
        InventoryManager.Instance.SaveData();

        //�����������
        QuestManager.Instance.SaveQuestManager();

        //��ʾ����ѱ���
        autoSaveCanvas.StopCoroutine(autoSaveCanvas.FadeOutIn());
        autoSaveCanvas.StartCoroutine(autoSaveCanvas.FadeOutIn());
    }

    public IEnumerator LoadPlayerData()
    {
        //�ȴ����ʵ����
        ///����³�����Ҹ��洫���ű仯λ�õ����������Ӻ���Ҳû���⣬ֻ�ǻᱨ�����ѣ�
        while (GameManager.Instance.playerStats == null)
            yield return null;

        CharacterStats playerStats = GameManager.Instance.playerStats;

        //�ȴ�characterData����
        while (playerStats.characterData == null) yield return null;

        //������õȼ��ı��
        PlayerPrefs.DeleteKey("ResetPlayerLevelNectEnter");

        Load(playerStats.characterData, playerStats.characterData.name);
        //��������
        Load(playerStats.attackData, playerStats.attackData.name);
        //Debug.Log("Load�� charaLV:" + GameManager.Instance.playerStats.characterData.lv
        //    + " AtkLV:" + GameManager.Instance.playerStats.attackData.lv);

        //������������ø���
        GameManager.Instance.player.GetComponent<PlayerController>().isDead = false;
        //�����������ʱ�������������������Ҳ�Ѫ����
        playerStats.CurrentHealth
            = (int)Mathf.Clamp(playerStats.CurrentHealth, playerStats.MaxHealth * 0.1f, playerStats.MaxHealth);

        //��ȡ����
        InventoryManager.Instance.LoadData();

        //��ȡ�������
        QuestManager.Instance.LoadQuestManager();

        //�����֪ͨ��Ҷ���
        GameManager.Instance.NotifyObserversGameLoad();
    }

    public void Save(Object data, string key)
    {
        string jsonData = JsonUtility.ToJson(data, true);

        //�� Windows �ϣ�PlayerPrefs �洢�� HKCU\Software\[��˾����]\[��Ʒ����] ���µ�ע����У�
        //���й�˾�Ͳ�Ʒ������ �ڡ�Project Settings�������õ����ơ�
        PlayerPrefs.SetString(key, jsonData);
        //�������д浵
        PlayerPrefs.SetString(sceneName, SceneManager.GetActiveScene().name);

        PlayerPrefs.Save();
    }

    public void Load(Object data, string key)
    {
        if (PlayerPrefs.HasKey(key))
        {
            JsonUtility.FromJsonOverwrite(PlayerPrefs.GetString(key), data);
        }
    }

}
