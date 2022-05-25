using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditorInternal;
using System;
using System.IO;

[CustomEditor(typeof(DialogueData_SO))]
public class DialogueCustomEditor : Editor
{
    public override void OnInspectorGUI()
    {
        if(GUILayout.Button("Open in Editor"))
        {
            DialogueEditor.InitWindow((DialogueData_SO)target);
        }
        base.OnInspectorGUI();

    }
}

public class DialogueEditor : EditorWindow
{
    DialogueData_SO currentData;

    ReorderableList piecesList = null;

    Vector2 scrollPos = Vector2.zero;

    Dictionary<string, ReorderableList> optionListDict = new Dictionary<string, ReorderableList>();

    [MenuItem("DevilPunyMagic/Dialogue Editor")]
    public static void Init()
    {
        DialogueEditor editorWindow = GetWindow<DialogueEditor>("Dialogue Editor");
        editorWindow.autoRepaintOnSceneChange = true;
    }

    public static void InitWindow(DialogueData_SO data)
    {
        DialogueEditor editorWindow = GetWindow<DialogueEditor>("Dialogue Editor");
        editorWindow.currentData = data;
    }
    [OnOpenAsset(465321450)]
    public static bool OpenAsset(int instanceID,int line)
    {
        DialogueData_SO data = EditorUtility.InstanceIDToObject(instanceID) as DialogueData_SO;
        if (data != null)
        {
            DialogueEditor.InitWindow(data);
            return true;
        }

        return false;
    }

    private void OnSelectionChange()
    {
        //选择改变时调用一次
        var newData = Selection.activeObject as DialogueData_SO;
        
        //数据更新
        if (newData != null)
        {
            currentData = newData;
            SetupRecorderableList();
        }
        else
        {
            currentData = null;
            piecesList = null;
        }
        //重新绘制
        Repaint();
    }

