using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

public class SpriteRandomizer : EditorWindow
{
    private GameObject parentObject;
    private List<SpriteProbability> spriteProbabilities = new List<SpriteProbability>();
    private Vector2 scrollPosition;
    private SpriteRandomizerConfig config;

    [System.Serializable]
    public class SpriteProbability
    {
        public Sprite sprite;
        [Range(0, 100)]
        public float probability = 50f;
    }

    [MenuItem("Tools/Sprite Randomizer")]
    public static void ShowWindow()
    {
        GetWindow<SpriteRandomizer>("Sprite Randomizer");
    }

    private void OnEnable()
    {
        LoadConfig();
    }

    private void OnDisable()
    {
        SaveConfig();
    }

    private void LoadConfig()
    {
        // 尝试加载现有配置
        string configPath = "Assets/Resources/SpriteRandomizerConfig.asset";
        config = AssetDatabase.LoadAssetAtPath<SpriteRandomizerConfig>(configPath);

        // 如果配置不存在，创建新的
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SpriteRandomizerConfig>();
            
            // 确保Resources文件夹存在
            string resourcesPath = "Assets/Resources";
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
            }
            
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
        }

        // 加载保存的数据
        parentObject = config.parentObject;
        spriteProbabilities = new List<SpriteProbability>(config.spriteProbabilities);
    }

    private void SaveConfig()
    {
        if (config == null) return;

        // 保存当前数据
        config.parentObject = parentObject;
        config.spriteProbabilities = new List<SpriteProbability>(spriteProbabilities);

        // 标记为已修改
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
    }

    private void OnGUI()
    {
        GUILayout.Label("Sprite Randomizer", EditorStyles.boldLabel);

        // 父物体选择
        GameObject newParentObject = (GameObject)EditorGUILayout.ObjectField("Parent Object", parentObject, typeof(GameObject), true);
        if (newParentObject != parentObject)
        {
            parentObject = newParentObject;
            SaveConfig();
        }

        // Sprite概率配置
        EditorGUILayout.LabelField("Sprite Probabilities", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < spriteProbabilities.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            Sprite newSprite = (Sprite)EditorGUILayout.ObjectField(spriteProbabilities[i].sprite, typeof(Sprite), false);
            float newProbability = EditorGUILayout.Slider(spriteProbabilities[i].probability, 0f, 100f);
            
            if (newSprite != spriteProbabilities[i].sprite || newProbability != spriteProbabilities[i].probability)
            {
                spriteProbabilities[i].sprite = newSprite;
                spriteProbabilities[i].probability = newProbability;
                SaveConfig();
            }
            
            if (GUILayout.Button("Remove"))
            {
                spriteProbabilities.RemoveAt(i);
                SaveConfig();
                i--;
            }
            
            EditorGUILayout.EndHorizontal();
        }
        
        EditorGUILayout.EndScrollView();

        if (GUILayout.Button("Add New Sprite"))
        {
            spriteProbabilities.Add(new SpriteProbability());
            SaveConfig();
        }

        EditorGUILayout.Space();

        // 生成按钮
        if (GUILayout.Button("Randomize Sprites"))
        {
            RandomizeSprites();
        }
    }

    private void RandomizeSprites()
    {
        if (parentObject == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a parent object", "OK");
            return;
        }

        if (spriteProbabilities.Count == 0)
        {
            EditorUtility.DisplayDialog("Error", "Please add at least one sprite", "OK");
            return;
        }

        // 计算总概率
        float totalProbability = 0f;
        foreach (var sp in spriteProbabilities)
        {
            totalProbability += sp.probability;
        }

        if (totalProbability <= 0)
        {
            EditorUtility.DisplayDialog("Error", "Total probability must be greater than 0", "OK");
            return;
        }

        // 获取所有子物体
        SpriteRenderer[] spriteRenderers = parentObject.GetComponentsInChildren<SpriteRenderer>();

        foreach (var renderer in spriteRenderers)
        {
            float randomValue = Random.Range(0f, totalProbability);
            float cumulativeProbability = 0f;

            foreach (var sp in spriteProbabilities)
            {
                cumulativeProbability += sp.probability;
                if (randomValue <= cumulativeProbability)
                {
                    renderer.sprite = sp.sprite;
                    break;
                }
            }
        }

        EditorUtility.DisplayDialog("Success", "Sprites have been randomized!", "OK");
    }
}

// 配置保存类
public class SpriteRandomizerConfig : ScriptableObject
{
    public GameObject parentObject;
    public List<SpriteRandomizer.SpriteProbability> spriteProbabilities = new List<SpriteRandomizer.SpriteProbability>();
} 