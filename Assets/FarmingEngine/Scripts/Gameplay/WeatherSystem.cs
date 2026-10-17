using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 将此脚本放置在每个场景中，用于管理该场景中可能的天气列表
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        [Header("Weather")]
        public WeatherData default_weather;     // 默认天气数据
        public WeatherData[] weathers;          // 可能的天气数据列表

        [Header("Weather Group")]
        public string group;                    // 具有相同组的场景将同步天气

        [Header("Weather Settings")]
        public float weather_change_time = 6f;  // 天气变化的时间（游戏时间，以小时为单位）

        private WeatherData current_weather;    // 当前天气数据
        private GameObject current_weather_fx;  // 当前天气效果对象
        private float update_timer = 0f;        // 更新计时器

        private static WeatherSystem instance;  // 单例实例

        private void Awake()
        {
            instance = this;                    // 设置单例实例为当前对象
            current_weather = null;             // 初始时当前天气为空
            if (default_weather == null)        // 如果默认天气为空，禁用此组件
                enabled = false;
        }

        void Start()
        {
            if (PlayerData.Get().HasCustomString("weather_" + group))  // 如果存在保存的天气数据
            {
                string weather_id = PlayerData.Get().GetCustomString("weather_" + group); // 获取保存的天气ID
                ChangeWeather(GetWeather(weather_id)); // 更改为保存的天气
            }
            else
            {
                ChangeWeather(default_weather); // 否则更改为默认天气
            }
        }

        void Update()
        {
            update_timer += Time.deltaTime;     // 更新计时器
            if (update_timer > 1f)              // 每秒更新一次
            {
                update_timer = 0f;              // 重置计时器
                SlowUpdate();                   // 慢速更新，检查是否新的一天或天气变化时间到了
            }
        }

        void SlowUpdate()
        {
            // 检查是否新的一天
            int day = PlayerData.Get().day;                     // 获取当前天数
            float time = PlayerData.Get().day_time;             // 获取当前游戏时间（小时）
            int prev_day = PlayerData.Get().GetCustomInt("weather_day_" + group); // 获取上次保存的天数
            if (day > prev_day && time >= weather_change_time)  // 如果当前天数大于上次保存的天数且当前时间超过天气变化时间
            {
                ChangeWeatherRandom();  // 随机更改天气
                PlayerData.Get().SetCustomInt("weather_day_" + group, day); // 保存当前天数
            }
        }

        // 随机更改天气
        public void ChangeWeatherRandom()
        {
            if (weathers.Length > 0)    // 如果有定义的天气数据
            {
                float total = 0f;
                foreach (WeatherData aweather in weathers)
                {
                    total += aweather.probability;  // 计算总概率
                }

                float value = Random.Range(0f, total);  // 随机一个值
                WeatherData weather = null;
                foreach (WeatherData aweather in weathers)
                {
                    if (weather == null && value < aweather.probability)
                        weather = aweather;  // 根据随机值选取天气数据
                    else
                        value -= aweather.probability; // 减去当前天气数据的概率
                }

                if (weather == null)
                    weather = default_weather; // 如果未选取到天气数据，则选择默认天气

                ChangeWeather(weather); // 更改天气
            }
        }

        // 更改天气
        public void ChangeWeather(WeatherData weather)
        {
            if (weather != null && current_weather != weather) // 如果新的天气不为空且与当前天气不同
            {
                current_weather = weather; // 设置当前天气为新的天气
                PlayerData.Get().SetCustomString("weather_" + group, weather.id); // 保存当前天气ID
                if (current_weather_fx != null)
                    Destroy(current_weather_fx); // 销毁当前天气效果对象
                if (current_weather.weather_fx != null)
                    current_weather_fx = Instantiate(current_weather.weather_fx, TheCamera.Get().GetTargetPos(), Quaternion.identity); // 实例化新的天气效果对象
            }
        }

        // 根据ID获取天气数据
        public WeatherData GetWeather(string id)
        {
            foreach (WeatherData weather in weathers)
            {
                if (weather.id == id)
                    return weather; // 返回匹配的天气数据
            }
            return null; // 如果未找到，返回空
        }

        // 获取光照倍数
        public float GetLightMult()
        {
            if (current_weather != null)
                return current_weather.light_mult; // 返回当前天气的光照倍数
            return 1f; // 默认返回1（正常光照）
        }

        // 获取当前天气效果
        public WeatherEffect GetWeatherEffect()
        {
            if (current_weather != null)
                return current_weather.effect; // 返回当前天气的效果
            return WeatherEffect.None; // 默认返回无效果
        }

        // 检查当前是否具有指定天气效果
        public bool HasWeatherEffect(WeatherEffect effect)
        {
            if(current_weather != null)
                return current_weather.effect == effect; // 返回当前天气是否具有指定效果
            return false; // 默认返回false
        }

        // 获取单例实例
        public static WeatherSystem Get()
        {
            return instance; // 返回单例实例
        }
    }
}
