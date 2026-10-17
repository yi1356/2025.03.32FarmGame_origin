using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if MAP_MINIMAP
using MapMinimap;
#endif

namespace FarmingEngine
{
    /// <summary>
    /// 对接 Map Minimap 的包装类
    /// </summary>
    public class MapMinimapWrap : MonoBehaviour
    {

#if MAP_MINIMAP

        static MapMinimapWrap()
        {
            TheGame.afterLoad += ReloadMM; // 游戏加载后重新加载地图小地图数据
            TheGame.afterNewGame += NewMM; // 新游戏开始后重置地图小地图数据
        }

        void Awake()
        {
            PlayerData.LoadLast(); // 确保游戏已加载

            TheGame the_game = FindObjectOfType<TheGame>(); // 获取游戏主控件
            MapManager map_manager = FindObjectOfType<MapManager>(); // 获取地图管理器

            if (map_manager != null)
            {
                map_manager.onOpenMap += OnOpen; // 注册打开地图事件
                map_manager.onCloseMap += OnClose; // 注册关闭地图事件
            }
            else
            {
                Debug.LogError("Map Minimap: 集成失败 - 请确保在场景中添加了 MapManager 组件");
            }

            if (the_game != null)
            {
                the_game.beforeSave += SaveDQ; // 在保存前保存地图小地图数据
                LoadMM(); // 加载地图小地图数据
            }
        }

        private void Start()
        {
            // 在此处添加需要在 Start 方法中进行的初始化操作（如果有的话）
        }

        private void Update()
        {
            // 在此处添加需要在 Update 方法中进行的逻辑更新（如果有的话）
        }

        private static void ReloadMM()
        {
            MapData.Unload(); // 卸载当前地图小地图数据
            LoadMM(); // 重新加载地图小地图数据
        }

        private static void NewMM()
        {
            PlayerData pdata = PlayerData.Get();
            if (pdata != null)
            {
                MapData.Unload(); // 卸载当前地图小地图数据
                MapData.NewGame(pdata.filename); // 在新游戏中创建新的地图小地图数据
            }
        }

        private static void LoadMM()
        {
            PlayerData pdata = PlayerData.Get();
            if (pdata != null)
            {
                MapData.AutoLoad(pdata.filename); // 自动加载地图小地图数据
            }
        }

        private void SaveDQ(string filename)
        {
            if (MapData.Get() != null && !string.IsNullOrEmpty(filename))
            {
                MapData.Save(filename, MapData.Get()); // 保存地图小地图数据
            }
        }

        private void OnOpen()
        {
            TheGame.Get().PauseScripts(); // 打开地图时暂停游戏脚本
        }

        private void OnClose()
        {
            TheGame.Get().UnpauseScripts(); // 关闭地图时恢复游戏脚本运行
        }

#endif

    }
}
