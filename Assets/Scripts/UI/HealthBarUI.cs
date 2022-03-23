using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class HealthBarUI : MonoBehaviour
{
    public GameObject healthUIPrefab;
    public Transform barPoint;
    public bool alwaysVisible;//设置这个血条是否一直可见
    public float visibleTime;//被攻击后可视化时间
    private float timeLeft;
    Image healthSlider;
    Transform UIbar;
    private Transform cam;//保证摄像机位置
    CharacterStats currentStats;
    void Awake()
    {
        currentStats = GetComponent<CharacterStats>();
        currentStats.UpdateHealthBarOnAttack += UpdateHealthBar;
    }
    void  OnEnable() {
        cam = Camera.main.transform;
        foreach(Canvas canvas in FindObjectsOfType<Canvas>())
        {
            if(canvas.renderMode == RenderMode.WorldSpace)
            {
                UIbar = Instantiate(healthUIPrefab,canvas.transform).transform;
                healthSlider = UIbar.GetChild(0).GetComponent<Image>();
                UIbar.gameObject.SetActive(alwaysVisible);
            }
        }
    }

    private void UpdateHealthBar(int currentHealth, int maxHealth)
    {
        if(currentHealth <= 0)
            Destroy(UIbar.gameObject);
        UIbar.gameObject.SetActive(true);
        timeLeft = visibleTime;
        float sliderPercent = (float)currentHealth/maxHealth;
        healthSlider.fillAmount = sliderPercent;
    }

    void LateUpdate()
    {
        if(UIbar!=null)
        {
            UIbar.position = barPoint.position;
            UIbar.forward = -cam.forward;
            if(timeLeft<=0&&!alwaysVisible)
                UIbar.gameObject.SetActive(false);
            else
                timeLeft -=Time.deltaTime;
        }//延迟更新函数
    }
}

