using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LootSpawner : MonoBehaviour
{
    [System.Serializable]
    public class LootItem
    {
        public GameObject item;
        [Range(0, 1)] public float weight;
    }

    public LootItem[] lootItems;

    public void Spawnloot()
    {
        for (int i = 0; i < lootItems.Length; i++)
        {
            float currentValue = Random.value;
            if (currentValue<=lootItems[i].weight)
            {
                //¹ÖÎïµôÂäÎïÆ·
                GameObject obj = Instantiate(lootItems[i].item);
                obj.GetComponent<ItemPickUp>()?.DurabilityRandomSet();

                obj.transform.position = transform.position + Vector3.up * 2;
                obj.transform.rotation = Random.rotation;
            }
        }
    }
}
