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
            //��ʼ�������ڵ�װ���;�
            foreach (var item in inventoryTemplate.items)
                item.itemData?.Awake();
            inventoryData = Instantiate(inventoryTemplate);
        }

        if (actionTemplate != null)
            actionData = Instantiate(actionTemplate);
        if (equipmentTemplate != null)
        {
            //��ʼ�������Ͷ����;öȣ�����У�
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
        //���������ʵʱ�����������
        if (isOpen) UpdateStatsText();
    }

    //�����ڵ���ƷΪ������ʱ���ݣ���Ҫ���л���ȡ
    ///�ؼ�ֵ��path���������;�
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
                //���ǿո��ӣ������Ӧ�ֶ�
                PlayerPrefs.DeleteKey(data.name + "_itemDataPath_" + i.ToString());
                PlayerPrefs.DeleteKey(data.name + "_amountInInventory_" + i.ToString());
                PlayerPrefs.DeleteKey(data.name + "_currentDurability_" + i.ToString());
                //��������ִ��
                continue;
            }

            //������Ʒ
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
            //������Ʒ
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

    #region �����ק��Ʒ�Ƿ���ÿһ�� Slot ��Χ��
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

    #region ���������Ʒ
    //���������󣬼�鱳���Ϳ�����������������Ʒ�������������
    public void CheckQuestItemInBag(string questItemName, QuestData_SO questData)
    {
        var task = QuestManager.Instance.GetTask(questData);
        foreach (var item in inventoryData.items)
        {
            if (item.itemData != null)
            {
                //�����������Ʒ�������������
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
                //�����������Ʒ�������������
                if (item.itemData.itemName == questItemName)
                {
                    var matchRequire = task.questData.questRequires.Find(r => r.name == item.itemData.itemName);
                    if (matchRequire != null)
                        matchRequire.currentAmount += item.amountInInventory;
                }
            }
        }
        //��������Ƿ����
        task.questData.CheckQuestProgress();
    }

    //��ⱳ���Ϳ��������Ʒ���ύ�������
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

    #region �۳������Ͷ��Ƶ��;ã�����Destroy����
    public void ReduceWeaponDurability(float damage, Vector3 attackDir)
    {
        if (GameManager.Instance.playerStats == null) return; 
        //���������;ã�����damageת��Ϊfloat��ÿ�ο۳����� = �˺� * 0.01
        equipmentData.items[0].itemData.currentDurability -= damage * 0.01f;
        //Debug.Log("ReduceWeaponDurability, currentDurability:" +
        //equipmentData.items[0].itemData.currentDurability);

        //�������ϣ�Destroy����
        if (equipmentData.items[0].itemData.currentDurability < 0)
        {
            GameObject weapon_eq = 
                GameManager.Instance.playerStats.weaponSlot.GetChild(0).gameObject;
            //���������������ر�Trigger
            GameObject broken = Instantiate(equipmentData.items[0].itemData.objectOnWorld,
                weapon_eq.transform.position, weapon_eq.transform.rotation);
            broken.GetComponent<CapsuleCollider>().enabled = false;

            //��������ۣ����¸���UI���ֳ��Զ����٣�
            InventoryItem weaponItem = InventoryManager.Instance.equipmentData.items[0];
            weaponItem.itemData = null;
            weaponItem.amountInInventory = 0;
            InventoryManager.Instance.equipmentUI.slotHolders[0].UpdateItem();

            Destroy(broken, 1.5f);

            //����������һ�����ɺ���ת�����������ڳ���Һ��Ϸ�
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
            //UI��ʾ��������;ü��٣������ڹ�Ǻ�ʯͷ��ʱ�򲻼��٣�
            StopCoroutine(DurabiFadeOutIn());
            StartCoroutine(DurabiFadeOutIn());
        }
    }

    public void ReduceSheildDurability(float damage, Vector3 attackDir)
    {
        if (GameManager.Instance.playerStats == null) return;
        //���ٶ����;ã�ÿ�ο۳����� = �˺� * 0.01
        equipmentData.items[1].itemData.currentDurability -= damage * 0.01f;
        //Debug.Log("ReduceSheildDurability, currentDurability:" + 
        //    equipmentData.items[1].itemData.currentDurability);

        //���Ʊ��ϣ�Destroy����
        if (equipmentData.items[1].itemData.currentDurability < 0)
        {
            GameObject sheild_eq =
                GameManager.Instance.playerStats.sheildSlot.GetChild(0).gameObject;
            //�������ɶ��ƣ��ر�Trigger
            GameObject broken = Instantiate(equipmentData.items[1].itemData.objectOnWorld,
                sheild_eq.transform.position, sheild_eq.transform.rotation);
            broken.GetComponent<CapsuleCollider>().enabled = false;

            //��ն��Ʋۣ����¸���UI���ֳ��Զ����٣�
            InventoryItem sheildItem = InventoryManager.Instance.equipmentData.items[1];
            sheildItem.itemData = null;
            sheildItem.amountInInventory = 0;
            InventoryManager.Instance.equipmentUI.slotHolders[1].UpdateItem();

            Destroy(broken, 1.5f);

            //���������һ�����ɺ���ת�����������ڳ���Һ��Ϸ�
            var force = (attackDir.normalized * 0.5f + Vector3.up * 0.5f
                + Random.onUnitSphere) * damage * 0.6f;
            broken.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);

            force = Random.onUnitSphere * force.magnitude * Random.Range(0.3f, 1.7f);
            broken.GetComponent<Rigidbody>().maxAngularVelocity = 99f;
            broken.GetComponent<Rigidbody>().AddTorque(force, ForceMode.Impulse);
        }
        else
        {
            //UI��ʾ��Ҷ����;ü��٣������ڹ�Ǻ�ʯͷ��ʱ�򲻼��٣�
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
        //���ٹ��;ã�����damageת��Ϊfloat��ÿ�ο۳����� = �˺� * 0.01
        equipmentData.items[0].itemData.currentDurability -= damage * 0.01f;

        //���������ϣ�Destroy����
        if (equipmentData.items[0].itemData.currentDurability < 0)
        {
            GameObject bow_eq =
                GameManager.Instance.playerStats.bowSlot.GetChild(0).gameObject;
            //�������ɹ����ر�Trigger
            GameObject broken = Instantiate(equipmentData.items[0].itemData.objectOnWorld,
                bow_eq.transform.position, bow_eq.transform.rotation);
            broken.GetComponent<CapsuleCollider>().enabled = false;

            //��������ۣ����¸���UI���ֳ��Զ����٣�
            InventoryItem weaponItem = InventoryManager.Instance.equipmentData.items[0];
            weaponItem.itemData = null;
            weaponItem.amountInInventory = 0;
            InventoryManager.Instance.equipmentUI.slotHolders[0].UpdateItem();

            Destroy(broken, 1.5f);

            //������������һ�����ɺ���ת�����������ڳ���ʸ�������
            var force = (attackDir.normalized * 0.5f + Random.onUnitSphere) * Random.Range(4f, 6f);
            if (GameManager.Instance.playerStats.isCritical) force *= 2f;
            broken.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);

            force = Random.onUnitSphere * force.magnitude * Random.Range(0.3f, 1.7f);
            broken.GetComponent<Rigidbody>().maxAngularVelocity = 99f;
            broken.GetComponent<Rigidbody>().AddTorque(force, ForceMode.Impulse);
        }
        else
        {
            //UI��ʾ��������;ü��٣������ڹ�Ǻ�ʯͷ��ʱ�򲻼��٣�
            StopCoroutine(DurabiFadeOutIn());
            StartCoroutine(DurabiFadeOutIn());
        }
    }

    #endregion

    #region ��ʸ
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

    //�����ֳֵļ�ʸ
    public void BrokenEquipArrow(float damage, Vector3 attackDir)
    {
        CharacterStats playerStats = GameManager.Instance.playerStats;
        if (playerStats == null) return;
        GameObject arrow_eq =
            GameManager.Instance.playerStats.arrowSlot.GetChild(0).gameObject;
        //�������ɼ�ʸ���رմ��Trigger������С��collider
        GameObject broken = Instantiate(playerStats.arrowItem.itemData.objectOnWorld,
            arrow_eq.transform.position, arrow_eq.transform.rotation);
        foreach (var col in broken.GetComponents<BoxCollider>())
        {
            if (col.isTrigger) col.enabled = false; 
            else col.enabled = true;
        }

        //���ٱ�����ʸ��ˢ�±�����ж��Slot��ʸ
        playerStats.arrowItem.amountInInventory--;
        if (playerStats.arrowItem.amountInInventory == 0)
            playerStats.arrowItem.itemData = null;
        playerStats.UnEquipArrow();
        InventoryManager.Instance.inventoryUI.RefreshUI();

        Destroy(broken, 1.5f);

        //�������ʸһ�����ɺ���ת�����������ڳ���Һ��Ϸ�
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
