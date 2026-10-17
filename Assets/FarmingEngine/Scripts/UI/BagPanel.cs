using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 胶囊面板，用于存储箱（背包）
    /// </summary>
    public class BagPanel : ItemSlotPanel
    {
        private static List<BagPanel> panel_list = new List<BagPanel>(); // 存储所有 BagPanel 面板的列表

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this); // 将当前面板添加到列表中
            unfocus_when_out = true; // 离开面板时取消焦点

            onSelectSlot += OnSelectSlot; // 注册选择槽位事件
            onMergeSlot += OnMergeSlot; // 注册合并槽位事件
        }

        /// <summary>
        /// 显示背包面板
        /// </summary>
        /// <param name="player">玩家角色</param>
        /// <param name="uid">库存的唯一标识符</param>
        /// <param name="max">背包的最大容量</param>
        public void ShowBag(PlayerCharacter player, string uid, int max)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                SetInventory(InventoryType.Bag, uid, max); // 设置背包的库存
                SetPlayer(player); // 设置玩家角色
                SetVisible(true); // 显示面板
            }
        }

        /// <summary>
        /// 隐藏背包面板
        /// </summary>
        public void HideBag()
        {
            SetInventory(InventoryType.Bag, "", 0); // 清空背包库存
            SetVisible(false); // 隐藏面板
        }

        private void OnSelectSlot(ItemSlot islot)
        {
            // 处理选择槽位的逻辑
        }

        private void OnMergeSlot(ItemSlot clicked_slot, ItemSlot selected_slot)
        {
            // 处理合并槽位的逻辑
        }

        /// <summary>
        /// 获取存储的唯一标识符
        /// </summary>
        /// <returns>库存的唯一标识符</returns>
        public string GetStorageUID()
        {
            return inventory_uid; // 返回库存的唯一标识符
        }

        /// <summary>
        /// 获取指定玩家 ID 的 BagPanel 面板
        /// </summary>
        /// <param name="player_id">玩家 ID</param>
        /// <returns>对应玩家 ID 的 BagPanel 面板，如果没有则返回 null</returns>
        public static BagPanel Get(int player_id = 0)
        {
            foreach (BagPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer(); // 获取面板上的玩家角色
                if (player != null && player.player_id == player_id)
                    return panel; // 返回对应玩家 ID 的面板
            }
            return null; // 如果没有找到则返回 null
        }

        /// <summary>
        /// 获取所有的 BagPanel 面板
        /// </summary>
        /// <returns>所有 BagPanel 面板的列表</returns>
        public static new List<BagPanel> GetAll()
        {
            return panel_list; // 返回所有 BagPanel 面板的列表
        }
    }

}
