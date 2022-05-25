using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

//���������б�����ͬһ��Npc���Ϲ���һϵ������
///����List��ֻ��������ȡ�������������ʱ���к��������������List��
///һЩ�����д�����������������ʱ���������ӦNpc������List��
///npc����ͬʱ���ж������ʱ��ÿ�μ���Ƿ�����ɵ�����������ʾ�Ի���������ʾ�������Ի�

//���һϵ�����������������ƽ���Ϸ���������߶���
//�ռ���֦����ʸ�����񣬳�����Ч

//���������淨
///Animator����IK���ֲ�̧���׼������򣬸���ʵ
///����ƶ���������Ŀ�������
///�Ҽ��������ڼ��泯Ŀ�꣬�ո��Ϊ����̣����泯Ŀ�꣩
///�ɿ��Ҽ��������Ԥ��Ŀ�����򣻰������ȡ��

[RequireComponent(typeof(DialogueController))]
public class QuestGiver : MonoBehaviour
{
    DialogueController controller;
    QuestData_SO currentQuest;

    public string npcName;
    public List<QuestData_SO> quests;

    #region �������״̬
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
        //��quests��������
        ///��������isFinished < isStart&&!isComplete < !isStart < isComplete&&!isFinished
        quests.Sort();

        while (quests.Count > 1 && QuestManager.Instance.HaveQuest(quests[quests.Count - 1]) &&
            QuestManager.Instance.GetTask(quests[quests.Count - 1]).IsFinished)
        {
            quests.RemoveAt(quests.Count - 1);
        }

        //����Դ�м��ض�Ӧ����
        currentQuest = quests[quests.Count - 1];

        return currentQuest;
    }

    public void UpdateQuestDialogue()
    {
        if (!FindCurrentQuest()) return;

        //���µ�ǰQuest��Ӧ��Dialogue
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
