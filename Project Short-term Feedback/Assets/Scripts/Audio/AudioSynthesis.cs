using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 简单的音效合成器，用于生成临时音效
/// </summary>
public class AudioSynthesis : MonoBehaviour
{
    private static AudioSynthesis instance;

    // 只允许通过CreateJumpSound方法访问
    private AudioSynthesis() { }

    /// <summary>
    /// 创建跳跃音效
    /// </summary>
    public static AudioClip CreateJumpSound()
    {
        float sampleRate = 44100;
        int sampleCount = Mathf.FloorToInt(sampleRate * 0.5f); // 0.5秒的音效

        // 创建一个新的AudioClip
        AudioClip clip = AudioClip.Create("JumpSound", sampleCount, 1, Mathf.FloorToInt(sampleRate), false);

        // 合成音效数据
        float[] samples = new float[sampleCount];
        
        // 设置初始频率和递减系数
        float initialFrequency = 440f;
        float frequencyDecay = 0.998f;
        float amplitude = 0.5f;
        float amplitudeDecay = 0.996f;
        
        // 当前频率
        float currentFrequency = initialFrequency;

        // 生成波形
        for (int i = 0; i < sampleCount; i++)
        {
            // 计算波形（使用正弦波模拟跳跃音效）
            samples[i] = amplitude * Mathf.Sin(2 * Mathf.PI * currentFrequency * i / sampleRate);
            
            // 每帧减小振幅和频率，产生衰减效果
            amplitude *= amplitudeDecay;
            currentFrequency *= frequencyDecay;
        }

        // 设置音效数据
        clip.SetData(samples, 0);
        
        return clip;
    }

    /// <summary>
    /// 创建脚步声音效
    /// </summary>
    public static AudioClip CreateFootstepsSound()
    {
        float sampleRate = 44100;
        int sampleCount = Mathf.FloorToInt(sampleRate * 0.3f); // 0.3秒的音效
        int stepCount = 4; // 步数

        // 创建一个新的AudioClip，长度为单步时长 * 步数
        AudioClip clip = AudioClip.Create("FootstepsSound", sampleCount * stepCount, 1, Mathf.FloorToInt(sampleRate), true);

        // 合成音效数据
        float[] samples = new float[sampleCount * stepCount];
        
        for (int step = 0; step < stepCount; step++)
        {
            // 每一步的起始索引
            int startIndex = step * sampleCount;
            
            // 设置初始振幅和衰减系数
            float amplitude = 0.3f;
            float amplitudeDecay = 0.98f;
            
            // 为当前步生成波形
            for (int i = 0; i < sampleCount / 2; i++) // 只生成半个时长，以留出空白间隔
            {
                // 使用噪声模拟脚步声
                float noise = (Random.value * 2 - 1) * amplitude;
                samples[startIndex + i] = noise;
                
                // 每帧减小振幅，产生衰减效果
                amplitude *= amplitudeDecay;
            }
        }

        // 设置音效数据
        clip.SetData(samples, 0);
        
        return clip;
    }
} 