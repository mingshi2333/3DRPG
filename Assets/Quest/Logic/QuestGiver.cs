using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//做成任务列表，可在同一个Npc身上挂载一系列任务
///任务List中只包含可领取的任务，完成任务时如有后续任务，再添加至List中
///一些任务有触发条件，满足条件时，添加至对应Npc的任务List中
///npc身上同时进行多个任务时，每次检查是否有完成的任务，优先显示对话；否则显示最近任务对话

//设计一系列任务，新手引导、推进游戏、讲述作者独白
//收集树枝换箭矢的任务，长期有效

//制作弓箭玩法
///Animator设置IK，手部抬起对准射出方向，更真实
///左键移动，左键点击目标近身攻击
///右键蓄力，期间面朝目标，空格变为向后冲刺（并面朝目标）
///松开右键射出，会预测目标走向；按下左键取消

[RequireComponent(typeof(DialogueController))]
public class QuestGiver : MonoBehaviour
{
    DialogueController controller;
    QuestData_SO currentQuest;

    public string npcName;
    public List<QuestData_SO> quests;

    #region 获得任务状态
    public bool IsStarted
    {
        get
        {
            if (QuestManager.Instance.HaveQuest(currentQuest))
            {
                return QuestManager.Instance.GetTask(currentQuest).IsStarted;
            }
            else return false;
        }
    }
    public bool IsComplete
    {
        get
        {
            if (QuestManager.Instance.HaveQuest(currentQuest))
            {
                return QuestManager.Instance.GetTask(currentQuest).IsComplete;
            }
            else return false;
        }
    }
    public bool IsFinished
    {
        get
        {
            if (QuestManager.Instance.HaveQuest(currentQuest))
            {
                return QuestManager.Instance.GetTask(currentQuest).IsFinished;
            }
            else return false;
        }
    }
    #endregion

    private void Awake()
    {
        controller = GetComponent<DialogueController>();
    }

    private void Start()
    {
        UpdateQuestDialogue();
        //controller.currentData = startDialogue;
        //currentQuest = controller.currentData.GetQuest();
    }

    private bool FindCurrentQuest()
    {
        if (quests.Count == 0) return false;
        //对quests进行排序
        ///升序排序：isFinished < isStart&&!isComplete < !isStart < isComplete&&!isFinished
        quests.Sort();

        while (quests.Count > 1 && QuestManager.Instance.HaveQuest(quests[quests.Count - 1]) &&
            QuestManager.Instance.GetTask(quests[quests.Count - 1]).IsFinished)
        {
            quests.RemoveAt(quests.Count - 1);
        }

        //从资源中加载对应任务
        currentQuest = quests[quests.Count - 1];

        return currentQuest;
    }

    public void UpdateQuestDialogue()
    {
        if (!FindCurrentQuest()) return;

        //更新当前Quest对应的Dialogue
        if (!IsStarted)
            controller.currentData = currentQuest.startDialogue;
        else
        if (IsFinished)
            controller.currentData = currentQuest.finishDialogue;
        else
        if (IsComplete)
            controller.currentData = currentQuest.completeDialogue;
        else
            controller.currentData = currentQuest.progressDialogue;
    }
}
