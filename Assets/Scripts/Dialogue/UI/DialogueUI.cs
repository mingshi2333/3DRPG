using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class DialogueUI : Singleton<DialogueUI>
{
    [Header("Basic Elements")]
    public Image icon;
    public Text mainText;
    public Button nextButton;
    public GameObject layoutControl;

    [Header("Options")]
    public RectTransform optionPanel;
    public OptionUI optionPrefab;

    [Header("Data")]
    public DialogueData_SO currentData;
    int currentIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        nextButton.onClick.AddListener(ContinueDialogue);
    }

    private void Update()
    {
        //�رնԻ�����
        if(layoutControl.activeSelf && Input.GetKeyDown(KeyCode.Escape))
            layoutControl.SetActive(false);
    }

    void ContinueDialogue()
    {
        if (currentIndex < currentData.dialoguePieces.Count)
            UpdateMainDialogue(currentData.dialoguePieces[currentIndex]);
        else
            layoutControl.SetActive(false);
    }

    public void UpdateDialogueData(DialogueData_SO data)
    {
        currentData = data;
        currentIndex = 0;
    }

    public void UpdateMainDialogue(DialoguePiece piece)
    {
        layoutControl.SetActive(true);

        //�����ͼ�꣨ͷ�����Ʒ������ʾ
        if(piece.image !=null)
        {
            icon.enabled = true;
            icon.sprite = piece.image;
        }
        else
            icon.enabled = false;

        //��ʾ��ǰ�Ի���䣨DoTween��̬��ʾ����Ч����
        StopAllCoroutines();
        StartCoroutine(DialogueDoText(piece));

        //Next��ť
        if (piece.options.Count == 0 && currentData && currentData.dialoguePieces.Count > 0)
        {
            nextButton.gameObject.SetActive(true);
            //Nextָ��ǰ�Ի�����һ��
            while (currentData.dialoguePieces[currentIndex] != piece)
                currentIndex++;
            currentIndex++;
        }
        else
            nextButton.gameObject.SetActive(false);

        //����Options
        CreateOptions(piece);
    }

    IEnumerator DialogueDoText(DialoguePiece piece)
    {
        mainText.DOText(string.Empty, 0.3f);
        yield return new WaitForSeconds(0.3f);
        mainText.DOText(piece.text, 1f);
    }

    void CreateOptions(DialoguePiece piece)
    {
        //����ԭ��Options
        if (optionPanel.childCount > 0)
        {
            for (int i = 0; i < optionPanel.childCount; i++)
            {
                Destroy(optionPanel.GetChild(i).gameObject);
            }
        }

        //���ɵ�ǰ����Options
        for (int i = 0; i < piece.options.Count; i++)
        {
            OptionUI option = Instantiate(optionPrefab, optionPanel);
            option.UpdateOption(piece, piece.options[i]);
        }
    }
}
