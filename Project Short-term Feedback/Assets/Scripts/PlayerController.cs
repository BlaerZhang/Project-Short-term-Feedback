using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using DG.Tweening.Plugins;
using TMPro;
using UnityEngine.UI;

// 移动动作类型枚举
public enum MoveActionType
{
    None,   // 未选择
    Run,    // 跑步
    Jump,   // 跳跃
    Turn    // 转向
}

// 信心状态枚举
public enum ConfidenceState
{
    Broken,     // 崩溃 (0-24)
    Unstable,   // 摇摆 (25-49)
    Stable,     // 稳定 (50-74)
    Confident,  // 自信 (75-90)
    Unshakable  // 坚不可摧 (91-100)
}

// 动作难度枚举
public enum ActionDifficulty
{
    VeryEasy,  // 极其简单 (1-20%)
    Easy,      // 简单 (21-40%)
    Medium,    // 中等 (41-60%)
    Hard,      // 困难 (61-80%)
    VeryHard   // 极其困难 (81-100%)
}

// 动作结果枚举
public enum ActionResult
{
    Success,    // 成功
    Failure,    // 失败
    Critical,   // 大成功
    Fumble      // 大失败
}

public class PlayerController : MonoBehaviour
{
    [Header("角色属性")]
    [SerializeField] private float maxSpeed = 5f;      // 角色最大移动速度
    [SerializeField] private float minSpeed = 1f;      // 角色最小移动速度
    [SerializeField] private float currentSpeed = 3f;  // 当前速度
    [SerializeField] private bool allowFreeMovementInSector = false; // 是否允许在扇形范围内自由移动

    [Header("角色碰撞设置")]
    [SerializeField] private float playerCollisionRadius = 0.5f; // 角色碰撞半径
    [SerializeField] private float collisionToleranceDistance = 0.1f; // 碰撞容差距离
    [SerializeField] private LayerMask obstacleLayer; // 障碍物层级
    [SerializeField] private bool drawCollisionGizmos = true; // 是否绘制碰撞调试信息
    [SerializeField] private Material collisionPathMaterial; // 碰撞路径材质（红色）

    [Header("跑步设置")]
    [SerializeField] private float maxTurnAngle = 120f; // 最小速度时可转弯的最大角度
    [SerializeField] private float minTurnAngle = 30f;  // 最大速度时可转弯的最小角度
    [SerializeField] private float baseMovementRadius = 3f; // 基础移动半径
    [SerializeField] private float maxMovementRadius = 6f;  // 最大移动半径
    [SerializeField] private float minMovementRadius = 1f;  // 最小移动半径
    [SerializeField] private float moveTime = 1f;      // 完成一次移动的时间

    [Header("跳跃设置")]
    [SerializeField] private float jumpMaxAngle = 90f; // 最小速度时跳跃的最大角度
    [SerializeField] private float jumpMinAngle = 30f; // 最大速度时跳跃的最小角度
    [SerializeField] private float jumpBaseRadius = 4f; // 基础跳跃半径
    [SerializeField] private float jumpMaxRadius = 8f;  // 最大跳跃半径
    [SerializeField] private float jumpMinRadius = 2f;  // 最小跳跃半径
    [SerializeField] private float jumpTime = 0.8f;     // 完成一次跳跃的时间
    [SerializeField] private AnimationCurve jumpHeightCurve; // 跳跃高度曲线

    [Header("路径显示")]
    [SerializeField] private LineRenderer pathPreview;  // 路径预览线条渲染器
    [SerializeField] private int pathResolution = 20;   // 路径分辨率
    [SerializeField] private GameObject arcIndicator;   // 显示可移动范围的圆弧指示器
    [SerializeField] private Material validPathMaterial;  // 有效路径材质
    [SerializeField] private Material invalidPathMaterial; // 无效路径材质
    [SerializeField] private float pathWidth = 0.1f;    // 路径宽度
    [SerializeField] private float directionLineWidth = 0.05f; // 方向指示线宽度
    [SerializeField] private float pathHeightOffset = 0.6f;   // 路径高度偏移，确保显示在扇形填充之上
    [SerializeField] private GameObject landingMarkerPrefab; // 落点标记预制体

    [Header("动画设置")]
    [SerializeField] private Ease moveEase = Ease.InOutSine;  // 移动缓动函数
    [SerializeField] private Ease rotateEase = Ease.InOutSine; // 旋转缓动函数
    [SerializeField] private float pathUpdateInterval = 0.05f; // 路径更新间隔
    [SerializeField] private Animator characterAnimator; // 角色动画控制器

    [Header("转向设置")]
    [Tooltip("转向方向指示线长度")]
    [SerializeField] private float turnIndicatorLength = 2f;
    [Tooltip("执行转向所需时间(秒)")]
    [SerializeField] private float turnTime = 0.5f;
    [Tooltip("转向方向指示器宽度")]
    [SerializeField] private float turnIndicatorWidth = 0.1f;
    [Tooltip("转向方向指示器材质")]
    [SerializeField] private Material turnIndicatorMaterial;

    [Header("系统设置")]
    [SerializeField] private bool enableConfidenceSystem = true; // 是否启用信心系统
    [SerializeField] private bool enableDoubtSystem = true;      // 是否启用自我怀疑系统

    [Header("信心系统")]
    [SerializeField] private int initialConfidence = 60; // 初始信心值
    [SerializeField] private int maxConfidence = 100;    // 最大信心值
    [SerializeField] private int minConfidence = 0;      // 最小信心值
    [SerializeField] private int currentConfidence;      // 当前信心值
    [Tooltip("信心值区间：91-100")]
    [SerializeField] private int unshakableConfidenceBonus = 5; // 坚不可摧状态的检定加成
    [Tooltip("信心值区间：75-90")]
    [SerializeField] private int confidentBonus = 3;  // 自信状态的检定加成
    [Tooltip("信心值区间：50-74")]
    [SerializeField] private int stableBonus = 0;     // 稳定状态的检定加成
    [Tooltip("信心值区间：25-49")]
    [SerializeField] private int unstableBonus = -1;  // 摇摆状态的检定加成
    [Tooltip("信心值区间：0-24")]
    [SerializeField] private int brokenBonus = -3;    // 崩溃状态的检定加成

    [Header("自我怀疑系统")]
    [SerializeField] private int doubtLevel = 0;      // 当前自我怀疑层数
    [SerializeField] private int maxDoubtLevel = 4;   // 最大自我怀疑层数
    [Tooltip("自我怀疑层数0的信心惩罚")]
    [SerializeField] private int doubtPenalty0 = 0;   // 自我怀疑0层的信心惩罚
    [Tooltip("自我怀疑层数1的信心惩罚")]
    [SerializeField] private int doubtPenalty1 = 3;   // 自我怀疑1层的信心惩罚
    [Tooltip("自我怀疑层数2的信心惩罚")]
    [SerializeField] private int doubtPenalty2 = 7;   // 自我怀疑2层的信心惩罚
    [Tooltip("自我怀疑层数3的信心惩罚")]
    [SerializeField] private int doubtPenalty3 = 10;  // 自我怀疑3层的信心惩罚
    [Tooltip("自我怀疑层数4的信心惩罚")]
    [SerializeField] private int doubtPenalty4 = 15;  // 自我怀疑4层的信心惩罚

    [Header("动作成功率系统")]
    [SerializeField] private bool enableActionCheck = true; // 是否启用动作检定
    [SerializeField] private int diceSize = 20;       // 骰子面数(d20)
    [Tooltip("极其简单(占最大距离1-20%)的动作难度")]
    [SerializeField] private int veryEasyDifficulty = 5;  // 极其简单难度
    [Tooltip("简单(占最大距离21-40%)的动作难度")]
    [SerializeField] private int easyDifficulty = 10;     // 简单难度
    [Tooltip("中等(占最大距离41-60%)的动作难度")]
    [SerializeField] private int mediumDifficulty = 15;   // 中等难度
    [Tooltip("困难(占最大距离61-80%)的动作难度")]
    [SerializeField] private int hardDifficulty = 20;     // 困难难度
    [Tooltip("极其困难(占最大距离81-100%)的动作难度")]
    [SerializeField] private int veryHardDifficulty = 25; // 极其困难难度

    [Tooltip("极其简单难度成功后的信心奖励")]
    [SerializeField] private int veryEasySuccessReward = 1;  // 极其简单成功的信心奖励
    [Tooltip("简单难度成功后的信心奖励")]
    [SerializeField] private int easySuccessReward = 4;      // 简单成功的信心奖励
    [Tooltip("中等难度成功后的信心奖励")]
    [SerializeField] private int mediumSuccessReward = 8;    // 中等成功的信心奖励
    [Tooltip("困难难度成功后的信心奖励")]
    [SerializeField] private int hardSuccessReward = 14;     // 困难成功的信心奖励
    [Tooltip("极其困难难度成功后的信心奖励")]
    [SerializeField] private int veryHardSuccessReward = 20; // 极其困难成功的信心奖励

    [Header("疲劳值系统")]
    [SerializeField] private float baseFatigueCost = 5f; // 起步疲劳值
    [SerializeField] private float fatiguePerMeter = 0.5f; // 每米消耗的疲劳值
    [SerializeField] private TextMeshProUGUI fatigueText; // 疲劳值文本

    [Header("回合系统")]
    [SerializeField] private int currentTurn = 1;     // 当前回合数
    [SerializeField] private bool isTurnActive = true; // 当前回合是否可以行动

    // 状态变量
    private MoveActionType currentAction = MoveActionType.None; // 当前选择的动作
    private Vector3 moveTargetPosition;  // 移动目标位置
    private Vector3 currentDirection;    // 当前朝向
    private bool isMoving = false;       // 是否正在移动
    private bool canMove = true;         // 是否可以移动
    private Camera mainCamera;           // 主相机引用
    private LayerMask groundLayer;       // 地面层级
    private List<Vector3> movementPath = new List<Vector3>(); // 当前移动路径
    private GameManager gameManager;     // 游戏管理器引用
    private GameObject landingMarker;    // 落点标记
    private bool landingPointCollision = false; // 落点是否发生碰撞
    private bool pathCollision = false;  // 路径是否发生碰撞
    private GameState gameState;         // 游戏状态
    private AudioManager audioManager;    // 添加对AudioManager的引用
    private FootAreaGenerator footAreaGenerator; // 添加对FootAreaGenerator的引用
    