    private void OnGUI()
    {
        if (currentData != null)
        {
            EditorGUILayout.LabelField(currentData.name, EditorStyles.boldLabel);
            GUILayout.Space(10);

            //窗口滚动条
            scrollPos = GUILayout.
                BeginScrollView(scrollPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            if (piecesList == null)
                SetupRecorderableList();

            piecesList.DoLayoutList();

            GUILayout.EndScrollView();
        }
        else
        {
            if (GUILayout.Button("Create New Dialogue"))
            {
                string dataPath = "Assets/Resources/Game Data/Dialogue Data/";
                if (!Directory.Exists(dataPath))
                    Directory.CreateDirectory(dataPath);

                DialogueData_SO newData = ScriptableObject.CreateInstance<DialogueData_SO>();
                AssetDatabase.CreateAsset(newData, dataPath + "New Dialogue.asset");
                currentData = newData;
            }
            GUILayout.Label("NO DATA SELECTED!", EditorStyles.boldLabel);
        }
    }

    private void OnDisable()
    {
        optionListDict.Clear();
    }

    void SetupRecorderableList()
    {
        //ReorderableList可调整条目顺序
        piecesList = new ReorderableList(currentData.dialoguePieces, typeof(DialoguePiece),
            true, true, true, true);

        //对于每个条目，绘制标题
        piecesList.drawHeaderCallback += OnDrawPieceHeader;
        //绘制元素
        piecesList.drawElementCallback += OnDrawPieceListElement;
        //设置元素高度
        piecesList.elementHeightCallback += OnHeightChanged;
    }

    private float OnHeightChanged(int index)
    {
        return GetPieceHeight(currentData.dialoguePieces[index]);
    }

    float GetPieceHeight(DialoguePiece piece)
    {
        var height = EditorGUIUtility.singleLineHeight;

        var isExpand = piece.canExpand;

        if (isExpand)
        {
            height += EditorGUIUtility.singleLineHeight * 9;

            var options = piece.options;

            if (options.Count > 1)
            {
                height += EditorGUIUtility.singleLineHeight * options.Count;
            }
        }

        return height;
    }
    //绘制PieceList的元素
    private void OnDrawPieceListElement(Rect rect, int index, bool isActive, bool isFocused)
    {
        //标记为脏数据，从而能够保存/撤销等操作
        EditorUtility.SetDirty(currentData);

        GUIStyle textStyle = new GUIStyle("TextField");
        if (index < currentData.dialoguePieces.Count)
        {
            var currentPiece = currentData.dialoguePieces[index];

            var tempRect = rect;
            tempRect.height = EditorGUIUtility.singleLineHeight;

            currentPiece.canExpand = EditorGUI.Foldout(tempRect, currentPiece.canExpand, currentPiece.ID);
            if (currentPiece.canExpand)
            {
                tempRect.y += tempRect.height;
                //对话ID
                tempRect.width = 30;
                EditorGUI.LabelField(tempRect, "ID");

                tempRect.x += tempRect.width;
                tempRect.width = 100;
                currentPiece.ID = EditorGUI.TextField(tempRect, currentPiece.ID);

                //任务Data
                tempRect.x += tempRect.width + 10;
                EditorGUI.LabelField(tempRect, "Quest");
                tempRect.x += 50;
                currentPiece.quest = EditorGUI.
                    ObjectField(tempRect, currentPiece.quest, typeof(QuestData_SO), false) as QuestData_SO;

                //头像，如果没有则默认头像
                tempRect.y += EditorGUIUtility.singleLineHeight + 10;
                tempRect.x = rect.x;
                tempRect.height = 60;
                tempRect.width = 60;
                if (currentPiece.image == null)
                    currentPiece.image = currentData.defaultImage;
                currentPiece.image = (Sprite)EditorGUI.
                    ObjectField(tempRect, currentPiece.image, typeof(Sprite), false);

                //对话文本框
                tempRect.x += tempRect.width + 5;
                tempRect.width = rect.width - tempRect.x;
                textStyle.wordWrap = true;
                currentPiece.text = (string)EditorGUI.TextField(tempRect, currentPiece.text, textStyle);

                //画选项
                tempRect.y += tempRect.height + 10;
                tempRect.x = rect.x;
                tempRect.width = rect.width;

                string optionListKey = currentPiece.ID + currentPiece.text;

                if (optionListKey != string.Empty)
                {
                    if (!optionListDict.ContainsKey(optionListKey))
                    {
                        var optionList = new ReorderableList(currentPiece.options, typeof(DialogueOption),
                            true, true, true, true);

                        //绘制选项Header
                        optionList.drawHeaderCallback = OnDrawOptionHeader;

                        //绘制选项元素
                        optionList.drawElementCallback = (optionRect, optionIndex, optionActive, optionFocused) =>
                         {
                             OnDrawOptionElement(currentPiece, optionRect, optionIndex, 
                                 optionActive, optionFocused);
                         };
                        //保存Lambda表达式
                        optionListDict[optionListKey] = optionList;
                    }

                    optionListDict[optionListKey].DoList(tempRect);
                }
            }

        }
    }

    private void OnDrawOptionHeader(Rect rect)
    {
        GUI.Label(rect, "Option Text");
        rect.x += rect.width * 0.5f + 10;
        GUI.Label(rect, "Target ID");
        rect.x += rect.width * 0.3f;
        GUI.Label(rect, "Apply");
    }

    //绘制选项元素
    private void OnDrawOptionElement(DialoguePiece currentPiece, Rect optionRect, int optionIndex, bool optionActive, bool optionFocused)
    {
        var currentOption = currentPiece.options[optionIndex];
        var tempRect = optionRect;

        tempRect.width = optionRect.width * 0.5f;
        currentOption.text = EditorGUI.TextField(tempRect, currentOption.text);

        tempRect.x += tempRect.width + 5;
        tempRect.width = optionRect.width * 0.3f;
        currentOption.targetID = EditorGUI.TextField(tempRect, currentOption.targetID);

        tempRect.x += tempRect.width + 5;
        tempRect.width = optionRect.width * 0.2f;
        currentOption.takeQuest = EditorGUI.Toggle(tempRect, currentOption.takeQuest);
    }


    //绘制标题
    private void OnDrawPieceHeader(Rect rect)
    {
        //Header
        GUI.Label(rect, "Dialogue Pieces");

        //默认头像
        var tempRect = rect;
        tempRect.height = EditorGUIUtility.singleLineHeight;
        tempRect.width = 110;
        tempRect.x += 155;
        currentData.defaultImage = (Sprite)EditorGUI.
            ObjectField(tempRect, currentData.defaultImage, typeof(Sprite), false);
    }
}
