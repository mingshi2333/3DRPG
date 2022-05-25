using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class DragPanel : MonoBehaviour, IDragHandler, IPointerDownHandler
{
    RectTransform rectTransform;
    Canvas canvas;

    Vector2 defaultAnchoredPos;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = InventoryManager.Instance.GetComponent<Canvas>();

        defaultAnchoredPos = rectTransform.anchoredPosition;
    }

    private void OnEnable()
    {
        rectTransform.anchoredPosition = defaultAnchoredPos;
    }

    public void OnDrag(PointerEventData eventData)
    {
        //���������λ���ƶ�������canvas.scaleFactor������Ļ����
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //ÿ�ε㰴�������㼶���Ӷ�������ʾ
        rectTransform.SetSiblingIndex(2);
    }
}
