using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 在动画播放期间播放音频
    /// </summary>

    public class PlayAudioAnim : MonoBehaviour
    {
        public string channel = "animation"; // 音频通道名称，默认为"animation"
        public float volume = 0.8f; // 音量，默认为0.8

        /// <summary>
        /// 播放音效
        /// </summary>
        /// <param name="clip">要播放的音频剪辑</param>
        public void PlaySound(AudioClip clip)
        {
            TheAudio.Get().PlaySFX(channel, clip, volume);
        }

        /// <summary>
        /// 播放音乐
        /// </summary>
        /// <param name="clip">要播放的音频剪辑</param>
        public void PlayMusic(AudioClip clip)
        {
            TheAudio.Get().PlayMusic(channel, clip, volume);
        }

        /// <summary>
        /// 设置音频通道
        /// </summary>
        /// <param name="channel">要设置的通道名称</param>
        public void SetChannel(string channel)
        {
            this.channel = channel;
        }

        /// <summary>
        /// 设置音量
        /// </summary>
        /// <param name="vol">要设置的音量值</param>
        public void SetVolume(float vol)
        {
            this.volume = vol;
        }

    }

}