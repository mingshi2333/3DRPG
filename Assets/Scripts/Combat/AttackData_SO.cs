using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Attcak",menuName = "Attack/Attack Data")]
public class AttackData_SO : ScriptableObject 
{
    public float attackRange;//基本攻击距离 
    public float skillRange;//远程攻击距离
    public float coolDown;//cd
    public int minDamage;//最小攻击数值
    public int maxDamage;//最大攻击数值
    public float criticalMultiplier;//暴击倍数
    public float criticalChance;//暴击率
}
