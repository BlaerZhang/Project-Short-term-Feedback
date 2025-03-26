using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 开发者工具管理器 - 用于游戏内调试
/// </summary>
public class DeveloperToolsManager : MonoBehaviour
{
    // 单例实例
    public static DeveloperToolsManager Instance { get; private set; }

    [Header("触发设置")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;
    [SerializeField] private bool requireControlKey = true;

    [Header("窗口设置")]
    [SerializeField] private Vector2 windowSize = new Vector2(400f, 500f);
    [SerializeField] private float windowAlpha = 0.9f;

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
            DontDestroyOnLoad(gameObject);
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
        AddLog("开发者工具已初始化");
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
        
        AddLog("系统引用已获取");
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
        windowRect = GUILayout.Window(0, windowRect, DrawWindow, "开发者工具", windowStyle);
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
        if (GUILayout.Toggle(currentTab == 0, "资源调整", GUI.skin.button, GUILayout.Height(30)))
            currentTab = 0;
        if (GUILayout.Toggle(currentTab == 1, "功能控制", GUI.skin.button, GUILayout.Height(30)))
            currentTab = 1;
        if (GUILayout.Toggle(currentTab == 2, "调试信息", GUI.skin.button, GUILayout.Height(30)))
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
        if (GUILayout.Button("关闭", GUILayout.Width(80), GUILayout.Height(25)))
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
        GUILayout.Label("资源调整", headerStyle);
        GUILayout.Space(10);

        if (resourceManager != null)
        {
            // 精力调整
            GUILayout.BeginHorizontal();
            GUILayout.Label("精力:", sliderLabelStyle);
            
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
                AddLog($"精力设置为: {energyValue}");
            }
            GUILayout.EndHorizontal();
            
            // 氧气调整
            GUILayout.BeginHorizontal();
            GUILayout.Label("氧气:", sliderLabelStyle);
            
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
                AddLog($"氧气设置为: {oxygenValue}");
            }
            GUILayout.EndHorizontal();
            
