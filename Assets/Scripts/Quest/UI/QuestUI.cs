using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestUI : Singleton<QuestUI>
{
    [Header("Elements")]
    public GameObject questPanel;
    public ItemTooltip tooltip;
    [HideInInspector]public bool isOpen;

    [Header("Quest Name")]
    public RectTransform questListTransform;
    public QuestNameButton questNameButton;

    [Header("Text Content")]
    public Text questTitleText;
    public Text questContentText;

    [Header("Requirement")]
    public RectTransform requireTransform;
    public QuestRequirement requirement;

    [Header("Reward Panel")]
    public RectTransform rewardTransform;
    public ItemUI rewardUI;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q)
            || (isOpen && Input.GetKeyDown(KeyCode.Escape)))
        {
            isOpen = !isOpen;
            questPanel.SetActive(isOpen);
            questContentText.text = "";
            //显示任务面板内容
            SetupQuestList();

            tooltip.gameObject.SetActive(false);
        }
    }

    public void SetupQuestList()
    {
        //销毁先前任务列表
        foreach (Transform item in questListTransform)
        {
            Destroy(item.gameObject);
        }
        //销毁Reward
        foreach (Transform item in rewardTransform)
        {
            Destroy(item.gameObject);
        }
        //销毁Requirement
        foreach (Transform item in requireTransform)
        {
            Destroy(item.gameObject);
        }

        bool hasShowFirstQuest = false;
        //设置任务列表
        foreach (var task in QuestManager.Instance.tasks)
        {
            var newTask = Instantiate(questNameButton, questListTransform);
            newTask.SetupNameButton(task.questData);
            //newTask.questContentText = questContentText;
            if (!hasShowFirstQuest && !task.questData.isFinished)
            {
                //默认选择显示第一个未完成的任务
                hasShowFirstQuest = true;
                newTask.GetComponent<Button>().Select();
                newTask.UpdateQuestContent();
            }
        }

    }

    public void SetupRequireList(QuestData_SO questData)
    {
        questTitleText.text = questData.questName;
        questContentText.text = questData.description;

        //销毁之前的Requirement
        foreach (Transform item in requireTransform)
        {
            Destroy(item.gameObject);
        }
        //设置Requirement
        foreach (var require in questData.questRequires)
        {
            var q = Instantiate(requirement, requireTransform);
            if (questData.isFinished)
                q.SetupRequirment(require.name, true);
            else
                q.SetupRequirment(require.name, require.requireAmount, require.currentAmount);
        }
    }

    public void SetupRewardItem(ItemData_SO itemData,int amount)
    {
        var item = Instantiate(rewardUI, rewardTransform);
        item.SetupItemUI(itemData, amount);
    }
}
