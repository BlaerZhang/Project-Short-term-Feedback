using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// 资源类型枚举，方便未来扩展
/// </summary>
public enum ResourceType
{
    Energy,   // 精力值
    Oxygen,   // 氧气值
    Pressure  // 压力值
}

/// <summary>
/// 资源管理器，负责管理游戏中的资源系统
/// </summary>
public class ResourceManager : MonoBehaviour
{
    // 单例实例
    public static ResourceManager Instance { get; private set; }

    [Header("精力值设置")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float initialEnergy = 100f;

    [Header("氧气值设置")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float initialOxygen = 100f;

    [Header("压力值设置")]
    [SerializeField] private float maxPressure = 100f;
    [SerializeField] private float initialPressure = 0f;

    [Title("当前资源值")]
    [ReadOnly][SerializeField] private float currentEnergy;
    [ReadOnly][SerializeField] private float currentOxygen;
    [ReadOnly][SerializeField] private float currentPressure;

    // 资源值变化事件
    public event Action<ResourceType, float, float> OnResourceChanged;

    // 资源耗尽事件
    public event Action<ResourceType> OnResourceDepleted;

    // 资源不足事件
    public event Action<ResourceType, float, float> OnResourceInsufficient;

    private void Awake()
    {
        // 单例设置
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 初始化资源值
        ResetResources();
    }

    /// <summary>
    /// 重置所有资源值为初始值
    /// </summary>
    public void ResetResources()
    {
        currentEnergy = initialEnergy;
        currentOxygen = initialOxygen;
        currentPressure = initialPressure;
    }

    /// <summary>
    /// 更改指定资源的值
    /// </summary>
    /// <param name="type">资源类型</param>
    /// <param name="amount">变化量（正数增加，负数减少）</param>
    /// <returns>是否成功修改资源值</returns>
    public bool ChangeResource(ResourceType type, float amount)
    {
        float currentValue = GetResourceValue(type);
        float maxValue = GetResourceMaxValue(type);
        float newValue = Mathf.Clamp(currentValue + amount, 0f, maxValue);

        // 检查是否有足够的资源
        if (amount < 0 && currentValue + amount < 0)
        {
            // 资源不足
            OnResourceInsufficient?.Invoke(type, currentValue, Mathf.Abs(amount));
            Debug.Log($"资源不足: {type}，当前: {currentValue}, 需要: {Mathf.Abs(amount)}");
            return false;
        }

        // 更新资源值
        SetResourceValue(type, newValue);

        // 触发资源变化事件
        OnResourceChanged?.Invoke(type, newValue, amount);

        // 检查资源是否耗尽
        if (newValue <= 0)
        {
            OnResourceDepleted?.Invoke(type);
            Debug.Log($"资源耗尽: {type}");
        }

        return true;
    }

    /// <summary>
    /// 获取指定资源的当前值
    /// </summary>
    public float GetResourceValue(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Energy:
                return currentEnergy;
            case ResourceType.Oxygen:
                return currentOxygen;
            case ResourceType.Pressure:
                return currentPressure;
            default:
                Debug.LogError($"未知资源类型: {type}");
                return 0f;
        }
    }

    /// <summary>
    /// 获取指定资源的最大值
    /// </summary>
    public float GetResourceMaxValue(ResourceType type)
    {
        switch (type)
        {
            case ResourceType.Energy:
                return maxEnergy;
            case ResourceType.Oxygen:
                return maxOxygen;
            case ResourceType.Pressure:
                return maxPressure;
            default:
                Debug.LogError($"未知资源类型: {type}");
                return 0f;
        }
    }

    /// <summary>
    /// 设置指定资源的当前值
    /// </summary>
    private void SetResourceValue(ResourceType type, float value)
    {
        switch (type)
        {
            case ResourceType.Energy:
                currentEnergy = value;
                break;
            case ResourceType.Oxygen:
                currentOxygen = value;
                break;
            case ResourceType.Pressure:
                currentPressure = value;
                break;
            default:
                Debug.LogError($"未知资源类型: {type}");
                break;
        }
    }

    /// <summary>
    /// 获取指定资源的百分比（0-1之间）
    /// </summary>
    public float GetResourcePercentage(ResourceType type)
    {
        float current = GetResourceValue(type);
        float max = GetResourceMaxValue(type);
        return max > 0 ? current / max : 0;
    }

    /// <summary>
    /// 批量修改多种资源
    /// </summary>
    /// <param name="changes">资源类型与变化量的字典</param>
    /// <returns>是否所有资源都成功修改</returns>
    public bool ChangeMultipleResources(Dictionary<ResourceType, float> changes)
    {
        bool allSuccessful = true;
        
        foreach (var change in changes)
        {
            bool success = ChangeResource(change.Key, change.Value);
            if (!success)
            {
                allSuccessful = false;
            }
        }
        
        return allSuccessful;
    }

    // 添加公共版本的SetResourceValue方法供开发者工具使用
    #if UNITY_EDITOR || DEVELOPMENT_BUILD
    /// <summary>
    /// 直接设置资源值（仅用于开发调试）
    /// </summary>
    public void DebugSetResourceValue(ResourceType type, float value)
    {
        // 确保值在有效范围内
        float maxValue = GetResourceMaxValue(type);
        value = Mathf.Clamp(value, 0, maxValue);
        
        // 设置资源值
        SetResourceValue(type, value);
        
        // 触发资源变化事件
        OnResourceChanged?.Invoke(type, value, maxValue);
        
        Debug.Log($"ResourceManager: 调试功能 - {type}设置为 {value}/{maxValue}");
    }
    #endif
} 