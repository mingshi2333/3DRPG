using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterStats : MonoBehaviour
{
    public CharacterData_SO characterData;
    public AttackData_SO attackDate;
    [HideInInspector]//在窗口中隐藏
    public bool isCritical;
    #region Read from Data_SO
    public int MaxHealth
    {
        get{if(characterData!=null) return characterData.maxHealth;else return 0;}
        set{characterData.maxHealth = value;}
    }
    public int CurrrentHealth
    {
        get{if(characterData!=null) return characterData.currentHealth;else return 0;}
        set{characterData.currentHealth = value;}
    }
    public int BaseDefence
    {
        get{if(characterData!=null) return characterData.baseDefence;else return 0;}
        set{characterData.baseDefence = value;}
    }
    public int CurrentDefence
    {
        get{if(characterData!=null) return characterData.currentDefence;else return 0;}
        set{characterData.currentDefence = value;}
    }
    #endregion

}
