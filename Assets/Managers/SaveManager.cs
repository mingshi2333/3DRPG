using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

//可保存所在场景，读取存档时进入对应场景和位置
//击杀小怪时，自动保存（但不保存位置）
//进入不同场景后，自动存档，无法通过读档返回原先场景


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
            //    //保存玩家所在场景、坐标、朝向
            //    SavePlayerScene_Pos_Rot();
            //    //保存玩家数据
            //    SavePlayerData();
            //}

            //if (Input.GetKeyDown(KeyCode.L))
            //{
            //    //读取玩家所在场景和坐标
            //    StartCoroutine(LoadPlayerScene_Pos_Rot());

            //    //读取玩家数据
            //    StartCoroutine(LoadPlayerData());
            //}
        }
    }
    public void SavePlayerScene_Pos_Rot()
    {
        //以此来判断，是否由主菜单进入；否则为场景测试，不保存
        if (autoSaveCanvas == null) return;

        GameObject player = GameManager.Instance.player;
        Vector3 pos = player.transform.position;
        CharacterData_SO playerData = GameManager.Instance.playerStats.characterData;

        //保存玩家场景名
        PlayerPrefs.SetString(SaveManager.Instance.sceneName, SceneManager.GetActiveScene().name);

        //保存玩家坐标
        PlayerPrefs.SetFloat(playerData.name + "_Position_X", pos.x);
        PlayerPrefs.SetFloat(playerData.name + "_Position_Y", pos.y);
        PlayerPrefs.SetFloat(playerData.name + "_Position_Z", pos.z);
        //清除传送点tag
        PlayerPrefs.DeleteKey(playerData.name + "_DestinationTag");

        pos += player.transform.forward;
        //保存玩家朝向！使用LookAt，减少保存一个浮点数
        PlayerPrefs.SetFloat(playerData.name + "_LookAt_X", pos.x);
        PlayerPrefs.SetFloat(playerData.name + "_LookAt_Y", pos.y);
        PlayerPrefs.SetFloat(playerData.name + "_LookAt_Z", pos.z);


        PlayerPrefs.Save();
    }

    //通过传送门前往不同场景时触发：保存玩家的目标传送点，作为下次的出生位置
    internal void SaveDestinationWhenDiffScene(string sceneName, TransitionDestination.DestinationTag destinationTag)
    {
        CharacterData_SO playerData = GameManager.Instance.playerStats.characterData;
        //保存玩家目标场景名
        PlayerPrefs.SetString(SaveManager.Instance.sceneName, sceneName);

        //保存目标传送点tag
        PlayerPrefs.SetInt(playerData.name + "_DestinationTag", (int)destinationTag);

        //清除玩家坐标
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
            //如果有传送点tag，则读取，并转换成对应enum类型
            destinationTag = (TransitionDestination.DestinationTag)
                PlayerPrefs.GetInt(playerData.name + "_DestinationTag");

            hasTag = true;
        }
       else
        if (PlayerPrefs.HasKey(playerData.name + "_Position_X"))
        {
            //否则读取玩家坐标
            x = PlayerPrefs.GetFloat(playerData.name + "_Position_X");
            y = PlayerPrefs.GetFloat(playerData.name + "_Position_Y");
            z = PlayerPrefs.GetFloat(playerData.name + "_Position_Z");
            hasPos = true;
        }

        string sceneName = SaveManager.Instance.SceneName;
        if (sceneName != SceneManager.GetActiveScene().name)
        {

            //Debug.Log("SceneManager.GetActiveScene().name: "+ SceneManager.GetActiveScene().name);
            //如果不是相同场景，则加载进入异场景，分三种情况
            if (hasTag)
                SceneController.Instance.LoadToDestination(sceneName, destinationTag);
            if (hasPos)
            {
                float lx = x, ly = y, lz = z + 1; //默认forward，z+1

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
            //相同场景，则设置玩家前往destinationTag，或坐标位置
            if (hasTag)
                SceneController.Instance.LoadToDestination(SceneManager.GetActiveScene().name, destinationTag);
            else if (hasPos)
            {
                GameObject player = GameManager.Instance.player;

                //关闭玩家agent，然后等下一帧
                player.GetComponent<NavMeshAgent>().enabled = false;
                player.GetComponent<PlayerController>().timeKeepOnGround = -1f;
                yield return null;

                //相同场景，赋值玩家坐标
                player.transform.position = new Vector3(x, y, z);
                //赋值玩家朝向
                if (PlayerPrefs.HasKey(playerData.name + "_LookAt_X"))
                {
                    x = PlayerPrefs.GetFloat(playerData.name + "_LookAt_X");
                    y = PlayerPrefs.GetFloat(playerData.name + "_LookAt_Y");
                    z = PlayerPrefs.GetFloat(playerData.name + "_LookAt_Z");

                    player.transform.LookAt(new Vector3(x, y, z));
                }

                //防止玩家出现在夹层里
                GameManager.Instance.SetPlayerOnGround();

                //再次开启agent（若改为在UpdateAllEnviNavMesh里开启，好像会有异步导致的问题）
                while (!player.GetComponent<NavMeshAgent>().enabled)
                {
                    player.GetComponent<NavMeshAgent>().enabled = true;
                    yield return null;
                }
                player.GetComponent<PlayerController>().timeKeepOnGround = 5f;
            }

            //同场景读档时的淡出淡入效果
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
        //击败梦魇史莱姆后，重置玩家等级（从而让该存档仍有继续挑战的空间）
        ///背包和任务面板不保存
        if(!PlayerPrefs.HasKey("ResetPlayerLevelNectEnter"))
        {
            PlayerPrefs.SetInt("ResetPlayerLevelNectEnter", 1);
            //保存属性和攻击
            Save(GameManager.Instance.playerStats.tempCharaData, GameManager.Instance.playerStats.characterData.name);
            Save(GameManager.Instance.playerStats.tempAtkData, GameManager.Instance.playerStats.attackData.name);

            //提示玩家已保存
            if (autoSaveCanvas != null)
            {
                autoSaveCanvas.StopCoroutine(autoSaveCanvas.FadeOutIn());
                autoSaveCanvas.StartCoroutine(autoSaveCanvas.FadeOutIn());
            }

            //提示玩家将重置等级
            ResetLevelCanvas.Instance?.StartCoroutine(ResetLevelCanvas.Instance.FadeOut());
        }
    }

    public void SavePlayerData()
    {
        //以此来判断，是否由主菜单进入；否则为场景测试，不保存
        if (autoSaveCanvas == null) return;

        //判断是否下次重置等级（击败梦魇史莱姆），不是才能保存
        if (!PlayerPrefs.HasKey("ResetPlayerLevelNectEnter"))
        {
            //保存基本属性
            Save(GameManager.Instance.playerStats.characterData,
                GameManager.Instance.playerStats.characterData.name);
            //别忘了保存攻击数据！
            Save(GameManager.Instance.playerStats.attackData,
                GameManager.Instance.playerStats.attackData.name);
        }

        //保存背包
        InventoryManager.Instance.SaveData();

        //保存任务面板
        QuestManager.Instance.SaveQuestManager();

        //提示玩家已保存
        autoSaveCanvas.StopCoroutine(autoSaveCanvas.FadeOutIn());
        autoSaveCanvas.StartCoroutine(autoSaveCanvas.FadeOutIn());
    }

    public IEnumerator LoadPlayerData()
    {
        //等待玩家实例化
        ///针对新场景玩家跟随传送门变化位置的情况（不添加好像也没问题，只是会报错提醒）
        while (GameManager.Instance.playerStats == null)
            yield return null;

        CharacterStats playerStats = GameManager.Instance.playerStats;

        //等待characterData加载
        while (playerStats.characterData == null) yield return null;

        //清除重置等级的标记
        PlayerPrefs.DeleteKey("ResetPlayerLevelNectEnter");

        Load(playerStats.characterData, playerStats.characterData.name);
        //攻击数据
        Load(playerStats.attackData, playerStats.attackData.name);
        //Debug.Log("Load！ charaLV:" + GameManager.Instance.playerStats.characterData.lv
        //    + " AtkLV:" + GameManager.Instance.playerStats.attackData.lv);

        //死后读档，设置复活
        GameManager.Instance.player.GetComponent<PlayerController>().isDead = false;
        //如果发生传送时死亡的尴尬情况，令玩家残血复活
        playerStats.CurrentHealth
            = (int)Mathf.Clamp(playerStats.CurrentHealth, playerStats.MaxHealth * 0.1f, playerStats.MaxHealth);

        //读取背包
        InventoryManager.Instance.LoadData();

        //读取任务面板
        QuestManager.Instance.LoadQuestManager();

        //向怪物通知玩家读档
        GameManager.Instance.NotifyObserversGameLoad();
    }

    public void Save(Object data, string key)
    {
        string jsonData = JsonUtility.ToJson(data, true);

        //在 Windows 上，PlayerPrefs 存储在 HKCU\Software\[公司名称]\[产品名称] 项下的注册表中，
        //其中公司和产品名称是 在“Project Settings”中设置的名称。
        PlayerPrefs.SetString(key, jsonData);
        //表明已有存档
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
