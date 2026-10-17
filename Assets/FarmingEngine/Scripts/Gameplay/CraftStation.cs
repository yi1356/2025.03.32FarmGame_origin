using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 将此脚本添加到建筑上，将其转换为一个工作台（Craft Station）（Z主要作用是管理游戏中的工作台）
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    public class CraftStation : MonoBehaviour
    {
        public GroupData[] craft_groups; // 可以进行合成的组数据
        public float range = 3f; // 工作台的使用范围

        private Selectable select; // 可选择组件
        private Buildable buildable; // 可建造组件（可能为空）

        private static List<CraftStation> station_list = new List<CraftStation>(); // 所有CraftStation对象的列表

        void Awake()
        {
            station_list.Add(this);
            select = GetComponent<Selectable>();
            buildable = GetComponent<Buildable>();
            select.onUse += OnUse; // 注册使用事件
        }

        private void OnDestroy()
        {
            station_list.Remove(this);
        }

        // 当被角色使用时的处理
        private void OnUse(PlayerCharacter character)
        {
            CraftPanel panel = CraftPanel.Get(character.player_id);
            if (panel != null && !panel.IsVisible())
                panel.Show(); // 显示合成面板
        }

        // 是否有可以进行合成的组
        public bool HasCrafting()
        {
            return craft_groups.Length > 0;
        }

        // 获取最近范围内的工作台
        public static CraftStation GetNearestInRange(Vector3 pos)
        {
            float min_dist = 99f; // 初始最小距离，假设一个较大的值
            CraftStation nearest = null;
            foreach (CraftStation station in station_list)
            {
                if (station.buildable == null || !station.buildable.IsBuilding())
                {
                    float dist = (pos - station.transform.position).magnitude;
                    if (dist < min_dist && dist < station.range)
                    {
                        min_dist = dist;
                        nearest = station;
                    }
                }
            }
            return nearest;
        }

        // 获取所有的工作台列表
        public static List<CraftStation> GetAll()
        {
            return station_list;
        }
    }
}