    // 回合和信心系统相关变量
    private ConfidenceState confidenceState = ConfidenceState.Stable; // 当前信心状态
    private ActionDifficulty currentDifficulty = ActionDifficulty.Medium; // 当前动作难度
    private bool actionSucceeded = false; // 上一次动作是否成功
    private bool turnSkipped = false; // 是否跳过了回合
    
    // 动作检定相关信息显示UI
    [SerializeField] private TextMeshProUGUI confidenceStateText; // 信心状态文本
    [SerializeField] private TextMeshProUGUI actionDifficultyText; // 动作难度文本
    [SerializeField] private TextMeshProUGUI turnCounterText; // 回合计数器
    [SerializeField] private Slider confidenceSlider; // 信心值滑块
    [SerializeField] private TextMeshProUGUI successRateText; // 成功率文本
    [SerializeField] private TextMeshProUGUI doubtLevelText; // 自我怀疑层数文本

    private void Awake()
    {
        mainCamera = Camera.main;
        groundLayer = LayerMask.GetMask("Ground");
        
        // 如果没有设置障碍物层，默认使用"Obstacle"层
        if (obstacleLayer.value == 0)
        {
            obstacleLayer = LayerMask.GetMask("Obstacle");
        }
        
        // 初始化朝向
        currentDirection = transform.forward;

        // 初始化信心系统
        currentConfidence = initialConfidence;
        UpdateConfidenceState();

        // 创建一个子物体来容纳LineRenderer，使其能够贴在地面上
        GameObject lineRendererObj = new GameObject("PathPreview");
        lineRendererObj.transform.SetParent(transform);
        lineRendererObj.transform.localPosition = Vector3.zero;
        lineRendererObj.transform.localRotation = Quaternion.Euler(90, 0, 0); // 旋转90度使线条贴地

        // 确保有LineRenderer组件
        if (pathPreview == null)
        {
            pathPreview = lineRendererObj.AddComponent<LineRenderer>();
        }
        else
        {
            // 如果已经有LineRenderer，把它移到新的子物体上
            Transform oldParent = pathPreview.transform.parent;
            pathPreview.transform.SetParent(lineRendererObj.transform);
            pathPreview.transform.localPosition = Vector3.up;
            pathPreview.transform.localRotation = Quaternion.identity;
            if (oldParent != transform && oldParent != null)
            {
                Destroy(oldParent.gameObject);
            }
        }
        
        // 设置路径渲染器的初始属性
        pathPreview.startWidth = playerCollisionRadius * 2f; // 使路径宽度等于玩家碰撞直径
        pathPreview.endWidth = playerCollisionRadius * 2f;
        pathPreview.positionCount = 0;
        pathPreview.alignment = LineAlignment.TransformZ; // 使用TransformZ，因为我们已经旋转了物体
        pathPreview.useWorldSpace = true; // 使用世界空间坐标
        pathPreview.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pathPreview.receiveShadows = false;

        // 创建落点标记
        if (landingMarkerPrefab != null)
        {
            landingMarker = Instantiate(landingMarkerPrefab, Vector3.zero, Quaternion.identity);
            landingMarker.SetActive(false);
            
            // 设置落点标记大小与玩家碰撞体一致
            if (landingMarker.transform.childCount > 0)
            {
                landingMarker.transform.GetChild(0).localScale = new Vector3(
                    playerCollisionRadius * 2f,
                    playerCollisionRadius * 2f,
                    1f
                );
            }
            else
            {
                landingMarker.transform.localScale = new Vector3(
                    playerCollisionRadius * 2f,
                    playerCollisionRadius * 2f,
                    1f
                );
            }
        }

        // 获取AudioManager引用
        audioManager = FindObjectOfType<AudioManager>();
        if (audioManager == null)
        {
            Debug.LogWarning("AudioManager未找到，将无法播放音效。请确保场景中有AudioManager对象。");
        }
        else
        {
            Debug.Log("成功找到AudioManager，音效系统已准备就绪。");
        }
    }

    private void Start()
    {
        // 初始化路径预览
        if (pathPreview != null)
        {
            pathPreview.positionCount = 0;
        }

        // 获取GameManager引用
        gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            Debug.LogWarning("PlayerController: 未找到GameManager！部分功能可能无法正常工作。");
        }

        // 获取FootAreaGenerator引用
        footAreaGenerator = FindObjectOfType<FootAreaGenerator>();
        if (footAreaGenerator == null)
        {
            Debug.LogWarning("PlayerController: 未找到FootAreaGenerator！颜色轮换功能将无法工作。");
        }

        // 初始化动画控制器
        if (characterAnimator == null)
        {
            characterAnimator = GetComponent<Animator>();
        }
        
        // 确认障碍物层设置
        if (obstacleLayer.value == 0)
        {
            Debug.LogError("PlayerController: 未正确设置障碍物层 (obstacleLayer)！碰撞检测将无法工作。");
        }
        else
        {
            Debug.Log("PlayerController: 障碍物层设置为: " + LayerMaskToString(obstacleLayer));
        }
        
