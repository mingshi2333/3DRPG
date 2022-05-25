using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

[CreateAssetMenu(fileName = "New Quest", menuName = "Quest/Quest Data")]
public class QuestData_SO : ScriptableObject, IComparable<QuestData_SO>
{
    [System.Serializable]
    public class QuestRequire
    {
        public string name;
        public int requireAmount;
        public int currentAmount;
    }

    [Tooltip("���ڼ�����������ļ�����ͬ��������·���ͺ�׺��")]
    public string sourceName;
    public string questName;
    public string startNpc;
    [TextArea]
    public string description;

    public bool isStarted;
    [HideInInspector] public bool isComplete;
    [HideInInspector] public bool isFinished;

    public DialogueData_SO startDialogue;
    public DialogueData_SO progressDialogue;
    public DialogueData_SO completeDialogue;
    public DialogueData_SO finishDialogue;

    public List<QuestRequire> questRequires = new List<QuestRequire>();
    public List<InventoryItem> rewards = new List<InventoryItem>();

    public string[] subsequentQuests;

    internal void UnlockSubsequentTasks()
    {
        if (subsequentQuests.Length == 0) return;

        //����finishedʱ���ã�����������������������ӦNpc
        var NPCs= FindObjectsOfType<QuestGiver>();
        foreach (var npc in NPCs)
        {
            var addQuests = subsequentQuests.
                Where(t => Resources.Load<QuestData_SO>
                ("Game Data/Quest Data/" + t).startNpc == npc.npcName);
            foreach (var quest in addQuests)
            {
                //ע����ӵ��Ǹ�����������dataԴ�ļ�
                npc.quests.Add(
                    Instantiate(Resources.Load<QuestData_SO>("Game Data/Quest Data/"+quest)));
                //�Ƴ�QuestManager�Ķ�Ӧ����
                var task = QuestManager.Instance.GetTask(npc.quests[npc.quests.Count - 1]);
                if (task != null)
                    QuestManager.Instance.tasks.Remove(task);
            }
        }
    }

    public void CheckQuestProgress()
    {
        var finishRequires = questRequires.Where(r => r.currentAmount >= r.requireAmount);
        isComplete = finishRequires.Count() == questRequires.Count;
    }

    public void GiveRewards()
    {
        foreach (var reward in rewards)
        {
            if (reward.amountInInventory < 0)
            {
                //�۳�������Ʒ
                int requireCount = Mathf.Abs(reward.amountInInventory);
                int decrease;
                InventoryItem item = null;
                while(requireCount>0 && InventoryManager.Instance.QuestItemInBag(reward.itemData) != null)
                {
                    item = InventoryManager.Instance.QuestItemInBag(reward.itemData);
                    decrease = Mathf.Min(requireCount, item.amountInInventory);
                    requireCount -= decrease;
                    item.amountInInventory -= decrease;
                    //�������Ʒ
                    if (item.amountInInventory == 0) item.itemData = null;
                }
                while (requireCount > 0 && InventoryManager.Instance.QuestItemInAction(reward.itemData) != null)
                {
                    item = InventoryManager.Instance.QuestItemInAction(reward.itemData);
                    decrease = Mathf.Min(requireCount, item.amountInInventory);
                    requireCount -= decrease;
                    item.amountInInventory -= decrease;
                    //�������Ʒ
                    if (item.amountInInventory == 0) item.itemData = null;
                }
            }
            else
            {
                //���������
                int lastAmount = InventoryManager.Instance.inventoryData.
                    AddItem(reward.itemData, reward.amountInInventory);
                //�Ų��µĵ���������
                if(lastAmount>0)
                {
                    var itemObj = Instantiate(reward.itemData.objectOnWorld);
                    Vector2 c = UnityEngine.Random.insideUnitCircle;
                    itemObj.transform.position = GameManager.Instance.player.transform.position
                        + new Vector3(c.x, 0.8f, c.y) * 2.5f;

                    itemObj.transform.rotation = UnityEngine.Random.rotation;
                }
            }

            InventoryManager.Instance.inventoryUI.RefreshUI();
            InventoryManager.Instance.actionUI.RefreshUI();
        }
    }

    //��ǰ��������Ҫ �ռ�/���� ��Ŀ�������б�
    public List<string> RequireTargetNames()
    {
        List<string> targetNameList = new List<string>();

        foreach (var require in questRequires)
        {
            targetNameList.Add(require.name);
        }

        return targetNameList;
    }

    //����������ȣ��Ƚ�������������ȼ���
    ///����һ����ͬһ��Npc���ض������ʱ����������ʾ����֮һ
    ///��������isFinished < isStart&&!isComplete < !isStart < isComplete&&!isFinished
    public int CompareTo(QuestData_SO other)
    {
        bool otherHave = QuestManager.Instance.HaveQuest(other);
        var otherTask = QuestManager.Instance.GetTask(other);
        
        bool thisHave = QuestManager.Instance.HaveQuest(this);
        var thisTask = QuestManager.Instance.GetTask(this);

        //!otherHave
        if (!otherHave || !otherTask.IsStarted)
        {
            if (!thisHave || !thisTask.IsStarted) return 0;
            if (thisTask.IsComplete && !thisTask.IsFinished) return 1;
            return -1;
        }

        //���¶���otherHave

        if (otherTask.IsFinished)
        {
            if (thisHave && thisTask.IsFinished) return 0;
            return 1;
        }

        if (otherTask.IsStarted && !otherTask.IsComplete)
        {
            if (!thisHave) return 1;
            if (thisTask.IsStarted && !thisTask.IsComplete) return 0;
            if (thisTask.IsFinished) return -1;
            return 1;
        }

        if (otherTask.IsComplete && !otherTask.IsFinished)
        {
            if (!thisHave) return -1;
            if (thisTask.IsComplete && !thisTask.IsFinished) return 0;
            return -1;
        }

        return 0;
    }
}
