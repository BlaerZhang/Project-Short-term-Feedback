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
    
    [Header("背景音乐设置")]
    [SerializeField] private AudioClip drumLoop; // 鼓点循环
    [SerializeField] private AudioClip melodyLoop; // 旋律循环

    [Header("音效设置")]
    [SerializeField, Range(0f, 1f)] private float footstepsVolume = 0.7f;
    [SerializeField, Range(0f, 1f)] private float jumpVolume = 0.8f;
    [SerializeField, Range(0f, 1f)] private float drumVolume = 0.5f; // 鼓点音量
    [SerializeField, Range(0f, 1f)] private float melodyVolume = 0.5f; // 旋律音量
    [SerializeField, Range(0.5f, 1.5f)] private float basePitch = 1.0f;
    
    [Header("Pitch设置")]
    [SerializeField, Range(0.1f, 1.0f)] private float minSfxPitch = 0.3f; // 音效最小pitch
    
    [Header("Fade设置")]
    [SerializeField, Range(0.1f, 2.0f)] private float melodyFadeTime = 0.5f; // 旋律淡入淡出时间

    // 音效播放器
    private AudioSource footstepsSource;
    private AudioSource jumpSource;
    private AudioSource drumSource; // 鼓点音频源
    private AudioSource melodySource; // 旋律音频源
    private AudioSource sfxSource; // 通用音效播放器

    // 时间缩放前的默认音调
    private float defaultFootstepsPitch;
    private float defaultJumpPitch;

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
        
        // 创建鼓点循环音频源
        if (drumSource == null)
        {
            drumSource = gameObject.AddComponent<AudioSource>();
            Debug.Log("已创建鼓点循环音频源");
        }
            
        // 设置鼓点循环音频源
        drumSource.clip = drumLoop;
        drumSource.volume = drumVolume;
        drumSource.loop = true;
        drumSource.playOnAwake = true; // 自动播放
        drumSource.pitch = 1.0f; // 固定音调
        Debug.Log($"鼓点循环音频源设置完成: 音量={drumVolume}, 音调=1.0, 循环={true}");
        
        // 创建旋律循环音频源
        if (melodySource == null)
        {
            melodySource = gameObject.AddComponent<AudioSource>();
            Debug.Log("已创建旋律循环音频源");
        }
            
        // 设置旋律循环音频源
        melodySource.clip = melodyLoop;
        melodySource.volume = 0f; // 初始静音
        melodySource.loop = true;
        melodySource.playOnAwake = true; // 自动播放
        melodySource.pitch = 1.0f; // 固定音调
        Debug.Log($"旋律循环音频源设置完成: 音量=0(初始静音), 音调=1.0, 循环={true}");
        
        // 如果有鼓点循环和旋律循环，自动播放
        if (drumLoop != null && !drumSource.isPlaying)
        {
            drumSource.Play();
            Debug.Log("鼓点循环已开始播放");
        }
        
        if (melodyLoop != null && !melodySource.isPlaying)
        {
            melodySource.Play();
            Debug.Log("旋律循环已开始播放(初始静音)");
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
        
        // 移除自动测试播放音效的功能
        // TestPlaySounds();
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

        // 更新音效音调
        if (footstepsSource != null && footstepsSource.pitch != sfxTargetPitch)
        {
            footstepsSource.DOPitch(sfxTargetPitch, 0.1f).SetUpdate(true);
        }

        if (jumpSource != null && jumpSource.pitch != sfxTargetPitch)
        {
            jumpSource.DOPitch(sfxTargetPitch, 0.1f).SetUpdate(true);
        }
    }

    private void Start()
    {
        // 检查并输出音频状态
        Debug.Log($"音频状态 - 脚步声: {(footstepsSound != null ? "已加载" : "未加载")}, " +
                  $"跳跃音效: {(jumpSound != null ? "已加载" : "未加载")}, " +
                  $"鼓点循环: {(drumLoop != null ? "已加载" : "未加载")}, " +
                  $"旋律循环: {(melodyLoop != null ? "已加载" : "未加载")}");
        
        // 检查音频源状态
        Debug.Log($"音频源状态 - 脚步声: {(footstepsSource != null ? "已创建" : "未创建")}, " +
                  $"跳跃音效: {(jumpSource != null ? "已创建" : "未创建")}, " +
                  $"鼓点循环: {(drumSource != null ? "已创建" : "未创建")}, " +
                  $"旋律循环: {(melodySource != null ? "已创建" : "未创建")}");
        
        // 确保音效音量不为0
        if (footstepsSource != null)
            Debug.Log($"脚步声音量: {footstepsSource.volume}, 音调: {footstepsSource.pitch}");
        
        if (jumpSource != null)
            Debug.Log($"跳跃音效音量: {jumpSource.volume}, 音调: {jumpSource.pitch}");
        
        if (drumSource != null)
            Debug.Log($"鼓点循环音量: {drumSource.volume}, 音调: {drumSource.pitch}, 是否播放: {drumSource.isPlaying}");
            
        if (melodySource != null)
            Debug.Log($"旋律循环音量: {melodySource.volume}, 音调: {melodySource.pitch}, 是否播放: {melodySource.isPlaying}");
        
        // 设置音效的音量，确保不为0
        SetSfxVolume(footstepsVolume, jumpVolume);
        
        // 寻找GameManager并订阅游戏状态变化事件
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            // 获取当前状态并设置音乐
            OnGameStateChanged(gameManager.CurrentState);
            
            // 订阅GameManager的状态变化事件
            gameManager.OnGameStateChangedEvent += OnGameStateChanged;
            Debug.Log("已订阅GameManager状态变化事件");
        }
        else
        {
            Debug.LogWarning("未找到GameManager，无法监听游戏状态变化");
        }
    }

    private void OnDestroy()
    {
        // 取消订阅GameManager事件，防止内存泄漏
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.OnGameStateChangedEvent -= OnGameStateChanged;
            Debug.Log("已取消订阅GameManager状态变化事件");
        }
    }

    /// <summary>
    /// 游戏状态变化处理
    /// </summary>
    public void OnGameStateChanged(GameState newState)
    {
        Debug.Log($"AudioManager响应游戏状态变化: {newState}");
        
        if (newState == GameState.Executing)
        {
            // 进入执行阶段，淡入旋律
            FadeInMelody();
        }
        else
        {
            // 离开执行阶段，淡出旋律
            FadeOutMelody();
        }
    }
    
    /// <summary>
    /// 淡入旋律
    /// </summary>
    private void FadeInMelody()
    {
        if (melodySource != null && melodyLoop != null)
        {
            // 停止任何正在进行的淡入淡出动画
            DOTween.Kill(melodySource);
            
            // 确保旋律正在播放
            if (!melodySource.isPlaying)
            {
                melodySource.Play();
            }
            
            // 淡入旋律
            melodySource.DOFade(melodyVolume, melodyFadeTime)
                .SetUpdate(true) // 忽略TimeScale
                .SetEase(Ease.Linear) // 使用线性过渡确保平滑
                .SetId(melodySource);
            Debug.Log($"淡入旋律循环: 目标音量={melodyVolume}, 时间={melodyFadeTime}秒, TimeScale={Time.timeScale}");
        }
    }
    
    /// <summary>
    /// 淡出旋律
    /// </summary>
    private void FadeOutMelody()
    {
        if (melodySource != null)
        {
            // 停止任何正在进行的淡入淡出动画
            DOTween.Kill(melodySource);
            
            // 淡出旋律
            melodySource.DOFade(0f, melodyFadeTime)
                .SetUpdate(true) // 忽略TimeScale
                .SetEase(Ease.Linear) // 使用线性过渡确保平滑
                .SetId(melodySource);
            Debug.Log($"淡出旋律循环: 目标音量=0, 时间={melodyFadeTime}秒, TimeScale={Time.timeScale}");
        }
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
    /// 设置BGM音量
    /// </summary>
    public void SetBgmVolume(float drumVol, float melodyVol)
    {
        drumVolume = Mathf.Clamp01(drumVol);
        melodyVolume = Mathf.Clamp01(melodyVol);
        
        if (drumSource != null)
            drumSource.volume = drumVolume;
            
        // 不直接设置旋律音量，以免打断fade效果
        // 只有在执行阶段才会立即应用旋律音量
        if (melodySource != null && melodySource.volume > 0)
            melodySource.volume = melodyVolume;
            
        Debug.Log($"已设置BGM音量 - 鼓点: {drumVolume}, 旋律: {melodyVolume}");
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
    public void PlayBgm()
    {
        // 播放鼓点循环
        if (drumSource != null && drumLoop != null && !drumSource.isPlaying)
        {
            drumSource.Play();
            Debug.Log("开始播放鼓点循环");
        }
        
        // 播放旋律循环（初始静音）
        if (melodySource != null && melodyLoop != null && !melodySource.isPlaying)
        {
            melodySource.volume = 0f; // 确保初始静音
            melodySource.Play();
            Debug.Log("开始播放旋律循环(初始静音)");
        }
    }
    
    /// <summary>
    /// 停止背景音乐
    /// </summary>
    public void StopBgm()
    {
        // 停止鼓点循环
        if (drumSource != null && drumSource.isPlaying)
        {
            drumSource.Stop();
            Debug.Log("停止播放鼓点循环");
        }
        
        // 停止旋律循环
        if (melodySource != null && melodySource.isPlaying)
        {
            melodySource.Stop();
            Debug.Log("停止播放旋律循环");
        }
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