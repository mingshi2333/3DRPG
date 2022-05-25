using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class QuestRequirement : MonoBehaviour
{
    Text requireName;
    Text progressNumber;

    private void Awake()
    {
        requireName = GetComponent<Text>();
        progressNumber = transform.GetChild(0).GetComponent<Text>();
    }

    public void SetupRequirment(string name, int requireAmount, int currentAmount)
    {
        if (requireName == null) return;
        requireName.text = name;
        progressNumber.text = currentAmount.ToString() + " / " + requireAmount.ToString();
    }

    public void SetupRequirment(string name, bool isFinished)
    {
        if (isFinished)
        {
            requireName.text = name;
            progressNumber.text = "Íê³É";
            requireName.color = Color.gray;
            progressNumber.color = Color.gray;
        }
    }
}
