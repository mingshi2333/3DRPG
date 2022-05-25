using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName ="New Dialogue",menuName ="Dialogue/Dialogue Data")]
public class DialogueData_SO : ScriptableObject
{
    public Sprite defaultImage;
    public List<DialoguePiece> dialoguePieces = new List<DialoguePiece>();
    public Dictionary<string, DialoguePiece> dialogueIndex = new Dictionary<string, DialoguePiece>();

#if UNITY_EDITOR
    private void OnValidate() //�����ڱ༭����ִ�У����´����Ϸ���ֵ����
    {
        dialogueIndex.Clear();
        foreach (var piece in dialoguePieces)
        {
            //��ΨһNpc�Ի���ʱ��ʡȥ����ͷ����ظ��Ͷ�
            if (piece.image == null) piece.image = defaultImage;

            if (!dialogueIndex.ContainsKey(piece.ID))
                dialogueIndex.Add(piece.ID, piece);
         }
    }
#else
    private void Awake() //��֤�ڴ��ִ�е���Ϸ���һʱ���öԻ��������ֵ�ƥ��
    {
        dialogueIndex.Clear();
        foreach (var piece in dialoguePieces)
        {
            if (!dialogueIndex.ContainsKey(piece.ID))
                dialogueIndex.Add(piece.ID, piece);
        }
    }
#endif

    public QuestData_SO GetQuest()
    {
        //ѭ���Ի����ÿ�仰���ҵ�Quest����
        QuestData_SO currentQuest = null;
        foreach (var piece in dialoguePieces)
        {
            if (piece.quest != null)
                currentQuest = piece.quest;
        }

        return currentQuest;
    }
}