        // 输出碰撞设置信息
        Debug.LogFormat("PlayerController: 碰撞半径={0}, 容差距离={1}", playerCollisionRadius, collisionToleranceDistance);
    }

    private void Update()
    {
        if (gameManager != null)
        {
            // 在执行阶段更新动作进度
            if (gameManager.CurrentState == GameState.Executing)
            {
                gameManager.UpdateActionProgress();
            }
            
            switch (gameManager.CurrentState)
            {
                case GameState.Planning:
                    // 处理动作选择输入
                    HandleActionSelection();
                    
                    // 处理空格键跳过回合
                    if (Keyboard.current.spaceKey.wasPressedThisFrame && isTurnActive)
                    {
                        SkipTurn();
                    }
                    break;

                case GameState.Targeting:
                    // 处理目标选择阶段的按键输入（直接切换动作类型）
                    HandleTargetingInput();
                    
                    // 处理目标选择
                    if (!isMoving && canMove)
                    {
                        switch (currentAction)
                        {
                            case MoveActionType.Run:
                                HandleRunTargeting();
                                break;

                            case MoveActionType.Jump:
                                HandleJumpTargeting();
                                break;

                            case MoveActionType.Turn:
                                HandleTurnTargeting();
                                break;
                        }
                    }
                    break;

                case GameState.Executing:
                    // 执行阶段，动作执行中
                    break;
            }
        }

        // 测试速度调整
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ChangeSpeed(1);
        }
        else if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            ChangeSpeed(3);
        }
        else if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            ChangeSpeed(5);
        }
        
        // 更新UI
        UpdateUI();
    }

    private void HandleActionSelection()
    {
        // 如果当前回合不可行动，直接返回
        if (!isTurnActive) return;
        
        // Q键选择跑步
        if (Keyboard.current.qKey.wasPressedThisFrame)
        {
            SelectAction(MoveActionType.Run);
        }
        // W键选择跳跃
        else if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            SelectAction(MoveActionType.Jump);
        }
        // E键选择转向
        else if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            SelectAction(MoveActionType.Turn);
        }
    }

    // 处理目标选择阶段的输入
    private void HandleTargetingInput()
    {
        // 在目标选择阶段，允许直接按Q或W切换动作类型
        if (Keyboard.current.qKey.wasPressedThisFrame && currentAction != MoveActionType.Run)
        {
            // 先取消当前目标选择，再选择新的动作
            if (gameManager != null)
            {
                gameManager.CancelTargetingPhase();
                SelectAction(MoveActionType.Run);
            }
        }
        else if (Keyboard.current.wKey.wasPressedThisFrame && currentAction != MoveActionType.Jump)
        {
            // 先取消当前目标选择，再选择新的动作
            if (gameManager != null)
            {
                gameManager.CancelTargetingPhase();
                SelectAction(MoveActionType.Jump);
            }
        }
        else if (Keyboard.current.eKey.wasPressedThisFrame && currentAction != MoveActionType.Turn)
        {
            // 先取消当前目标选择，再选择新的动作
            if (gameManager != null)
            {
                gameManager.CancelTargetingPhase();
                SelectAction(MoveActionType.Turn);
            }
        }
        else if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 取消当前动作
            CancelCurrentAction();
            if (gameManager != null)
            {
                gameManager.SetGameState(GameState.Planning);
            }
        }
    }

    // 选择动作方法
    private void SelectAction(MoveActionType actionType)
    {
        // 设置当前动作
        currentAction = actionType;
        
        // 根据动作类型更新状态
        switch (actionType)
        {
            case MoveActionType.Run:
                Debug.Log("选择跑步动作");
                break;
            case MoveActionType.Jump:
                Debug.Log("选择跳跃动作");
                break;
            case MoveActionType.Turn:
                Debug.Log("选择转向动作");
                // 转向动作立即隐藏弧形指示器
                if (arcIndicator != null)
                {
                    ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
                    if (arc != null)
                    {
                        arc.FadeOut();
                    }
                }
                break;
        }
        
        // 通知游戏管理器进入目标选择阶段
        if (gameManager != null)
        {
            gameManager.StartTargetingPhase();
        }

        // 显示圆弧指示器 (仅当不是转向动作时)
        if (actionType != MoveActionType.Turn)
        {
            UpdateArcIndicator();
            if (arcIndicator != null)
            {
                ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
                if (arc != null)
                {
                    arc.FadeIn();
                }
            }
        }
    }

    private void HandleRunTargeting()
    {
        // 获取鼠标位置
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            Vector3 hitPoint = hit.point;
            // 确保hitPoint与角色在同一高度
            hitPoint.y = transform.position.y;

            // 计算点击位置是否在允许的移动范围内
            bool isValidMoveTarget = IsPointValidForRunning(hitPoint, out Vector3 validPoint);

            // 显示路径预览，这里会更新pathCollision状态
            ShowRunPathPreview(validPoint);

            // 综合考虑落点和路径碰撞状态
            bool isFullyValid = isValidMoveTarget && !pathCollision;

            // 显示落点标记
            UpdateLandingMarker(validPoint, isFullyValid);

            // 计算距离和动作难度
            float distance = Vector3.Distance(transform.position, validPoint);
            float maxDistance = GetCurrentRunRadius();
            ActionDifficulty difficulty = CalculateActionDifficulty(distance, maxDistance);
            currentDifficulty = difficulty;
            
            // 更新成功率显示
            UpdateSuccessRateDisplay(difficulty);

            // 如果鼠标左键点击且位置有效，开始移动
            if (Mouse.current.leftButton.wasPressedThisFrame && isFullyValid)
            {
                StartRunning(validPoint);
            }
        }
        else
        {
            // 没有命中地面，隐藏路径和标记
            HidePathPreview();
            HideLandingMarker();
            // 隐藏动作UI
            HideActionUI();
        }
    }

    private void HandleJumpTargeting()
    {
        // 获取鼠标位置
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            Vector3 hitPoint = hit.point;
            // 确保hitPoint与角色在同一高度
            hitPoint.y = transform.position.y;

            // 计算点击位置是否在允许的跳跃范围内
            bool isValidJumpTarget = IsPointValidForJumping(hitPoint, out Vector3 validPoint);

            // 不显示路径，只显示方向指示线，这里会更新pathCollision状态
            ShowJumpDirectionPreview(validPoint);

            // 综合考虑落点和路径碰撞状态
            bool isFullyValid = isValidJumpTarget && !pathCollision;

            // 显示落点标记
            UpdateLandingMarker(validPoint, isFullyValid);

            // 计算距离和动作难度
            float distance = Vector3.Distance(transform.position, validPoint);
            float maxDistance = GetCurrentJumpRadius();
            ActionDifficulty difficulty = CalculateActionDifficulty(distance, maxDistance);
            currentDifficulty = difficulty;
            
            // 更新成功率显示
            UpdateSuccessRateDisplay(difficulty);

            // 如果鼠标左键点击且位置有效，开始跳跃
            if (Mouse.current.leftButton.wasPressedThisFrame && isFullyValid)
            {
                StartJumping(validPoint);
            }
        }
        else
        {
            // 没有命中地面，隐藏路径和标记
            HidePathPreview();
            HideLandingMarker();
            // 隐藏动作UI
            HideActionUI();
        }
    }

    // 检查点是否在允许的跑步范围内
    private bool IsPointValidForRunning(Vector3 point, out Vector3 validPoint)
    {
        // 计算到目标点的向量
        Vector3 toTarget = point - transform.position;
        toTarget.y = 0; // 确保在水平面上计算

        // 目标点距离
        float distance = toTarget.magnitude;

        // 如果目标点太近，视为无效
        if (distance < 0.5f)
        {
            validPoint = point;
            return false;
        }

        // 计算目标点方向与当前朝向的夹角
        float angle = Vector3.SignedAngle(currentDirection, toTarget.normalized, Vector3.up);

        // 根据当前速度计算允许的转向角度
        float allowedAngle = Mathf.Lerp(maxTurnAngle, minTurnAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed)) * 0.5f;
        
        // 获取当前移动半径
        float currentRadius = GetCurrentRunRadius();

        // 检查角度和距离是否在允许范围内
        bool withinAngle = Mathf.Abs(angle) <= allowedAngle;
        bool withinDistance = distance >= minMovementRadius && distance <= currentRadius;

        if (allowFreeMovementInSector)
        {
            Vector3 adjustedPoint = point;
            bool needsAdjustment = false;

            // 调整角度
            if (!withinAngle)
            {
                needsAdjustment = true;
                // 限制角度到允许范围
                float clampedAngle = Mathf.Clamp(angle, -allowedAngle, allowedAngle);
                // 计算新的方向
                Quaternion rotation = Quaternion.AngleAxis(clampedAngle, Vector3.up);
                Vector3 newDirection = rotation * currentDirection;
                // 保持原始距离
                adjustedPoint = transform.position + newDirection * distance;
            }

            // 调整距离
            if (!withinDistance)
            {
                needsAdjustment = true;
                Vector3 direction = (adjustedPoint - transform.position).normalized;
                float clampedDistance = Mathf.Clamp(distance, minMovementRadius, currentRadius);
                adjustedPoint = transform.position + direction * clampedDistance;
            }

            // 使用调整后的点
            validPoint = adjustedPoint;
        }
        else
        {
            // 原始行为：只能移动到圆弧上
            if (!withinAngle)
            {
                // 如果角度超出范围，限制到最大允许角度
                angle = Mathf.Clamp(angle, -allowedAngle, allowedAngle);
            }
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
        validPoint = transform.position + (rotation * currentDirection) * currentRadius;
        }

        // 检查最终确定的落点是否与障碍物碰撞
        landingPointCollision = CheckPointCollision(validPoint);

        // 只有当落点不碰撞时，才返回true
        return !landingPointCollision;
    }

    // 检查点是否在允许的跳跃范围内
    private bool IsPointValidForJumping(Vector3 point, out Vector3 validPoint)
    {
        // 计算到目标点的向量
        Vector3 toTarget = point - transform.position;
        toTarget.y = 0; // 确保在水平面上计算

        // 目标点距离
        float distance = toTarget.magnitude;

        // 如果目标点太近，视为无效
        if (distance < 0.5f)
        {
            validPoint = point;
            return false;
        }

        // 计算目标点方向与当前朝向的夹角
        float angle = Vector3.SignedAngle(currentDirection, toTarget.normalized, Vector3.up);

        // 根据当前速度计算允许的跳跃角度
        float allowedAngle = Mathf.Lerp(jumpMaxAngle, jumpMinAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed)) * 0.5f;
        
        // 获取当前跳跃半径
        float currentRadius = GetCurrentJumpRadius();

        // 检查角度和距离是否在允许范围内
        bool withinAngle = Mathf.Abs(angle) <= allowedAngle;
        bool withinDistance = distance >= jumpMinRadius && distance <= currentRadius;

        if (allowFreeMovementInSector)
        {
            Vector3 adjustedPoint = point;
            bool needsAdjustment = false;

            // 调整角度
            if (!withinAngle)
            {
                needsAdjustment = true;
                // 限制角度到允许范围
                float clampedAngle = Mathf.Clamp(angle, -allowedAngle, allowedAngle);
                // 计算新的方向
                Quaternion rotation = Quaternion.AngleAxis(clampedAngle, Vector3.up);
                Vector3 newDirection = rotation * currentDirection;
                // 保持原始距离
                adjustedPoint = transform.position + newDirection * distance;
            }

            // 调整距离
            if (!withinDistance)
            {
                needsAdjustment = true;
                Vector3 direction = (adjustedPoint - transform.position).normalized;
                float clampedDistance = Mathf.Clamp(distance, jumpMinRadius, currentRadius);
                adjustedPoint = transform.position + direction * clampedDistance;
            }

            // 使用调整后的点
            validPoint = adjustedPoint;
        }
        else
        {
            // 原始行为：只能移动到圆弧上
            if (!withinAngle)
            {
                // 如果角度超出范围，限制到最大允许角度
                angle = Mathf.Clamp(angle, -allowedAngle, allowedAngle);
            }
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
        validPoint = transform.position + (rotation * currentDirection) * currentRadius;
        }

        // 检查落点是否与障碍物碰撞
        landingPointCollision = CheckPointCollision(validPoint);

        // 只有当落点不碰撞时，才返回true
        return !landingPointCollision;
    }

    // 显示跑步路径预览
    private void ShowRunPathPreview(Vector3 targetPoint)
    {
        // 计算路径点
        CalculateRunPath(targetPoint);

        // 检查路径是否与障碍物碰撞
        pathCollision = CheckPathCollision(movementPath);

        // 设置路径宽度 - 确保与角色碰撞体一致
        pathPreview.startWidth = playerCollisionRadius * 2f;
        pathPreview.endWidth = playerCollisionRadius * 2f;

        // 设置路径预览线条渲染器的点
        pathPreview.positionCount = movementPath.Count;
        for (int i = 0; i < movementPath.Count; i++)
        {
            // 为路径添加高度偏移，确保显示在地面之上
            Vector3 pathPoint = movementPath[i];
            pathPoint.y += pathHeightOffset;
            pathPreview.SetPosition(i, pathPoint);
        }

        // 设置路径材质（根据落点碰撞和路径碰撞决定）
        UpdatePathMaterial();
    }

    // 显示跳跃方向预览
    private void ShowJumpDirectionPreview(Vector3 targetPoint)
    {
        // 计算起点和终点
        Vector3 startPos = transform.position;
        startPos.y += pathHeightOffset;
        Vector3 endPos = targetPoint;
        endPos.y += pathHeightOffset;

        // 设置路径宽度 - 确保与角色碰撞体一致
        pathPreview.startWidth = playerCollisionRadius * 2f;
        pathPreview.endWidth = playerCollisionRadius * 2f;

        // 设置路径预览线条渲染器的点（只有两个点的直线）
        pathPreview.positionCount = 2;
        pathPreview.SetPosition(0, startPos);
        pathPreview.SetPosition(1, endPos);
        
        // 检查路径是否与障碍物碰撞
        List<Vector3> jumpPath = new List<Vector3> { transform.position, targetPoint };
        pathCollision = CheckPathCollision(jumpPath);

        // 设置路径材质（根据落点碰撞和路径碰撞决定）
        UpdatePathMaterial();
    }

    // 更新路径材质
    private void UpdatePathMaterial()
    {
        // 根据路径有效性设置材质和颜色
        if (landingPointCollision || pathCollision)
        {
            pathPreview.material = collisionPathMaterial; // 路径或落点碰撞使用碰撞材质
        }
        else
        {
            pathPreview.material = validPathMaterial; // 正常使用有效路径材质
        }
    }

    // 隐藏路径预览
    private void HidePathPreview()
    {
        pathPreview.positionCount = 0;
    }
    
    // 更新落点标记
    private void UpdateLandingMarker(Vector3 position, bool isValid)
    {
        if (landingMarker != null)
        {
            landingMarker.SetActive(true);
            
            // 设置位置
            position.y += 0.05f; // 稍微抬高以避免z-fighting
            landingMarker.transform.position = position;
            
            // 获取LandingMarker组件
            LandingMarker marker = landingMarker.GetComponent<LandingMarker>();
            if (marker != null)
            {
                // 检查是否有碰撞
                bool isValidWithoutCollision = isValid && !landingPointCollision && !pathCollision;
                marker.SetValid(isValidWithoutCollision);
                
                // 计算朝向方向（从玩家到目标位置）
                Vector3 direction = position - transform.position;
                direction.y = 0; // 确保在水平面上
                
                // 设置箭头指向方向
                if (direction.magnitude > 0.1f)
                {
                    marker.SetDirection(direction.normalized);
                }
            }
        }
    }
    
    // 隐藏落点标记
    private void HideLandingMarker()
    {
        if (landingMarker != null)
        {
            landingMarker.SetActive(false);
        }
    }

    // 计算跑步路径（使用圆弧或直线）
    private void CalculateRunPath(Vector3 targetPoint)
    {
        movementPath.Clear();

        Vector3 startPos = transform.position;
        Vector3 toTarget = targetPoint - startPos;
        toTarget.y = 0;
        
        if (allowFreeMovementInSector)
        {
            // 当启用扇形内自由移动时，使用直线路径
            // 只需要起点和终点
            movementPath.Add(startPos);
            movementPath.Add(targetPoint);
        }
        else
        {
            // 原始行为：使用圆弧路径
        // 计算当前朝向与目标方向的夹角
        float angle = Vector3.SignedAngle(currentDirection, toTarget.normalized, Vector3.up);
        
        // 根据当前速度计算允许的转向角度
        float allowedAngle = Mathf.Lerp(maxTurnAngle, minTurnAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed)) * 0.5f;
        
        // 限制角度在允许范围内
        angle = Mathf.Clamp(angle, -allowedAngle, allowedAngle);
        
        // 获取当前移动半径
        float radius = GetCurrentRunRadius();
        
        // 计算路径点
        int segments = pathResolution;
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float currentAngle = angle * t;
            
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
            Vector3 direction = rotation * currentDirection;
            
            Vector3 position = startPos + direction * radius * t;
            movementPath.Add(position);
            }
        }
    }

    // 开始跑步
    private void StartRunning(Vector3 targetPoint)
    {
        // 隐藏路径预览和落点标记
        HidePathPreview();
        HideLandingMarker();
        // 隐藏动作UI
        HideActionUI();

        // 计算路径
        CalculateRunPath(targetPoint);

        // 路径中是否有足够的点
        if (movementPath.Count < 2)
        {
            Debug.LogWarning("路径点不足，无法开始移动");
            return;
        }

        // 计算总路径长度和预期移动时间
        float totalPathLength = 0f;
        for (int i = 0; i < movementPath.Count - 1; i++)
        {
            totalPathLength += Vector3.Distance(movementPath[i], movementPath[i + 1]);
        }
        
        // 计算预期移动时间 (根据速度和路径长度)
        float expectedMoveDuration = totalPathLength / currentSpeed;
        
        // 通知GameManager设置预期动作时间
        if (gameManager != null)
        {
            gameManager.SetExpectedActionDuration(expectedMoveDuration);
        }

        // 更新动画器速度
        if (characterAnimator != null)
        {
            characterAnimator.SetFloat("Speed", currentSpeed);
            characterAnimator.SetBool("IsMoving", true);
        }

        // 改变游戏状态为执行中
        if (gameManager != null)
        {
            gameManager.StartExecutionPhase();
        }

        // 如果存在音效管理器，播放脚步声
        if (audioManager != null)
        {
            audioManager.PlayFootsteps();
            Debug.Log("PlayerController: 尝试播放脚步声");
        }
        else
        {
            Debug.LogWarning("PlayerController: 无法播放脚步声，audioManager为null");
        }

        // 执行动作检定
        ActionResult result = PerformActionCheck(currentDifficulty);
        
        // 标记回合已使用
        isTurnActive = false;
        
        // 根据检定结果执行不同的移动
        switch (result)
        {
            case ActionResult.Success:
            case ActionResult.Critical:
                // 动作成功，正常移动
                actionSucceeded = true;
                Debug.Log("跑步动作检定成功！");
                
                // 重置自我怀疑层数
                ResetDoubtLevel();
                
                // 增加信心值
                int reward = GetSuccessReward(currentDifficulty);
                if (result == ActionResult.Critical)
                {
                    reward = reward * 2; // 大成功，双倍奖励
                    Debug.Log("大成功！获得双倍信心奖励");
                }
                ChangeConfidence(reward);

        // 启动移动协程
                StartCoroutine(RunningCoroutine(movementPath, true, 1.0f));
                break;
                
            case ActionResult.Failure:
                // 动作失败，移动到部分路径
                actionSucceeded = false;
                Debug.Log("跑步动作检定失败！将只移动部分距离");
                
                // 增加自我怀疑层数
                IncreaseDoubtLevel();
                
                // 应用自我怀疑惩罚
                ApplyDoubtPenalty();
                
                // 计算失败后的随机停止点（完成20%-70%的路径）
                float failureProgress = Random.Range(0.2f, 0.7f);
                
                // 启动移动协程（部分移动）
                StartCoroutine(RunningCoroutine(movementPath, false, failureProgress));
                break;
                
            case ActionResult.Fumble:
                // 大失败，几乎不移动或摔倒
                actionSucceeded = false;
                Debug.Log("跑步动作大失败！几乎不移动");
                
                // 增加自我怀疑层数（大失败增加2层）
                doubtLevel = Mathf.Min(doubtLevel + 2, maxDoubtLevel);
                
                // 应用自我怀疑惩罚
                ApplyDoubtPenalty();
                
                // 计算大失败后的停止点（最多完成10%的路径）
                float fumbleProgress = Random.Range(0.01f, 0.1f);
                
                // 启动移动协程（几乎不移动）
                StartCoroutine(RunningCoroutine(movementPath, false, fumbleProgress));
                break;
        }
    }

    // 使用协程执行跑步移动
    private IEnumerator RunningCoroutine(List<Vector3> path, bool fullMovement, float progress)
    {
        if (path.Count < 2)
            yield break;

        isMoving = true;
        canMove = false;

        // 处理部分移动的情况
        List<Vector3> actualPath = new List<Vector3>(path);
        Vector3 finalPosition;
        
        if (!fullMovement && progress < 1.0f)
        {
            // 计算实际要移动的路径点
            int totalPoints = path.Count;
            
            // 使用progress来确定停止点
            float stopT = progress; // 0.2-0.7之间的随机值
            int targetIndex = Mathf.Max(1, Mathf.FloorToInt(totalPoints * stopT));
            
            if (targetIndex < totalPoints)
            {
                // 计算精确的停止点，而不是直接使用路径点
                float exactT = stopT * (totalPoints - 1);
                int prevIndex = Mathf.FloorToInt(exactT);
                int nextIndex = Mathf.Min(prevIndex + 1, totalPoints - 1);
                float lerpT = exactT - prevIndex;
                
                // 在两个路径点之间进行插值
                Vector3 prevPoint = path[prevIndex];
                Vector3 nextPoint = path[nextIndex];
                finalPosition = Vector3.Lerp(prevPoint, nextPoint, lerpT);
                
                // 创建到停止点的新路径
                actualPath = new List<Vector3>();
                for (int i = 0; i <= prevIndex; i++)
                {
                    actualPath.Add(path[i]);
                }
                actualPath.Add(finalPosition);
                
                Debug.LogFormat("动作失败，在路径{0:P0}处停止，最终路径点数：{1}", 
                    stopT, actualPath.Count);
            }
            else
            {
                finalPosition = path[path.Count - 1];
            }
        }
        else
        {
            finalPosition = path[path.Count - 1];
        }

        // 获取初始和目标朝向
        Vector3 finalDirection = (finalPosition - transform.position).normalized;
        finalDirection.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(finalDirection);

        // 计算移动时间（如果是直线，可能需要调整时间）
        float pathMoveDuration = moveTime;
        
        // 如果是部分移动，根据进度缩短时间
        if (!fullMovement)
        {
            pathMoveDuration *= progress;
        }
        
        if (allowFreeMovementInSector && actualPath.Count == 2)
        {
            // 对于直线移动，根据距离和速度计算时间
            float distance = Vector3.Distance(actualPath[0], actualPath[1]);
            pathMoveDuration = distance / currentSpeed;
        }

        // 使用DOTween创建移动
        Transform t = transform; // 缓存transform引用提高性能
        Sequence moveSequence = DOTween.Sequence();

        // 记录起始时间和路径总时长
        float startTime = Time.time;
        float totalTime = pathMoveDuration;

        // 添加路径移动
        moveSequence.Append(t.DOPath(actualPath.ToArray(), pathMoveDuration, PathType.Linear)
            .SetEase(moveEase)
            .OnUpdate(() => {
                // 计算动作进度基于已过时长，这避免了路径计算问题
                if (gameManager != null)
                {
                    float elapsedTime = Time.time - startTime;
                    float currentProgress = elapsedTime / totalTime;
                    
                    // 通知GameManager更新进度
                    gameManager.UpdateActionProgressByDistance(currentProgress);
                }
            }));

        // 同时进行朝向旋转
        t.DORotateQuaternion(targetRotation, pathMoveDuration * 0.5f) // 旋转时间为移动时间的一半，使转向更快
            .SetEase(rotateEase);

        // 等待移动完成
        yield return moveSequence.WaitForCompletion();

        // 确保精确位置
        t.position = finalPosition;
        t.rotation = targetRotation;
            
        // 更新当前朝向
        currentDirection = finalDirection;
            
        // 结束动画
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("IsMoving", false);
        }

        // 重置状态
        isMoving = false;
        canMove = true;
        currentAction = MoveActionType.None;

        // 通知GameManager回到规划阶段
        if (gameManager != null)
        {
            gameManager.EndExecutionPhase();
        }

        // 停止脚步声
        if (audioManager != null)
        {
            audioManager.StopFootsteps();
            Debug.Log("PlayerController: 尝试停止脚步声");
        }
        
        // 结束回合
        EndTurn();
    }

    // 开始跳跃
    private void StartJumping(Vector3 targetPoint)
    {
        // 隐藏路径预览和落点标记
        HidePathPreview();
        HideLandingMarker();
        // 隐藏动作UI
        HideActionUI();

        // 计算跳跃轨迹
        List<Vector3> jumpPath = CalculateJumpPath(targetPoint);

        // 计算预期跳跃时间
        float expectedJumpDuration = jumpTime;
        
        // 通知GameManager设置预期动作时间
        if (gameManager != null)
        {
            gameManager.SetExpectedActionDuration(expectedJumpDuration);
        }

        // 更新动画器设置 - 使用Trigger而非Bool
        if (characterAnimator != null)
        {
            characterAnimator.SetTrigger("Jump");
        }

        // 改变游戏状态为执行中
        if (gameManager != null)
        {
            gameManager.StartExecutionPhase();
        }

        // 如果存在音效管理器，播放跳跃音效
        if (audioManager != null)
        {
            audioManager.PlayJump();
            Debug.Log("PlayerController: 尝试播放跳跃音效");
        }
        else
        {
            Debug.LogWarning("PlayerController: 无法播放跳跃音效，audioManager为null");
        }
        
        // 执行动作检定
        ActionResult result = PerformActionCheck(currentDifficulty);
        
        // 标记回合已使用
        isTurnActive = false;
        
        // 根据检定结果执行不同的跳跃
        switch (result)
        {
            case ActionResult.Success:
            case ActionResult.Critical:
                // 动作成功，正常跳跃
                actionSucceeded = true;
                Debug.Log("跳跃动作检定成功！");
                
                // 重置自我怀疑层数
                ResetDoubtLevel();
                
                // 增加信心值
                int reward = GetSuccessReward(currentDifficulty);
                if (result == ActionResult.Critical)
                {
                    reward = reward * 2; // 大成功，双倍奖励
                    Debug.Log("大成功！获得双倍信心奖励");
                }
                ChangeConfidence(reward);

        // 启动跳跃协程
                StartCoroutine(JumpingCoroutine(targetPoint, jumpPath, true, 1.0f));
                break;
                
            case ActionResult.Failure:
                // 动作失败，跳跃到部分距离
                actionSucceeded = false;
                Debug.Log("跳跃动作检定失败！将只跳跃部分距离");
                
                // 增加自我怀疑层数
                IncreaseDoubtLevel();
                
                // 应用自我怀疑惩罚
                ApplyDoubtPenalty();
                
                // 计算失败后的随机停止点（完成20%-70%的路径）
                float failureProgress = Random.Range(0.2f, 0.7f);
                
                // 启动跳跃协程（部分移动）
                StartCoroutine(JumpingCoroutine(targetPoint, jumpPath, false, failureProgress));
                break;
                
            case ActionResult.Fumble:
                // 大失败，几乎不跳跃或摔倒
                actionSucceeded = false;
                Debug.Log("跳跃动作大失败！几乎不移动");
                
                // 增加自我怀疑层数（大失败增加2层）
                doubtLevel = Mathf.Min(doubtLevel + 2, maxDoubtLevel);
                
                // 应用自我怀疑惩罚
                ApplyDoubtPenalty();
                
                // 计算大失败后的停止点（最多完成10%的路径）
                float fumbleProgress = Random.Range(0.01f, 0.1f);
                
                // 启动跳跃协程（几乎不移动）
                StartCoroutine(JumpingCoroutine(targetPoint, jumpPath, false, fumbleProgress));
                break;
        }
    }

    // 使用协程执行跳跃
    private IEnumerator JumpingCoroutine(Vector3 targetPoint, List<Vector3> jumpPath, bool fullMovement, float progress)
    {
        if (jumpPath.Count < 2)
            yield break;

        isMoving = true;
        canMove = false;

        // 处理部分移动的情况
        List<Vector3> actualPath = new List<Vector3>();
        Vector3 finalPosition;
        
        if (!fullMovement && progress < 1.0f)
        {
            // 使用progress作为停止点的位置（0.2-0.7之间的随机值）
            float stopT = progress;
            int totalPoints = jumpPath.Count;
            
            // 计算精确的停止点
            float exactT = stopT * (totalPoints - 1);
            int prevIndex = Mathf.FloorToInt(exactT);
            int nextIndex = Mathf.Min(prevIndex + 1, totalPoints - 1);
            float lerpT = exactT - prevIndex;
            
            // 在两个路径点之间进行插值
            Vector3 prevPoint = jumpPath[prevIndex];
            Vector3 nextPoint = jumpPath[nextIndex];
            finalPosition = Vector3.Lerp(prevPoint, nextPoint, lerpT);
            
            // 创建到停止点的新路径
            for (int i = 0; i <= prevIndex; i++)
            {
                actualPath.Add(jumpPath[i]);
            }
            actualPath.Add(finalPosition);
            
            Debug.LogFormat("跳跃失败，在路径{0:P0}处停止，最终路径点数：{1}", 
                stopT, actualPath.Count);
        }
        else
        {
            actualPath = jumpPath;
            finalPosition = targetPoint;
        }

        // 计算跳跃开始和结束的朝向
        Vector3 jumpDirection = (finalPosition - transform.position).normalized;
        jumpDirection.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(jumpDirection);

        // 计算跳跃时间（根据进度调整）
        float jumpDuration = jumpTime;
        if (!fullMovement)
        {
            jumpDuration *= progress;
        }

        // 记录开始时间和总时长
        float startTime = Time.time;
        float totalTime = jumpDuration;

        // 使用DOTween创建跳跃序列
        Transform t = transform; // 缓存transform引用提高性能
        Sequence jumpSequence = DOTween.Sequence();

        // 先旋转到跳跃方向
        jumpSequence.Append(t.DORotateQuaternion(targetRotation, jumpDuration * 0.2f));

        // 然后沿路径移动
        jumpSequence.Append(t.DOPath(actualPath.ToArray(), jumpDuration * 0.8f, PathType.CatmullRom)
            .SetEase(Ease.OutQuad)
            .OnUpdate(() => {
                // 计算基于时间的进度
                if (gameManager != null)
                {
                    float elapsedTime = Time.time - startTime;
                    float currentProgress = elapsedTime / totalTime;
                    
                    // 通知GameManager更新进度
                    gameManager.UpdateActionProgressByDistance(currentProgress);
                }
            }));

        // 等待跳跃完成
        yield return jumpSequence.WaitForCompletion();

        // 确保精确位置
        t.position = finalPosition;
        t.rotation = targetRotation;
            
        // 更新当前朝向
        currentDirection = jumpDirection;
            
        // 重置状态
        isMoving = false;
        canMove = true;
        currentAction = MoveActionType.None;

        // 通知GameManager回到规划阶段
        if (gameManager != null)
        {
            gameManager.EndExecutionPhase();
        }
        
        // 结束回合
        EndTurn();
    }

    // 更新可移动范围的圆弧指示器
    private void UpdateArcIndicator()
    {
        if (arcIndicator != null)
        {
            // 如果是转向动作，直接隐藏弧形指示器并返回
            if (currentAction == MoveActionType.Turn)
            {
                ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
                if (arc != null)
                {
                    arc.FadeOut();
                }
                return;
            }

            float allowedAngle, currentRadius;

            // 根据当前动作类型和速度计算角度和半径
            if (currentAction == MoveActionType.Run)
            {
                // 根据当前速度计算允许的转向角度
                allowedAngle = Mathf.Lerp(maxTurnAngle, minTurnAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed));
                
                // 获取当前移动半径
                currentRadius = GetCurrentRunRadius();
                
                // 显示圆弧指示器
                ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
                if (arc != null)
                {
                    arc.UpdateArc(transform.position, currentDirection, currentRadius, allowedAngle);
                    arc.FadeIn();
                }
            }
            else if (currentAction == MoveActionType.Jump)
            {
                // 根据当前速度计算允许的跳跃角度
                allowedAngle = Mathf.Lerp(jumpMaxAngle, jumpMinAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed));
                
                // 获取当前跳跃半径
                currentRadius = GetCurrentJumpRadius();
                
                // 显示圆弧指示器
                ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
                if (arc != null)
                {
                    arc.UpdateArc(transform.position, currentDirection, currentRadius, allowedAngle);
                    arc.FadeIn();
                }
            }
            else
            {
                // 默认情况下隐藏圆弧指示器
                ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
                if (arc != null)
                {
                    arc.FadeOut();
                }
            }
        }
    }

    // 改变角色速度
    public void ChangeSpeed(float newSpeed)
    {
        currentSpeed = Mathf.Clamp(newSpeed, minSpeed, maxSpeed);
        
        // 如果处于目标选择阶段，更新圆弧指示器
        if (gameManager != null && gameManager.CurrentState == GameState.Targeting)
        {
            UpdateArcIndicator();
        }
    }

    // 获取当前跑步半径
    private float GetCurrentRunRadius()
    {
        // 根据速度计算移动半径
        return Mathf.Lerp(minMovementRadius, maxMovementRadius, (currentSpeed - minSpeed) / (maxSpeed - minSpeed));
    }

    // 获取当前跳跃半径
    private float GetCurrentJumpRadius()
    {
        // 根据速度计算跳跃半径
        return Mathf.Lerp(jumpMinRadius, jumpMaxRadius, (currentSpeed - minSpeed) / (maxSpeed - minSpeed));
    }

    // 接收游戏状态变化通知
    public void OnGameStateChanged(GameState newState)
    {
        gameState = newState;

        switch (newState)
        {
            case GameState.Planning:
                // 在计划阶段，隐藏移动范围指示器
                if (arcIndicator != null)
                {
                    ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
                    if (arc != null)
                    {
                        arc.FadeOut();
                    }
                }
                // 隐藏路径预览和落点标记
                HidePathPreview();
                HideLandingMarker();
                // 重置当前动作
                currentAction = MoveActionType.None;
                break;

            case GameState.Targeting:
                // 如果当前动作是转向，确保不显示弧形指示器
                if (currentAction == MoveActionType.Turn)
                {
                    if (arcIndicator != null)
                    {
                        ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
                        if (arc != null)
                        {
                            arc.FadeOut();
                        }
                    }
                }
                else
                {
                    // 对于其他动作，正常显示移动范围指示器
                    UpdateArcIndicator();
                }
                
                // 根据当前选择的动作类型显示相应的UI
                switch (currentAction)
                {
                    case MoveActionType.Run:
                        // 显示跑步范围提示
                        break;
                    case MoveActionType.Jump:
                        // 显示跳跃范围提示
                        break;
                    case MoveActionType.Turn:
                        // 转向不需要显示范围提示
                        break;
                }
                break;

            case GameState.Executing:
                // 在执行阶段，隐藏移动范围指示器
                if (arcIndicator != null)
                {
                    ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
                    if (arc != null)
                    {
                        arc.FadeOut();
                    }
                }
                // 隐藏路径预览和落点标记
                HidePathPreview();
                HideLandingMarker();
                break;

            case GameState.Paused:
                // 游戏暂停时的处理
                break;
        }
    }

    private void OnDestroy()
    {
        // 清理DOTween动画
        DOTween.Kill(transform);
        
        // 清理落点标记
        if (landingMarker != null)
        {
            Destroy(landingMarker);
        }
    }

    // 检查点是否与障碍物碰撞
    private bool CheckPointCollision(Vector3 point)
    {
        // 使用OverlapSphere检测点周围是否有障碍物
        Collider[] colliders = Physics.OverlapSphere(point, playerCollisionRadius, obstacleLayer);
        
        if (colliders.Length > 0)
        {
            // 计算碰撞体积或穿透深度
            foreach (Collider collider in colliders)
            {
                // 计算玩家位置到碰撞体最近点的距离
                Vector3 closestPoint = collider.ClosestPoint(point);
                float distance = Vector3.Distance(point, closestPoint);
                
                // 如果距离小于碰撞半径减去容差距离，则视为碰撞
                if (distance < playerCollisionRadius - collisionToleranceDistance)
                {
                    return true;
                }
            }
        }
        
        return false;
    }

    // 检查碰撞器是否具有不可跃过属性
    private bool IsNotJumpable(Collider collider)
    {
        // 尝试获取MonoBehaviour组件
        MonoBehaviour[] components = collider.GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in components)
        {
            // 通过反射检查组件是否有CannotJumpOver属性
            System.Type type = component.GetType();
            System.Reflection.PropertyInfo propInfo = type.GetProperty("CannotJumpOver");
            
            if (propInfo != null)
            {
                // 如果存在该属性，获取其值
                try
                {
                    return (bool)propInfo.GetValue(component, null);
                }
                catch
                {
                    // 属性访问出错，忽略
                }
            }
        }
        
        // 默认返回false，表示可以跳过
        return false;
    }

    // 检查路径是否与障碍物碰撞
    private bool CheckPathCollision(List<Vector3> path)
    {
        return CheckPathCollision(path, currentAction);
    }

    // 重载方法：检查路径是否与障碍物碰撞，考虑动作类型
    private bool CheckPathCollision(List<Vector3> path, MoveActionType actionType)
    {
        // 路径太短，无需检查
        if (path.Count < 2)
        {
            return false;
        }
        
        // 分段检查路径碰撞
        for (int i = 0; i < path.Count - 1; i += Mathf.Max(1, path.Count / 10)) // 减少检查点以提高性能
        {
            Vector3 start = path[i];
            // 确保我们不超出数组范围
            int endIndex = Mathf.Min(i + Mathf.Max(1, path.Count / 10), path.Count - 1);
            Vector3 end = path[endIndex];
            Vector3 direction = (end - start).normalized;
            float distance = Vector3.Distance(start, end);
            
            // 使用OverlapSphere检测路径点是否与障碍物碰撞（不依赖于物理更新）
            Collider[] colliders = Physics.OverlapSphere(start, playerCollisionRadius - collisionToleranceDistance, obstacleLayer);
            
            if (colliders.Length > 0)
            {
                // 判断是否需要考虑障碍物的可跳跃属性
                if (actionType == MoveActionType.Jump)
                {
                    // 对于跳跃，只有"不可跃过"的障碍物才算碰撞
                    foreach (Collider collider in colliders)
                    {
                        // 使用辅助方法检查障碍物属性
                        if (IsNotJumpable(collider))
                        {
                            // Debug路径碰撞
                            Debug.LogFormat("跳跃路径点 {0} 与不可跃过障碍物碰撞: {1}", i, collider.name);
                            return true;
                        }
                    }
                }
                else
                {
                    // 对于其他动作类型（如跑步），任何障碍物都算碰撞
                    Debug.LogFormat("路径点 {0} 与障碍物碰撞: {1}", i, colliders[0].name);
                    return true;
                }
            }
            
            // 为起点和终点之间的路径进行离散采样检测
            int steps = 5; // 每段路径的采样点数
            for (int step = 1; step < steps; step++)
            {
                float t = step / (float)steps;
                Vector3 samplePoint = Vector3.Lerp(start, end, t);
                
                colliders = Physics.OverlapSphere(samplePoint, playerCollisionRadius - collisionToleranceDistance, obstacleLayer);
                if (colliders.Length > 0)
                {
                    // 判断是否需要考虑障碍物的可跳跃属性
                    if (actionType == MoveActionType.Jump)
                    {
                        // 对于跳跃，只有"不可跃过"的障碍物才算碰撞
                        bool foundNotJumpable = false;
                        foreach (Collider collider in colliders)
                        {
                            // 使用辅助方法检查障碍物属性
                            if (IsNotJumpable(collider))
                            {
                                foundNotJumpable = true;
                                Debug.LogFormat("跳跃路径采样点 ({0}-{1}) 与不可跃过障碍物碰撞: {2}", i, step, collider.name);
                                break;
                            }
                        }
                        if (foundNotJumpable)
                        {
                            return true;
                        }
                    }
                    else
                    {
                        // 对于其他动作类型（如跑步），任何障碍物都算碰撞
                        Debug.LogFormat("路径采样点 ({0}-{1}) 与障碍物碰撞: {2}", i, step, colliders[0].name);
                        return true;
                    }
                }
            }
        }

        // 最后检查终点
        Collider[] endColliders = Physics.OverlapSphere(path[path.Count - 1], playerCollisionRadius - collisionToleranceDistance, obstacleLayer);
        if (endColliders.Length > 0)
        {
            // 判断是否需要考虑障碍物的可跳跃属性
            if (actionType == MoveActionType.Jump)
            {
                // 对于跳跃，只有"不可跃过"的障碍物才算碰撞
                foreach (Collider collider in endColliders)
                {
                    // 使用辅助方法检查障碍物属性
                    if (IsNotJumpable(collider))
                    {
                        Debug.LogFormat("跳跃路径终点与不可跃过障碍物碰撞: {0}", collider.name);
                        return true;
                    }
                }
            }
            else
            {
                // 对于其他动作类型（如跑步），任何障碍物都算碰撞
                Debug.LogFormat("路径终点与障碍物碰撞: {0}", endColliders[0].name);
                return true;
            }
        }
        
        return false;
    }

    // 在Editor中绘制Gizmos
    private void OnDrawGizmos()
    {
        if (drawCollisionGizmos)
        {
            // 绘制玩家碰撞体
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, playerCollisionRadius);
            
            // 绘制带容差的碰撞体
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, playerCollisionRadius - collisionToleranceDistance);
        }
    }

    // 在Editor中验证参数
    private void OnValidate()
    {
        // 确保路径宽度与玩家碰撞直径一致
        pathWidth = playerCollisionRadius * 2f;
        
        // 如果有路径预览组件，更新其宽度
        if (pathPreview != null)
        {
            pathPreview.startWidth = playerCollisionRadius * 2f;
            pathPreview.endWidth = playerCollisionRadius * 2f;
        }
        
        // 确保落点标记大小与玩家碰撞体一致
        if (landingMarker != null)
        {
            // 更新落点标记大小
            if (landingMarker.transform.childCount > 0)
            {
                landingMarker.transform.GetChild(0).localScale = new Vector3(
                    playerCollisionRadius * 2f, 
                    playerCollisionRadius * 2f, 
                    1f
                );
            }
            else
            {
                landingMarker.transform.localScale = new Vector3(
                    playerCollisionRadius * 2f,
                    playerCollisionRadius * 2f,
                    1f
                );
            }
        }
    }

    // 辅助方法：将LayerMask转换为字符串
    private string LayerMaskToString(LayerMask mask)
    {
        string result = "";
        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                result += LayerMask.LayerToName(i) + ", ";
            }
        }
        return result.TrimEnd(' ', ',');
    }

    // 处理转向目标选择
    private void HandleTurnTargeting()
    {
        // ESC键取消当前动作
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CancelCurrentAction();
            if (gameManager != null)
            {
                gameManager.CancelTargetingPhase();
            }
            return;
        }
        
        // 获取鼠标位置
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            Vector3 hitPoint = hit.point;
            // 确保hitPoint与角色在同一高度
            hitPoint.y = transform.position.y;

            // 计算角色到点击位置的方向向量（不考虑高度）
            Vector3 directionToTarget = hitPoint - transform.position;
            directionToTarget.y = 0f;
            
            // 如果方向向量过短，可能是点击了角色位置附近，此时不显示指示器
            if (directionToTarget.magnitude < 0.1f)
            {
                HidePathPreview();
                return;
            }
            
            // 计算目标方向的单位向量
            Vector3 targetDirection = directionToTarget.normalized;
            
            // 显示转向方向指示器
            ShowTurnDirectionIndicator(targetDirection);
            
            // 计算角度差（转向难度基于角度差）
            float angleDifference = Vector3.Angle(currentDirection, targetDirection);
            
            // 基于角度差计算难度（使用最大180度作为基准）
            float maxTurnAngle = 180f;
            ActionDifficulty difficulty = CalculateActionDifficulty(angleDifference, maxTurnAngle);
            currentDifficulty = difficulty;
            
            // 更新成功率显示
            UpdateSuccessRateDisplay(difficulty);
            
            // 如果鼠标左键点击，开始转向
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                StartTurning(targetDirection);
            }
        }
        else
        {
            // 没有命中地面，隐藏路径
            HidePathPreview();
            // 隐藏动作UI
            HideActionUI();
        }
    }
    
    // 开始执行转向
    private void StartTurning(Vector3 targetDirection)
    {
        // 隐藏路径预览
        HidePathPreview();
        // 隐藏动作UI
        HideActionUI();
        
        // 记录当前朝向和目标朝向
        Vector3 currentForward = transform.forward;
        currentForward.y = 0f;
        currentForward.Normalize();
        
        targetDirection.y = 0f;
        targetDirection.Normalize();
        
        // 计算旋转角度（有符号角度，正值表示顺时针，负值表示逆时针）
        float angle = Vector3.SignedAngle(currentForward, targetDirection, Vector3.up);
        
        // 计算预期转向时间
        float expectedTurnDuration = turnTime;
        
        // 通知GameManager设置预期动作时间
        if (gameManager != null)
        {
            gameManager.SetExpectedActionDuration(expectedTurnDuration);
        }
        
        // 设置动画状态
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("IsTurning", true);
        }
        
        // 改变游戏状态为执行中
        if (gameManager != null)
        {
            gameManager.StartExecutionPhase();
        }
        
        // 执行动作检定
        ActionResult result = PerformActionCheck(currentDifficulty);
        
        // 标记回合已使用
        isTurnActive = false;
        
        // 根据检定结果执行不同的转向
        switch (result)
        {
            case ActionResult.Success:
            case ActionResult.Critical:
                // 动作成功，正常转向
                actionSucceeded = true;
                Debug.Log("转向动作检定成功！");
                
                // 重置自我怀疑层数
                ResetDoubtLevel();
                
                // 增加信心值
                int reward = GetSuccessReward(currentDifficulty);
                if (result == ActionResult.Critical)
                {
                    reward = reward * 2; // 大成功，双倍奖励
                    Debug.Log("大成功！获得双倍信心奖励");
                }
                ChangeConfidence(reward);
                
                // 启动转向协程
                StartCoroutine(TurningCoroutine(angle, true, 1.0f));
                break;
                
            case ActionResult.Failure:
                // 动作失败，转向一部分角度
                actionSucceeded = false;
                Debug.Log("转向动作检定失败！将只转向部分角度");
                
                // 增加自我怀疑层数
                IncreaseDoubtLevel();
                
                // 应用自我怀疑惩罚
                ApplyDoubtPenalty();
                
                // 计算失败后的随机转向比例（完成20%-70%的角度）
                float failureProgress = Random.Range(0.2f, 0.7f);
                
                // 启动转向协程（部分转向）
                StartCoroutine(TurningCoroutine(angle * failureProgress, false, failureProgress));
                break;
                
            case ActionResult.Fumble:
                // 大失败，几乎不转向或朝错误方向转少量角度
                actionSucceeded = false;
                Debug.Log("转向动作大失败！几乎不转向或反向");
                
                // 增加自我怀疑层数（大失败增加2层）
                doubtLevel = Mathf.Min(doubtLevel + 2, maxDoubtLevel);
                
                // 应用自我怀疑惩罚
                ApplyDoubtPenalty();
                
                // 计算大失败后的转向角度，可能是反向的微小转向
                float fumbleProgress = Random.Range(-0.1f, 0.1f);
                
                // 启动转向协程（几乎不转向或反向）
                StartCoroutine(TurningCoroutine(angle * fumbleProgress, false, Mathf.Abs(fumbleProgress)));
                break;
        }
    }
    
    // 转向协程
    private IEnumerator TurningCoroutine(float angle, bool fullTurn, float progress)
    {
        isMoving = true;
        canMove = false;
        
        // 记录开始时间和总时长
        float startTime = Time.time;
        float totalTime = turnTime * progress;
        
        // 计算旋转速度（度/秒）
        float rotationSpeed = Mathf.Abs(angle) / totalTime;
        
        // 记录初始旋转和已旋转角度
        Quaternion startRotation = transform.rotation;
        float rotatedAngle = 0f;
        
        // 获取旋转方向（1为顺时针，-1为逆时针）
        float direction = Mathf.Sign(angle);
        
        // 旋转过程
        while (rotatedAngle < Mathf.Abs(angle))
        {
            // 使用deltaTime确保受到timeScale影响
            float deltaAngle = rotationSpeed * Time.deltaTime;
            
            // 确保不会旋转过头
            deltaAngle = Mathf.Min(deltaAngle, Mathf.Abs(angle) - rotatedAngle);
            
            // 应用旋转
            transform.Rotate(Vector3.up, deltaAngle * direction);
            
            // 更新已旋转角度
            rotatedAngle += deltaAngle;
            
            // 计算进度并通知GameManager - 使用基于时间的进度计算
            if (gameManager != null)
            {
                float elapsedTime = Time.time - startTime;
                float currentProgress = elapsedTime / totalTime;
                gameManager.UpdateActionProgressByDistance(currentProgress);
            }
            
            yield return null;
        }
        
        // 确保精确旋转到目标角度
        transform.rotation = Quaternion.Euler(0, startRotation.eulerAngles.y + angle, 0);
        
        // 更新当前朝向为新的前方向
        currentDirection = transform.forward;
        
        // 设置动画状态
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("IsTurning", false);
        }
        
        // 重置状态
        isMoving = false;
        canMove = true;
        currentAction = MoveActionType.None;
        
        // 转向完成，回到计划状态
        if (gameManager != null)
        {
            gameManager.EndExecutionPhase();
        }
        
        // 结束回合
        EndTurn();
    }

    private void ApplyDoubtPenalty()
    {
        if (!enableDoubtSystem) return;
        
        int penalty = 0;
        
        // 根据自我怀疑层数确定惩罚
        switch (doubtLevel)
        {
            case 0: penalty = doubtPenalty0; break;
            case 1: penalty = doubtPenalty1; break;
            case 2: penalty = doubtPenalty2; break;
            case 3: penalty = doubtPenalty3; break;
            case 4: penalty = doubtPenalty4; break;
        }
        
        // 扣除信心值
        if (penalty > 0)
        {
            ChangeConfidence(-penalty);
            Debug.LogFormat("自我怀疑惩罚：-{0}点信心", penalty);
        }
    }
    
    // 更改信心值
    private void ChangeConfidence(int amount)
    {
        if (!enableConfidenceSystem) return;
        
        int previousConfidence = currentConfidence;
        currentConfidence = Mathf.Clamp(currentConfidence + amount, minConfidence, maxConfidence);
        
        // 如果信心值改变，更新信心状态
        if (previousConfidence != currentConfidence)
        {
            UpdateConfidenceState();
            Debug.LogFormat("信心值从{0}变为{1}, 当前状态：{2}", previousConfidence, currentConfidence, confidenceState);
        }
    }
    
    // 更新UI显示
    private void UpdateUI()
    {
        // 更新信心状态文本
        if (confidenceStateText != null && enableConfidenceSystem)
        {
            string stateText = "";
            Color stateColor = Color.white;
            
            switch (confidenceState)
            {
                case ConfidenceState.Broken:
                    stateText = "Broken";
                    stateColor = new Color(1f, 0.2f, 0.2f); // 深红色
                    break;
                case ConfidenceState.Unstable:
                    stateText = "Unstable";
                    stateColor = new Color(1f, 0.6f, 0.2f); // 橙色
                    break;
                case ConfidenceState.Stable:
                    stateText = "Stable";
                    stateColor = Color.white; // 白色
                    break;
                case ConfidenceState.Confident:
                    stateText = "Confident";
                    stateColor = new Color(0.2f, 1f, 0.2f); // 浅绿色
                    break;
                case ConfidenceState.Unshakable:
                    stateText = "Unshakable";
                    stateColor = new Color(0f, 0.8f, 1f); // 青色
                    break;
            }
            confidenceStateText.text = "State: " + stateText;
            confidenceStateText.color = stateColor;
        }
        else if (confidenceStateText != null)
        {
            confidenceStateText.gameObject.SetActive(false);
        }
        
        // 更新回合计数器
        if (turnCounterText != null)
        {
            turnCounterText.text = "Turn: " + currentTurn;
        }
        
        // 更新信心值滑块
        if (confidenceSlider != null && enableConfidenceSystem)
        {
            confidenceSlider.minValue = minConfidence;
            confidenceSlider.maxValue = maxConfidence;
            confidenceSlider.value = currentConfidence;
            
            // 获取滑块的填充图像组件
            Image fillImage = confidenceSlider.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                // 根据当前信心值设置滑块颜色
                if (currentConfidence >= 91)
                {
                    fillImage.color = new Color(0f, 0.8f, 1f); // 青色 - Unshakable
                }
                else if (currentConfidence >= 75)
                {
                    fillImage.color = new Color(0.2f, 1f, 0.2f); // 浅绿色 - Confident
                }
                else if (currentConfidence >= 50)
                {
                    fillImage.color = Color.white; // 白色 - Stable
                }
                else if (currentConfidence >= 25)
                {
                    fillImage.color = new Color(1f, 0.6f, 0.2f); // 橙色 - Unstable
                }
                else
                {
                    fillImage.color = new Color(1f, 0.2f, 0.2f); // 深红色 - Broken
                }
            }
        }
        else if (confidenceSlider != null)
        {
            confidenceSlider.gameObject.SetActive(false);
        }

        // 更新自我怀疑层数文本
        if (doubtLevelText != null && enableDoubtSystem)
        {
            doubtLevelText.text = "Doubt Level: " + doubtLevel;
            // 根据怀疑层数改变颜色
            if (doubtLevel == 0)
                doubtLevelText.color = Color.green;
            else if (doubtLevel <= 2)
                doubtLevelText.color = Color.yellow;
            else
                doubtLevelText.color = Color.red;
        }
        else if (doubtLevelText != null)
        {
            doubtLevelText.gameObject.SetActive(false);
        }

        // 更新疲劳值和成功率显示
        if (landingMarker != null && landingMarker.activeSelf)
        {
            Vector3 markerPosition = landingMarker.transform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(markerPosition);
            
            // 更新疲劳值显示
            if (fatigueText != null)
            {
                float fatigueCost = CalculateFatigueCost(markerPosition);
                fatigueText.text = string.Format("Fatigue: {0:0}", fatigueCost);
                fatigueText.gameObject.SetActive(true);
                fatigueText.transform.position = screenPos + new Vector3(0, 90, 0); // 放在落点上方
            }
            
            // 更新成功率显示
            if (successRateText != null && actionDifficultyText != null)
            {
                float rate = CalculateSuccessRate(currentDifficulty);
                successRateText.text = string.Format("Success Rate: {0:0}%", rate);
                actionDifficultyText.text = "Difficulty: " + currentDifficulty.ToString();
                
                // 根据成功率改变颜色
                if (rate >= 80)
                    successRateText.color = Color.green;
                else if (rate >= 50)
                    successRateText.color = Color.yellow;
                else
                    successRateText.color = Color.red;
                
                successRateText.gameObject.SetActive(true);
                actionDifficultyText.gameObject.SetActive(true);
                
                // 设置文本位置
                successRateText.transform.position = screenPos + new Vector3(0, 60, 0);
                actionDifficultyText.transform.position = screenPos + new Vector3(0, 30, 0);
            }
        }
        else
        {
            // 隐藏所有与落点相关的UI
            if (fatigueText != null) fatigueText.gameObject.SetActive(false);
            if (successRateText != null) successRateText.gameObject.SetActive(false);
            if (actionDifficultyText != null) actionDifficultyText.gameObject.SetActive(false);
        }
    }
    
    // 获取当前信心加成
    private int GetConfidenceBonus()
    {
        switch (confidenceState)
        {
            case ConfidenceState.Unshakable: return unshakableConfidenceBonus;
            case ConfidenceState.Confident: return confidentBonus;
            case ConfidenceState.Stable: return stableBonus;
            case ConfidenceState.Unstable: return unstableBonus;
            case ConfidenceState.Broken: return brokenBonus;
            default: return 0;
        }
    }
    
    // 结束当前回合
    private void EndTurn()
    {
        Debug.Log("PlayerController: 结束当前回合，准备调用颜色轮换");
        
        // 增加回合计数
        currentTurn++;
        
        // 重置回合状态
        isTurnActive = true;
        
        // 轮换区域颜色
        if (footAreaGenerator != null)
        {
            Debug.Log("PlayerController: footAreaGenerator不为空，调用RotateColors方法");
            footAreaGenerator.RotateColors();
        }
        else
        {
            Debug.LogError("PlayerController: footAreaGenerator为空，无法轮换颜色");
        }
        
        // 通知游戏管理器回合结束
        if (gameManager != null)
        {
            // TODO: 在GameManager中实现OnTurnEnded方法
            // gameManager.OnTurnEnded();
        }
        
        Debug.LogFormat("回合 {0} 开始", currentTurn);
    }

    // 计算动作难度
    private ActionDifficulty CalculateActionDifficulty(float distance, float maxDistance)
    {
        // 计算距离占最大距离的百分比
        float percentage = (distance / maxDistance) * 100f;
        
        // 根据百分比确定难度
        if (percentage <= 20f)
            return ActionDifficulty.VeryEasy;
        else if (percentage <= 40f)
            return ActionDifficulty.Easy;
        else if (percentage <= 60f)
            return ActionDifficulty.Medium;
        else if (percentage <= 80f)
            return ActionDifficulty.Hard;
        else
            return ActionDifficulty.VeryHard;
    }
    
    // 获取动作难度值
    private int GetDifficultyValue(ActionDifficulty difficulty)
    {
        switch (difficulty)
        {
            case ActionDifficulty.VeryEasy: return veryEasyDifficulty;
            case ActionDifficulty.Easy: return easyDifficulty;
            case ActionDifficulty.Medium: return mediumDifficulty;
            case ActionDifficulty.Hard: return hardDifficulty;
            case ActionDifficulty.VeryHard: return veryHardDifficulty;
            default: return mediumDifficulty;
        }
    }
    
    // 获取成功奖励
    private int GetSuccessReward(ActionDifficulty difficulty)
    {
        switch (difficulty)
        {
            case ActionDifficulty.VeryEasy: return veryEasySuccessReward;
            case ActionDifficulty.Easy: return easySuccessReward;
            case ActionDifficulty.Medium: return mediumSuccessReward;
            case ActionDifficulty.Hard: return hardSuccessReward;
            case ActionDifficulty.VeryHard: return veryHardSuccessReward;
            default: return 0;
        }
    }
    
    // 执行动作检定
    private ActionResult PerformActionCheck(ActionDifficulty difficulty)
    {
        // 如果禁用了动作检定，始终返回成功
        if (!enableActionCheck)
            return ActionResult.Success;
            
        // 投掷d20
        int diceRoll = Random.Range(1, diceSize + 1);
        
        // 获取信心加成
        int bonus = GetConfidenceBonus();
        
        // 计算最终检定值
        int checkValue = diceRoll + bonus;
        
        // 获取难度值
        int difficultyValue = GetDifficultyValue(difficulty);
        
        // 记录检定信息
        Debug.LogFormat("动作检定: d20={0}, 信心加成={1}, 总值={2}, 难度={3}", 
            diceRoll, bonus, checkValue, difficultyValue);
        
        // 大失败（骰子为1）
        if (diceRoll == 1)
            return ActionResult.Fumble;
            
        // 大成功（骰子为20）
        if (diceRoll == diceSize)
            return ActionResult.Critical;
            
        // 普通成功
        if (checkValue >= difficultyValue)
            return ActionResult.Success;
            
        // 失败
        return ActionResult.Failure;
    }
    
    // 计算动作成功率
    private float CalculateSuccessRate(ActionDifficulty difficulty)
    {
        // 获取难度值
        int difficultyValue = GetDifficultyValue(difficulty);
        
        // 获取信心加成
        int bonus = GetConfidenceBonus();
        
        // 计算需要骰出的最小值
        int minRoll = difficultyValue - bonus;
        
        // 确保最小值在有效范围内
        minRoll = Mathf.Clamp(minRoll, 1, diceSize);
        
        // 计算成功的概率
        float successRate = (float)(diceSize - minRoll + 1) / diceSize;
        
        // 返回百分比形式
        return successRate * 100f;
    }
    
    // 更新成功率显示
    private void UpdateSuccessRateDisplay(ActionDifficulty difficulty)
    {
        if (successRateText != null && actionDifficultyText != null)
        {
            // 计算成功率
            float rate = CalculateSuccessRate(difficulty);
            
            // 更新成功率文本
            successRateText.text = string.Format("Success Rate: {0:0}%", rate);
            
            // 根据成功率改变颜色
            if (rate >= 80)
                successRateText.color = Color.green;
            else if (rate >= 50)
                successRateText.color = Color.yellow;
            else
                successRateText.color = Color.red;
            
            // 更新难度文本
            string difficultyName = "";
            switch (difficulty)
            {
                case ActionDifficulty.VeryEasy: difficultyName = "Very Easy"; break;
                case ActionDifficulty.Easy: difficultyName = "Easy"; break;
                case ActionDifficulty.Medium: difficultyName = "Medium"; break;
                case ActionDifficulty.Hard: difficultyName = "Hard"; break;
                case ActionDifficulty.VeryHard: difficultyName = "Very Hard"; break;
            }
            actionDifficultyText.text = "Difficulty: " + difficultyName;

            // 显示文本
            successRateText.gameObject.SetActive(true);
            actionDifficultyText.gameObject.SetActive(true);

            // 更新文本位置
            if (landingMarker != null && landingMarker.activeSelf)
            {
                Vector3 markerPosition = landingMarker.transform.position;
                
                // 将世界坐标转换为屏幕坐标
                Vector3 screenPos = Camera.main.WorldToScreenPoint(markerPosition);
                
                // 设置文本位置在落点标记上方
                successRateText.transform.position = screenPos + new Vector3(0, 60, 0);
                actionDifficultyText.transform.position = screenPos + new Vector3(0, 30, 0);
            }
        }
    }

    // 隐藏成功率和难度文本
    private void HideActionUI()
    {
        if (successRateText != null)
        {
            successRateText.gameObject.SetActive(false);
        }
        if (actionDifficultyText != null)
        {
            actionDifficultyText.gameObject.SetActive(false);
        }
    }

    private void CancelCurrentAction()
    {
        // 隐藏路径预览和落点标记
        HidePathPreview();
        HideLandingMarker();
        // 隐藏动作UI
        HideActionUI();
    }

    // 显示转向方向指示器
    private void ShowTurnDirectionIndicator(Vector3 direction)
    {
        // 计算起点和终点
        Vector3 startPos = transform.position;
        startPos.y += pathHeightOffset;
        Vector3 endPos = startPos + direction * turnIndicatorLength;
        
        // 设置路径预览线条渲染器的点
        pathPreview.positionCount = 2;
        pathPreview.SetPosition(0, startPos);
        pathPreview.SetPosition(1, endPos);
        
        // 设置线条宽度
        pathPreview.startWidth = turnIndicatorWidth;
        pathPreview.endWidth = 0f; // 末端为尖头形状
        
        // 设置转向指示器材质
        pathPreview.material = turnIndicatorMaterial != null ? turnIndicatorMaterial : validPathMaterial;
        
        // 确保路径可见
        pathPreview.enabled = true;
    }
    
    // 跳过当前回合
    private void SkipTurn()
    {
        if (!isTurnActive) return;
        
        Debug.Log("跳过回合");
        
        // 标记为跳过回合
        turnSkipped = true;
        
        // 增加自我怀疑层数，因为跳过回合视为不使用动作
        IncreaseDoubtLevel();
        
        // 应用自我怀疑惩罚
        ApplyDoubtPenalty();
        
        // 结束回合
        EndTurn();
    }
    
    // 增加自我怀疑层数
    private void IncreaseDoubtLevel()
    {
        if (!enableDoubtSystem) return;
        
        doubtLevel = Mathf.Min(doubtLevel + 1, maxDoubtLevel);
        Debug.LogFormat("自我怀疑层数增加到 {0}", doubtLevel);
    }
    
    // 重置自我怀疑层数
    private void ResetDoubtLevel()
    {
        if (!enableDoubtSystem) return;
        
        doubtLevel = 0;
        Debug.Log("自我怀疑层数重置为 0");
    }

    // 计算跳跃路径
    private List<Vector3> CalculateJumpPath(Vector3 targetPoint)
    {
        List<Vector3> jumpPath = new List<Vector3>();
        Vector3 startPos = transform.position;
        
        // 创建路径点数组
        int segments = 20; // 跳跃路径分段数
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 pathPoint = Vector3.Lerp(startPos, targetPoint, t);
            
            // 根据跳跃曲线添加高度
            float heightOffset = jumpHeightCurve.Evaluate(t) * 
                                Vector3.Distance(startPos, targetPoint) * 0.3f; // 高度为距离的30%
            
            pathPoint.y = startPos.y + heightOffset;
            jumpPath.Add(pathPoint);
        }
        
        return jumpPath;
    }

    // 更新信心状态
    private void UpdateConfidenceState()
    {
        if (!enableConfidenceSystem) return;
        
        if (currentConfidence >= 91)
        {
            confidenceState = ConfidenceState.Unshakable;
        }
        else if (currentConfidence >= 75)
        {
            confidenceState = ConfidenceState.Confident;
        }
        else if (currentConfidence >= 50)
        {
            confidenceState = ConfidenceState.Stable;
        }
        else if (currentConfidence >= 25)
        {
            confidenceState = ConfidenceState.Unstable;
        }
        else
        {
            confidenceState = ConfidenceState.Broken;
        }
    }

    // 计算疲劳值
    private int CalculateFatigueCost(Vector3 targetPosition)
    {
        float distance = Vector3.Distance(transform.position, targetPosition);
        // 每2米消耗一次疲劳值
        int distanceCost = Mathf.FloorToInt(distance / 2f) * Mathf.RoundToInt(fatiguePerMeter);
        return Mathf.RoundToInt(baseFatigueCost) + distanceCost;
    }
} 