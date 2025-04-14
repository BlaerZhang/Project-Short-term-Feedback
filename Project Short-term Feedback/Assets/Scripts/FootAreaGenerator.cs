using UnityEngine;
using System.Collections.Generic;

public class FootAreaGenerator : MonoBehaviour
{
    [Header("区域设置")]
    [SerializeField] private float areaWidth = 60f;    // 区域宽度
    [SerializeField] private float areaHeight = 30f;   // 区域高度
    [SerializeField] private Vector3 areaPosition = Vector3.zero; // 区域位置
    [SerializeField] private float cellSize = 1f;      // 单元格大小

    [Header("生成设置")]
    [SerializeField] private int redAreaCount = 2;     // 红色区域数量
    [SerializeField] private int yellowAreaCount = 2;  // 黄色区域数量
    [SerializeField] private int greenAreaCount = 2;   // 绿色区域数量
    [SerializeField] private float areaSizeRatio = 0.9f; // 区域大小比例（相对于总面积的百分比）

    [Header("材质设置")]
    [SerializeField] private Material redMaterial;     // 红色材质
    [SerializeField] private Material yellowMaterial;  // 黄色材质
    [SerializeField] private Material greenMaterial;   // 绿色材质
    [SerializeField] private Material emptyMaterial;   // 空单元格材质

    private List<GameObject> generatedCells = new List<GameObject>(); // 存储生成的单元格
    private Dictionary<int, Material> colorMaterials = new Dictionary<int, Material>(); // 存储颜色索引对应的材质

    private void Start()
    {
        // 初始化颜色材质映射
        colorMaterials[1] = redMaterial;
        colorMaterials[2] = yellowMaterial;
        colorMaterials[3] = greenMaterial;
        
        GenerateFootArea();
    }

    public void GenerateFootArea()
    {
        // 清除之前生成的单元格
        ClearGeneratedCells();

        // 计算网格尺寸
        int gridWidth = Mathf.FloorToInt(areaWidth / cellSize);
        int gridHeight = Mathf.FloorToInt(areaHeight / cellSize);

        // 创建网格
        bool[,] grid = new bool[gridWidth, gridHeight];
        int[,] colorGrid = new int[gridWidth, gridHeight]; // 用于记录每个单元格的颜色

        // 计算每个区域的目标大小
        int totalCells = gridWidth * gridHeight;
        int targetCellsPerColor = Mathf.FloorToInt(totalCells * areaSizeRatio / 3);

        // 生成红色区域
        for (int i = 0; i < redAreaCount; i++)
        {
            GenerateArea(grid, colorGrid, targetCellsPerColor / redAreaCount, 1); // 1表示红色
        }

        // 生成黄色区域
        for (int i = 0; i < yellowAreaCount; i++)
        {
            GenerateArea(grid, colorGrid, targetCellsPerColor / yellowAreaCount, 2); // 2表示黄色
        }

        // 生成绿色区域
        for (int i = 0; i < greenAreaCount; i++)
        {
            GenerateArea(grid, colorGrid, targetCellsPerColor / greenAreaCount, 3); // 3表示绿色
        }

        // 可视化网格
        VisualizeGrid(grid, colorGrid);
    }

