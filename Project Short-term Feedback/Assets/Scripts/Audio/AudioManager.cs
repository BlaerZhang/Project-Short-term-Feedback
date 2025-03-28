using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 音效管理器，负责管理游戏中的音效播放
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("音效引用")]
    [SerializeField] private AudioClip footstepsSound;
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip backgroundMusic; // 添加背景音乐

    [Header("音效设置")]
    [SerializeField, Range(0f, 1f)] private float footstepsVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float jumpVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float bgmVolume = 0.5f; // 背景音乐音量
    [SerializeField, Range(0.5f, 1.5f)] private float basePitch = 1.0f;
    
    [Header("Pitch设置")]
    [SerializeField, Range(0.1f, 1.0f)] private float minSfxPitch = 0.3f; // 音效最小pitch
    [SerializeField, Range(0.1f, 1.0f)] private float minBgmPitch = 0.5f; // 背景音乐最小pitch

    // 音效播放器
    private AudioSource footstepsSource;
    private AudioSource jumpSource;
    private AudioSource bgmSource; // 背景音乐播放器
    private AudioSource sfxSource; // 通用音效播放器

    // 时间缩放前的默认音调
    private float defaultFootstepsPitch;
    private float defaultJumpPitch;
    private float defaultBgmPitch;

    private void Awake()
    {
        // 单例模式设置
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // 初始化音频源
        InitAudioSources();
    }

    /// <summary>
    /// 初始化音频源
    /// </summary>
    private void InitAudioSources()
    {
        Debug.Log("正在初始化音频源...");

        // 创建脚步声音频源
        if (footstepsSource == null)
        {
            footstepsSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("已创建脚步声音频源");
        }
        
        // 设置脚步声音频源
        footstepsSource.clip = footstepsSound;
        footstepsSource.volume = footstepsVolume;
        footstepsSource.loop = true;
        footstepsSource.playOnAwake = false;
        footstepsSource.pitch = basePitch;
        defaultFootstepsPitch = basePitch;
        Debug.Log($"脚步声音频源设置完成: 音量={footstepsVolume}, 音调={basePitch}, 循环={true}");

        // 创建跳跃音频源
        if (jumpSource == null)
        {
            jumpSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("已创建跳跃音频源");
        }
        
        // 设置跳跃音频源
        jumpSource.clip = jumpSound;
        jumpSource.volume = jumpVolume;
        jumpSource.loop = false;
        jumpSource.playOnAwake = false;
        jumpSource.pitch = basePitch;
        defaultJumpPitch = basePitch;
        Debug.Log($"跳跃音频源设置完成: 音量={jumpVolume}, 音调={basePitch}, 循环={false}");
        
        // 创建背景音乐音频源
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("已创建背景音乐音频源");
        }
            
        // 设置背景音乐音频源
        bgmSource.clip = backgroundMusic;
        bgmSource.volume = bgmVolume;
        bgmSource.loop = true;
        bgmSource.playOnAwake = true; // 自动播放
        bgmSource.pitch = basePitch;
        defaultBgmPitch = basePitch;
        Debug.Log($"背景音乐音频源设置完成: 音量={bgmVolume}, 音调={basePitch}, 循环={true}");
        
        // 如果有背景音乐，自动播放
        if (backgroundMusic != null && !bgmSource.isPlaying)
        {
            bgmSource.Play();
            Debug.Log("背景音乐已开始播放");
        }

        // 创建通用音效音频源
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("已创建通用音效音频源");
        }
        
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        Debug.Log($"通用音效音频源设置完成: 循环={false}");
        
        // 测试播放音效
        TestPlaySounds();
    }

    /// <summary>
    /// 测试播放所有音效
    /// </summary>
    private void TestPlaySounds()
    {
        StartCoroutine(TestSoundsCoroutine());
    }

    private IEnumerator TestSoundsCoroutine()
    {
        yield return new WaitForSeconds(1.0f);
        
        // 测试播放跳跃音效
        if (jumpSource != null && jumpSound != null)
        {
            Debug.Log("测试播放跳跃音效");
            jumpSource.PlayOneShot(jumpSound);
        }
        else
        {
            Debug.LogWarning("无法测试播放跳跃音效，组件或音频文件缺失");
        }
    }

    /// <summary>
    /// 更新音频音调以适应时间缩放
    /// </summary>
    private void Update()
    {
        // 计算音效的目标音调
        float sfxTargetPitch = basePitch * Mathf.Max(Time.timeScale, 0.01f);
        sfxTargetPitch = Mathf.Max(sfxTargetPitch, minSfxPitch); // 应用最小音效音调

        // 计算背景音乐的目标音调
        float bgmTargetPitch = basePitch * Mathf.Max(Time.timeScale, 0.01f);
        bgmTargetPitch = Mathf.Max(bgmTargetPitch, minBgmPitch); // 应用最小背景音乐音调

        // 更新音效音调
        if (footstepsSource != null && footstepsSource.pitch != sfxTargetPitch)
        {
            footstepsSource.DOPitch(sfxTargetPitch, 0.1f).SetUpdate(true);
        }

        if (jumpSource != null && jumpSource.pitch != sfxTargetPitch)
        {
            jumpSource.DOPitch(sfxTargetPitch, 0.1f).SetUpdate(true);
        }
        
        // 更新背景音乐音调
        if (bgmSource != null && bgmSource.pitch != bgmTargetPitch)
        {
            bgmSource.DOPitch(bgmTargetPitch, 0.1f).SetUpdate(true);
        }
    }

    private void Start()
    {
        // 检查并输出音频状态
        Debug.Log($"音频状态 - 脚步声: {(footstepsSound != null ? "已加载" : "未加载")}, " +
                  $"跳跃音效: {(jumpSound != null ? "已加载" : "未加载")}, " +
                  $"背景音乐: {(backgroundMusic != null ? "已加载" : "未加载")}");
        
        // 检查音频源状态
        Debug.Log($"音频源状态 - 脚步声: {(footstepsSource != null ? "已创建" : "未创建")}, " +
                  $"跳跃音效: {(jumpSource != null ? "已创建" : "未创建")}, " +
                  $"背景音乐: {(bgmSource != null ? "已创建" : "未创建")}");
        
        // 确保音效音量不为0
        if (footstepsSource != null)
            Debug.Log($"脚步声音量: {footstepsSource.volume}, 音调: {footstepsSource.pitch}");
        
        if (jumpSource != null)
            Debug.Log($"跳跃音效音量: {jumpSource.volume}, 音调: {jumpSource.pitch}");
        
        if (bgmSource != null)
            Debug.Log($"背景音乐音量: {bgmSource.volume}, 音调: {bgmSource.pitch}, 是否播放: {bgmSource.isPlaying}");
        
        // 设置音效的音量，确保不为0
        SetSfxVolume(footstepsVolume, jumpVolume);
    }

    /// <summary>
    /// 设置所有音效的音量
    /// </summary>
    public void SetSfxVolume(float footstepsVol, float jumpVol)
    {
        footstepsVolume = Mathf.Clamp01(footstepsVol);
        jumpVolume = Mathf.Clamp01(jumpVol);
        
        if (footstepsSource != null)
            footstepsSource.volume = footstepsVolume;
            
        if (jumpSource != null)
            jumpSource.volume = jumpVolume;
            
        Debug.Log($"已设置音效音量 - 脚步声: {footstepsVolume}, 跳跃: {jumpVolume}");
    }

    /// <summary>
    /// 播放脚步声
    /// </summary>
    public void PlayFootsteps()
    {
        if (footstepsSource == null)
        {
            Debug.LogWarning("脚步声音频源未初始化");
            return;
        }
        
        if (footstepsSound == null)
        {
            Debug.LogWarning("脚步声音效未加载");
            return;
        }

        // 确保脚步声音量不为0
        if (footstepsSource.volume <= 0)
            footstepsSource.volume = footstepsVolume;

        if (!footstepsSource.isPlaying)
        {
            footstepsSource.Play();
            Debug.Log("开始播放脚步声音效");
        }
    }

    /// <summary>
    /// 停止脚步声
    /// </summary>
    public void StopFootsteps()
    {
        if (footstepsSource == null) return;

        if (footstepsSource.isPlaying)
        {
            footstepsSource.Stop();
        }
    }

    /// <summary>
    /// 播放跳跃音效
    /// </summary>
    public void PlayJump()
    {
        if (jumpSource == null)
        {
            Debug.LogWarning("跳跃音频源未初始化");
            return;
        }
        
        if (jumpSound == null)
        {
            Debug.LogWarning("跳跃音效未加载");
            return;
        }

        // 确保跳跃音效音量不为0
        if (jumpSource.volume <= 0)
            jumpSource.volume = jumpVolume;

        jumpSource.Play();
        Debug.Log("播放跳跃音效");
    }

    /// <summary>
    /// 播放背景音乐
    /// </summary>
    public void PlayBackgroundMusic()
    {
        if (bgmSource == null || backgroundMusic == null) return;
        
        if (!bgmSource.isPlaying)
        {
            bgmSource.Play();
        }
    }
    
    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBackgroundMusic()
    {
        if (bgmSource == null) return;
        
        if (bgmSource.isPlaying)
        {
            bgmSource.Stop();
        }
    }
    
    /// <summary>
    /// 设置背景音乐音量
    /// </summary>
    public void SetBackgroundMusicVolume(float volume)
    {
        if (bgmSource == null) return;
        
        bgmSource.volume = Mathf.Clamp01(volume);
    }
    
    /// <summary>
    /// 设置最小背景音乐音调
    /// </summary>
    public void SetMinBgmPitch(float pitch)
    {
        minBgmPitch = Mathf.Clamp(pitch, 0.1f, 1.0f);
    }
    
    /// <summary>
    /// 设置最小音效音调
    /// </summary>
    public void SetMinSfxPitch(float pitch)
    {
        minSfxPitch = Mathf.Clamp(pitch, 0.1f, 1.0f);
    }

    /// <summary>
    /// 播放一般音效
    /// </summary>
    public void PlaySFX(AudioClip clip, float volume = 1.0f)
    {
        if (clip == null || sfxSource == null) return;

        sfxSource.clip = clip;
        sfxSource.volume = volume;
        sfxSource.Play();
    }
} 