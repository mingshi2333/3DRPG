using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
// [System.Serializable]//系统序列化才能显示出来事件
// public class EventVector3:UnityEvent<Vector3> {}//返回坐标事件

public class MouseManager : MonoBehaviour
{
    private RaycastHit hitInfo;
    public event Action<Vector3> OnMouseClicked;//using system可以直接创建一个event
    public event Action<GameObject> OnEnemyClicked;
    public static MouseManager Instance;
    public Texture2D point,doorway,attack,target,arrow;
    
    void Awake()
    {
        if(Instance != null)

            Destroy(gameObject);
        Instance = this;
    }
     void Update()
    {
        SetCursorTexture();
        MouseControl();
    }

    void SetCursorTexture()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hitInfo))
        {
            //切换鼠标贴图
            switch(hitInfo.collider.gameObject.tag)
            {
                case "Ground":
                    Cursor.SetCursor(target,new Vector2(16,16),CursorMode.Auto);
                    break;
                case "Enemy":
                    Cursor.SetCursor(attack,new Vector2(16,16),CursorMode.Auto);
                    break;
            }
        }
    }

    void MouseControl()
    {
        if (Input.GetMouseButtonDown(0) && hitInfo.collider != null)
        {
            if(hitInfo.collider.gameObject.CompareTag("Ground"))
                OnMouseClicked?.Invoke(hitInfo.point);
            if(hitInfo.collider.gameObject.CompareTag("Enemy"))
                OnEnemyClicked?.Invoke(hitInfo.collider.gameObject);
        }
    }
}