    private void GenerateArea(bool[,] grid, int[,] colorGrid, int targetCellCount, int colorIndex)
    {
        int gridWidth = grid.GetLength(0);
        int gridHeight = grid.GetLength(1);

        // 随机选择一个起始点
        int startX = Random.Range(0, gridWidth);
        int startY = Random.Range(0, gridHeight);

        // 使用洪水填充算法生成区域
        Queue<Vector2Int> queue = new Queue<Vector2Int>();
        queue.Enqueue(new Vector2Int(startX, startY));
        int filledCells = 0;

        // 添加噪声参数
        float noiseScale = 0.1f; // 噪声缩放
        float noiseThreshold = 0.3f; // 噪声阈值
        float noiseOffset = Random.Range(0f, 1000f); // 随机噪声偏移

        while (queue.Count > 0 && filledCells < targetCellCount)
        {
            Vector2Int current = queue.Dequeue();
            if (grid[current.x, current.y]) continue;

            // 使用柏林噪声来决定是否填充当前单元格
            float noise = Mathf.PerlinNoise(
                (current.x + noiseOffset) * noiseScale,
                (current.y + noiseOffset) * noiseScale
            );

            // 根据噪声值决定是否填充
            if (noise < noiseThreshold) continue;

            grid[current.x, current.y] = true;
            colorGrid[current.x, current.y] = colorIndex;
            filledCells++;

            // 添加相邻单元格，增加随机性
            List<Vector2Int> neighbors = new List<Vector2Int>();
            if (current.x > 0 && !grid[current.x - 1, current.y])
                neighbors.Add(new Vector2Int(current.x - 1, current.y));
            if (current.x < gridWidth - 1 && !grid[current.x + 1, current.y])
                neighbors.Add(new Vector2Int(current.x + 1, current.y));
            if (current.y > 0 && !grid[current.x, current.y - 1])
                neighbors.Add(new Vector2Int(current.x, current.y - 1));
            if (current.y < gridHeight - 1 && !grid[current.x, current.y + 1])
                neighbors.Add(new Vector2Int(current.x, current.y + 1));

            // 随机打乱邻居顺序
            for (int i = 0; i < neighbors.Count; i++)
            {
                int randomIndex = Random.Range(i, neighbors.Count);
                Vector2Int temp = neighbors[i];
                neighbors[i] = neighbors[randomIndex];
                neighbors[randomIndex] = temp;
            }

            // 添加打乱后的邻居
            foreach (Vector2Int neighbor in neighbors)
            {
                queue.Enqueue(neighbor);
            }
        }
    }

    private void VisualizeGrid(bool[,] grid, int[,] colorGrid)
    {
        int gridWidth = grid.GetLength(0);
        int gridHeight = grid.GetLength(1);

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grid[x, y])
                {
                    // 创建单元格
                    GameObject cell = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cell.transform.parent = transform;
                    cell.transform.localScale = new Vector3(cellSize, 0.1f, cellSize);
                    cell.transform.position = new Vector3(
                        areaPosition.x + x * cellSize - areaWidth / 2 + cellSize / 2,
                        areaPosition.y,
                        areaPosition.z + y * cellSize - areaHeight / 2 + cellSize / 2
                    );

                    // 根据颜色索引分配材质
                    int colorIndex = colorGrid[x, y];
                    if (colorMaterials.ContainsKey(colorIndex))
                    {
                        cell.GetComponent<Renderer>().material = colorMaterials[colorIndex];
                    }

                    generatedCells.Add(cell);
                }
            }
        }
    }

    // 轮换颜色
    public void RotateColors()
    {
        Debug.Log("FootAreaGenerator: 开始轮换颜色");
        int changedCount = 0;
        
        // 遍历所有生成的单元格
        foreach (GameObject cell in generatedCells)
        {
            Renderer renderer = cell.GetComponent<Renderer>();
            if (renderer != null)
            {
                Material currentMaterial = renderer.material;
                Material originalMaterial = currentMaterial;
                bool changed = false;
                
                // 红色 → 绿色
                if (currentMaterial.name.Contains(redMaterial.name))
                {
                    renderer.material = greenMaterial;
                    changed = true;
                    changedCount++;
                }
                // 黄色 → 红色
                else if (currentMaterial.name.Contains(yellowMaterial.name))
                {
                    renderer.material = redMaterial;
                    changed = true;
                    changedCount++;
                }
                // 绿色 → 黄色
                else if (currentMaterial.name.Contains(greenMaterial.name))
                {
                    renderer.material = yellowMaterial;
                    changed = true;
                    changedCount++;
                }
                
                if (changed)
                {
                    Debug.LogFormat("材质从 {0} 改变为 {1}", originalMaterial.name, renderer.material.name);
                }
            }
        }
        
        Debug.LogFormat("FootAreaGenerator: 颜色轮换完成，共修改了 {0} 个单元格的颜色", changedCount);
    }

    private void ClearGeneratedCells()
    {
        foreach (GameObject cell in generatedCells)
        {
            Destroy(cell);
        }
        generatedCells.Clear();
    }

    private void OnDrawGizmos()
    {
        // 绘制区域边界
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(areaPosition, new Vector3(areaWidth, 0.1f, areaHeight));
    }
} 