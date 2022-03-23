using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    private static T instance;
    public static T Instance
    {
        get{return instance;}//单例模式只需可读
    }

    protected virtual void Awake()
    {
        if(instance!=null)
            Destroy(gameObject);
        else
        {
            instance =(T)this;            
        }
    }//virtual可以在继承函数中重写。protected子类访问

    public static bool IsInitialized
    {
        get {return instance != null;}
    }//返回当前泛型单例模式是否生成，不为空可以做注册订阅等其他变量

    protected virtual void OnDestroy()
    {
        if(instance == this)
        {
            instance = null;
        }
    }
}
