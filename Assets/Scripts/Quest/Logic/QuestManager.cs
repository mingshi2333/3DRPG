using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class QuestManager : Singleton<QuestManager>
{
    [System.Serializable]
    public class QuestTask
    {
        public QuestData_SO questData;
        public bool IsStarted { get { return questData.isStarted; } set { questData.isStarted = value; } }
        public bool IsFinished 
        { 
            get { return questData.isFinished; } 
            set
            {
                //��һ�α���������ʱ�������������񣨱�����ѭ����
                if (!questData.isFinished && value)
                {
                    questData.isFinished = value;
                    questData.UnlockSubsequentTasks();
                }
                else
                    questData.isFinished = value;
            } 
        }
        public bool IsComplete { get { return questData.isComplete; } set { questData.isComplete = value; } }
    }

    public List<QuestTask> tasks = new List<QuestTask>();

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);

        //������ʼ���񸱱�
        for (int i = 0; i < tasks.Count; i++)
        {
            tasks[i].questData = Instantiate(tasks[i].questData);
        }
    }

    public void SaveQuestManager()
    {
        //�����������
        PlayerPrefs.SetInt("TaskCount", tasks.Count);
        for (int i = 0; i < tasks.Count; i++)
        {
            SaveManager.Instance.Save(tasks[i].questData, "task" + i);
        }

        //����Npc��������(ֻ������Դ��)
        var NPCs = FindObjectsOfType<QuestGiver>();
        foreach (var npc in NPCs)
        {
            PlayerPrefs.SetInt(npc.npcName + "_QuestCount", npc.quests.Count);

            for (int i = 0; i < npc.quests.Count; i++)
            {
                PlayerPrefs.SetString(npc.npcName + "_Quest_" + i, npc.quests[i].sourceName);
            }
        }

        PlayerPrefs.Save();
    }

    public void LoadQuestManager()
    {
        var questCount = PlayerPrefs.GetInt("TaskCount");
        tasks.Clear();

        //��ȡ�������
        for (int i = 0; i < questCount; i++)
        {
            var newQuest = ScriptableObject.CreateInstance<QuestData_SO>();
            SaveManager.Instance.Load(newQuest, "task" + i);
            tasks.Add(new QuestTask { questData = newQuest });
        }
        //��ȡNpc��������
        var NPCs = FindObjectsOfType<QuestGiver>();
        string path;
        foreach (var npc in NPCs)
        {
            if (!PlayerPrefs.HasKey(npc.npcName + "_QuestCount")) continue;
            npc.quests.Clear();
            for (int i = 0; i < PlayerPrefs.GetInt(npc.npcName + "_QuestCount"); i++)
            {
                path = "Game Data/Quest Data/" + PlayerPrefs.GetString(npc.npcName + "_Quest_" + i);
                //���ض�Ӧ�����(ע���Ǹ�����)�������б�
                npc.quests.Add(Instantiate(Resources.Load<QuestData_SO>(path)));
            }
        }
    }

    //��������orʰȡ��Ʒ
    public void UpdateQuestProgress(string requireName, int amount)
    {
        foreach (var task in tasks)
        {
            //�ѽ�������������
            if (task.IsFinished) continue;

            var matchRequire = task.questData.questRequires.Find(r => r.name == requireName);
            if (matchRequire != null)
                matchRequire.currentAmount += amount;

            task.questData.CheckQuestProgress();
        }
    }

    public bool HaveQuest(QuestData_SO data)
    {
        //Linq �����Һͷ��� tasks �б�������ָ������������
        if (data != null)
        {
            return tasks.Any(q => q.questData.questName == data.questName);
        }
        return false;
    }

    public QuestTask GetTask(QuestData_SO data)
    {
        return tasks.Find(q => q.questData.questName == data.questName);
    }
}
