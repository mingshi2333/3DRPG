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
    private void OnValidate() //仅限在编辑器内执行，导致打包游戏后字典空了
    {
        dialogueIndex.Clear();
        foreach (var piece in dialoguePieces)
        {
            //和唯一Npc对话的时候，省去设置头像的重复劳动
            if (piece.image == null) piece.image = defaultImage;

            if (!dialogueIndex.ContainsKey(piece.ID))
                dialogueIndex.Add(piece.ID, piece);
         }
    }
#else
    private void Awake() //保证在打包执行的游戏里第一时间获得对话的所有字典匹配
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
        //循环对话里的每句话，找到Quest返回
        QuestData_SO currentQuest = null;
        foreach (var piece in dialoguePieces)
        {
            if (piece.quest != null)
                currentQuest = piece.quest;
        }

        return currentQuest;
    }
}
