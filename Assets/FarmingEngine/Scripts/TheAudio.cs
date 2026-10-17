using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 音频管理器脚本，用于在不同频道上播放音频，确保两个声音不会同时播放在同一个频道。
    /// 有助于避免声音重叠播放的问题。
    /// </summary>
    public class TheAudio : MonoBehaviour
    {
        private static TheAudio _instance; // 单例实例

        private Dictionary<string, AudioSource> channels_sfx = new Dictionary<string, AudioSource>(); // 存储音效频道的字典
        private Dictionary<string, AudioSource> channels_music = new Dictionary<string, AudioSource>(); // 存储音乐频道的字典
        private Dictionary<string, float> channels_volume = new Dictionary<string, float>(); // 存储各频道音量的字典

        void Awake()
        {
            _instance = this;
            //DontDestroyOnLoad(gameObject); // 取消注释可以使音频管理器在场景切换时不销毁
        }

        private void Start()
        {
            // 设置全局音量
            AudioListener.volume = PlayerData.Get().master_volume;
        }

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="channel">频道：同一个频道上的两个声音不会同时播放，不同频道上的声音可以同时播放。</param>
        /// <param name="sound">音效剪辑</param>
        /// <param name="vol">音效音量</param>
        /// <param name="priority">优先级：如果为true，当频道中已有声音时会替换当前声音；如果为false，则不会播放。</param>
        public void PlaySFX(string channel, AudioClip sound, float vol = 0.8f, bool priority = true)
        {
            if (string.IsNullOrEmpty(channel) || sound == null)
                return;

            AudioSource source = GetChannel(channel);
            float volume = PlayerData.Get() != null ? PlayerData.Get().sfx_volume : 1f;
            channels_volume[channel] = vol;

            if (source == null)
            {
                source = CreateChannel(channel); // 如果频道不存在，则创建频道
                channels_sfx[channel] = source;
            }

            if (source)
            {
                if (priority || !source.isPlaying)
                {
                    source.clip = sound;
                    source.volume = vol * volume;
                    source.Play();
                }
            }
        }

        /// <summary>
        /// 播放音乐
        /// </summary>
        /// <param name="channel">频道：同一个频道上的两段音乐不会同时播放。如果频道中已有音乐播放，则新音乐会被播放，除非音乐相同（在这种情况下不会重新播放）。</param>
        /// <param name="music">音乐剪辑</param>
        /// <param name="vol">音乐音量</param>
        /// <param name="loop">是否循环播放</param>
        public void PlayMusic(string channel, AudioClip music, float vol = 0.4f, bool loop = true)
        {
            if (string.IsNullOrEmpty(channel) || music == null)
                return;

            AudioSource source = GetMusicChannel(channel);
            float volume = PlayerData.Get() != null ? PlayerData.Get().music_volume : 1f;
            channels_volume[channel] = vol;

            if (source == null)
            {
                source = CreateChannel(channel); // 如果频道不存在，则创建频道
                channels_music[channel] = source;
            }

            if (source)
            {
                if (!source.isPlaying || source.clip != music)
                {
                    source.clip = music;
                    source.volume = vol * volume;
                    source.loop = loop;
                    source.Play();
                }
            }
        }

        /// <summary>
        /// 停止播放指定频道的音乐
        /// </summary>
        /// <param name="channel">频道</param>
        public void StopMusic(string channel)
        {
            if (string.IsNullOrEmpty(channel))
                return;

            AudioSource source = GetMusicChannel(channel);
            if (source)
            {
                source.Stop();
            }
        }

        /// <summary>
        /// 刷新音量设置
        /// </summary>
        public void RefreshVolume()
        {
            AudioListener.volume = PlayerData.Get().master_volume;

            // 更新音效频道的音量
            foreach (KeyValuePair<string, AudioSource> pair in channels_sfx)
            {
                if (pair.Value != null)
                {
                    float vol = channels_volume.ContainsKey(pair.Key) ? channels_volume[pair.Key] : 0.8f;
                    pair.Value.volume = vol * PlayerData.Get().sfx_volume;
                }
            }

            // 更新音乐频道的音量
            foreach (KeyValuePair<string, AudioSource> pair in channels_music)
            {
                if (pair.Value != null)
                {
                    float vol = channels_volume.ContainsKey(pair.Key) ? channels_volume[pair.Key] : 0.4f;
                    pair.Value.volume = vol * PlayerData.Get().music_volume;
                }
            }
        }

        /// <summary>
        /// 检查指定频道是否正在播放音乐
        /// </summary>
        /// <param name="channel">频道</param>
        /// <returns>是否正在播放音乐</returns>
        public bool IsMusicPlaying(string channel)
        {
            AudioSource source = GetMusicChannel(channel);
            if (source != null)
                return source.isPlaying;
            return false;
        }

        /// <summary>
        /// 获取指定频道的音效频道
        /// </summary>
        /// <param name="channel">频道</param>
        /// <returns>音效频道的 AudioSource 组件</returns>
        public AudioSource GetChannel(string channel)
        {
            if (channels_sfx.ContainsKey(channel))
                return channels_sfx[channel];
            return null;
        }

        /// <summary>
        /// 获取指定频道的音乐频道
        /// </summary>
        /// <param name="channel">频道</param>
        /// <returns>音乐频道的 AudioSource 组件</returns>
        public AudioSource GetMusicChannel(string channel)
        {
            if (channels_music.ContainsKey(channel))
                return channels_music[channel];
            return null;
        }

        /// <summary>
        /// 检查指定频道的音效频道是否存在
        /// </summary>
        /// <param name="channel">频道</param>
        /// <returns>频道是否存在</returns>
        public bool DoesChannelExist(string channel)
        {
            return channels_sfx.ContainsKey(channel);
        }

        /// <summary>
        /// 检查指定频道的音乐频道是否存在
        /// </summary>
        /// <param name="channel">频道</param>
        /// <returns>频道是否存在</returns>
        public bool DoesMusicChannelExist(string channel)
        {
            return channels_music.ContainsKey(channel);
        }

        /// <summary>
        /// 创建新的音频频道
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="priority">优先级</param>
        /// <returns>新创建的 AudioSource 组件</returns>
        public AudioSource CreateChannel(string channel, int priority = 128)
        {
            if (string.IsNullOrEmpty(channel))
                return null;

            GameObject cobj = new GameObject("AudioChannel-" + channel); // 创建新的游戏对象作为频道
            cobj.transform.parent = transform;
            AudioSource caudio = cobj.AddComponent<AudioSource>(); // 添加 AudioSource 组件
            caudio.playOnAwake = false; // 初始化时不播放
            caudio.loop = false; // 不循环播放
            caudio.priority = priority; // 设置优先级
            return caudio;
        }

        // 静态方法快捷方式
        public static void Music(string channel, AudioClip audio, float volume = 1f) { _instance?.PlayMusic(channel, audio, volume); }
        public static void SFX(string channel, AudioClip audio, float volume = 1f) { _instance?.PlaySFX(channel, audio, volume); }
        public static void Stop(string channel) { _instance?.StopMusic(channel); } // 停止音乐

        // 获取单例实例
        public static TheAudio Get()
        {
            return _instance;
        }
    }
}
