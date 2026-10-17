using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 管理器脚本，在运行时加载所有可编写对象以供使用（Z：游戏开始时加载UI管理器TheUI）
    /// </summary>
    public class TheData : MonoBehaviour
    {
        public GameData data; // 游戏数据对象
        public AssetData assets; // 资源数据对象

        [Header("Resources Sub Folder")]
        public string load_folder = ""; // 资源加载的子文件夹名称

        private static TheData _instance; // 静态实例

        void Awake()
        {
            _instance = this; // 设置实例为当前对象

            // 加载各种数据对象
            CraftData.Load(load_folder); // 加载合成数据
            ItemData.Load(load_folder); // 加载物品数据
            ConstructionData.Load(load_folder); // 加载建筑数据
            PlantData.Load(load_folder); // 加载植物数据
            CharacterData.Load(load_folder); // 加载角色数据
            SpawnData.Load(load_folder); // 加载生成数据
            LevelData.Load(load_folder); // 加载关卡数据

            // 加载管理器
            if (!FindObjectOfType<TheUI>()) // 如果找不到UI管理器，则实例化相应的UI画布
                Instantiate(TheGame.IsMobile() ? assets.ui_canvas_mobile : assets.ui_canvas);
            if (!FindObjectOfType<TheAudio>()) // 如果找不到音频管理器，则实例化音频管理器
                Instantiate(assets.audio_manager);
            if (!FindObjectOfType<ActionSelector>()) // 如果找不到动作选择器，则实例化动作选择器
                Instantiate(assets.action_selector);
        }

        /// <summary>
        /// 获取TheData的静态实例
        /// </summary>
        /// <returns>TheData的当前实例</returns>
        public static TheData Get()
        {
            if (_instance == null)
                _instance = FindObjectOfType<TheData>(); // 如果实例为空，则尝试通过查找获取实例
            return _instance;
        }
    }
}