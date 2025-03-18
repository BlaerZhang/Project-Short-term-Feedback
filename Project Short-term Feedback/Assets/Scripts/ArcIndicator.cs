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
    [SerializeField] private float heightOffset = 0.05f;    // 高度偏移，避免z-fighting
    
    private MeshFilter meshFilter;        // 用于扇形填充的网格过滤器
    private MeshRenderer meshRenderer;    // 用于扇形填充的网格渲染器
    private Mesh arcMesh;                // 扇形网格
    private float currentAlpha = 1f;     // 当前透明度
    private Coroutine fadeCoroutine;     // 淡入淡出协程

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

        // 创建用于填充的子物体
        GameObject fillObject = new GameObject("ArcFill");
        fillObject.transform.SetParent(transform, false);
        
        // 添加并初始化网格组件
        meshFilter = fillObject.AddComponent<MeshFilter>();
        meshRenderer = fillObject.AddComponent<MeshRenderer>();
        
        // 创建新的网格
        arcMesh = new Mesh();
        arcMesh.name = "ArcMesh";
        meshFilter.mesh = arcMesh;
        
        // 设置填充材质
        if (arcMaterial != null)
        {
            Material fillMaterial = new Material(arcMaterial);
            fillMaterial.color = new Color(arcColor.r, arcColor.g, arcColor.b, arcColor.a * 0.3f);
            meshRenderer.material = fillMaterial;
        }

        // 设置渲染顺序，确保填充在线条下方
        meshRenderer.sortingOrder = arcLineRenderer.sortingOrder - 1;
    }

    // 更新圆弧显示
    public void UpdateArc(Vector3 center, Vector3 direction, float radius, float totalAngle)
    {
        // 添加高度偏移
        center.y += heightOffset;
        
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

        // 更新填充网格
        UpdateArcMesh(center, direction, radius, startAngle, endAngle);
        
        // 确保显示
        SetVisibility(true);
    }

    // 更新扇形填充网格
    private void UpdateArcMesh(Vector3 center, Vector3 direction, float radius, float startAngle, float endAngle)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        // 添加中心点
        vertices.Add(transform.InverseTransformPoint(center));
        
        // 生成圆弧上的点
        for (int i = 0; i <= arcResolution; i++)
        {
            float t = i / (float)arcResolution;
            float angle = Mathf.Lerp(startAngle, endAngle, t);
            
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 currentDirection = rotation * direction;
            Vector3 point = center + currentDirection * radius;
            
            vertices.Add(transform.InverseTransformPoint(point));
            
            // 添加三角形（除了最后一个点）
            if (i < arcResolution)
            {
                triangles.Add(0); // 中心点
                triangles.Add(i + 1);
                triangles.Add(i + 2);
            }
        }
        
        // 清除旧的网格数据
        arcMesh.Clear();
        
        // 设置新的网格数据
        arcMesh.vertices = vertices.ToArray();
        arcMesh.triangles = triangles.ToArray();
        
        // 重新计算网格的法线
        arcMesh.RecalculateNormals();
        arcMesh.RecalculateBounds();
    }

    // 设置可见性
    private void SetVisibility(bool visible)
    {
        Color lineColor = arcLineRenderer.startColor;
        Color meshColor = meshRenderer.material.color;
        
        lineColor.a = visible ? arcColor.a : 0f;
        meshColor.a = visible ? arcColor.a * 0.3f : 0f;
        
        arcLineRenderer.startColor = lineColor;
        arcLineRenderer.endColor = lineColor;
        meshRenderer.material.color = meshColor;
    }

    // 淡出效果
    public void FadeOut(float duration = 0.3f)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeCoroutine(false, duration));
    }

    // 淡入效果
    public void FadeIn(float duration = 0.3f)
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }
        fadeCoroutine = StartCoroutine(FadeCoroutine(true, duration));
    }

    // 淡入淡出协程
    private IEnumerator FadeCoroutine(bool fadeIn, float duration)
    {
        float startAlpha = fadeIn ? 0f : arcColor.a;
        float targetAlpha = fadeIn ? arcColor.a : 0f;
        float elapsedTime = 0f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            Color lineColor = arcLineRenderer.startColor;
            Color meshColor = meshRenderer.material.color;
            
            lineColor.a = currentAlpha;
            meshColor.a = currentAlpha * 0.3f;
            
            arcLineRenderer.startColor = lineColor;
            arcLineRenderer.endColor = lineColor;
            meshRenderer.material.color = meshColor;

            yield return null;
        }

        if (!fadeIn)
        {
            HideArc();
        }
    }

    // 隐藏圆弧
    public void HideArc()
    {
        arcLineRenderer.positionCount = 0;
        if (arcMesh != null)
        {
            arcMesh.Clear();
        }
    }

    private void OnDestroy()
    {
        // 清理网格资源
        if (arcMesh != null)
        {
            if (Application.isPlaying)
            {
                Destroy(arcMesh);
            }
            else
            {
                DestroyImmediate(arcMesh);
            }
        }
    }
} 