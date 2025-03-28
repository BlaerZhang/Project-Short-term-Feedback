using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 呼吸状态枚举
/// </summary>
public enum BreathState
{
    Regular, // 普通呼吸
    Rush,    // 急促呼吸
    Steady,  // 稳定呼吸
    Core     // 核心呼吸
}

/// <summary>
/// 呼吸系统管理器，负责处理不同呼吸状态的切换和呼吸触发
/// </summary>
public class BreathManager : MonoBehaviour
{
    // 单例实例
    public static BreathManager Instance { get; private set; }

    [Header("呼吸状态设置")]
    [Tooltip("当前呼吸状态")]
    [SerializeField] private BreathState currentState = BreathState.Regular;
    [Tooltip("当前呼吸进度")]
    [SerializeField] private int currentProgress = 0;

    [Header("呼吸周期设置")]
    [Tooltip("普通呼吸周期")]
    [SerializeField] private int regularCycle = 1;
    [Tooltip("急促呼吸周期")]
    [SerializeField] private int rushCycle = 1;
    [Tooltip("稳定呼吸周期")]
    [SerializeField] private int steadyCycle = 2;
    [Tooltip("核心呼吸周期")]
    [SerializeField] private int coreCycle = 2;

    [Header("呼吸效果设置")]
    [Tooltip("普通呼吸精力消耗")]
    [SerializeField] private int regularEnergyCost = 0;
    [Tooltip("急促呼吸精力消耗")]
    [SerializeField] private int rushEnergyCost = 1;
    [Tooltip("稳定呼吸精力消耗")]
    [SerializeField] private int steadyEnergyCost = 1;
    [Tooltip("核心呼吸精力消耗")]
    [SerializeField] private int coreEnergyCost = 1;

    [Header("呼吸状态描述")]
    [Tooltip("普通呼吸描述")]
    [TextArea(3, 5)]
    [SerializeField] private string regularDescription = "普通呼吸\n周期: 1\n不消耗精力";
    [Tooltip("急促呼吸描述")]
    [TextArea(3, 5)]
    [SerializeField] private string rushDescription = "急促呼吸\n周期: 1\n呼吸消耗1精力\n奔跑氧气消耗-1";
    [Tooltip("稳定呼吸描述")]
    [TextArea(3, 5)]
    [SerializeField] private string steadyDescription = "稳定呼吸\n周期: 2\n呼吸消耗1精力";
    [Tooltip("核心呼吸描述")]
    [TextArea(3, 5)]
    [SerializeField] private string coreDescription = "核心呼吸\n周期: 2\n呼吸消耗1精力";

    [Header("UI设置")]
    [Tooltip("呼吸触发提示文本")]
    [SerializeField] private TextMeshProUGUI breatheText;
    [Tooltip("呼吸提示持续时间")]
    [SerializeField] private float breatheTextDuration = 2f;

    [Header("引用")]
    [SerializeField] private PlayerController playerController;
    [SerializeField] private GameManager gameManager;

    // 呼吸状态变化事件
    public event Action<BreathState> OnBreathStateChanged;
    // 呼吸进度变化事件
    public event Action<int, int> OnBreathProgressChanged;
    // 呼吸触发事件
    public event Action OnBreathTriggered;

    // 是否可以切换呼吸状态
    private bool canSwitchState = true;
    // 呼吸文本显示协程
    private Coroutine breatheTextCoroutine;

    // 是否有待结算的呼吸（Targeting结束后，等待Executing结束再结算）
    private bool hasPendingBreaths = false;

