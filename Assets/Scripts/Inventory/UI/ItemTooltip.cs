using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemTooltip : MonoBehaviour
{
    public Text itemName;
    public Text itemInfo;
    public Text durability;

    RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void SetupToolTip(ItemData_SO item)
    {
        itemName.text = item.itemName;
        itemInfo.text = item.description;

        if (item.itemType == ItemType.Weapon || item.itemType == ItemType.Sheild)
        {
            //�������������ƣ���ʾ�;ö�
            durability.text = "�;öȣ�" + item.currentDurability.ToString("0.")
                + " / " + item.weaponDurability.ToString("0.");
            durability.gameObject.SetActive(true);
        }
        else
            durability.gameObject.SetActive(false);

        ForceRebuildLayouts();
    }

    public void ForceRebuildLayouts()
    {
        //ǿ��ˢ��Layouts������Content Size Filterˢ�²���ʱ������
        var childRects = GetComponentsInChildren<RectTransform>();
        foreach (var rect in childRects)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        }
        LayoutRebuilder.ForceRebuildLayoutImmediate(gameObject.GetComponent<RectTransform>());
    }

    private void OnEnable()
    {
        ForceRebuildLayouts();
        UpdatePosition();
    }

    private void Update()
    {
        ForceRebuildLayouts();
        UpdatePosition();
    }

    void UpdatePosition()
    {
        Vector3 mousePos = Input.mousePosition;

        Vector3[] corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);

        float width = corners[3].x - corners[0].x;
        float height = corners[1].y - corners[0].y;

        if (mousePos.y < height)
            rectTransform.position = mousePos + Vector3.up * height * 0.6f;
        else if (Screen.width - mousePos.x > width)
            rectTransform.position = mousePos + Vector3.right * width * 0.6f;
        else
            rectTransform.position = mousePos + Vector3.left * width * 0.6f;
        
    }
}