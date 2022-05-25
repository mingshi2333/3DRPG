using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System;
using UnityEngine.EventSystems;

//[System.Serializable]
//public class EventVector3 : UnityEvent<Vector3> { }

[DefaultExecutionOrder(-490)]
public class MouseManager : Singleton<MouseManager>
{

    public Texture2D point, doorway, attack, target, arrow;
    [Range(0.5f, 2)] public float MouseSize = 1.0f;
    RaycastHit hit;
    //public EventVector3 OnMouseClicked;
    public event Action<Vector3> OnMouseClicked;
    public event Action<GameObject> OnEnemyClicked;
    public event Action OnPlayerKeyInput;

    private GameObject hitObj;
    private EnemyController enemy;

    [HideInInspector] public bool isDragging;
    [HideInInspector] public bool onNoBlockCanvas;

    private void Update()
    {
        //SetCursorTexture();
        
        if (InteractWithUI()) return;
        MouseControl();
        KeyControl();
    }

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(this);
    }

    //void SetCursorTexture()
    void OnGUI()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Vector2 mP = Input.mousePosition;
        float w = MouseSize, h = MouseSize;

        //鼠标在UI面板上时
        if (InteractWithUI())
        {
            w *= target.width;
            h *= target.height;
            //Cursor.SetCursor(target, new Vector2(0, 0), CursorMode.Auto);
            GUI.DrawTexture(new Rect(mP.x - w * 0.375f, Screen.height - mP.y - h * 0.25f,
                w, h), target);
            return;
        }

        //鼠标点击和鼠标图标，注意怪物还有一层Insight可透视；无视Trigger
        if (Physics.Raycast(ray, out hit, 99f, LayerMask.GetMask("Ground", "Enemy","Enemy Insight", "Rock","Npc", "Item"),
            QueryTriggerInteraction.Ignore))
        {
            switch (hit.collider.gameObject.tag)
            {
                case "Enemy":
                case "Boss":
                case "Attackable":
                    //Cursor.SetCursor(target, new Vector2(0, 0), CursorMode.Auto);
                    w *= attack.width;
                    h *= attack.height;
                    GUI.DrawTexture(new Rect(mP.x - w * 0.375f, Screen.height - mP.y - h * 0.375f,
                        w, h), attack);
                    break;
                case "Item":
                    w *= point.width;
                    h *= point.height;
                    GUI.DrawTexture(new Rect(mP.x - w * 0.375f, Screen.height - mP.y - h * 0.375f,
                        w, h), point);
                    break;
                default:
                    w *= target.width;
                    h *= target.height;
                    //Cursor.SetCursor(target, new Vector2(0, 0), CursorMode.Auto);
                    GUI.DrawTexture(new Rect(mP.x - w * 0.375f, Screen.height - mP.y - h * 0.25f,
                        w, h), target);
                    break;
            }
        }
        else
        {
            w *= target.width;
            h *= target.height;
            //Cursor.SetCursor(target, new Vector2(0, 0), CursorMode.Auto);
            GUI.DrawTexture(new Rect(mP.x - w * 0.375f, Screen.height - mP.y - h * 0.25f,
                w, h), target);
        }
    }

    void MouseControl()
    {
        //游戏暂停时，不输入
        if (Time.timeScale == 0) return;

        if ((Input.GetMouseButton(0) || Input.GetMouseButton(1))
            && hit.collider != null)
        {
            Cursor.visible = false;  //隐藏系统鼠标
            hitObj = hit.collider.gameObject;

            if (hitObj.CompareTag("Ground"))
            {
                OnMouseClicked?.Invoke(hit.point);
            }
            else
            if (hitObj.CompareTag("Enemy") || hitObj.CompareTag("Boss"))
            {
                //Boss等一些怪物，碰撞体绑定在子骨骼上
                enemy = enemy.GetEnemy(hitObj);

                OnEnemyClicked?.Invoke(enemy.gameObject);
            }
            else
            if (hitObj.CompareTag("Attackable"))
            {
                OnEnemyClicked?.Invoke(hitObj);
            }
            else
            if (hitObj.CompareTag("Item"))
            {
                OnMouseClicked?.Invoke(hit.point);
            }
            else
            if (hitObj.CompareTag("Npc"))
            {
                var dir = GameManager.Instance.player.transform.position - hit.transform.position;
                if (dir.magnitude > 2)
                {
                    dir = dir.normalized * 2f;
                    OnMouseClicked?.Invoke(hit.point + dir);
                }
            }
        }
    }

    bool InteractWithUI()
    {
        if (isDragging) return true;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (onNoBlockCanvas) return false;
            else
                return true;
        }

        return false;
    }

    private void KeyControl()
    {
        //游戏暂停时，不输入
        if (Time.timeScale == 0) return;

        //空格键冲刺闪避，有CD
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnPlayerKeyInput?.Invoke();
        }
    }
}
