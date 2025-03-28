using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 用于控制速度分段显示条的状态
/// </summary>
public class SpeedSegmentBar : MonoBehaviour
{
    [Tooltip("分段显示的图标数组，顺序从低速到高速")]
    [SerializeField] private List<GameObject> speedSegments = new List<GameObject>();
    
    [Tooltip("每个分段显示的颜色数组，顺序与speedSegments一致")]
    [SerializeField] private List<Color> segmentColors = new List<Color>();
    
    [Tooltip("未激活状态的颜色")]
    [SerializeField] private Color inactiveColor = Color.gray;
    
    [Tooltip("是否自动从子物体获取分段")]
    [SerializeField] private bool autoGetSegments = true;
    
    // 玩家控制器引用
    private PlayerController playerController;
    
    // 存储每个分段的Image组件
    private List<Image> segmentImages = new List<Image>();
    
    // 存储分段对应的速度阈值
    private List<float> speedThresholds = new List<float>();

    private void Awake()
    {
        if (autoGetSegments)
        {
            // 自动从子物体获取分段
            speedSegments.Clear();
            for (int i = 0; i < transform.childCount; i++)
            {
                speedSegments.Add(transform.GetChild(i).gameObject);
            }
        }
        
        // 获取每个分段的Image组件
        foreach (GameObject segment in speedSegments)
        {
            Image image = segment.GetComponent<Image>();
            if (image != null)
            {
                segmentImages.Add(image);
            }
            else
            {
                Debug.LogWarning($"SpeedSegmentBar: 分段 {segment.name} 缺少Image组件！");
            }
        }
        
        // 如果未设置颜色，使用当前图标颜色
        if (segmentColors.Count < segmentImages.Count)
        {
            segmentColors.Clear();
            foreach (Image image in segmentImages)
            {
                segmentColors.Add(image.color);
            }
        }
    }

    private void Start()
    {
        // 获取PlayerController引用
        playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("SpeedSegmentBar: 未找到PlayerController!");
            return;
        }
        
        // 计算每个分段对应的速度阈值
        CalculateSpeedThresholds();
        
        // 初始更新UI
        UpdateSpeedBar(playerController.GetCurrentSpeed());
    }

    private void Update()
    {
        if (playerController != null)
        {
            // 每帧更新速度条
            UpdateSpeedBar(playerController.GetCurrentSpeed());
        }
    }

    // 计算速度阈值
    private void CalculateSpeedThresholds()
    {
        if (playerController == null)
            return;
            
        float minSpeed = playerController.GetMinSpeed();
        float maxSpeed = playerController.GetMaxSpeed();
        int segments = speedSegments.Count;
        
        // 清空并重新计算阈值
        speedThresholds.Clear();
        
        if (segments <= 1)
        {
            // 如果只有一个分段，设置最小速度为阈值
            speedThresholds.Add(minSpeed);
            return;
        }
        
        // 均匀分配速度区间
        float speedRange = maxSpeed - minSpeed;
        float segmentRange = speedRange / segments;
        
        for (int i = 0; i < segments; i++)
        {
            // 计算每个分段的最小速度阈值
            float threshold = minSpeed + segmentRange * i;
            speedThresholds.Add(threshold);
        }
    }

    // 更新速度条显示
    public void UpdateSpeedBar(float currentSpeed)
    {
        if (segmentImages.Count == 0 || speedThresholds.Count == 0)
            return;
            
        // 更新每个分段的显示状态
        for (int i = 0; i < segmentImages.Count; i++)
        {
            // 如果当前速度大于等于该分段的阈值，激活该分段
            if (currentSpeed >= speedThresholds[i])
            {
                // 使用原始颜色
                segmentImages[i].color = segmentColors[i];
            }
            else
            {
                // 使用未激活颜色
                segmentImages[i].color = inactiveColor;
            }
        }
    }
    
    // 外部调用更新UI
    public void ForceUpdateUI(float speed)
    {
        UpdateSpeedBar(speed);
    }
} 