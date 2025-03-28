using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 资源效果：定义某个资源的变化量
/// </summary>
[Serializable]
public class ResourceEffect
{
    public ResourceType resourceType;
    public float amount;

    public ResourceEffect(ResourceType type, float value)
    {
        resourceType = type;
        amount = value;
    }
}

/// <summary>
/// 速度动作配置：定义特定速度档位下动作的资源效果
/// </summary>
[Serializable]
public class SpeedActionConfig
{
    [Range(1, 3)]
    public int speedLevel = 1;
    public List<ResourceEffect> resourceEffects = new List<ResourceEffect>();
}

/// <summary>
/// 动作资源配置：整合动作类型与不同速度下的效果
/// </summary>
[Serializable]
public class ActionConfig
{
    public MoveActionType actionType;
    public List<SpeedActionConfig> speedConfigs = new List<SpeedActionConfig>();

    // 获取指定速度档位的资源效果
    public Dictionary<ResourceType, float> GetResourceEffects(int speedLevel)
    {
        Dictionary<ResourceType, float> effects = new Dictionary<ResourceType, float>();

        // 查找匹配的速度配置
        SpeedActionConfig config = speedConfigs.Find(c => c.speedLevel == speedLevel);
        
        if (config != null)
        {
            // 添加所有资源效果到字典
            foreach (ResourceEffect effect in config.resourceEffects)
            {
                effects[effect.resourceType] = effect.amount;
            }
        }

        return effects;
    }
}

/// <summary>
/// 全局动作资源配置，所有动作类型的资源效果都在这里配置
/// </summary>
[CreateAssetMenu(fileName = "ActionResourceConfig", menuName = "Game/Action Resource Config")]
public class ActionResourceConfig : ScriptableObject
{
    public List<ActionConfig> actions = new List<ActionConfig>();

    /// <summary>
    /// 获取指定动作类型和速度档位的资源效果
    /// </summary>
    public Dictionary<ResourceType, float> GetResourceEffects(MoveActionType actionType, int speedLevel)
    {
        // 查找匹配的动作配置
        ActionConfig actionConfig = actions.Find(a => a.actionType == actionType);
        
        if (actionConfig != null)
        {
            return actionConfig.GetResourceEffects(speedLevel);
        }

        // 如果没有找到匹配的配置，返回空字典
        return new Dictionary<ResourceType, float>();
    }
} 