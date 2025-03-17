using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float maxSpeed = 5f;      // 角色最大移动速度
    [SerializeField] private float minSpeed = 1f;      // 角色最小移动速度
    [SerializeField] private float currentSpeed = 3f;  // 当前速度
    [SerializeField] private float maxTurnAngle = 120f; // 最小速度时可转弯的最大角度
    [SerializeField] private float minTurnAngle = 30f;  // 最大速度时可转弯的最小角度
    [SerializeField] private float baseMovementRadius = 3f; // 基础移动半径
    [SerializeField] private float maxMovementRadius = 6f;  // 最大移动半径
    [SerializeField] private float minMovementRadius = 1f;  // 最小移动半径
    [SerializeField] private float moveTime = 1f;      // 完成一次移动的时间

    [Header("路径显示")]
    [SerializeField] private LineRenderer pathPreview;  // 路径预览线条渲染器
    [SerializeField] private int pathResolution = 20;   // 路径分辨率
    [SerializeField] private GameObject arcIndicator;   // 显示可移动范围的圆弧指示器
    [SerializeField] private Material validPathMaterial;  // 有效路径材质
    [SerializeField] private Material invalidPathMaterial; // 无效路径材质

    [Header("动画设置")]
    [SerializeField] private Ease moveEase = Ease.InOutSine;  // 移动缓动函数
    [SerializeField] private Ease rotateEase = Ease.InOutSine; // 旋转缓动函数
    [SerializeField] private float pathUpdateInterval = 0.05f; // 路径更新间隔

    // 状态变量
    private Vector3 moveTargetPosition;  // 移动目标位置
    private Vector3 currentDirection;    // 当前朝向
    private bool isMoving = false;       // 是否正在移动
    private bool canMove = true;         // 是否可以移动
    private Camera mainCamera;           // 主相机引用
    private LayerMask groundLayer;       // 地面层级
    private List<Vector3> movementPath = new List<Vector3>(); // 当前移动路径
    private GameManager gameManager;     // 游戏管理器引用

    private void Awake()
    {
        mainCamera = Camera.main;
        groundLayer = LayerMask.GetMask("Ground");
        
        // 初始化朝向
        currentDirection = transform.forward;

        // 确保有LineRenderer组件
        if (pathPreview == null)
        {
            pathPreview = GetComponent<LineRenderer>();
            if (pathPreview == null)
            {
                pathPreview = gameObject.AddComponent<LineRenderer>();
                pathPreview.startWidth = 0.1f;
                pathPreview.endWidth = 0.1f;
                pathPreview.positionCount = 0;
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
    }

    private void Update()
    {
        // 只在规划阶段处理移动输入
        if (gameManager != null && gameManager.CurrentState == GameState.Planning)
        {
            if (!isMoving && canMove)
            {
                // 鼠标处理
                HandleMouseInput();

                // 显示可移动范围
                UpdateArcIndicator();
            }
        }
    }

    private void HandleMouseInput()
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
            bool isValidMoveTarget = IsPointValidForMovement(hitPoint, out Vector3 validPoint);

            // 总是显示路径预览，使用validPoint
            ShowPathPreview(validPoint);

            // 设置路径材质
            pathPreview.material = isValidMoveTarget ? validPathMaterial : invalidPathMaterial;

            // 如果鼠标左键点击且位置有效，开始移动
            if (Mouse.current.leftButton.wasPressedThisFrame && isValidMoveTarget)
            {
                StartMoving(validPoint);
            }
        }
        else
        {
            // 没有命中地面，隐藏路径
            HidePathPreview();
        }
    }

    // 检查点是否在允许的移动范围内
    private bool IsPointValidForMovement(Vector3 point, out Vector3 validPoint)
    {
        validPoint = point;

        // 计算到目标点的向量
        Vector3 toTarget = point - transform.position;
        toTarget.y = 0; // 确保在水平面上计算

        // 如果目标点太近，视为无效
        if (toTarget.magnitude < 0.5f)
        {
            return false;
        }

        // 计算目标点方向与当前朝向的夹角
        float angle = Vector3.SignedAngle(currentDirection, toTarget.normalized, Vector3.up);

        // 根据当前速度计算允许的转向角度
        float allowedAngle = Mathf.Lerp(maxTurnAngle, minTurnAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed));

        // 获取当前移动半径
        float currentRadius = GetCurrentMovementRadius();

        // 计算目标点到圆弧的最近点
        float targetAngle = Mathf.Clamp(angle, -allowedAngle, allowedAngle);
        Quaternion rotation = Quaternion.AngleAxis(targetAngle, Vector3.up);
        Vector3 arcPoint = transform.position + (rotation * currentDirection) * currentRadius;

        // 计算点到圆弧的距离
        float distanceToArc = Vector3.Distance(point, arcPoint);
        float snapThreshold = 0.5f; // 吸附阈值

        // 如果点足够接近圆弧，则吸附到圆弧上
        if (distanceToArc <= snapThreshold)
        {
            validPoint = arcPoint;
            return true;
        }

        // 如果点不在圆弧附近，返回最近的圆弧点但标记为无效
        validPoint = arcPoint;
        return false;
    }

    // 显示路径预览
    private void ShowPathPreview(Vector3 targetPoint)
    {
        // 计算贝塞尔曲线路径点
        CalculateMovementPath(targetPoint);

        // 更新路径渲染器
        pathPreview.positionCount = movementPath.Count;
        for (int i = 0; i < movementPath.Count; i++)
        {
            pathPreview.SetPosition(i, movementPath[i]);
        }

        // 设置有效路径材质
        pathPreview.material = validPathMaterial;
    }

    // 隐藏路径预览
    private void HidePathPreview()
    {
        pathPreview.positionCount = 0;
    }

    // 计算移动路径（使用圆弧）
    private void CalculateMovementPath(Vector3 targetPoint)
    {
        movementPath.Clear();

        Vector3 startPos = transform.position;
        Vector3 toTarget = targetPoint - startPos;
        toTarget.y = 0;
        
        // 计算当前朝向与目标方向的夹角
        float angle = Vector3.SignedAngle(currentDirection, toTarget.normalized, Vector3.up);
        
        // 根据当前速度计算允许的转向角度
        float allowedAngle = Mathf.Lerp(maxTurnAngle, minTurnAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed));
        
        // 确保角度在允许范围内
        float targetAngle = Mathf.Clamp(angle, -allowedAngle, allowedAngle);
        
        // 生成圆弧路径点
        for (int i = 0; i <= pathResolution; i++)
        {
            float t = i / (float)pathResolution;
            // 从0到targetAngle进行插值
            float currentAngle = Mathf.Lerp(0, targetAngle, t);
            
            // 计算当前方向
            Quaternion rotation = Quaternion.AngleAxis(currentAngle, Vector3.up);
            Vector3 currentDirection = rotation * this.currentDirection;
            
            // 计算路径点
            float currentDistance = Mathf.Lerp(0, toTarget.magnitude, t);
            Vector3 point = startPos + currentDirection * currentDistance;
            
            movementPath.Add(point);
        }
    }

    // 沿路径移动的方法（使用DOTween）
    private void StartMovingWithDOTween()
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
        moveSequence.Append(transform.DOPath(pathPoints, moveTime, PathType.Linear) // 改用Linear路径类型
            .SetEase(moveEase)
            .SetOptions(false) // 不使用路径朝向
            .OnUpdate(() =>
            {
                // 路径移动过程中不更新朝向
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
            
            // 清除路径预览
            HidePathPreview();

            // 通知GameManager回到规划阶段
            if (gameManager != null)
            {
                gameManager.EndExecutionPhase();
            }
        });
    }

    // 开始移动到目标点
    private void StartMoving(Vector3 targetPoint)
    {
        if (isMoving || !canMove)
            return;

        moveTargetPosition = targetPoint;
        isMoving = true;
        canMove = false;

        // 通知GameManager进入执行阶段
        if (gameManager != null)
        {
            gameManager.StartExecutionPhase();
        }

        // 使用DOTween进行移动
        StartMovingWithDOTween();
    }

    // 更新可移动范围的圆弧指示器
    private void UpdateArcIndicator()
    {
        if (arcIndicator != null)
        {
            // 根据当前速度计算允许的转向角度
            float allowedAngle = Mathf.Lerp(maxTurnAngle, minTurnAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed));
            
            // 获取当前移动半径
            float currentRadius = GetCurrentMovementRadius();
            
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
    }

    // 获取角色当前速度
    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    // 获取角色当前朝向
    public Vector3 GetCurrentDirection()
    {
        return currentDirection;
    }

    // 游戏状态变化响应
    public void OnGameStateChanged(GameState newState)
    {
        switch (newState)
        {
            case GameState.Planning:
                // 在规划阶段，显示移动范围指示器
                if (arcIndicator != null)
                {
                    arcIndicator.SetActive(true);
                }
                break;

            case GameState.Executing:
                // 在执行阶段，隐藏移动范围指示器
                if (arcIndicator != null)
                {
                    arcIndicator.SetActive(false);
                }
                // 隐藏路径预览
                HidePathPreview();
                break;

            case GameState.Paused:
                // 游戏暂停时的处理
                break;
        }
    }

    // 获取当前移动半径
    private float GetCurrentMovementRadius()
    {
        // 根据速度计算移动半径
        return Mathf.Lerp(minMovementRadius, maxMovementRadius, (currentSpeed - minSpeed) / (maxSpeed - minSpeed));
    }

    private void OnDestroy()
    {
        // 清理DOTween动画
        DOTween.Kill(transform);
    }
} 