using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Developer Tools Manager - For in-game debugging
/// </summary>
public class DeveloperToolsManager : MonoBehaviour
{
    // 单例实例
    public static DeveloperToolsManager Instance { get; private set; }

    [Header("Trigger Settings")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private bool requireControlKey = true;

    [Header("Window Settings")]
    [SerializeField] private Vector2 windowSize = new Vector2(400f, 500f);
    [SerializeField] private float windowAlpha = 1.0f;

    // 资源引用
    private ResourceManager resourceManager;
    private BreathManager breathManager;
    private PlayerController playerController;
    private GameManager gameManager;

    // 窗口状态
    private bool isWindowVisible = false;
    private Rect windowRect;
    private int currentTab = 0;
    private Vector2 scrollPosition;

    // 工具数据
    private float energyValue = 0f;
    private float oxygenValue = 0f;
    private float pressureValue = 0f;
    private int breathProgress = 0;
    private BreathState breathState = BreathState.Regular;
    private int speedLevel = 1;
    private bool enableTurningAction = false;

    // 窗口样式
    private GUIStyle windowStyle;
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private GUIStyle sliderLabelStyle;
    private GUIStyle toggleStyle;

    // 日志记录
    private List<string> actionLogs = new List<string>();
    private int maxLogEntries = 10;

    // 转向按钮引用
    private ActionButtonController turningActionButton;

    private void Awake()
    {
        // 单例设置
        if (Instance == null)
        {
            Instance = this;
            // DontDestroyOnLoad(gameObject); // 保持注释状态，避免重置场景时出问题
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 初始化窗口位置
        windowRect = new Rect(
            Screen.width - windowSize.x - 20,
            20,
            windowSize.x,
            windowSize.y
        );

        // 创建日志记录
        AddLog("Developer Tools initialized");
    }

    private void Start()
    {
        // 获取所需引用
        resourceManager = ResourceManager.Instance;
        breathManager = BreathManager.Instance;
        playerController = FindObjectOfType<PlayerController>();
        gameManager = GameManager.Instance;

        // 查找转向按钮
        FindTurningActionButton();

        // 初始化数据
        RefreshResourceValues();
        
        AddLog("System references acquired");
    }

    private void Update()
    {
        // 检查开发者工具切换快捷键
        bool controlPressed = !requireControlKey || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
        if (controlPressed && Input.GetKeyDown(toggleKey))
        {
            ToggleWindow();
        }
    }

    private void OnGUI()
    {
        if (!isWindowVisible)
            return;

        // 初始化样式
        InitializeStyles();

        // 绘制主窗口
        windowRect = GUILayout.Window(0, windowRect, DrawWindow, "Developer Tools", windowStyle);
    }

    /// <summary>
    /// 初始化GUI样式
    /// </summary>
    private void InitializeStyles()
    {
        if (windowStyle == null)
        {
            // 窗口样式
            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, windowAlpha));

            // 标题样式
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.fontSize = 14;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = Color.white;
            
            // 按钮样式
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.textColor = Color.white;
            buttonStyle.hover.textColor = Color.yellow;
            
            // 滑动条标签样式
            sliderLabelStyle = new GUIStyle(GUI.skin.label);
            sliderLabelStyle.normal.textColor = Color.white;
            sliderLabelStyle.fixedWidth = 100;
            
            // 复选框样式
            toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.normal.textColor = Color.white;
        }
    }

    /// <summary>
    /// 绘制窗口内容
    /// </summary>
    private void DrawWindow(int windowID)
    {
        // 标签页按钮
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(currentTab == 0, "Resources", GUI.skin.button, GUILayout.Height(30)))
            currentTab = 0;
        if (GUILayout.Toggle(currentTab == 1, "Controls", GUI.skin.button, GUILayout.Height(30)))
            currentTab = 1;
        if (GUILayout.Toggle(currentTab == 2, "Debug Info", GUI.skin.button, GUILayout.Height(30)))
            currentTab = 2;
        GUILayout.EndHorizontal();

        // 分隔线
        GUILayout.Box("", GUILayout.ExpandWidth(true), GUILayout.Height(2));

        // 滚动视图
        scrollPosition = GUILayout.BeginScrollView(scrollPosition);

