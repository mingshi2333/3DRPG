using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.AI;

public class DialogueController : MonoBehaviour
{
    public DialogueData_SO currentData;
    [HideInInspector] public bool canTalk;

    Ray ray;
    RaycastHit hit;
    protected float lerpLookAtTime;
    Transform target;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && currentData != null)
        {
            canTalk = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && currentData != null)
        {
            canTalk = false;
            DialogueUI.Instance.layoutControl.SetActive(false);
        }
    }

    //private void OnCollisionStay(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Player"))
    //    {
    //        //玩家推动Npc时产生斥力！(防止玩家穿透Npc)
    //        collision.gameObject.GetComponent<NavMeshAgent>().velocity =
    //            collision.gameObject.transform.position - transform.position;
    //    }
    //}


    private void Update()
    {
        if (canTalk && !InteractWithUI() &&
            (Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(0)))
        {
            //射线判断是否点击Npc(无视外围Trigger)
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, 99f, LayerMask.GetMask("Npc"), QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    OpenDialogue();

                    lerpLookAtTime = 0.6f;
                    target = GameManager.Instance.player.transform;
                }
            }
        }

        //Npc面朝玩家
        if (lerpLookAtTime > 0 && target != null)
            LerpLookAt(target);
    }


    private void LerpLookAt(Transform targetTransform)
    {
        Vector3 tarDir = targetTransform.position - transform.position;
        tarDir.y = 0;
        Quaternion tarRot = Quaternion.FromToRotation(Vector3.forward, tarDir);

        //Lerp朝向，执行若干帧帧
        transform.rotation = Quaternion.Lerp(transform.rotation, tarRot, 0.06f);

        lerpLookAtTime -= Time.deltaTime;
    }

    private void OpenDialogue()
    {
        //防止重复开启，防止没有对话报错
        if (DialogueUI.Instance.layoutControl.activeSelf
            || currentData.dialoguePieces.Count == 0) return;

        //玩家面朝Npc，并停下
        if (!GameManager.Instance.player.transform.CatchedTarget(transform.position))
            GameManager.Instance.player.transform.LookAt(transform);
        GameManager.Instance.player.GetComponent<NavMeshAgent>().isStopped = true;

        //更新Npc当前QuestDialogue
        GetComponent<QuestGiver>()?.UpdateQuestDialogue();

        //打开UI面板
        //传输对话内容信息
        DialogueUI.Instance.UpdateDialogueData(currentData);
        DialogueUI.Instance.UpdateMainDialogue(currentData.dialoguePieces[0]);
    }

    bool InteractWithUI()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return true;

        return false;
    }
}
