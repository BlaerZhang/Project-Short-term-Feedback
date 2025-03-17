using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArcIndicator : MonoBehaviour
{
    [SerializeField] private LineRenderer arcLineRenderer;  // 用于绘制圆弧的线渲染器
    [SerializeField] private int arcResolution = 50;        // 圆弧分辨率
    [SerializeField] private float arcWidth = 0.1f;         // 圆弧宽度
    [SerializeField] private Material arcMaterial;          // 圆弧材质
    [SerializeField] private Color arcColor = new Color(0.2f, 0.8f, 1f, 0.5f); // 圆弧颜色

    private void Awake()
    {
        // 确保有LineRenderer组件
        if (arcLineRenderer == null)
        {
            arcLineRenderer = GetComponent<LineRenderer>();
            if (arcLineRenderer == null)
            {
                arcLineRenderer = gameObject.AddComponent<LineRenderer>();
            }
        }

        // 初始化LineRenderer
        arcLineRenderer.startWidth = arcWidth;
        arcLineRenderer.endWidth = arcWidth;
        arcLineRenderer.positionCount = 0;
        
        // 设置材质
        if (arcMaterial != null)
        {
            arcLineRenderer.material = arcMaterial;
        }
        
        // 设置颜色
        arcLineRenderer.startColor = arcColor;
        arcLineRenderer.endColor = arcColor;
    }

    // 更新圆弧显示
    public void UpdateArc(Vector3 center, Vector3 direction, float radius, float totalAngle)
    {
        // 确保方向向量是单位向量并且在水平面上
        direction.y = 0;
        direction.Normalize();

        // 计算起始和结束角度
        float halfAngle = totalAngle * 0.5f;
        float startAngle = -halfAngle;
        float endAngle = halfAngle;

        // 计算圆弧上的点
        List<Vector3> arcPoints = new List<Vector3>();
        
        // 添加中心点（用于绘制方向指示线）
        arcPoints.Add(center);
        
        // 添加起始点（确保圆弧闭合）
        Quaternion startRotation = Quaternion.AngleAxis(startAngle, Vector3.up);
        Vector3 startDirection = startRotation * direction;
        arcPoints.Add(center + startDirection * radius);
        
        // 从起点到终点生成圆弧
        for (int i = 0; i <= arcResolution; i++)
        {
            float t = i / (float)arcResolution;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            
            // 计算当前方向向量
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 currentDirection = rotation * direction;
            
            // 计算圆弧上的点
            Vector3 point = center + currentDirection * radius;
            
            // 添加到点列表
            arcPoints.Add(point);
        }

        // 添加终点（确保圆弧闭合）
        Quaternion endRotation = Quaternion.AngleAxis(endAngle, Vector3.up);
        Vector3 endDirection = endRotation * direction;
        arcPoints.Add(center + endDirection * radius);
        
        // 回到中心点（完成方向指示线）
        arcPoints.Add(center);
        
        // 添加当前朝向的指示线终点
        arcPoints.Add(center + direction * radius);

        // 更新LineRenderer
        arcLineRenderer.positionCount = arcPoints.Count;
        for (int i = 0; i < arcPoints.Count; i++)
        {
            arcLineRenderer.SetPosition(i, arcPoints[i]);
        }

        // 设置线条宽度渐变
        arcLineRenderer.widthCurve = new AnimationCurve(
            new Keyframe(0, arcWidth * 0.5f),     // 中心点
            new Keyframe(0.1f, arcWidth),         // 圆弧
            new Keyframe(0.9f, arcWidth),         // 圆弧
            new Keyframe(1, arcWidth * 0.5f)      // 指示线末端
        );
    }

    // 隐藏圆弧
    public void HideArc()
    {
        arcLineRenderer.positionCount = 0;
    }
} 