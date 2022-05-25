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
                //添加到任务列表里
                //判断是否已有任务
                if (QuestManager.Instance.HaveQuest(newTask.questData))
                {
                    if (!QuestManager.Instance.GetTask(newTask.questData).IsStarted)
                    {
                        //标记任务实例开始（注意不是临时数据newTask）
                        QuestManager.Instance.GetTask(newTask.questData).IsStarted = true;
                    }
                    else
                    if (QuestManager.Instance.GetTask(newTask.questData).IsComplete)
                    {
                        newTask.questData.GiveRewards();
                        //标记任务完成，给予奖励（同时解锁后续任务）
                        QuestManager.Instance.GetTask(newTask.questData).IsFinished = true;
                    }
                }
                else
                {
                    //将新任务放在最前面
                    QuestManager.Instance.tasks.Insert(0, newTask);
                    //标记任务实例开始（注意不是临时数据newTask）
                    QuestManager.Instance.GetTask(newTask.questData).IsStarted = true;

                    foreach (var requireItem in newTask.questData.RequireTargetNames())
                    {
                        //检查背包和快捷栏，如果有任务物品，更新任务进度
                        InventoryManager.Instance.CheckQuestItemInBag(requireItem,
                            QuestManager.Instance.tasks[0].questData);
                    }
                }

                //任务更新时，自动保存（保存位置）
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
