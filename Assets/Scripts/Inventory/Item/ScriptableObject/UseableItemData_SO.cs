using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Useable Item",menuName ="Inventory/Useable Item Data")]
public class UseableItemData_SO : ScriptableObject
{
    [Header("����Ч��")]
    //��Ģ��
    public int healthPoint;

    [Header("����Ч��")]
    //��Ģ��
    public float keepTime;
    public float getDefence;
    public float expUpRate;
}
