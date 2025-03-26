using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

/// <summary>
/// 呼吸状态按钮控制器
/// </summary>
public class BreathStateButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Tooltip("对应的呼吸状态")]
    [SerializeField] private BreathState breathState;

    [Header("UI组件")]
    [Tooltip("滑动条")]
    [SerializeField] private Slider progressSlider;
    [Tooltip("Outline组件")]
    [SerializeField] private UnityEngine.UI.Outline outlineComponent;
    [Tooltip("选中色")]
    [SerializeField] private Color selectedColor = new Color(0, 1, 0, 1);
    [Tooltip("未选中色")]
    [SerializeField] private Color unselectedColor = new Color(0.5f, 0.5f, 0.5f, 1);
    
    [Header("提示框")]
    [Tooltip("提示信息控制器")]
    [SerializeField] private TooltipController tooltipController;
    [Tooltip("提示框位置偏移")]
    [SerializeField] private Vector2 tooltipOffset = new Vector2(20, 20);

    [Header("动画设置")]
    [Tooltip("进度条动画持续时间")]
    [SerializeField] private float progressAnimationDuration = 0.3f;
    [Tooltip("进度条动画曲线")]
    [SerializeField] private Ease progressAnimationEase = Ease.OutQuad;

    // 呼吸管理器引用
    private BreathManager breathManager;
    // 提示框实例
    private GameObject tooltipInstance;

    private void Start()
    {
        // 获取呼吸管理器
        breathManager = BreathManager.Instance;
        if (breathManager == null)
        {
            Debug.LogError("BreathStateButton: 未找到BreathManager!");
            return;
        }

        // 找到场景中的提示控制器（如果未手动设置）
        if (tooltipController == null)
        {
            tooltipController = FindObjectOfType<TooltipController>();
            if (tooltipController == null)
            {
                Debug.LogWarning("BreathStateButton: 未找到TooltipController!");
            }
        }

        // 添加呼吸状态变化事件监听
        breathManager.OnBreathStateChanged += OnBreathStateChanged;
        // 添加呼吸进度变化事件监听
        breathManager.OnBreathProgressChanged += OnBreathProgressChanged;

        // 初始化按钮状态
        UpdateButtonState();
        // 初始化进度条
        UpdateProgressBar();
    }

    private void OnDestroy()
    {
        // 移除事件监听
        if (breathManager != null)
        {
            breathManager.OnBreathStateChanged -= OnBreathStateChanged;
            breathManager.OnBreathProgressChanged -= OnBreathProgressChanged;
        }
    }

    /// <summary>
    /// 处理点击事件
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"呼吸状态被点击: {breathState}");
        
        if (breathManager == null)
        {
            Debug.LogError("BreathStateButton: 呼吸管理器为空，无法切换状态!");
            return;
        }
        
        breathManager.SwitchBreathState(breathState);
    }

    /// <summary>
    /// 处理呼吸状态变化事件
    /// </summary>
    private void OnBreathStateChanged(BreathState newState)
    {
        UpdateButtonState();
    }

    /// <summary>
    /// 处理呼吸进度变化事件
    /// </summary>
    private void OnBreathProgressChanged(int progress, int cycle)
    {
        UpdateProgressBar();
    }

    /// <summary>
    /// 更新按钮状态（选中状态）
    /// </summary>
    private void UpdateButtonState()
    {
        if (breathManager == null || outlineComponent == null)
            return;

        // 获取当前呼吸状态
        BreathState currentState = breathManager.GetCurrentState();

        // 更新outline颜色
        outlineComponent.effectColor = (currentState == breathState) ? selectedColor : unselectedColor;
    }

    /// <summary>
    /// 更新进度条
    /// </summary>
    private void UpdateProgressBar()
    {
        if (breathManager == null || progressSlider == null)
            return;

        // 获取当前进度和周期
        int progress = 0;
        int cycle = 1;
        
        // 只更新当前选中状态的进度条
        if (breathManager.GetCurrentState() == breathState)
        {
            progress = breathManager.GetCurrentProgress();
            cycle = breathManager.GetCurrentCycle();
            
            // 设置进度条的最大值为周期值
            progressSlider.maxValue = cycle;
        }
        
        // 使用DOTween动画平滑过渡到新值
        progressSlider.DOValue(progress, progressAnimationDuration)
            .SetEase(progressAnimationEase)
            .SetUpdate(true); // 忽略时间缩放
    }

    /// <summary>
    /// 鼠标进入时显示提示框
    /// </summary>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (tooltipController != null && breathManager != null)
        {
            // 设置提示内容
            string description = breathManager.GetBreathStateDescription(breathState);
            tooltipController.SetContent(breathState.ToString(), description, "");
            tooltipController.Show();
        }
    }

    /// <summary>
    /// 鼠标离开时隐藏提示框
    /// </summary>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (tooltipController != null)
        {
            tooltipController.Hide();
        }
    }
} 