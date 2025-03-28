using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class ActionButtonController : MonoBehaviour
{
    [Header("按钮设置")]
    [SerializeField] private MoveActionType actionType; // 对应的动作类型
    [SerializeField] private KeyCode keyboardShortcut; // 对应的键盘快捷键

    [Header("视觉反馈")]
    [SerializeField] private Color normalColor = Color.white; // 正常状态颜色
    [SerializeField] private Color selectedColor = Color.cyan; // 选中状态颜色
    [SerializeField] private Color hoverColor = Color.yellow; // 悬停状态颜色

    [Header("技能信息")]
    [SerializeField] private string actionName = "动作"; // 技能名称
    [TextArea(3, 10)] // 增大文本框
    [SerializeField] private string actionDescription = "描述"; // 技能描述
    [SerializeField] private float tooltipDelay = 0.5f; // 显示提示的延迟时间（秒）
    [SerializeField] private TooltipController tooltipController; // 提示控制器引用

    // 组件引用
    private Button button;
    private Image buttonImage;
    private PlayerController playerController;
    private GameManager gameManager;

    // 状态变量
    private bool isSelected = false;
    private bool isHovering = false;
    private Coroutine tooltipCoroutine;
    private static TooltipController sharedTooltipController; // 共享的提示控制器引用

    private void Awake()
    {
        // 获取组件引用
        button = GetComponent<Button>();
        buttonImage = GetComponent<Image>();

        // 确保有这些组件
        if (button == null)
        {
            Debug.LogError($"ActionButtonController: {gameObject.name} 缺少 Button 组件!");
            return;
        }

        if (buttonImage == null)
        {
            Debug.LogError($"ActionButtonController: {gameObject.name} 缺少 Image 组件!");
            return;
        }
    }

    private void Start()
    {
        // 获取PlayerController和GameManager引用
        playerController = FindObjectOfType<PlayerController>();
        gameManager = GameManager.Instance;

        if (playerController == null)
        {
            Debug.LogError("ActionButtonController: 未找到PlayerController!");
        }

        if (gameManager == null)
        {
            Debug.LogError("ActionButtonController: 未找到GameManager!");
        }

        // 设置按钮事件监听
        SetupButtonListeners();
        
        // 寻找全局提示控制器（如果没有设置）
        if (tooltipController == null)
        {
            // 优先使用共享实例
            if (sharedTooltipController == null)
            {
                // 查找场景中唯一的TooltipController
                sharedTooltipController = FindObjectOfType<TooltipController>();
                if (sharedTooltipController == null)
                {
                    Debug.LogWarning("ActionButtonController: 未找到TooltipController!");
                }
            }
            tooltipController = sharedTooltipController;
        }
    }

    private void Update()
    {
        // 不再在这里响应键盘快捷键，避免重复触发
        // 键盘快捷键的处理已经在PlayerController中完成
        
        // 只更新按钮状态，确保UI状态与游戏状态一致
        UpdateButtonState();
    }

    private void SetupButtonListeners()
    {
        // 点击事件
        button.onClick.AddListener(OnButtonClicked);

        // 鼠标进入/离开事件
        EventTrigger eventTrigger = gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = gameObject.AddComponent<EventTrigger>();
        }

        // 鼠标进入事件
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { OnPointerEnter(); });
        eventTrigger.triggers.Add(enterEntry);

        // 鼠标离开事件
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { OnPointerExit(); });
        eventTrigger.triggers.Add(exitEntry);
    }

    private void OnButtonClicked()
    {
        // 检查是否可以点击（游戏状态为规划或目标选择阶段）
        if (gameManager != null && 
            (gameManager.CurrentState == GameState.Planning || 
             gameManager.CurrentState == GameState.Targeting))
        {
            // 如果已经选中了这个技能，再次点击就取消
            if (isSelected && gameManager.CurrentState == GameState.Targeting)
            {
                CancelAction();
            }
            // 否则选择这个技能
            else
            {
                SelectAction();
            }
        }
    }

    private void OnPointerEnter()
    {
        isHovering = true;
        
        // 如果未选中，改变颜色为悬停颜色
        if (!isSelected)
        {
            buttonImage.color = hoverColor;
        }
        
        // 启动显示提示的协程
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
        }
        tooltipCoroutine = StartCoroutine(ShowTooltipDelayed());
    }

    private void OnPointerExit()
    {
        isHovering = false;
        
        // 还原按钮颜色
        if (!isSelected)
        {
            buttonImage.color = normalColor;
        }
        
        // 停止并隐藏提示
        if (tooltipCoroutine != null)
        {
            StopCoroutine(tooltipCoroutine);
            tooltipCoroutine = null;
        }
        
        // 隐藏提示控制器
        if (tooltipController != null)
        {
            tooltipController.Hide();
        }
    }

    // 这个方法现在只在其他地方调用，不在Update中调用
    public void HandleKeyboardShortcut()
    {
        // 如果游戏状态不允许，直接返回
        if (gameManager == null || 
            (gameManager.CurrentState != GameState.Planning && 
             gameManager.CurrentState != GameState.Targeting))
        {
            return;
        }

        // 在目标选择阶段，如果再次按下相同技能的快捷键，则取消该技能
        if (isSelected && gameManager.CurrentState == GameState.Targeting)
        {
            CancelAction();
        }
        // 否则选择该技能
        else
        {
            SelectAction();
        }
    }

    private void SelectAction()
    {
        if (playerController != null)
        {
            // 调用PlayerController的选择动作方法
            playerController.SelectActionFromUI(actionType);
            
            // 更新按钮状态
            isSelected = true;
            buttonImage.color = selectedColor;
        }
    }

    private void CancelAction()
    {
        if (playerController != null && gameManager != null)
        {
            // 取消当前动作
            playerController.CancelCurrentAction();
            gameManager.CancelTargetingPhase();
            
            // 更新按钮状态
            isSelected = false;
            buttonImage.color = isHovering ? hoverColor : normalColor;
        }
    }

    private void UpdateButtonState()
    {
        // 根据PlayerController当前选择的动作更新按钮视觉状态
        if (playerController != null)
        {
            bool shouldBeSelected = playerController.GetCurrentAction() == actionType;
            
            // 状态变化时更新UI
            if (isSelected != shouldBeSelected)
            {
                isSelected = shouldBeSelected;
                buttonImage.color = isSelected ? selectedColor : (isHovering ? hoverColor : normalColor);
            }
        }
    }

    private IEnumerator ShowTooltipDelayed()
    {
        // 等待指定的延迟时间
        yield return new WaitForSecondsRealtime(tooltipDelay);
        
        // 显示提示面板
        if (tooltipController != null)
        {
            // 设置提示内容
            string shortcutText = keyboardShortcut.ToString();
            tooltipController.SetContent(actionName, actionDescription, $"[{shortcutText}]");
            
            // 显示提示
            tooltipController.Show();
        }
    }
    
    // Unity编辑器中用于在Inspector显示快捷键设置的方法
    public void SetKeyboardShortcut(KeyCode key)
    {
        keyboardShortcut = key;
    }
    
    // 动作名称设置方法
    public void SetActionName(string name)
    {
        actionName = name;
    }
    
    // 动作描述设置方法
    public void SetActionDescription(string description)
    {
        actionDescription = description;
    }
    
    // 提供给PlayerController调用的方法，用于触发按钮的快捷键逻辑
    public void TriggerKeyboardShortcut()
    {
        HandleKeyboardShortcut();
    }
} 