        // 根据当前标签页绘制不同内容
        switch (currentTab)
        {
            case 0:
                DrawResourceAdjustTab();
                break;
            case 1:
                DrawControlTab();
                break;
            case 2:
                DrawDebugInfoTab();
                break;
        }

        GUILayout.EndScrollView();

        // 关闭按钮
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Close", GUILayout.Width(80), GUILayout.Height(25)))
        {
            ToggleWindow();
        }
        GUILayout.EndHorizontal();

        // 允许窗口拖动
        GUI.DragWindow(new Rect(0, 0, windowRect.width, 20));
    }

    /// <summary>
    /// 绘制资源调整标签页
    /// </summary>
    private void DrawResourceAdjustTab()
    {
        GUILayout.Label("Resource Adjustment", headerStyle);
        GUILayout.Space(10);

        if (resourceManager != null)
        {
            // 精力调整
            GUILayout.BeginHorizontal();
            GUILayout.Label("Energy:", sliderLabelStyle);
            
            // 使用更简单的滑动条方式
            float newEnergyValue = energyValue;
            GUI.changed = false;
            newEnergyValue = GUILayout.HorizontalSlider(
                newEnergyValue, 
                0, 
                resourceManager.GetResourceMaxValue(ResourceType.Energy),
                GUILayout.ExpandWidth(true)
            );
            bool sliderChanged = GUI.changed;
            
            string energyString = GUILayout.TextField(
                energyValue.ToString("F0"), 
                GUILayout.Width(50)
            );
            
            if (float.TryParse(energyString, out float parsedEnergy))
            {
                newEnergyValue = parsedEnergy;
                sliderChanged = true;
            }
            
            if (sliderChanged && newEnergyValue != energyValue)
            {
                energyValue = newEnergyValue;
                resourceManager.DebugSetResourceValue(ResourceType.Energy, energyValue);
                AddLog($"Energy set to: {energyValue}");
            }
            GUILayout.EndHorizontal();
            
            // 氧气调整
            GUILayout.BeginHorizontal();
            GUILayout.Label("Oxygen:", sliderLabelStyle);
            
            // 使用更简单的滑动条方式
            float newOxygenValue = oxygenValue;
            GUI.changed = false;
            newOxygenValue = GUILayout.HorizontalSlider(
                newOxygenValue, 
                0, 
                resourceManager.GetResourceMaxValue(ResourceType.Oxygen),
                GUILayout.ExpandWidth(true)
            );
            sliderChanged = GUI.changed;
            
            string oxygenString = GUILayout.TextField(
                oxygenValue.ToString("F0"), 
                GUILayout.Width(50)
            );
            
            if (float.TryParse(oxygenString, out float parsedOxygen))
            {
                newOxygenValue = parsedOxygen;
                sliderChanged = true;
            }
            
            if (sliderChanged && newOxygenValue != oxygenValue)
            {
                oxygenValue = newOxygenValue;
                resourceManager.DebugSetResourceValue(ResourceType.Oxygen, oxygenValue);
                AddLog($"Oxygen set to: {oxygenValue}");
            }
            GUILayout.EndHorizontal();
            
            // 压力调整
            GUILayout.BeginHorizontal();
            GUILayout.Label("Pressure:", sliderLabelStyle);
            
            // 使用更简单的滑动条方式
            float newPressureValue = pressureValue;
            GUI.changed = false;
            newPressureValue = GUILayout.HorizontalSlider(
                newPressureValue, 
                0, 
                resourceManager.GetResourceMaxValue(ResourceType.Pressure),
                GUILayout.ExpandWidth(true)
            );
            sliderChanged = GUI.changed;
            
            string pressureString = GUILayout.TextField(
                pressureValue.ToString("F0"), 
                GUILayout.Width(50)
            );
            
            if (float.TryParse(pressureString, out float parsedPressure))
            {
                newPressureValue = parsedPressure;
                sliderChanged = true;
            }
            
            if (sliderChanged && newPressureValue != pressureValue)
            {
                pressureValue = newPressureValue;
                resourceManager.DebugSetResourceValue(ResourceType.Pressure, pressureValue);
                AddLog($"Pressure set to: {pressureValue}");
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("Resource Manager unavailable", sliderLabelStyle);
        }
        
        GUILayout.Space(15);
        GUILayout.Label("Breathing System", headerStyle);
        GUILayout.Space(10);
        
        if (breathManager != null)
        {
            // 呼吸状态选择
            GUILayout.BeginHorizontal();
            GUILayout.Label("Breath State:", sliderLabelStyle);
            
            GUILayout.BeginVertical();
            bool isRegular = GUILayout.Toggle(breathState == BreathState.Regular, "Regular", toggleStyle);
            bool isRush = GUILayout.Toggle(breathState == BreathState.Rush, "Rush", toggleStyle);
            bool isSteady = GUILayout.Toggle(breathState == BreathState.Steady, "Steady", toggleStyle);
            bool isCore = GUILayout.Toggle(breathState == BreathState.Core, "Core", toggleStyle);
            GUILayout.EndVertical();
            
            if (isRegular && breathState != BreathState.Regular)
            {
                breathState = BreathState.Regular;
                breathManager.SwitchBreathState(breathState);
                AddLog($"Breath state set to: {breathState}");
            }
            else if (isRush && breathState != BreathState.Rush)
            {
                breathState = BreathState.Rush;
                breathManager.SwitchBreathState(breathState);
                AddLog($"Breath state set to: {breathState}");
            }
            else if (isSteady && breathState != BreathState.Steady)
            {
                breathState = BreathState.Steady;
                breathManager.SwitchBreathState(breathState);
                AddLog($"Breath state set to: {breathState}");
            }
            else if (isCore && breathState != BreathState.Core)
            {
                breathState = BreathState.Core;
                breathManager.SwitchBreathState(breathState);
                AddLog($"Breath state set to: {breathState}");
            }
            GUILayout.EndHorizontal();
            
            // 呼吸进度调整
            GUILayout.BeginHorizontal();
            GUILayout.Label("Breath Progress:", sliderLabelStyle);
            int cycle = breathManager.GetCurrentCycle();

            // 使用同样的改进方式
            int newBreathProgress = breathProgress;
            GUI.changed = false;
            newBreathProgress = Mathf.RoundToInt(GUILayout.HorizontalSlider(
                newBreathProgress, 
                0, 
                cycle,
                GUILayout.ExpandWidth(true)
            ));
            bool sliderChanged = GUI.changed;

            string progressString = GUILayout.TextField(
                $"{breathProgress}/{cycle}", 
                GUILayout.Width(50)
            );

            string[] progressParts = progressString.Split('/');
            if (progressParts.Length > 0 && int.TryParse(progressParts[0], out int parsedProgress))
            {
                newBreathProgress = parsedProgress;
                sliderChanged = true;
            }

            if (sliderChanged && newBreathProgress != breathProgress)
            {
                breathProgress = newBreathProgress;
                
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (breathManager != null)
                {
                    breathManager.SetBreathProgressForDebug(breathProgress);
                    AddLog($"Breath progress set to: {breathProgress}/{cycle}");
                }
                #endif
            }
            GUILayout.EndHorizontal();
            
            // 触发呼吸按钮
            if (GUILayout.Button("Trigger Breath Effect", buttonStyle, GUILayout.Height(30)))
            {
                // 让呼吸进度达到周期值来触发呼吸
                breathManager.AddBreathProgress(cycle - breathManager.GetCurrentProgress());
                AddLog("Manually triggered breath effect");
            }
        }
        else
        {
            GUILayout.Label("Breath Manager unavailable", sliderLabelStyle);
        }
        
        GUILayout.Space(15);
        GUILayout.Label("Character Control", headerStyle);
        GUILayout.Space(10);
        
        if (playerController != null)
        {
            // 速度档位调整
            GUILayout.BeginHorizontal();
            GUILayout.Label("Speed Level:", sliderLabelStyle);
            
            bool isSpeed1 = GUILayout.Toggle(speedLevel == 1, "Level 1", buttonStyle);
            bool isSpeed2 = GUILayout.Toggle(speedLevel == 2, "Level 2", buttonStyle);
            bool isSpeed3 = GUILayout.Toggle(speedLevel == 3, "Level 3", buttonStyle);
            
            if (isSpeed1 && speedLevel != 1)
            {
                speedLevel = 1;
                playerController.SetSpeed(speedLevel);
                AddLog($"Speed directly set to: Level {speedLevel} (no energy cost)");
            }
            else if (isSpeed2 && speedLevel != 2)
            {
                speedLevel = 2;
                playerController.SetSpeed(speedLevel);
                AddLog($"Speed directly set to: Level {speedLevel} (no energy cost)");
            }
            else if (isSpeed3 && speedLevel != 3)
            {
                speedLevel = 3;
                playerController.SetSpeed(speedLevel);
                AddLog($"Speed directly set to: Level {speedLevel} (no energy cost)");
            }
            GUILayout.EndHorizontal();

            // 添加开发者功能说明
            GUI.contentColor = Color.yellow;
            GUILayout.Label("Note: Developer speed setting bypasses game rules (no energy cost, no turn limit)", GUI.skin.label);
            GUI.contentColor = Color.white;
        }
        else
        {
            GUILayout.Label("Player Controller unavailable", sliderLabelStyle);
        }
        
        GUILayout.Space(10);
        
        // 刷新按钮
        if (GUILayout.Button("Refresh Data", buttonStyle, GUILayout.Height(30)))
        {
            RefreshResourceValues();
            AddLog("Refreshed all data");
        }
    }

    /// <summary>
    /// 绘制功能控制标签页
    /// </summary>
    private void DrawControlTab()
    {
        GUILayout.Label("Function Controls", headerStyle);
        GUILayout.Space(10);
        
        // 转向动作开关
        GUILayout.BeginHorizontal();
        bool newEnableTurningAction = GUILayout.Toggle(enableTurningAction, "Enable Turn Action", toggleStyle);
        if (newEnableTurningAction != enableTurningAction)
        {
            enableTurningAction = newEnableTurningAction;
            
            if (turningActionButton != null)
            {
                // 直接设置按钮的活动状态
                turningActionButton.gameObject.SetActive(enableTurningAction);
                AddLog($"Turn button {(enableTurningAction ? "enabled" : "disabled")}");
            }
            else if (playerController != null)
            {
                // 如果没有找到按钮，使用玩家控制器的方法
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                playerController.EnableTurningAction(enableTurningAction);
                #endif
                AddLog($"Turn action {(enableTurningAction ? "enabled" : "disabled")}");
                
                // 尝试再次查找按钮
                FindTurningActionButton();
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(15);
        GUILayout.Label("Game State Control", headerStyle);
        GUILayout.Space(10);
        
        if (gameManager != null)
        {
            // 游戏状态切换按钮
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Planning Phase", buttonStyle, GUILayout.Height(30)))
            {
                gameManager.SetGameState(GameState.Planning);
                AddLog("Switched to Planning phase");
            }
            if (GUILayout.Button("Targeting Phase", buttonStyle, GUILayout.Height(30)))
            {
                gameManager.SetGameState(GameState.Targeting);
                AddLog("Switched to Targeting phase");
            }
            if (GUILayout.Button("Execution Phase", buttonStyle, GUILayout.Height(30)))
            {
                gameManager.SetGameState(GameState.Executing);
                AddLog("Switched to Execution phase");
            }
            GUILayout.EndHorizontal();
            
            // 时间控制按钮
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Pause", buttonStyle, GUILayout.Height(30)))
            {
                Time.timeScale = 0f;
                AddLog("Game paused");
            }
            if (GUILayout.Button("1/4 Speed", buttonStyle, GUILayout.Height(30)))
            {
                Time.timeScale = 0.25f;
                AddLog("Game speed set to 1/4");
            }
            if (GUILayout.Button("1/2 Speed", buttonStyle, GUILayout.Height(30)))
            {
                Time.timeScale = 0.5f;
                AddLog("Game speed set to 1/2");
            }
            if (GUILayout.Button("Normal Speed", buttonStyle, GUILayout.Height(30)))
            {
                Time.timeScale = 1f;
                AddLog("Game speed restored to normal");
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("Game Manager unavailable", sliderLabelStyle);
        }
    }

    /// <summary>
    /// 绘制调试信息标签页
    /// </summary>
    private void DrawDebugInfoTab()
    {
        GUILayout.Label("Debug Information", headerStyle);
        GUILayout.Space(10);
        
        // 游戏状态信息
        GUILayout.Label("Basic Information", headerStyle);
        GUILayout.Space(5);
        
        if (gameManager != null)
        {
            GUILayout.Label($"Current Game State: {gameManager.CurrentState}");
            GUILayout.Label($"Time Scale: {Time.timeScale:F2}");
            GUILayout.Label($"FPS: {(int)(1.0f / Time.deltaTime)}");
        }
        
        if (playerController != null)
        {
            GUILayout.Label($"Character Position: {playerController.transform.position}");
            GUILayout.Label($"Current Speed: {playerController.GetCurrentSpeed():F1}");
        }
        
        if (resourceManager != null)
        {
            GUILayout.Label($"Energy: {resourceManager.GetResourceValue(ResourceType.Energy)} / {resourceManager.GetResourceMaxValue(ResourceType.Energy)}");
            GUILayout.Label($"Oxygen: {resourceManager.GetResourceValue(ResourceType.Oxygen)} / {resourceManager.GetResourceMaxValue(ResourceType.Oxygen)}");
            GUILayout.Label($"Pressure: {resourceManager.GetResourceValue(ResourceType.Pressure)} / {resourceManager.GetResourceMaxValue(ResourceType.Pressure)}");
        }
        
        if (breathManager != null)
        {
            GUILayout.Label($"Breath State: {breathManager.GetCurrentState()}");
            GUILayout.Label($"Breath Progress: {breathManager.GetCurrentProgress()} / {breathManager.GetCurrentCycle()}");
        }
        
        // 操作日志
        GUILayout.Space(15);
        GUILayout.Label("Action Log", headerStyle);
        GUILayout.Space(5);
        
        foreach (string log in actionLogs)
        {
            GUILayout.Label(log);
        }
    }

    /// <summary>
    /// 创建纯色纹理
    /// </summary>
    private Texture2D MakeTexture(int width, int height, Color color)
    {
        Color[] pixels = new Color[width * height];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }

        Texture2D texture = new Texture2D(width, height);
        texture.SetPixels(pixels);
        texture.Apply();

        return texture;
    }

    /// <summary>
    /// 切换窗口显示状态
    /// </summary>
    public void ToggleWindow()
    {
        isWindowVisible = !isWindowVisible;
        if (isWindowVisible)
        {
            RefreshResourceValues();
        }
    }

    /// <summary>
    /// 刷新资源数值
    /// </summary>
    private void RefreshResourceValues()
    {
        if (resourceManager != null)
        {
            energyValue = resourceManager.GetResourceValue(ResourceType.Energy);
            oxygenValue = resourceManager.GetResourceValue(ResourceType.Oxygen);
            pressureValue = resourceManager.GetResourceValue(ResourceType.Pressure);
        }

        if (breathManager != null)
        {
            breathProgress = breathManager.GetCurrentProgress();
            breathState = breathManager.GetCurrentState();
        }

        if (playerController != null && speedLevel == 0)
        {
            // 仅在初始化时读取当前速度，后续保持用户设置值
            speedLevel = Mathf.RoundToInt(playerController.GetCurrentSpeed());
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 获取转向动作是否启用的状态
            enableTurningAction = playerController.IsTurningActionEnabled();
            #endif
        }
    }

    /// <summary>
    /// 添加操作日志
    /// </summary>
    private void AddLog(string message)
    {
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        actionLogs.Insert(0, $"[{timestamp}] {message}");
        
        // 限制日志数量
        while (actionLogs.Count > maxLogEntries)
        {
            actionLogs.RemoveAt(actionLogs.Count - 1);
        }
    }

    /// <summary>
    /// 清理资源
    /// </summary>
    private void OnDestroy()
    {
        AddLog("Developer Tools destroyed");
    }

    /// <summary>
    /// 查找场景中的转向按钮
    /// </summary>
    private void FindTurningActionButton()
    {
        ActionButtonController[] allButtons = FindObjectsOfType<ActionButtonController>();
        foreach (var button in allButtons)
        {
            // 仅通过按钮名称查找转向按钮
            if (button.gameObject.name.ToLower().Contains("turn") || 
                button.gameObject.name.ToLower().Contains("转向"))
            {
                turningActionButton = button;
                AddLog("Found turn button: " + button.gameObject.name);
                
                // 获取当前转向按钮启用状态
                enableTurningAction = turningActionButton.gameObject.activeSelf;
                break;
            }
        }
        
        if (turningActionButton == null)
        {
            AddLog("Turn button not found!");
        }
    }
} 