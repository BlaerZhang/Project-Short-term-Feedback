using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 速度切换按钮控制器
/// </summary>
public class SpeedSwitchButton : MonoBehaviour
{
    [Tooltip("目标速度档位（1-3）")]
    [Range(1, 3)]
    [SerializeField] private int targetSpeedLevel = 1;

    [Tooltip("按钮图像")]
    [SerializeField] private Image buttonImage;

    [Tooltip("按钮文本")]
    [SerializeField] private TextMeshProUGUI buttonText;

    [Tooltip("正常状态颜色")]
    [SerializeField] private Color normalColor = Color.white;

    [Tooltip("选中状态颜色")]
    [SerializeField] private Color selectedColor = Color.green;

    [Tooltip("禁用状态颜色")]
    [SerializeField] private Color disabledColor = Color.gray;

    [Tooltip("消耗精力文本")]
    [SerializeField] private TextMeshProUGUI costText;

    // 玩家控制器引用
    private PlayerController playerController;
    
    // 游戏管理器引用
    private GameManager gameManager;
    
    // 资源管理器引用
    private ResourceManager resourceManager;

    private void Start()
    {
        // 获取引用
        playerController = FindObjectOfType<PlayerController>();
        gameManager = GameManager.Instance;
        resourceManager = ResourceManager.Instance;
        
        if (playerController == null)
        {
            Debug.LogError("SpeedSwitchButton: 未找到PlayerController");
        }
        
        if (gameManager == null)
        {
            Debug.LogError("SpeedSwitchButton: 未找到GameManager");
        }
        
        if (resourceManager == null)
        {
            Debug.LogError("SpeedSwitchButton: 未找到ResourceManager");
        }
        
        // 初始化按钮文本
        if (buttonText != null)
        {
            buttonText.text = targetSpeedLevel.ToString();
        }
        
        // 不再尝试注册GameManager的OnGameStateChanged事件
        // 而是定期检查游戏状态
        
        // 更新按钮状态
        UpdateButtonState();
        
        // 获取按钮组件并添加点击事件
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClick);
        }
    }
    
    private void OnDestroy()
    {
        // 不需要移除事件监听
        
        // 移除按钮点击事件
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(OnButtonClick);
        }
    }
    
    private void Update()
    {
        // 每帧更新按钮状态
        UpdateButtonState();
    }
    
    /// <summary>
    /// 更新按钮状态（颜色、交互性等）
    /// </summary>
    private void UpdateButtonState()
    {
        if (playerController == null || buttonImage == null)
            return;
            
        // 获取当前速度
        float currentSpeed = playerController.GetCurrentSpeed();
        
        // 获取当前游戏状态
        GameState state = (gameManager != null) ? gameManager.CurrentState : GameState.Planning;
        
        // 计算切换所需精力值
        float speedDifference = Mathf.Abs(targetSpeedLevel - currentSpeed);
        float energyCost = (speedDifference == 1) ? 1f : 3f;
        
        // 更新精力消耗文本
        if (costText != null)
        {
            if (Mathf.Approximately(currentSpeed, targetSpeedLevel))
            {
                costText.text = "";  // 当前速度就是目标速度，不显示消耗
            }
            else
            {
                costText.text = $"-{energyCost}";
            }
        }
        
        // 检查按钮是否可交互
        bool canInteract = (state == GameState.Planning) && 
                          (!Mathf.Approximately(currentSpeed, targetSpeedLevel));
        
        // 如果有资源管理器，检查精力是否足够
        if (canInteract && resourceManager != null)
        {
            float currentEnergy = resourceManager.GetResourceValue(ResourceType.Energy);
            canInteract = currentEnergy >= energyCost;
        }
        
        // 更新按钮组件的交互状态
        Button button = GetComponent<Button>();
        if (button != null)
        {
            button.interactable = canInteract;
        }
        
        // 更新按钮颜色
        if (Mathf.Approximately(currentSpeed, targetSpeedLevel))
        {
            // 当前速度，选中状态
            buttonImage.color = selectedColor;
        }
        else if (!canInteract)
        {
            // 不可交互状态
            buttonImage.color = disabledColor;
        }
        else
        {
            // 正常状态
            buttonImage.color = normalColor;
        }
    }
    
    /// <summary>
    /// 按钮点击处理
    /// </summary>
    private void OnButtonClick()
    {
        if (playerController != null)
        {
            playerController.ChangeSpeed(targetSpeedLevel);
            
            // 更新按钮状态
            UpdateButtonState();
        }
    }
} 