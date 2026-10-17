using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 通用资源数据（仅一个文件）
    /// </summary>

    [CreateAssetMenu(fileName = "AssetData", menuName = "FarmingEngine/AssetData", order = 0)]
    public class AssetData : ScriptableObject
    {
        [Header("系统预制体")]
        public GameObject ui_canvas; // UI 画布
        public GameObject ui_canvas_mobile; // 移动设备UI画布
        public GameObject audio_manager; // 音频管理器
        
        [Header("用户界面")]
        public GameObject action_selector; // 动作选择器
        public GameObject action_progress; // 动作进度条

        [Header("特效")]
        public GameObject item_take_fx; // 拿取物品特效
        public GameObject item_select_fx; // 选择物品特效
        public GameObject item_drag_fx; // 拖拽物品特效
        public GameObject item_merge_fx; // 合并物品特效

        [Header("音乐")]
        public AudioClip[] music_playlist; // 音乐播放列表

        public static AssetData Get()
        {
            return TheData.Get().assets; // 获取数据单例中的资源数据
        }
    }

}