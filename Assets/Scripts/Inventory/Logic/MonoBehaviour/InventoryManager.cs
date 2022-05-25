using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : Singleton<InventoryManager>
{
    public class DragData
    {
        public SlotHolder originalHolder;
        public RectTransform originalParent;
    }

    [Header("Inventory Data")]
    public InventoryData_SO inventoryTemplate;
    public InventoryData_SO actionTemplate;
    public InventoryData_SO equipmentTemplate;

    public InventoryData_SO inventoryData;
    public InventoryData_SO actionData;
    public InventoryData_SO equipmentData;

    [Header("Containers")]
    public ContainerUI inventoryUI;
    public ContainerUI actionUI;
    public ContainerUI equipmentUI;

    [Header("Drag Canvas")] 
    public Canvas dragCanvas;
    public DragData currentDrag;

    [Header("UI Panel")]
    public GameObject bagPanel;
    public GameObject statsPanel;

   [HideInInspector]public bool isOpen = false;

    [Header("Stats Text")]
    public Text maxHealth;
    public Text currentDefence;
    public Text healthRegenerateIn10Sec;
    public Text inToLongSightRad;
    public Text sheildDefence;
    public Text attackDamage;
    public Text attackCoolDown;
    public Text attackRange;
    public Text criticalMultiplier;
    public Text criticalChance;
    public Text sheildHitBackDefence;
    public Text sheildDamage;
    public Text sheildCritMult;
    public Text sheildCritChan;

    [Header("Tooltip")]
    public ItemTooltip tooltip;

    [Header("Status Display")]
    public CanvasGroup durabiDownCanvasGroup;

    protected override void Awake()
    {
        base.Awake();
        if (inventoryTemplate != null)
        {
            //初始化背包内的装备耐久
            foreach (var item in inventoryTemplate.items)
                item.itemData?.Awake();
            inventoryData = Instantiate(inventoryTemplate);
        }

        if (actionTemplate != null)
            actionData = Instantiate(actionTemplate);
        if (equipmentTemplate != null)
        {
            //初始化武器和盾牌耐久度（如果有）
            equipmentTemplate.items[0].itemData?.Awake();
            equipmentTemplate.items[1].itemData?.Awake();

            equipmentData = Instantiate(equipmentTemplate);
        }
    }

    private void Start()
    {
        inventoryUI.RefreshUI();
        actionUI.RefreshUI();
        equipmentUI.RefreshUI();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.B)
            || (isOpen && Input.GetKeyDown(KeyCode.Escape)))
        {
            isOpen = !isOpen;
            bagPanel.SetActive(isOpen);
            statsPanel.SetActive(isOpen);
        }
        //如果开启，实时更新属性面板
        if (isOpen) UpdateStatsText();
    }

    //背包内的物品为场景临时数据，需要序列化存取
    ///关键值：path，数量，耐久
    public void SaveData()
    {
        //SaveManager.Instance.Save(inventoryData, inventoryData.name);
        //SaveManager.Instance.Save(actionData, actionData.name);
        //SaveManager.Instance.Save(equipmentData, equipmentData.name);
        Save(inventoryData);
        Save(actionData);
        Save(equipmentData);
    }

    private void Save(InventoryData_SO data)
    {
        string path;
        for (int i = 0; i < data.items.Count; i++)
        {
            if (data.items[i].itemData == null)
            {
                //若是空格子，清除对应字段
                PlayerPrefs.DeleteKey(data.name + "_itemDataPath_" + i.ToString());
                PlayerPrefs.DeleteKey(data.name + "_amountInInventory_" + i.ToString());
                PlayerPrefs.DeleteKey(data.name + "_currentDurability_" + i.ToString());
                //后面无须执行
                continue;
            }

            //保存物品
            path = "Game Data/Item Data/" + data.items[i].itemData.folderPath;
            if (path[path.Length - 1] != '/') path += "/";

            path += data.items[i].itemData.objectOnWorld.name;
            PlayerPrefs.SetString(data.name + "_itemDataPath_" + i.ToString(), path);

            PlayerPrefs.SetInt(data.name + "_amountInInventory_" + i.ToString(),
                data.items[i].amountInInventory);
            PlayerPrefs.SetFloat(data.name + "_currentDurability_" + i.ToString(),
                data.items[i].itemData.currentDurability);
        }
        PlayerPrefs.Save();
    }

    public void LoadData()
    {
        //SaveManager.Instance.Load(inventoryData, inventoryData.name);
        //SaveManager.Instance.Load(actionData, actionData.name);
        //SaveManager.Instance.Load(equipmentData, equipmentData.name);
        Load(inventoryData);
        Load(actionData);
        Load(equipmentData);
    }

    private void Load(InventoryData_SO data)
    {
        string path;
        for (int i = 0; i < data.items.Count; i++)
        {
            if (!PlayerPrefs.HasKey(data.name + "_itemDataPath_" + i.ToString()))
            {
                data.items[i].itemData = null;
                data.items[i].amountInInventory = 0;
                continue;
            }
            //加载物品
            path = PlayerPrefs.GetString(data.name + "_itemDataPath_" + i.ToString());
            data.items[i].itemData = Instantiate(Resources.Load<ItemData_SO>(path));

            data.items[i].amountInInventory =
                PlayerPrefs.GetInt(data.name + "_amountInInventory_" + i.ToString());
            data.items[i].itemData.currentDurability =
                PlayerPrefs.GetFloat(data.name + "_currentDurability_" + i.ToString());
        }

        inventoryUI.RefreshUI();
        actionUI.RefreshUI();
        equipmentUI.RefreshUI();
    }

    public void UpdateStatsText()
    {
        CharacterStats STs = GameManager.Instance.playerStats;
        if (STs == null) return;

        maxHealth.text = STs.MaxHealth.ToString();
        currentDefence.text = STs.CurrentDefence.ToString();
        healthRegenerateIn10Sec.text = STs.HealthRegenerateIn10Sec.ToString("0.0");

        inToLongSightRad.text = STs.InSightRadius.ToString("0.0") +
            " / " + STs.OutSightRadius.ToString("0.0") + " / " + STs.LongSightRadius.ToString("0.0");
        sheildDefence.text = STs.SheildDefence.ToString();

        attackDamage.text = STs.MinDamage.ToString() + "~" + STs.MaxDamage.ToString();
        attackCoolDown.text = STs.AttackCoolDown.ToString("0.00");
        attackRange.text = STs.AttackRange.ToString("0.0");

        criticalMultiplier.text = STs.CriticalMultiplier.ToString("0.00");
        criticalChance.text = STs.CriticalChance.ToString("0.%");
        sheildHitBackDefence.text = STs.ShieldHitBackDefence.ToString();

        sheildDamage.text = STs.ShieldDamage.ToString();
        sheildCritMult.text = STs.ShieldCritMult.ToString("0.00");
        sheildCritChan.text = STs.ShieldCritChan.ToString("0.%");
    }

    #region 检查拖拽物品是否在每一个 Slot 范围内
    public bool CheckInInventoryUI(Vector3 position)
    {
        for (int i = 0; i < inventoryUI.slotHolders.Length; i++)
        {
            RectTransform t = inventoryUI.slotHolders[i].transform as RectTransform;

            if(RectTransformUtility.RectangleContainsScreenPoint(t,position))
            {
                return true;
            }
        }
        return false;
    }

    public bool CheckInActionUI(Vector3 position)
    {
        for (int i = 0; i < actionUI.slotHolders.Length; i++)
        {
            RectTransform t = actionUI.slotHolders[i].transform as RectTransform;

            if (RectTransformUtility.RectangleContainsScreenPoint(t, position))
            {
                return true;
            }
        }
        return false;
    }
    public bool CheckInEquitmentUI(Vector3 position)
    {
        for (int i = 0; i < equipmentUI.slotHolders.Length; i++)
        {
            RectTransform t = equipmentUI.slotHolders[i].transform as RectTransform;

            if (RectTransformUtility.RectangleContainsScreenPoint(t, position))
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region 检测任务物品
    //获得新任务后，检查背包和快捷栏，如果有任务物品，更新任务进度
    public void CheckQuestItemInBag(string questItemName, QuestData_SO questData)
    {
        var task = QuestManager.Instance.GetTask(questData);
        foreach (var item in inventoryData.items)
        {
            if (item.itemData != null)
            {
                //如果有任务物品，更新任务进度
                if (item.itemData.itemName == questItemName)
                {

                    var matchRequire = task.questData.questRequires.Find(r => r.name == item.itemData.itemName);
                    if (matchRequire != null)
                        matchRequire.currentAmount += item.amountInInventory;
                }
            }
        }

        foreach (var item in actionData.items)
        {
            if (item.itemData != null)
            {
                //如果有任务物品，更新任务进度
                if (item.itemData.itemName == questItemName)
                {
                    var matchRequire = task.questData.questRequires.Find(r => r.name == item.itemData.itemName);
                    if (matchRequire != null)
                        matchRequire.currentAmount += item.amountInInventory;
                }
            }
        }
        //检查任务是否完成
        task.questData.CheckQuestProgress();
    }

    //检测背包和快捷栏的物品，提交任务道具
    public InventoryItem QuestItemInBag(ItemData_SO questItem)
    {
        return inventoryData.items.Find(i => i.itemData?.itemName == questItem.itemName
        && i.amountInInventory > 0);
    }
    public InventoryItem QuestItemInAction(ItemData_SO questItem)
    {
        return actionData.items.Find(i => i.itemData?.itemName == questItem.itemName
        && i.amountInInventory > 0);
    }
    #endregion

    #region 扣除武器和盾牌的耐久，报废Destroy动画
    public void ReduceWeaponDurability(float damage, Vector3 attackDir)
    {
        if (GameManager.Instance.playerStats == null) return; 
        //减少武器耐久，传入damage转换为float，每次扣除点数 = 伤害 * 0.01
        equipmentData.items[0].itemData.currentDurability -= damage * 0.01f;
        //Debug.Log("ReduceWeaponDurability, currentDurability:" +
        //equipmentData.items[0].itemData.currentDurability);

        //武器报废，Destroy动画
        if (equipmentData.items[0].itemData.currentDurability < 0)
        {
            GameObject weapon_eq = 
                GameManager.Instance.playerStats.weaponSlot.GetChild(0).gameObject;
            //世界生成武器，关闭Trigger
            GameObject broken = Instantiate(equipmentData.items[0].itemData.objectOnWorld,
                weapon_eq.transform.position, weapon_eq.transform.rotation);
            broken.GetComponent<CapsuleCollider>().enabled = false;

            //清空武器槽，更新格子UI（手持自动销毁）
            InventoryItem weaponItem = InventoryManager.Instance.equipmentData.items[0];
            weaponItem.itemData = null;
            weaponItem.amountInInventory = 0;
            InventoryManager.Instance.equipmentUI.slotHolders[0].UpdateItem();

            Destroy(broken, 1.5f);

            //给世界武器一个击飞和旋转的力，倾向于朝玩家后上方
            var force = (-attackDir.normalized * 0.5f + Vector3.up * 0.5f
                + Random.onUnitSphere) * Random.Range(4f, 6f);
            if (GameManager.Instance.playerStats.isCritical) force *= 2f;
            broken.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);

            force = Random.onUnitSphere * force.magnitude * Random.Range(0.3f, 1.7f);
            broken.GetComponent<Rigidbody>().maxAngularVelocity = 99f;
            broken.GetComponent<Rigidbody>().AddTorque(force, ForceMode.Impulse);
        }
        else
        {
            //UI提示玩家武器耐久减少（击飞乌龟壳和石头的时候不减少）
            StopCoroutine(DurabiFadeOutIn());
            StartCoroutine(DurabiFadeOutIn());
        }
    }

    public void ReduceSheildDurability(float damage, Vector3 attackDir)
    {
        if (GameManager.Instance.playerStats == null) return;
        //减少盾牌耐久，每次扣除点数 = 伤害 * 0.01
        equipmentData.items[1].itemData.currentDurability -= damage * 0.01f;
        //Debug.Log("ReduceSheildDurability, currentDurability:" + 
        //    equipmentData.items[1].itemData.currentDurability);

        //盾牌报废，Destroy动画
        if (equipmentData.items[1].itemData.currentDurability < 0)
        {
            GameObject sheild_eq =
                GameManager.Instance.playerStats.sheildSlot.GetChild(0).gameObject;
            //世界生成盾牌，关闭Trigger
            GameObject broken = Instantiate(equipmentData.items[1].itemData.objectOnWorld,
                sheild_eq.transform.position, sheild_eq.transform.rotation);
            broken.GetComponent<CapsuleCollider>().enabled = false;

            //清空盾牌槽，更新格子UI（手持自动销毁）
            InventoryItem sheildItem = InventoryManager.Instance.equipmentData.items[1];
            sheildItem.itemData = null;
            sheildItem.amountInInventory = 0;
            InventoryManager.Instance.equipmentUI.slotHolders[1].UpdateItem();

            Destroy(broken, 1.5f);

            //给世界盾牌一个击飞和旋转的力，倾向于朝玩家后上方
            var force = (attackDir.normalized * 0.5f + Vector3.up * 0.5f
                + Random.onUnitSphere) * damage * 0.6f;
            broken.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);

            force = Random.onUnitSphere * force.magnitude * Random.Range(0.3f, 1.7f);
            broken.GetComponent<Rigidbody>().maxAngularVelocity = 99f;
            broken.GetComponent<Rigidbody>().AddTorque(force, ForceMode.Impulse);
        }
        else
        {
            //UI提示玩家盾牌耐久减少（击飞乌龟壳和石头的时候不减少）
            StopCoroutine(DurabiFadeOutIn());
            StartCoroutine(DurabiFadeOutIn());
        }
    }

    public IEnumerator DurabiFadeOutIn(float keepTime = 1f, float fadeInTime = 1f)
    {
        durabiDownCanvasGroup.alpha = 1f;
        yield return new WaitForSeconds(keepTime);

        while (durabiDownCanvasGroup.alpha > 0)
        {
            durabiDownCanvasGroup.alpha -= Time.deltaTime / fadeInTime;
            yield return null;
        }
    }


    public void ReduceBowDurability(float damage, Vector3 attackDir)
    {
        if (GameManager.Instance.playerStats == null) return;
        //减少弓耐久，传入damage转换为float，每次扣除点数 = 伤害 * 0.01
        equipmentData.items[0].itemData.currentDurability -= damage * 0.01f;

        //武器弓报废，Destroy动画
        if (equipmentData.items[0].itemData.currentDurability < 0)
        {
            GameObject bow_eq =
                GameManager.Instance.playerStats.bowSlot.GetChild(0).gameObject;
            //世界生成弓，关闭Trigger
            GameObject broken = Instantiate(equipmentData.items[0].itemData.objectOnWorld,
                bow_eq.transform.position, bow_eq.transform.rotation);
            broken.GetComponent<CapsuleCollider>().enabled = false;

            //清空武器槽，更新格子UI（手持自动销毁）
            InventoryItem weaponItem = InventoryManager.Instance.equipmentData.items[0];
            weaponItem.itemData = null;
            weaponItem.amountInInventory = 0;
            InventoryManager.Instance.equipmentUI.slotHolders[0].UpdateItem();

            Destroy(broken, 1.5f);

            //给世界武器弓一个击飞和旋转的力，倾向于朝箭矢射击方向
            var force = (attackDir.normalized * 0.5f + Random.onUnitSphere) * Random.Range(4f, 6f);
            if (GameManager.Instance.playerStats.isCritical) force *= 2f;
            broken.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);

            force = Random.onUnitSphere * force.magnitude * Random.Range(0.3f, 1.7f);
            broken.GetComponent<Rigidbody>().maxAngularVelocity = 99f;
            broken.GetComponent<Rigidbody>().AddTorque(force, ForceMode.Impulse);
        }
        else
        {
            //UI提示玩家武器耐久减少（击飞乌龟壳和石头的时候不减少）
            StopCoroutine(DurabiFadeOutIn());
            StartCoroutine(DurabiFadeOutIn());
        }
    }

    #endregion

    #region 箭矢
    internal InventoryItem FindArrow()
    {
        foreach (var item in inventoryData.items)
        {
            if (item != null && item.itemData != null &&
                item.itemData.itemType == ItemType.Arrow && item.amountInInventory > 0)
                return item;
        }
        return null;
    }

    //报废手持的箭矢
    public void BrokenEquipArrow(float damage, Vector3 attackDir)
    {
        CharacterStats playerStats = GameManager.Instance.playerStats;
        if (playerStats == null) return;
        GameObject arrow_eq =
            GameManager.Instance.playerStats.arrowSlot.GetChild(0).gameObject;
        //世界生成箭矢，关闭大的Trigger，开启小的collider
        GameObject broken = Instantiate(playerStats.arrowItem.itemData.objectOnWorld,
            arrow_eq.transform.position, arrow_eq.transform.rotation);
        foreach (var col in broken.GetComponents<BoxCollider>())
        {
            if (col.isTrigger) col.enabled = false; 
            else col.enabled = true;
        }

        //减少背包箭矢，刷新背包，卸载Slot箭矢
        playerStats.arrowItem.amountInInventory--;
        if (playerStats.arrowItem.amountInInventory == 0)
            playerStats.arrowItem.itemData = null;
        playerStats.UnEquipArrow();
        InventoryManager.Instance.inventoryUI.RefreshUI();

        Destroy(broken, 1.5f);

        //给世界箭矢一个击飞和旋转的力，倾向于朝玩家后上方
        var force = (-attackDir.normalized * 0.5f + Vector3.up * 0.5f
            + Random.onUnitSphere) * Random.Range(4f, 6f);
        if (GameManager.Instance.playerStats.isCritical) force *= 2f;
        broken.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);

        force = Random.onUnitSphere * force.magnitude * Random.Range(0.3f, 1.7f);
        broken.GetComponent<Rigidbody>().maxAngularVelocity = 99f;
        broken.GetComponent<Rigidbody>().AddTorque(force, ForceMode.Impulse);
    }
    #endregion
}
