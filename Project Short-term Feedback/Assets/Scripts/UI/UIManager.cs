using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// UI管理器，负责管理游戏中的UI元素
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI元素引用")]
    [SerializeField] private Canvas mainCanvas;
    [SerializeField] private TextMeshProUGUI confidenceStateText;
    [SerializeField] private TextMeshProUGUI actionDifficultyText;
    [SerializeField] private TextMeshProUGUI turnCounterText;
    [SerializeField] private Slider confidenceSlider;
    [SerializeField] private TextMeshProUGUI successRateText;
    [SerializeField] private TextMeshProUGUI doubtLevelText;
    [SerializeField] private TextMeshProUGUI fatigueText;

    private PlayerController playerController;

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

        // 初始化UI
        InitializeUI();
    }

    private void Start()
    {
        // 获取PlayerController引用
        playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("UIManager: 未找到PlayerController！");
        }
        else
        {
            // 设置UI引用
            SetUIReferences();
        }
    }

    /// <summary>
    /// 初始化UI元素
    /// </summary>
    private void InitializeUI()
    {
        // 如果没有主Canvas，创建一个
        if (mainCanvas == null)
        {
            GameObject canvasObj = new GameObject("MainCanvas");
            mainCanvas = canvasObj.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            // 设置Canvas的排序层
            mainCanvas.sortingOrder = 100;
            
            // 保持Canvas在场景切换时不被销毁
            DontDestroyOnLoad(canvasObj);
            
            Debug.Log("UIManager: 已创建主Canvas");
        }

        // 创建UI元素
        CreateUIElements();
    }

    /// <summary>
    /// 创建必要的UI元素
    /// </summary>
    private void CreateUIElements()
    {
        // 创建信心状态文本
        if (confidenceStateText == null)
        {
            confidenceStateText = CreateTextElement("ConfidenceStateText", "State: Stable", new Vector2(10, Screen.height - 30));
            Debug.Log("UIManager: Created confidence state text");
        }

        // 创建动作难度文本
        if (actionDifficultyText == null)
        {
            actionDifficultyText = CreateTextElement("ActionDifficultyText", "Difficulty: ", new Vector2(10, Screen.height - 60));
            Debug.Log("UIManager: Created action difficulty text");
        }

        // 创建回合计数器
        if (turnCounterText == null)
        {
            turnCounterText = CreateTextElement("TurnCounterText", "Turn: 1", new Vector2(10, Screen.height - 90));
            Debug.Log("UIManager: Created turn counter text");
        }

        // 创建成功率文本
        if (successRateText == null)
        {
            successRateText = CreateTextElement("SuccessRateText", "Success Rate: ", new Vector2(10, Screen.height - 120));
            Debug.Log("UIManager: Created success rate text");
        }

        // 创建自我怀疑层数文本
        if (doubtLevelText == null)
        {
            doubtLevelText = CreateTextElement("DoubtLevelText", "Doubt Level: 0", new Vector2(10, Screen.height - 150));
            doubtLevelText.color = Color.green;
            Debug.Log("UIManager: Created doubt level text");
        }

        // 创建疲劳值文本
        if (fatigueText == null)
        {
            fatigueText = CreateTextElement("FatigueText", "Fatigue: 0.0", new Vector2(10, Screen.height - 180));
            Debug.Log("UIManager: Created fatigue text");
            confidenceSlider = CreateSlider("ConfidenceSlider", new Vector2(Screen.width / 2, 20), new Vector2(200, 20));
            confidenceSlider.minValue = 0;
            confidenceSlider.maxValue = 100;
            confidenceSlider.value = 60;
            Debug.Log("UIManager: Created confidence slider");
        }
    }

    /// <summary>
    /// 创建文本元素
    /// </summary>
    private TextMeshProUGUI CreateTextElement(string name, string text, Vector2 position)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform rectTransform = textObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(0, 1);
        rectTransform.pivot = new Vector2(0, 1);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = new Vector2(200, 30);
        
        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 16;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Left;
        
        return tmpText;
    }

    /// <summary>
    /// 创建滑块元素
    /// </summary>
    private Slider CreateSlider(string name, Vector2 position, Vector2 size)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(mainCanvas.transform, false);
        
        RectTransform rectTransform = sliderObj.AddComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0);
        rectTransform.anchorMax = new Vector2(0.5f, 0);
        rectTransform.pivot = new Vector2(0.5f, 0);
        rectTransform.anchoredPosition = position;
        rectTransform.sizeDelta = size;
        
        Slider slider = sliderObj.AddComponent<Slider>();
        
        // 创建背景
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRectTransform = background.AddComponent<RectTransform>();
        bgRectTransform.anchorMin = Vector2.zero;
        bgRectTransform.anchorMax = Vector2.one;
        bgRectTransform.sizeDelta = Vector2.zero;
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        
        // 创建填充区域
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRectTransform = fillArea.AddComponent<RectTransform>();
        fillAreaRectTransform.anchorMin = new Vector2(0, 0.25f);
        fillAreaRectTransform.anchorMax = new Vector2(1, 0.75f);
        fillAreaRectTransform.offsetMin = new Vector2(5, 0);
        fillAreaRectTransform.offsetMax = new Vector2(-5, 0);
        
        // 创建填充
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRectTransform = fill.AddComponent<RectTransform>();
        fillRectTransform.anchorMin = Vector2.zero;
        fillRectTransform.anchorMax = Vector2.one;
        fillRectTransform.sizeDelta = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0, 0.8f, 0.2f, 0.8f);
        
        // 设置滑块引用
        slider.fillRect = fillRectTransform;
        slider.targetGraphic = bgImage;
        slider.direction = Slider.Direction.LeftToRight;
        
        return slider;
    }

    /// <summary>
    /// 设置PlayerController的UI引用
    /// </summary>
    private void SetUIReferences()
    {
        if (playerController != null)
        {
            // 通过反射设置私有字段
            System.Type type = playerController.GetType();
            
            // 设置信心状态文本
            System.Reflection.FieldInfo confidenceStateField = type.GetField("confidenceStateText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (confidenceStateField != null)
            {
                confidenceStateField.SetValue(playerController, confidenceStateText);
                Debug.Log("UIManager: Set confidence state text reference");
            }
            
            // 设置动作难度文本
            System.Reflection.FieldInfo actionDifficultyField = type.GetField("actionDifficultyText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (actionDifficultyField != null)
            {
                actionDifficultyField.SetValue(playerController, actionDifficultyText);
                Debug.Log("UIManager: Set action difficulty text reference");
            }
            
            // 设置回合计数器
            System.Reflection.FieldInfo turnCounterField = type.GetField("turnCounterText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (turnCounterField != null)
            {
                turnCounterField.SetValue(playerController, turnCounterText);
                Debug.Log("UIManager: Set turn counter text reference");
            }
            
            // 设置信心值滑块
            System.Reflection.FieldInfo confidenceSliderField = type.GetField("confidenceSlider", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (confidenceSliderField != null)
            {
                confidenceSliderField.SetValue(playerController, confidenceSlider);
                Debug.Log("UIManager: Set confidence slider reference");
            }
            
            // 设置成功率文本
            System.Reflection.FieldInfo successRateField = type.GetField("successRateText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (successRateField != null)
            {
                successRateField.SetValue(playerController, successRateText);
                Debug.Log("UIManager: Set success rate text reference");
            }

            // 设置自我怀疑层数文本
            System.Reflection.FieldInfo doubtLevelField = type.GetField("doubtLevelText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (doubtLevelField != null)
            {
                doubtLevelField.SetValue(playerController, doubtLevelText);
                Debug.Log("UIManager: Set doubt level text reference");
            }
        }
    }

    public void UpdateConfidenceState(string state)
    {
        if (confidenceStateText != null)
        {
            confidenceStateText.text = "State: " + state;
        }
    }

    public void UpdateActionDifficulty(string difficulty)
    {
        if (actionDifficultyText != null)
        {
            actionDifficultyText.text = "Difficulty: " + difficulty;
        }
    }

    public void UpdateTurnCounter(int turn)
    {
        if (turnCounterText != null)
        {
            turnCounterText.text = "Turn: " + turn;
        }
    }

    public void UpdateConfidenceSlider(int value, int min, int max)
    {
        if (confidenceSlider != null)
        {
            confidenceSlider.minValue = min;
            confidenceSlider.maxValue = max;
            confidenceSlider.value = value;
        }
    }

    public void UpdateSuccessRate(string rate, Color color)
    {
        if (successRateText != null)
        {
            successRateText.text = "Success Rate: " + rate;
            successRateText.color = color;
        }
    }
} 