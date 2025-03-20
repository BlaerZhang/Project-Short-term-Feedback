using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

// 移动动作类型枚举
public enum MoveActionType
{
    None,   // 未选择
    Run,    // 跑步
    Jump    // 跳跃
}

public class PlayerController : MonoBehaviour
{
    [Header("角色属性")]
    [SerializeField] private float maxSpeed = 5f;      // 角色最大移动速度
    [SerializeField] private float minSpeed = 1f;      // 角色最小移动速度
    [SerializeField] private float currentSpeed = 3f;  // 当前速度

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
            switch (gameManager.CurrentState)
            {
                case GameState.Planning:
                    // 处理动作选择输入
                    HandleActionSelection();
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
    }

    private void HandleActionSelection()
    {
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
    }

    private void SelectAction(MoveActionType actionType)
    {
        currentAction = actionType;
        if (gameManager != null)
        {
            gameManager.StartTargetingPhase();
        }

        // 显示圆弧指示器
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
        }
    }

    // 检查点是否在允许的跑步范围内
    private bool IsPointValidForRunning(Vector3 point, out Vector3 validPoint)
    {
        // 计算到目标点的向量
        Vector3 toTarget = point - transform.position;
        toTarget.y = 0; // 确保在水平面上计算

        // 如果目标点太近，视为无效
        if (toTarget.magnitude < 0.5f)
        {
            validPoint = point;
            return false;
        }

        // 计算目标点方向与当前朝向的夹角
        float angle = Vector3.SignedAngle(currentDirection, toTarget.normalized, Vector3.up);

        // 根据当前速度计算允许的转向角度
        float allowedAngle = Mathf.Lerp(maxTurnAngle, minTurnAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed)) * 0.5f;

        // 计算最终角度（限制在允许范围内）
        float targetAngle = Mathf.Clamp(angle, -allowedAngle, allowedAngle);
        
        // 获取当前移动半径
        float currentRadius = GetCurrentRunRadius();

        // 计算圆弧上的有效点
        Quaternion rotation = Quaternion.AngleAxis(targetAngle, Vector3.up);
        validPoint = transform.position + (rotation * currentDirection) * currentRadius;

        // 检查落点是否与障碍物碰撞
        landingPointCollision = CheckPointCollision(validPoint);

        // 所有落在圆弧上的点都是有效的（角度和半径的限制）
        // 但如果与障碍物碰撞，则返回false
        return !landingPointCollision;
    }

    // 检查点是否在允许的跳跃范围内
    private bool IsPointValidForJumping(Vector3 point, out Vector3 validPoint)
    {
        // 计算到目标点的向量
        Vector3 toTarget = point - transform.position;
        toTarget.y = 0; // 确保在水平面上计算

        // 如果目标点太近，视为无效
        if (toTarget.magnitude < 0.5f)
        {
            validPoint = point;
            return false;
        }

        // 计算目标点方向与当前朝向的夹角
        float angle = Vector3.SignedAngle(currentDirection, toTarget.normalized, Vector3.up);

        // 根据当前速度计算允许的跳跃角度
        float allowedAngle = Mathf.Lerp(jumpMaxAngle, jumpMinAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed)) * 0.5f;

        // 计算最终角度（限制在允许范围内）
        float targetAngle = Mathf.Clamp(angle, -allowedAngle, allowedAngle);
        
        // 获取当前跳跃半径
        float currentRadius = GetCurrentJumpRadius();

        // 计算圆弧上的有效点
        Quaternion rotation = Quaternion.AngleAxis(targetAngle, Vector3.up);
        validPoint = transform.position + (rotation * currentDirection) * currentRadius;

        // 检查落点是否与障碍物碰撞
        landingPointCollision = CheckPointCollision(validPoint);

        // 所有落在圆弧上的点都是有效的（角度和半径的限制）
        // 但如果与障碍物碰撞，则返回false
        return !landingPointCollision;
    }

    // 显示跑步路径预览
    private void ShowRunPathPreview(Vector3 targetPoint)
    {
        // 计算路径点
        CalculateRunPath(targetPoint);

        // 计算最终朝向
        Vector3 finalDirection = (targetPoint - transform.position).normalized;
        finalDirection.y = 0;

        // 检查路径是否与障碍物碰撞
        pathCollision = CheckPathCollision(movementPath);

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
            
            // 使用LandingMarker脚本中的SetValid方法设置颜色
            LandingMarker marker = landingMarker.GetComponent<LandingMarker>();
            if (marker != null)
            {
                // 检查是否有碰撞
                bool isValidWithoutCollision = isValid && !landingPointCollision && !pathCollision;
                marker.SetValid(isValidWithoutCollision);
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

    // 计算跑步路径（使用圆弧）
    private void CalculateRunPath(Vector3 targetPoint)
    {
        movementPath.Clear();

        Vector3 startPos = transform.position;
        Vector3 toTarget = targetPoint - startPos;
        toTarget.y = 0;
        
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

    // 开始跑步移动到目标点
    private void StartRunning(Vector3 targetPoint)
    {
        if (isMoving || !canMove)
            return;

        moveTargetPosition = targetPoint;
        isMoving = true;
        canMove = false;

        // 淡出圆弧指示器
        if (arcIndicator != null)
        {
            ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
            if (arc != null)
            {
                arc.FadeOut();
            }
        }

        // 隐藏落点标记
        HideLandingMarker();

        // 通知GameManager进入执行阶段
        if (gameManager != null)
        {
            gameManager.StartExecutionPhase();
        }

        // 使用DOTween进行移动
        StartRunningWithDOTween();
    }

    // 开始跳跃移动到目标点
    private void StartJumping(Vector3 targetPoint)
    {
        if (isMoving || !canMove)
            return;

        moveTargetPosition = targetPoint;
        isMoving = true;
        canMove = false;

        // 淡出圆弧指示器
        if (arcIndicator != null)
        {
            ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
            if (arc != null)
            {
                arc.FadeOut();
            }
        }

        // 隐藏落点标记
        HideLandingMarker();

        // 通知GameManager进入执行阶段
        if (gameManager != null)
        {
            gameManager.StartExecutionPhase();
        }

        // 使用DOTween进行跳跃
        StartJumpingWithDOTween();
    }

    // 使用DOTween沿路径移动（跑步）
    private void StartRunningWithDOTween()
    {
        if (movementPath.Count == 0)
            return;

        // 创建路径点数组
        Vector3[] pathPoints = movementPath.ToArray();

        // 获取初始和目标朝向
        Quaternion startRotation = transform.rotation;
        Vector3 finalDirection = (moveTargetPosition - transform.position).normalized;
        finalDirection.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(finalDirection);

        // 创建移动序列
        Sequence moveSequence = DOTween.Sequence();

        // 添加路径移动
        moveSequence.Append(transform.DOPath(pathPoints, moveTime, PathType.Linear)
            .SetEase(moveEase)
            .SetOptions(false)
            .OnStart(() => {
                // 播放跑步动画
                if (characterAnimator != null)
                {
                    characterAnimator.SetTrigger("Run");
                }
            }));

        // 同时进行朝向旋转
        transform.DORotateQuaternion(targetRotation, moveTime)
            .SetEase(rotateEase);

        // 移动完成后的回调
        moveSequence.OnComplete(() =>
        {
            // 确保精确位置
            transform.position = moveTargetPosition;
            transform.rotation = targetRotation;
            
            // 更新当前朝向
            currentDirection = finalDirection;
            
            // 重置状态
            isMoving = false;
            canMove = true;
            currentAction = MoveActionType.None;
            
            // 清除路径预览
            HidePathPreview();

            // 结束动画
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger("Idle");
            }

            // 通知GameManager回到规划阶段
            if (gameManager != null)
            {
                gameManager.EndExecutionPhase();
            }
        });
    }

    // 使用DOTween执行跳跃
    private void StartJumpingWithDOTween()
    {
        // 获取初始和目标朝向
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        Vector3 finalDirection = (moveTargetPosition - transform.position).normalized;
        finalDirection.y = 0;
        Quaternion targetRotation = Quaternion.LookRotation(finalDirection);

        // 创建移动序列
        Sequence jumpSequence = DOTween.Sequence();

        // 先旋转朝向目标
        jumpSequence.Append(transform.DORotateQuaternion(targetRotation, jumpTime * 0.1f)
            .SetEase(rotateEase));

        // 然后执行跳跃
        jumpSequence.Append(transform.DOMove(moveTargetPosition, jumpTime * 0.9f)
            .SetEase(moveEase)
            .OnStart(() => {
                // 播放跳跃动画
                if (characterAnimator != null)
                {
                    characterAnimator.SetTrigger("Jump");
                }
            })
            .OnUpdate(() => {
                // 根据跳跃曲线更新Y轴高度
                float jumpProgress = (transform.position - startPosition).magnitude / 
                                    (moveTargetPosition - startPosition).magnitude;
                
                // 应用跳跃高度曲线
                float heightOffset = jumpHeightCurve.Evaluate(jumpProgress) * 
                                    Vector3.Distance(startPosition, moveTargetPosition) * 0.3f; // 高度为距离的30%
                
                Vector3 currentPos = transform.position;
                currentPos.y = startPosition.y + heightOffset;
                transform.position = currentPos;
            }));

        // 移动完成后的回调
        jumpSequence.OnComplete(() =>
        {
            // 确保精确位置
            transform.position = moveTargetPosition;
            transform.rotation = targetRotation;
            
            // 更新当前朝向
            currentDirection = finalDirection;
            
            // 重置状态
            isMoving = false;
            canMove = true;
            currentAction = MoveActionType.None;
            
            // 清除路径预览
            HidePathPreview();

            // 结束动画
            if (characterAnimator != null)
            {
                characterAnimator.SetTrigger("Idle");
            }

            // 通知GameManager回到规划阶段
            if (gameManager != null)
            {
                gameManager.EndExecutionPhase();
            }
        });
    }

    // 更新可移动范围的圆弧指示器
    private void UpdateArcIndicator()
    {
        if (arcIndicator != null)
        {
            float allowedAngle, currentRadius;
            
            // 根据当前动作类型和速度计算角度和半径
            if (currentAction == MoveActionType.Run)
            {
                // 根据当前速度计算允许的转向角度
                allowedAngle = Mathf.Lerp(maxTurnAngle, minTurnAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed));
                
                // 获取当前移动半径
                currentRadius = GetCurrentRunRadius();
            }
            else if (currentAction == MoveActionType.Jump)
            {
                // 根据当前速度计算允许的跳跃角度
                allowedAngle = Mathf.Lerp(jumpMaxAngle, jumpMinAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed));
                
                // 获取当前跳跃半径
                currentRadius = GetCurrentJumpRadius();
            }
            else
            {
                // 默认值
                allowedAngle = 90f;
                currentRadius = 3f;
            }
            
            // 更新圆弧指示器
            ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
            if (arc != null)
            {
                arc.UpdateArc(transform.position, currentDirection, currentRadius, allowedAngle);
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

    // 游戏状态变化响应
    public void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Planning:
                // 在规划阶段，隐藏移动范围指示器
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

            case GameState.Targeting:
                // 在目标选择阶段，显示移动范围指示器
                UpdateArcIndicator();
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
} 