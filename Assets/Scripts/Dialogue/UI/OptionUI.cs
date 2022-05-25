using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OptionUI : MonoBehaviour
{
    public Text optionText;
    private Button thisButton;
    private DialoguePiece currentPiece;

    private bool takeQuest;
    private string nextPieceID;
    private void Awake()
    {
        thisButton = GetComponent<Button>();
        thisButton.onClick.AddListener(OnOptionClicked);
    }

    private void OnOptionClicked()
    {
        if(currentPiece.quest !=null)
        {
            var newTask = new QuestManager.QuestTask
            {
                questData = Instantiate(currentPiece.quest)
            };

            if(takeQuest)
            {
                //��ӵ������б���
                //�ж��Ƿ���������
                if (QuestManager.Instance.HaveQuest(newTask.questData))
                {
                    if (!QuestManager.Instance.GetTask(newTask.questData).IsStarted)
                    {
                        //�������ʵ����ʼ��ע�ⲻ����ʱ����newTask��
                        QuestManager.Instance.GetTask(newTask.questData).IsStarted = true;
                    }
                    else
                    if (QuestManager.Instance.GetTask(newTask.questData).IsComplete)
                    {
                        newTask.questData.GiveRewards();
                        //���������ɣ����轱����ͬʱ������������
                        QuestManager.Instance.GetTask(newTask.questData).IsFinished = true;
                    }
                }
                else
                {
                    //�������������ǰ��
                    QuestManager.Instance.tasks.Insert(0, newTask);
                    //�������ʵ����ʼ��ע�ⲻ����ʱ����newTask��
                    QuestManager.Instance.GetTask(newTask.questData).IsStarted = true;

                    foreach (var requireItem in newTask.questData.RequireTargetNames())
                    {
                        //��鱳���Ϳ�����������������Ʒ�������������
                        InventoryManager.Instance.CheckQuestItemInBag(requireItem,
                            QuestManager.Instance.tasks[0].questData);
                    }
                }

                //�������ʱ���Զ����棨����λ�ã�
                SaveManager.Instance.SavePlayerScene_Pos_Rot();
                SaveManager.Instance.SavePlayerData();
            }
        }

        if (nextPieceID == "")
        {
            DialogueUI.Instance.layoutControl.SetActive(false);
            return;
        }
        else
        {
            DialogueUI.Instance.
                UpdateMainDialogue(DialogueUI.Instance.currentData.dialogueIndex[nextPieceID]);
        }
    }

    public void UpdateOption(DialoguePiece piece, DialogueOption option)
    {
        currentPiece = piece;
        optionText.text = option.text;
        nextPieceID = option.targetID;
        takeQuest = option.takeQuest;
    }
}
