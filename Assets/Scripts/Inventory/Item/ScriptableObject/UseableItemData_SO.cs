using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName ="Useable Item",menuName ="Inventory/Useable Item Data")]
public class UseableItemData_SO : ScriptableObject
{
    [Header("立即效果")]
    //红蘑菇
    public int healthPoint;

    [Header("持续效果")]
    //黄蘑菇
    public float keepTime;
    public float getDefence;
    public float expUpRate;
}
