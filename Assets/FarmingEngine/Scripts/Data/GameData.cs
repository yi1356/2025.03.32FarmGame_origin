using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 通用游戏数据（仅一个文件）
    /// </summary>

    [CreateAssetMenu(fileName = "GameData", menuName = "FarmingEngine/GameData", order = 0)]
    public class GameData : ScriptableObject
    {
        [Header("游戏时间")]
        public float game_time_mult = 24f; // 值为1表示游戏时间与现实时间同步，值为24表示1小时现实时间对应1天游戏时间
        public float start_day_time = 6f; // 开始一天的时间
        public float end_day_time = 2f; // 自动过到下一天的时间

        [Header("昼夜变化")]
        public float day_light_dir_intensity = 1f; // 白天方向光强度
        public float day_light_ambient_intensity = 1f; // 白天环境光强度
        public float night_light_dir_intensity = 0.2f; // 夜晚方向光强度
        public float night_light_ambient_intensity = 0.5f; // 夜晚环境光强度
        public bool rotate_shadows = true; // 是否根据白天转动阴影

        [Header("优化")]
        public float optim_refresh_rate = 0.5f; // 秒为单位，显示/隐藏可选择物体的刷新率间隔
        public float optim_distance_multiplier = 1f; // 所有可选择物体的活动范围乘数
        public float optim_facing_offset = 10f; // 在摄像机朝向的方向上，活动区域将被偏移X单位
        public bool optim_turn_off_gameobjects = false; // 如果开启，将关闭整个游戏物体，否则仅关闭脚本

        public static GameData Get()
        {
            return TheData.Get().data;
        }
    }

}