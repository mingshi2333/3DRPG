using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestNameButton : MonoBehaviour
{
    public Text questNameText;
    public QuestData_SO currentData;
    //public Text questContentText;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(UpdateQuestContent);
    }

    //列表按下任务Button后，显示右侧所有详情
    public void UpdateQuestContent()
    {
        //questContentText.text = currentData.description;
        QuestUI.Instance.SetupRequireList(currentData);

        //销毁之前的Reward
        foreach (Transform item in QuestUI.Instance.rewardTransform)
        {
            Destroy(item.gameObject);
        }

        //显示新的Reward
        foreach (var item in currentData.rewards)
        {
            QuestUI.Instance.SetupRewardItem(item.itemData, item.amountInInventory);
        }

    }

    public void SetupNameButton(QuestData_SO questData)
    {
        currentData = questData;

        if (questData.isComplete)
        {
            questNameText.text = questData.questName + " (完成)";
            if (questData.isFinished)
                questNameText.color = Color.gray;
        }
        else
            questNameText.text = questData.questName;
    }
}