    private void Awake()
    {
        // 单例设置
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 隐藏呼吸提示文本
        if (breatheText != null)
        {
            breatheText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        // 获取引用
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }

        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        // 注册游戏状态变化事件
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged += HandleGameStateChanged;
            Debug.Log("BreathManager: 已注册游戏状态变更事件");
        }
        else
        {
            Debug.LogWarning("BreathManager: GameManager为空，无法监听游戏状态变化");
        }
    }

    private void OnDestroy()
    {
        // 取消订阅事件
        if (gameManager != null)
        {
            gameManager.OnGameStateChanged -= HandleGameStateChanged;
        }
    }

    // 修改HandleGameStateChanged方法，只标记状态，不触发结算
    private void HandleGameStateChanged(GameState newState, GameState previousState)
    {
        Debug.Log($"BreathManager: 游戏状态变更 {previousState} -> {newState}");
        
        // 从Targeting进入Executing时，只标记，不触发结算
        if (previousState == GameState.Targeting && newState == GameState.Executing)
        {
            Debug.Log("BreathManager: Targeting阶段结束，标记等待Executing结束后再结算呼吸");
            // 只标记，不触发结算
            hasPendingBreaths = true;
        }
        
        // 从Executing退出时才结算
        if (previousState == GameState.Executing && 
            (newState == GameState.Planning || newState == GameState.Targeting))
        {
            if (hasPendingBreaths)
            {
                Debug.Log("BreathManager: Executing阶段结束，现在结算之前累积的呼吸");
                SettlePendingBreaths();
                hasPendingBreaths = false;
            }
        }
    }

    /// <summary>
    /// 获取当前呼吸状态
    /// </summary>
    public BreathState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// 获取当前呼吸进度
    /// </summary>
    public int GetCurrentProgress()
    {
        return currentProgress;
    }

    /// <summary>
    /// 获取当前呼吸状态的周期
    /// </summary>
    public int GetCurrentCycle()
    {
        switch (currentState)
        {
            case BreathState.Regular:
                return regularCycle;
            case BreathState.Rush:
                return rushCycle;
            case BreathState.Steady:
                return steadyCycle;
            case BreathState.Core:
                return coreCycle;
            default:
                return 1;
        }
    }

    /// <summary>
    /// 获取当前呼吸状态的精力消耗
    /// </summary>
    public int GetCurrentEnergyCost()
    {
        switch (currentState)
        {
            case BreathState.Regular:
                return regularEnergyCost;
            case BreathState.Rush:
                return rushEnergyCost;
            case BreathState.Steady:
                return steadyEnergyCost;
            case BreathState.Core:
                return coreEnergyCost;
            default:
                return 0;
        }
    }

    /// <summary>
    /// 切换呼吸状态
    /// </summary>
    public void SwitchBreathState(BreathState newState)
    {
        Debug.Log($"尝试切换呼吸状态：当前={currentState}, 目标={newState}");
        
        // 如果是当前状态，不需要切换
        if (newState == currentState)
        {
            Debug.Log("已经是当前状态，无需切换");
            return;
        }

        // 检查是否可以切换状态
        if (!canSwitchState)
        {
            Debug.LogWarning("当前不能切换呼吸状态");
            return;
        }

        // 检查游戏状态是否允许切换
        if (gameManager != null)
        {
            Debug.Log($"当前游戏状态: {gameManager.CurrentState}");
            if (gameManager.CurrentState != GameState.Planning && 
                gameManager.CurrentState != GameState.Targeting)
            {
                Debug.LogWarning("只能在规划或目标选择阶段切换呼吸状态");
                return;
            }
        }
        else
        {
            Debug.LogWarning("GameManager为空，无法检查游戏状态");
        }

        // 切换状态
        currentState = newState;
        
        // 重置呼吸进度
        currentProgress = 0;
        
        // 触发事件
        OnBreathStateChanged?.Invoke(newState);
        OnBreathProgressChanged?.Invoke(currentProgress, GetCurrentCycle());
        
        Debug.Log($"呼吸状态已成功切换为: {newState}");
    }

    /// <summary>
    /// 增加呼吸进度
    /// </summary>
    public void AddBreathProgress(int amount)
    {
        if (amount <= 0)
            return;

        int currentCycle = GetCurrentCycle();
        
        // 记录增加前的进度，用于判断是否会触发呼吸
        int previousProgress = currentProgress;
        bool wouldTriggerBreath = (previousProgress + amount) >= currentCycle;
        
        // 增加进度
        currentProgress += amount;
        
        Debug.Log($"BreathManager: 增加呼吸进度 +{amount}，当前进度={currentProgress}/{currentCycle}");
        
        // 检查是否在Executing阶段且会触发呼吸
        if (gameManager != null && gameManager.CurrentState == GameState.Executing && wouldTriggerBreath)
        {
            Debug.Log($"BreathManager: 在Executing阶段累积了足够触发呼吸的进度，但会等待阶段结束后再结算");
            // 只标记有待结算的呼吸，不触发呼吸效果
            hasPendingBreaths = true;
            
            // 触发进度变化事件，让UI更新
            OnBreathProgressChanged?.Invoke(currentProgress, currentCycle);
            return;
        }
        
        // 不在Executing阶段，或者虽然在Executing但不会触发呼吸（进度不够）时
        // 可以正常检查和触发呼吸（如在Planning阶段）
        if (gameManager == null || gameManager.CurrentState != GameState.Executing)
        {
            CheckAndTriggerBreath();
        }
        
        // 无论如何都更新进度条
        OnBreathProgressChanged?.Invoke(currentProgress, currentCycle);
    }

    /// <summary>
    /// 结算待处理的呼吸
    /// </summary>
    private void SettlePendingBreaths()
    {
        if (!hasPendingBreaths)
            return;
        
        Debug.Log("BreathManager: 开始结算待处理的呼吸");
        
        // 检查并触发呼吸
        CheckAndTriggerBreath();
        
        // 重置待结算标记
        hasPendingBreaths = false;
    }

    /// <summary>
    /// 检查并触发呼吸
    /// </summary>
    private void CheckAndTriggerBreath()
    {
        int currentCycle = GetCurrentCycle();
        
        // 检查是否触发呼吸
        while (currentProgress >= currentCycle)
        {
            // 触发呼吸
            TriggerBreath();
            
            // 减去一个周期的进度
            currentProgress -= currentCycle;
        }
        
        // 再次触发进度变化事件（因为进度可能被减少）
        OnBreathProgressChanged?.Invoke(currentProgress, currentCycle);
    }

    /// <summary>
    /// 触发呼吸效果
    /// </summary>
    private void TriggerBreath()
    {
        // 获取当前呼吸状态的精力消耗
        int energyCost = GetCurrentEnergyCost();
        
        Debug.Log($"BreathManager: 触发呼吸效果 - 状态={currentState}, 消耗精力={energyCost}");
        
        // 如果有精力消耗且资源管理器存在，扣除精力
        if (energyCost > 0 && ResourceManager.Instance != null)
        {
            ResourceManager.Instance.ChangeResource(ResourceType.Energy, -energyCost);
        }
        
        // 应用呼吸状态效果 - 这些在其他地方处理，如Rush状态下奔跑氧气消耗减少
        
        // 显示呼吸提示
        ShowBreatheText();
        
        // 触发呼吸事件
        OnBreathTriggered?.Invoke();
        
        Debug.Log($"已触发呼吸: {currentState}, 消耗精力: {energyCost}");
    }

    /// <summary>
    /// 显示呼吸提示文本
    /// </summary>
    private void ShowBreatheText()
    {
        if (breatheText == null)
            return;
            
        // 如果有正在运行的协程，停止它
        if (breatheTextCoroutine != null)
        {
            StopCoroutine(breatheTextCoroutine);
        }
        
        // 启动新的协程
        breatheTextCoroutine = StartCoroutine(ShowBreatheTextCoroutine());
    }

    /// <summary>
    /// 呼吸提示文本显示协程
    /// </summary>
    private IEnumerator ShowBreatheTextCoroutine()
    {
        // 显示文本
        breatheText.gameObject.SetActive(true);
        breatheText.text = "Breathe!";
        
        // 等待指定时间
        yield return new WaitForSecondsRealtime(breatheTextDuration);
        
        // 隐藏文本
        breatheText.gameObject.SetActive(false);
        
        // 清空协程引用
        breatheTextCoroutine = null;
    }

    /// <summary>
    /// 设置是否可以切换呼吸状态
    /// </summary>
    public void SetCanSwitchState(bool canSwitch)
    {
        canSwitchState = canSwitch;
    }

    /// <summary>
    /// 获取呼吸状态的描述
    /// </summary>
    public string GetBreathStateDescription(BreathState state)
    {
        switch (state)
        {
            case BreathState.Regular:
                return regularDescription;
            case BreathState.Rush:
                return rushDescription;
            case BreathState.Steady:
                return steadyDescription;
            case BreathState.Core:
                return coreDescription;
            default:
                return "";
        }
    }

    // 在类中添加一个专门用于调试的设置呼吸进度方法
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// 设置呼吸进度（仅用于开发调试）
    /// </summary>
    public void SetBreathProgressForDebug(int progress)
    {
        // 确保进度在有效范围内
        progress = Mathf.Clamp(progress, 0, GetCurrentCycle());
        
        // 设置进度
        currentProgress = progress;
        
        // 触发进度变化事件
        OnBreathProgressChanged?.Invoke(currentProgress, GetCurrentCycle());
        
        Debug.Log($"BreathManager: 调试功能 - 呼吸进度设置为 {currentProgress}/{GetCurrentCycle()}");
    }
    #endif
} 