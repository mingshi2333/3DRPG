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
        //面板跟随鼠标位移移动，除以canvas.scaleFactor修正屏幕缩放
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        //每次点按设置面板层级，从而优先显示
        rectTransform.SetSiblingIndex(2);
    }
}