            // 压力调整
            GUILayout.BeginHorizontal();
            GUILayout.Label("压力:", sliderLabelStyle);
            
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
                AddLog($"压力设置为: {pressureValue}");
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("资源管理器不可用", sliderLabelStyle);
        }
        
        GUILayout.Space(15);
        GUILayout.Label("呼吸系统", headerStyle);
        GUILayout.Space(10);
        
        if (breathManager != null)
        {
            // 呼吸状态选择
            GUILayout.BeginHorizontal();
            GUILayout.Label("呼吸状态:", sliderLabelStyle);
            
            GUILayout.BeginVertical();
            bool isRegular = GUILayout.Toggle(breathState == BreathState.Regular, "普通呼吸", toggleStyle);
            bool isRush = GUILayout.Toggle(breathState == BreathState.Rush, "急促呼吸", toggleStyle);
            bool isSteady = GUILayout.Toggle(breathState == BreathState.Steady, "稳定呼吸", toggleStyle);
            bool isCore = GUILayout.Toggle(breathState == BreathState.Core, "核心呼吸", toggleStyle);
            GUILayout.EndVertical();
            
            if (isRegular && breathState != BreathState.Regular)
            {
                breathState = BreathState.Regular;
                breathManager.SwitchBreathState(breathState);
                AddLog($"呼吸状态设置为: {breathState}");
            }
            else if (isRush && breathState != BreathState.Rush)
            {
                breathState = BreathState.Rush;
                breathManager.SwitchBreathState(breathState);
                AddLog($"呼吸状态设置为: {breathState}");
            }
            else if (isSteady && breathState != BreathState.Steady)
            {
                breathState = BreathState.Steady;
                breathManager.SwitchBreathState(breathState);
                AddLog($"呼吸状态设置为: {breathState}");
            }
            else if (isCore && breathState != BreathState.Core)
            {
                breathState = BreathState.Core;
                breathManager.SwitchBreathState(breathState);
                AddLog($"呼吸状态设置为: {breathState}");
            }
            GUILayout.EndHorizontal();
            
            // 呼吸进度调整
            GUILayout.BeginHorizontal();
            GUILayout.Label("呼吸进度:", sliderLabelStyle);
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
                    AddLog($"呼吸进度设置为: {breathProgress}/{cycle}");
                }
                #endif
            }
            GUILayout.EndHorizontal();
            
            // 触发呼吸按钮
            if (GUILayout.Button("触发呼吸效果", buttonStyle, GUILayout.Height(30)))
            {
                // 让呼吸进度达到周期值来触发呼吸
                breathManager.AddBreathProgress(cycle - breathManager.GetCurrentProgress());
                AddLog("手动触发呼吸效果");
            }
        }
        else
        {
            GUILayout.Label("呼吸管理器不可用", sliderLabelStyle);
        }
        
        GUILayout.Space(15);
        GUILayout.Label("角色控制", headerStyle);
        GUILayout.Space(10);
        
        if (playerController != null)
        {
            // 速度档位调整
            GUILayout.BeginHorizontal();
            GUILayout.Label("速度档位:", sliderLabelStyle);
            
            bool isSpeed1 = GUILayout.Toggle(speedLevel == 1, "1档", buttonStyle);
            bool isSpeed2 = GUILayout.Toggle(speedLevel == 2, "2档", buttonStyle);
            bool isSpeed3 = GUILayout.Toggle(speedLevel == 3, "3档", buttonStyle);
            
            if (isSpeed1 && speedLevel != 1)
            {
                speedLevel = 1;
                playerController.SetSpeed(speedLevel);
                AddLog($"速度设置为: {speedLevel}档");
            }
            else if (isSpeed2 && speedLevel != 2)
            {
                speedLevel = 2;
                playerController.SetSpeed(speedLevel);
                AddLog($"速度设置为: {speedLevel}档");
            }
            else if (isSpeed3 && speedLevel != 3)
            {
                speedLevel = 3;
                playerController.SetSpeed(speedLevel);
                AddLog($"速度设置为: {speedLevel}档");
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("玩家控制器不可用", sliderLabelStyle);
        }
        
        GUILayout.Space(10);
        
        // 刷新按钮
        if (GUILayout.Button("刷新数据", buttonStyle, GUILayout.Height(30)))
        {
            RefreshResourceValues();
            AddLog("刷新所有数据");
        }
    }

    /// <summary>
    /// 绘制功能控制标签页
    /// </summary>
    private void DrawControlTab()
    {
        GUILayout.Label("功能控制", headerStyle);
        GUILayout.Space(10);
        
        // 转向动作开关
        GUILayout.BeginHorizontal();
        bool newEnableTurningAction = GUILayout.Toggle(enableTurningAction, "启用转向动作", toggleStyle);
        if (newEnableTurningAction != enableTurningAction)
        {
            enableTurningAction = newEnableTurningAction;
            
            if (turningActionButton != null)
            {
                // 直接设置按钮的活动状态
                turningActionButton.gameObject.SetActive(enableTurningAction);
                AddLog($"转向按钮已{(enableTurningAction ? "启用" : "禁用")}");
            }
            else if (playerController != null)
            {
                // 如果没有找到按钮，使用玩家控制器的方法
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                playerController.EnableTurningAction(enableTurningAction);
                #endif
                AddLog($"转向动作已{(enableTurningAction ? "启用" : "禁用")}");
                
                // 尝试再次查找按钮
                FindTurningActionButton();
            }
        }
        GUILayout.EndHorizontal();
        
        GUILayout.Space(15);
        GUILayout.Label("游戏状态控制", headerStyle);
        GUILayout.Space(10);
        
        if (gameManager != null)
        {
            // 游戏状态切换按钮
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("规划阶段", buttonStyle, GUILayout.Height(30)))
            {
                gameManager.SetGameState(GameState.Planning);
                AddLog("切换到规划阶段");
            }
            if (GUILayout.Button("目标选择", buttonStyle, GUILayout.Height(30)))
            {
                gameManager.SetGameState(GameState.Targeting);
                AddLog("切换到目标选择阶段");
            }
            if (GUILayout.Button("执行阶段", buttonStyle, GUILayout.Height(30)))
            {
                gameManager.SetGameState(GameState.Executing);
                AddLog("切换到执行阶段");
            }
            GUILayout.EndHorizontal();
            
            // 时间控制按钮
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("暂停", buttonStyle, GUILayout.Height(30)))
            {
                Time.timeScale = 0f;
                AddLog("游戏已暂停");
            }
            if (GUILayout.Button("1/4速度", buttonStyle, GUILayout.Height(30)))
            {
                Time.timeScale = 0.25f;
                AddLog("游戏速度设为1/4");
            }
            if (GUILayout.Button("1/2速度", buttonStyle, GUILayout.Height(30)))
            {
                Time.timeScale = 0.5f;
                AddLog("游戏速度设为1/2");
            }
            if (GUILayout.Button("正常速度", buttonStyle, GUILayout.Height(30)))
            {
                Time.timeScale = 1f;
                AddLog("游戏速度恢复正常");
            }
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.Label("游戏管理器不可用", sliderLabelStyle);
        }
    }

    /// <summary>
    /// 绘制调试信息标签页
    /// </summary>
    private void DrawDebugInfoTab()
    {
        GUILayout.Label("调试信息", headerStyle);
        GUILayout.Space(10);
        
        // 游戏状态信息
        GUILayout.Label("基本信息", headerStyle);
        GUILayout.Space(5);
        
        if (gameManager != null)
        {
            GUILayout.Label($"当前游戏状态: {gameManager.CurrentState}");
            GUILayout.Label($"时间缩放: {Time.timeScale:F2}");
            GUILayout.Label($"帧率: {(int)(1.0f / Time.deltaTime)} FPS");
        }
        
        if (playerController != null)
        {
            GUILayout.Label($"角色位置: {playerController.transform.position}");
            GUILayout.Label($"当前速度: {playerController.GetCurrentSpeed():F1}");
        }
        
        if (resourceManager != null)
        {
            GUILayout.Label($"精力: {resourceManager.GetResourceValue(ResourceType.Energy)} / {resourceManager.GetResourceMaxValue(ResourceType.Energy)}");
            GUILayout.Label($"氧气: {resourceManager.GetResourceValue(ResourceType.Oxygen)} / {resourceManager.GetResourceMaxValue(ResourceType.Oxygen)}");
            GUILayout.Label($"压力: {resourceManager.GetResourceValue(ResourceType.Pressure)} / {resourceManager.GetResourceMaxValue(ResourceType.Pressure)}");
        }
        
        if (breathManager != null)
        {
            GUILayout.Label($"呼吸状态: {breathManager.GetCurrentState()}");
            GUILayout.Label($"呼吸进度: {breathManager.GetCurrentProgress()} / {breathManager.GetCurrentCycle()}");
        }
        
        // 操作日志
        GUILayout.Space(15);
        GUILayout.Label("操作日志", headerStyle);
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
        AddLog("开发者工具已销毁");
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
                AddLog("找到转向按钮: " + button.gameObject.name);
                
                // 获取当前转向按钮启用状态
                enableTurningAction = turningActionButton.gameObject.activeSelf;
                break;
            }
        }
        
        if (turningActionButton == null)
        {
            AddLog("未找到转向按钮!");
        }
    }
} 