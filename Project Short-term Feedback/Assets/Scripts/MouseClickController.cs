using UnityEngine;

public class MouseClickController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float moveSpeed = 5f; // 移动速度
    [SerializeField] private float stoppingDistance = 0.1f; // 停止距离

    [Header("调试显示")]
    [SerializeField] private Color pathColor = new Color(1f, 0.92f, 0.016f, 1f); // 路径颜色
    [SerializeField] private Color targetColor = new Color(1f, 0.2f, 0.2f, 1f); // 目标点颜色
    [SerializeField] private float targetRadius = 0.3f; // 目标点半径
    [SerializeField] private float pathWidth = 0.1f; // 路径宽度

    [Header("视野设置")]
    [SerializeField] private GameObject viewArea; // 视野范围的GameObject
    [SerializeField] private Vector3 expandedScale = new Vector3(2f, 2f, 1f); // 扩大后的scale
    private Vector3 originalScale; // 原始scale
    private bool isExpanded = false; // 是否已扩大视野

    private Vector3 targetPosition; // 目标位置
    private bool isMoving = false; // 是否正在移动
    private LineRenderer pathLine; // 路径线
    private GameObject targetMarker; // 目标点标记

    private void Start()
    {
        // 保存原始scale
        if (viewArea != null)
        {
            originalScale = viewArea.transform.localScale;
        }
        else
        {
            Debug.LogError("View Area GameObject is not assigned!");
        }

        // 创建路径线
        GameObject pathObj = new GameObject("PathLine");
        pathLine = pathObj.AddComponent<LineRenderer>();
        pathLine.material = new Material(Shader.Find("Sprites/Default"));
        pathLine.startColor = pathColor;
        pathLine.endColor = pathColor;
        pathLine.startWidth = pathWidth;
        pathLine.endWidth = pathWidth;
        pathLine.positionCount = 2;
        pathLine.sortingOrder = 1;
        pathLine.enabled = false; // 初始时禁用

        // 创建目标点标记
        targetMarker = new GameObject("TargetMarker");
        SpriteRenderer markerRenderer = targetMarker.AddComponent<SpriteRenderer>();
        markerRenderer.sprite = CreateTargetSprite();
        markerRenderer.color = targetColor;
        targetMarker.transform.localScale = Vector3.one * targetRadius * 2;
        targetMarker.SetActive(false);
    }

    private void Update()
    {
        // 检测空格键按下
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleViewArea();
        }

        // 检测鼠标点击
        if (Input.GetMouseButtonDown(0))
        {
            // 获取鼠标在屏幕上的位置
            Vector3 mouseScreenPosition = Input.mousePosition;
            // 设置z轴为相机到物体的距离
            mouseScreenPosition.z = Mathf.Abs(Camera.main.transform.position.z - transform.position.z);
            
            // 将屏幕坐标转换为世界坐标
            Vector3 mousePosition = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
            mousePosition.z = transform.position.z; // 保持z轴不变
            
            // 设置目标位置
            targetPosition = mousePosition;
            isMoving = true;
            
            // 显示目标点和路径线
            targetMarker.transform.position = targetPosition;
            targetMarker.SetActive(true);
            pathLine.enabled = true;
        }

        // 如果正在移动
        if (isMoving)
        {
            // 更新路径线
            pathLine.SetPosition(0, transform.position);
            pathLine.SetPosition(1, targetPosition);
            
            // 计算当前位置到目标位置的方向
            Vector3 direction = (targetPosition - transform.position).normalized;
            
            // 计算移动距离
            float distance = Vector3.Distance(transform.position, targetPosition);
            
            // 如果距离大于停止距离，继续移动
            if (distance > stoppingDistance)
            {
                // 移动角色
                transform.position += direction * moveSpeed * Time.deltaTime;
            }
            else
            {
                // 到达目标位置，停止移动
                isMoving = false;
                transform.position = targetPosition;
                
                // 隐藏路径和目标点
                pathLine.enabled = false;
                targetMarker.SetActive(false);
            }
        }
    }

    private void ToggleViewArea()
    {
        if (viewArea == null) return;

        if (isExpanded)
        {
            // 还原视野
            viewArea.transform.localScale = originalScale;
        }
        else
        {
            // 扩大视野
            viewArea.transform.localScale = expandedScale;
        }

        isExpanded = !isExpanded;
    }

    private Sprite CreateTargetSprite()
    {
        // 创建一个简单的十字形Sprite
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                // 绘制十字形
                if (x == 16 || y == 16)
                {
                    colors[y * 32 + x] = Color.white;
                }
                else
                {
                    colors[y * 32 + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        return Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
    }

    // 在Scene视图中绘制移动路径
    private void OnDrawGizmos()
    {
        if (isMoving)
        {
            // 绘制路径
            Gizmos.color = pathColor;
            Vector3 startPos = transform.position;
            Vector3 endPos = targetPosition;
            
            // 绘制路径线
            Gizmos.DrawLine(startPos, endPos);
            
            // 绘制路径宽度
            Vector3 perpendicular = Vector3.Cross(endPos - startPos, Vector3.forward).normalized * pathWidth;
            Gizmos.DrawLine(startPos + perpendicular, endPos + perpendicular);
            Gizmos.DrawLine(startPos - perpendicular, endPos - perpendicular);
            
            // 绘制目标点
            Gizmos.color = targetColor;
            Gizmos.DrawWireSphere(targetPosition, targetRadius);
            
            // 绘制目标点十字
            float crossSize = targetRadius * 0.5f;
            Gizmos.DrawLine(targetPosition + Vector3.right * crossSize, targetPosition - Vector3.right * crossSize);
            Gizmos.DrawLine(targetPosition + Vector3.up * crossSize, targetPosition - Vector3.up * crossSize);
        }
    }
} 