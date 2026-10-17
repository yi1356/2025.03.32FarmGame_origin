using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 天气效果枚举
    /// </summary>
    public enum WeatherEffect
    {
        None = 0,   // 无效果
        Rain = 10,  // 下雨
    }

    /// <summary>
    /// 天气数据的ScriptableObject类
    /// </summary>
    [CreateAssetMenu(fileName ="Weather", menuName = "FarmingEngine/Weather", order =10)]
    public class WeatherData : ScriptableObject
    {
        public string id;                // 天气数据的唯一标识符
        public float probability = 1f;   // 天气发生的概率

        [Header("Gameplay")]
        public WeatherEffect effect;     // 天气效果枚举

        [Header("Visuals")]
        public GameObject weather_fx;    // 天气效果的游戏对象
        public float light_mult = 1f;    // 光照倍数
    }
}