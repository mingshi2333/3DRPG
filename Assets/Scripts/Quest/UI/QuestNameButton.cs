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

    //�б�������Button����ʾ�Ҳ���������
    public void UpdateQuestContent()
    {
        //questContentText.text = currentData.description;
        QuestUI.Instance.SetupRequireList(currentData);

        //����֮ǰ��Reward
        foreach (Transform item in QuestUI.Instance.rewardTransform)
        {
            Destroy(item.gameObject);
        }

        //��ʾ�µ�Reward
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
            questNameText.text = questData.questName + " (���)";
            if (questData.isFinished)
                questNameText.color = Color.gray;
        }
        else
            questNameText.text = questData.questName;
    }
}
