using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
public enum GameState
{
    Planning,   // 规划阶段，玩家选择要执行的动作类型
    Targeting,  // 目标选择阶段，玩家选择路径/落点等
    Executing,  // 执行阶段，角色移动，时间流动
    Paused      // 游戏暂停
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏设置")]
    [SerializeField] private PlayerController playerController;  // 玩家控制器引用
    [SerializeField] private float fixedTimeScale = 1.0f;        // 执行阶段的时间流速

    [Header("UI引用")]
    [SerializeField] private GameObject planningUI;              // 规划阶段UI
    [SerializeField] private GameObject targetingUI;             // 目标选择阶段UI
    [SerializeField] private GameObject executingUI;             // 执行阶段UI

    // 当前游戏状态
    public GameState CurrentState { get; private set; } = GameState.Planning;

    private void Awake()
    {
        // 单例模式设置
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 不随场景加载而销毁
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // 如果未指定玩家控制器，尝试查找
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            if (playerController == null)
            {
                Debug.LogError("GameManager: 未找到PlayerController！");
            }
        }

        // 初始化为规划阶段
        SetGameState(GameState.Planning);
    }

    // 设置游戏状态
    public void SetGameState(GameState newState)
    {
        // 退出当前状态
        switch (CurrentState)
        {
            case GameState.Planning:
                if (planningUI != null) planningUI.SetActive(false);
                break;

            case GameState.Targeting:
                if (targetingUI != null) targetingUI.SetActive(false);
                break;

            case GameState.Executing:
                if (executingUI != null) executingUI.SetActive(false);
                Time.timeScale = 0f; // 停止时间
                break;

            case GameState.Paused:
                // 退出暂停状态的逻辑
                break;
        }

        // 进入新状态
        CurrentState = newState;
        switch (newState)
        {
            case GameState.Planning:
                if (planningUI != null) planningUI.SetActive(true);
                Time.timeScale = 0f; // 停止时间
                break;

            case GameState.Targeting:
                if (targetingUI != null) targetingUI.SetActive(true);
                Time.timeScale = 0f; // 停止时间
                break;

            case GameState.Executing:
                if (executingUI != null) executingUI.SetActive(true);
                Time.timeScale = fixedTimeScale; // 恢复时间流动
                break;

            case GameState.Paused:
                Time.timeScale = 0f; // 停止时间
                // 显示暂停UI或其他暂停逻辑
                break;
        }

        // 通知系统状态变化
        OnGameStateChanged(newState);
    }

    // 游戏状态变化时的处理
    private void OnGameStateChanged(GameState newState)
    {
        // 通知其他系统游戏状态变化
        // 可以使用事件系统或直接调用
        
        // 通知玩家控制器
        if (playerController != null)
        {
            playerController.OnGameStateChanged(newState);
        }
    }

    // 进入目标选择阶段
    public void StartTargetingPhase()
    {
        if (CurrentState == GameState.Planning)
        {
            SetGameState(GameState.Targeting);
        }
    }

    // 目标选择阶段结束，返回规划阶段
    public void CancelTargetingPhase()
    {
        if (CurrentState == GameState.Targeting)
        {
            SetGameState(GameState.Planning);
        }
    }

    // 开始执行阶段（通常由玩家控制器调用）
    public void StartExecutionPhase()
    {
        if (CurrentState == GameState.Targeting)
        {
            SetGameState(GameState.Executing);
        }
    }

    // 执行阶段结束，返回规划阶段（通常由玩家控制器调用）
    public void EndExecutionPhase()
    {
        if (CurrentState == GameState.Executing)
        {
            SetGameState(GameState.Planning);
        }
    }

    // 暂停游戏
    public void PauseGame()
    {
        if (CurrentState != GameState.Paused)
        {
            SetGameState(GameState.Paused);
        }
    }

    // 恢复游戏到之前的状态
    public void ResumeGame()
    {
        // 这里可以存储进入暂停前的状态，然后恢复
        SetGameState(GameState.Planning); // 默认恢复到规划阶段
    }

    private void Update()
    {
        // 游戏逻辑更新
        switch (CurrentState)
        {
            case GameState.Planning:
                // 规划阶段的更新逻辑
                // 例如：检查玩家输入，更新UI等
                break;

            case GameState.Targeting:
                // 目标选择阶段的更新逻辑
                // 例如：检查玩家输入，显示路径预览等
                break;

            case GameState.Executing:
                // 执行阶段的更新逻辑
                // 例如：检查是否所有行动都执行完毕
                break;

            case GameState.Paused:
                // 暂停状态的更新逻辑
                break;
        }

        // Press R to reset the game
        if (Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // Press Escape or Right Mouse Button to cancel targeting
        if (CurrentState == GameState.Targeting && (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown(1)))
        {
            CancelTargetingPhase();
        }
    }
} 