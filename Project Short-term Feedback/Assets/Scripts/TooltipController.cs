using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 控制技能提示面板的显示和内容
/// </summary>
public class TooltipController : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private TextMeshProUGUI titleText; // 技能名称
    [SerializeField] private TextMeshProUGUI descriptionText; // 技能描述
    [SerializeField] private TextMeshProUGUI shortcutText; // 快捷键提示

    [Header("设置")]
    [SerializeField] private float followCursorOffset = 20f; // 面板跟随鼠标的偏移量
    [SerializeField] private bool followCursor = true; // 是否跟随鼠标

    private RectTransform rectTransform;
    private Canvas canvas;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        
        // 默认隐藏提示面板
        gameObject.SetActive(false);
    }

    // 设置提示内容
    public void SetContent(string title, string description, string shortcut)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
        
        if (descriptionText != null)
        {
            descriptionText.text = description;
        }
        
        if (shortcutText != null)
        {
            shortcutText.text = shortcut;
        }
    }

    // 更新位置跟随鼠标
    private void Update()
    {
        if (followCursor && gameObject.activeSelf)
        {
            UpdatePosition();
        }
    }

    // 更新提示面板位置
    private void UpdatePosition()
    {
        // 获取鼠标位置
        Vector2 mousePosition = Input.mousePosition;
        
        // 转换为Canvas中的位置
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // 调整位置，使提示框不超出屏幕边界
            Vector2 position = mousePosition + new Vector2(followCursorOffset, -followCursorOffset);
            
            // 计算提示框尺寸
            Vector2 tooltipSize = rectTransform.sizeDelta * canvas.scaleFactor;
            
            // 确保提示框不会超出屏幕右侧
            if (position.x + tooltipSize.x > Screen.width)
            {
                position.x = mousePosition.x - tooltipSize.x - followCursorOffset;
            }
            
            // 确保提示框不会超出屏幕底部
            if (position.y - tooltipSize.y < 0)
            {
                position.y = mousePosition.y + tooltipSize.y + followCursorOffset;
            }
            
            rectTransform.position = position;
        }
        else if (canvas.renderMode == RenderMode.ScreenSpaceCamera)
        {
            Vector2 position;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                mousePosition + new Vector2(followCursorOffset, -followCursorOffset),
                canvas.worldCamera,
                out position
            );
            
            rectTransform.localPosition = position;
        }
    }

    // 显示提示面板
    public void Show()
    {
        gameObject.SetActive(true);
        if (followCursor)
        {
            UpdatePosition();
        }
    }

    // 隐藏提示面板
    public void Hide()
    {
        gameObject.SetActive(false);
    }
} 