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
    [SerializeField] private float pathWidth = 0.1f;    // 路径宽度
    [SerializeField] private float directionLineWidth = 0.05f; // 方向指示线宽度
    [SerializeField] private float pathHeightOffset = 0.01f;   // 路径高度偏移，避免Z轴抖动

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
        pathPreview.startWidth = pathWidth;
        pathPreview.endWidth = pathWidth;
        pathPreview.positionCount = 0;
        pathPreview.alignment = LineAlignment.TransformZ; // 使用TransformZ，因为我们已经旋转了物体
        pathPreview.useWorldSpace = true; // 使用世界空间坐标
        pathPreview.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        pathPreview.receiveShadows = false;
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

        // Press 1 to set speed to 1
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            ChangeSpeed(1);
        }

        // Press 2 to set speed to 3
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            ChangeSpeed(3);
        }

        // Press 3 to set speed to 5
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            ChangeSpeed(5);
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
        float currentRadius = GetCurrentMovementRadius();

        // 计算圆弧上的有效点
        Quaternion rotation = Quaternion.AngleAxis(targetAngle, Vector3.up);
        validPoint = transform.position + (rotation * currentDirection) * currentRadius;

        // 所有落在圆弧上的点都是有效的
        return true;
    }

    // 显示路径预览
    private void ShowPathPreview(Vector3 targetPoint)
    {
        // 计算贝塞尔曲线路径点
        CalculateMovementPath(targetPoint);

        // 计算最终朝向
        Vector3 finalDirection = (targetPoint - transform.position).normalized;
        finalDirection.y = 0;

        // 创建包含路径点和预览方向的完整点列表
        List<Vector3> allPoints = new List<Vector3>(movementPath);
        
        // 添加最终朝向的预览线
        float directionPreviewLength = GetCurrentMovementRadius() * 1.5f;
        Vector3 directionEnd = targetPoint + finalDirection * directionPreviewLength;
        
        // 添加方向预览线的点
        allPoints.Add(targetPoint);
        allPoints.Add(directionEnd);

        // 更新路径渲染器，应用高度偏移
        pathPreview.positionCount = allPoints.Count;
        for (int i = 0; i < allPoints.Count; i++)
        {
            Vector3 point = allPoints[i];
            point.y += pathHeightOffset;
            pathPreview.SetPosition(i, point);
        }

        // 设置路径宽度渐变
        float pathEndIndex = (float)(movementPath.Count - 1) / (allPoints.Count - 1);
        AnimationCurve widthCurve = new AnimationCurve(
            new Keyframe(0, pathWidth),                // 路径起点
            new Keyframe(pathEndIndex, pathWidth),     // 路径终点
            new Keyframe(pathEndIndex + 0.001f, directionLineWidth), // 方向线起点（添加一个微小偏移以确保突变）
            new Keyframe(1f, directionLineWidth)       // 方向线终点（保持相同宽度）
        );
        pathPreview.widthCurve = widthCurve;

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
        float allowedAngle = Mathf.Lerp(maxTurnAngle, minTurnAngle, (currentSpeed - minSpeed) / (maxSpeed - minSpeed)) * 0.5f;
        
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
        moveSequence.Append(transform.DOPath(pathPoints, moveTime, PathType.Linear)
            .SetEase(moveEase)
            .SetOptions(false)
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

            // 淡入圆弧指示器
            if (arcIndicator != null)
            {
                ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
                if (arc != null)
                {
                    arc.FadeIn();
                }
            }

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

        // 淡出圆弧指示器
        if (arcIndicator != null)
        {
            ArcIndicator arc = arcIndicator.GetComponent<ArcIndicator>();
            if (arc != null)
            {
                arc.FadeOut();
            }
        }

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