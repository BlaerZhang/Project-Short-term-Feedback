using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
/// <summary>
/// 资源UI显示控制器，用于展示玩家资源状态
/// </summary>
public class ResourceUI : MonoBehaviour
{
    [Header("动画设置")]
    [Tooltip("滑动条动画持续时间")]
    [SerializeField] private float sliderAnimationDuration = 0.5f;
    [Tooltip("滑动条动画缓动类型")]
    [SerializeField] private Ease sliderAnimationEase = Ease.OutQuad;

    [Header("精力值设置")]
    [SerializeField] private Slider energySlider;
    [SerializeField] private TextMeshProUGUI energyText;
    [SerializeField] private Image energyFillImage;
    [SerializeField] private Color energyNormalColor = Color.green;
    [SerializeField] private Color energyLowColor = Color.red;
    [SerializeField] private float energyLowThreshold = 0.3f; // 低于30%视为精力不足

    [Header("氧气值设置")]
    [SerializeField] private Slider oxygenSlider;
    [SerializeField] private TextMeshProUGUI oxygenText;
    [SerializeField] private Image oxygenFillImage;
    [SerializeField] private Color oxygenNormalColor = Color.blue;
    [SerializeField] private Color oxygenLowColor = Color.red;
    [SerializeField] private float oxygenLowThreshold = 0.2f; // 低于20%视为氧气不足

    [Header("压力值设置")]
    [SerializeField] private Slider pressureSlider;
    [SerializeField] private TextMeshProUGUI pressureText;
    [SerializeField] private Image pressureFillImage;
    [SerializeField] private Color pressureNormalColor = Color.yellow;
    [SerializeField] private Color pressureHighColor = Color.red;
    [SerializeField] private float pressureHighThreshold = 0.7f; // 高于70%视为压力过大

    // 资源管理器引用
    private ResourceManager resourceManager;

    private void Start()
    {
        // 获取资源管理器引用
        resourceManager = ResourceManager.Instance;
        if (resourceManager == null)
        {
            Debug.LogError("ResourceUI: 未找到ResourceManager!");
            return;
        }

        // 注册资源变化事件
        resourceManager.OnResourceChanged += HandleResourceChanged;

        // 初始更新UI显示
        UpdateAllResourceUI();
    }

    private void OnDestroy()
    {
        // 取消注册事件
        if (resourceManager != null)
        {
            resourceManager.OnResourceChanged -= HandleResourceChanged;
        }
    }

    /// <summary>
    /// 处理资源变化事件
    /// </summary>
    private void HandleResourceChanged(ResourceType type, float newValue, float amount)
    {
        // 根据变化的资源类型更新相应的UI
        switch (type)
        {
            case ResourceType.Energy:
                UpdateEnergyUI();
                break;

            case ResourceType.Oxygen:
                UpdateOxygenUI();
                break;

            case ResourceType.Pressure:
                UpdatePressureUI();
                break;
        }
    }

    /// <summary>
    /// 更新所有资源UI
    /// </summary>
    private void UpdateAllResourceUI()
    {
        UpdateEnergyUI();
        UpdateOxygenUI();
        UpdatePressureUI();
    }

    /// <summary>
    /// 更新精力值UI
    /// </summary>
    private void UpdateEnergyUI()
    {
        if (resourceManager == null) return;

        float currentEnergy = resourceManager.GetResourceValue(ResourceType.Energy);
        float maxEnergy = resourceManager.GetResourceMaxValue(ResourceType.Energy);
        float percentage = resourceManager.GetResourcePercentage(ResourceType.Energy);

        // 更新滑动条（设置为忽略时间缩放）
        if (energySlider != null)
        {
            energySlider.DOValue(percentage, sliderAnimationDuration)
                .SetUpdate(true) // 忽略时间缩放
                .SetEase(sliderAnimationEase);
        }

        // 更新文本
        if (energyText != null)
        {
            energyText.text = $"{Mathf.RoundToInt(currentEnergy)}/{Mathf.RoundToInt(maxEnergy)}";
        }

        // 更新填充颜色
        if (energyFillImage != null)
        {
            Color targetColor = (percentage <= energyLowThreshold) ? energyLowColor : energyNormalColor;
            energyFillImage.DOColor(targetColor, sliderAnimationDuration).SetUpdate(true);
        }
    }

    /// <summary>
    /// 更新氧气值UI
    /// </summary>
    private void UpdateOxygenUI()
    {
        if (resourceManager == null) return;

        float currentOxygen = resourceManager.GetResourceValue(ResourceType.Oxygen);
        float maxOxygen = resourceManager.GetResourceMaxValue(ResourceType.Oxygen);
        float percentage = resourceManager.GetResourcePercentage(ResourceType.Oxygen);

        // 更新滑动条（设置为忽略时间缩放）
        if (oxygenSlider != null)
        {
            oxygenSlider.DOValue(percentage, sliderAnimationDuration)
                .SetUpdate(true) // 忽略时间缩放
                .SetEase(sliderAnimationEase);
        }

        // 更新文本
        if (oxygenText != null)
        {
            oxygenText.text = $"{Mathf.RoundToInt(currentOxygen)}/{Mathf.RoundToInt(maxOxygen)}";
        }

        // 更新填充颜色
        if (oxygenFillImage != null)
        {
            Color targetColor = (percentage <= oxygenLowThreshold) ? oxygenLowColor : oxygenNormalColor;
            oxygenFillImage.DOColor(targetColor, sliderAnimationDuration).SetUpdate(true);
        }
    }

    /// <summary>
    /// 更新压力值UI
    /// </summary>
    private void UpdatePressureUI()
    {
        if (resourceManager == null) return;

        float currentPressure = resourceManager.GetResourceValue(ResourceType.Pressure);
        float maxPressure = resourceManager.GetResourceMaxValue(ResourceType.Pressure);
        float percentage = resourceManager.GetResourcePercentage(ResourceType.Pressure);

        // 更新滑动条（设置为忽略时间缩放）
        if (pressureSlider != null)
        {
            pressureSlider.DOValue(percentage, sliderAnimationDuration)
                .SetUpdate(true) // 忽略时间缩放
                .SetEase(sliderAnimationEase);
        }

        // 更新文本
        if (pressureText != null)
        {
            pressureText.text = $"{Mathf.RoundToInt(currentPressure)}/{Mathf.RoundToInt(maxPressure)}";
        }

        // 更新填充颜色
        if (pressureFillImage != null)
        {
            Color targetColor = (percentage >= pressureHighThreshold) ? pressureHighColor : pressureNormalColor;
            pressureFillImage.DOColor(targetColor, sliderAnimationDuration).SetUpdate(true);
        }
    }
} 