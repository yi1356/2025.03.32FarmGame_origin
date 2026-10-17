using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 显示单个制作项详细信息的面板（可以理解为三级建造界面，根据二级建造界面传入的craftable刷新页面，显示需要的材料等）
    /// </summary>
    public class CraftInfoPanel : UIPanel
    {
        public ItemSlot slot; // 显示制作项的 UI 槽位
        public Text title; // 显示制作项标题的文本
        public Text desc; // 显示制作项描述的文本
        public Button craft_btn; // 制作按钮

        public ItemSlot[] craft_slots; // 显示制作所需物品的 UI 槽位数组

        private PlayerUI parent_ui; // 父级 UI
        private CraftData data; // 当前显示的制作数据

        private float update_timer = 0f; // 更新计时器

        private static List<CraftInfoPanel> panel_list = new List<CraftInfoPanel>(); // 存储所有 CraftInfoPanel 实例的列表

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this); // 将当前实例添加到列表中
            parent_ui = GetComponentInParent<PlayerUI>(); // 获取父级 UI
        }

        private void OnDestroy()
        {
            panel_list.Remove(this); // 销毁时从列表中移除实例
        }

        protected override void Update()
        {
            base.Update();

            update_timer += Time.deltaTime; // 增加计时器时间
            if (update_timer > 0.5f)
            {
                update_timer = 0f;
                SlowUpdate(); // 每隔0.5秒执行一次 SlowUpdate
            }
        }

        private void SlowUpdate()
        {
            if (data != null && IsVisible())
            {
                RefreshPanel(); // 刷新面板内容
            }
        }

        private void RefreshPanel()
        {
            // 设置显示的制作项槽位
            slot.SetSlot(data, data.craft_quantity, true);
            title.text = data.title; // 设置标题
            desc.text = data.desc; // 设置描述

            // 隐藏所有制作槽位
            foreach (ItemSlot slot in craft_slots)
                slot.Hide();

            PlayerCharacter player = GetPlayer(); // 获取玩家角色

            CraftCostData cost = data.GetCraftCost(); // 获取制作成本数据
            int index = 0;
            // 设置制作所需物品的槽位
            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                if (index < craft_slots.Length)
                {
                    ItemSlot slot = craft_slots[index];
                    slot.SetSlot(pair.Key, pair.Value, false);
                    slot.SetFilter(player.Inventory.HasItem(pair.Key, pair.Value) ? 0 : 2); // 根据是否拥有物品设置过滤器
                    slot.ShowTitle(); // 显示标题
                }
                index++;
            }

            // 设置填充物品的槽位
            foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
            {
                if (index < craft_slots.Length)
                {
                    ItemSlot slot = craft_slots[index];
                    slot.SetSlotCustom(pair.Key.icon, pair.Key.title, pair.Value, false);
                    slot.SetFilter(player.Inventory.HasItemInGroup(pair.Key, pair.Value) ? 0 : 2); // 根据是否拥有物品组设置过滤器
                    slot.ShowTitle(); // 显示标题
                }
                index++;
            }

            // 设置制作要求的槽位
            foreach (KeyValuePair<CraftData, int> pair in cost.craft_requirements)
            {
                if (index < craft_slots.Length)
                {
                    ItemSlot slot = craft_slots[index];
                    slot.SetSlot(pair.Key, pair.Value, false);
                    slot.SetFilter(player.Crafting.CountRequirements(pair.Key) >= pair.Value ? 0 : 2); // 根据是否满足要求设置过滤器
                    slot.ShowTitle(); // 显示标题
                }
                index++;
            }

            // 设置临近物品的槽位
            if (index < craft_slots.Length)
            {
                ItemSlot slot = craft_slots[index];
                if (cost.craft_near != null)
                {
                    slot.SetSlotCustom(cost.craft_near.icon, cost.craft_near.title, 1, false);
                    bool isnear = player.IsNearGroup(cost.craft_near) || player.EquipData.HasItemInGroup(cost.craft_near);
                    slot.SetFilter(isnear ? 0 : 2); // 根据是否接近设置过滤器
                    slot.ShowTitle(); // 显示标题
                }
            }

            // 更新制作按钮的可交互状态
            craft_btn.interactable = player.Crafting.CanCraft(data);
        }

        /// <summary>
        /// 显示制作数据
        /// </summary>
        /// <param name="item">要显示的制作数据</param>
        public void ShowData(CraftData item)
        {
            this.data = item; // 设置当前数据
            RefreshPanel(); // 刷新面板内容
            slot.AnimateGain(); // 播放获取动画
            Show(); // 显示面板
        }

        /// <summary>
        /// 点击制作按钮时调用
        /// </summary>
        public void OnClickCraft()
        {
            PlayerCharacter player = GetPlayer(); // 获取玩家角色

            if (player.Crafting.CanCraft(data)) // 检查是否可以制作
            {
                player.Crafting.StartCraftingOrBuilding(data); // 开始制作或建造

                craft_btn.interactable = false; // 禁用制作按钮
                Hide(); // 隐藏面板
            }
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant); // 调用基类隐藏方法
            data = null; // 清除当前数据
        }

        /// <summary>
        /// 获取当前显示的制作数据
        /// </summary>
        /// <returns>当前制作数据</returns>
        public CraftData GetData()
        {
            return data;
        }

        /// <summary>
        /// 获取父级 UI
        /// </summary>
        /// <returns>父级 UI</returns>
        public PlayerUI GetParentUI()
        {
            return parent_ui;
        }

        /// <summary>
        /// 获取玩家角色
        /// </summary>
        /// <returns>玩家角色</returns>
        public PlayerCharacter GetPlayer()
        {
            return parent_ui ? parent_ui.GetPlayer() : PlayerCharacter.GetFirst();
        }

        /// <summary>
        /// 获取玩家 ID
        /// </summary>
        /// <returns>玩家 ID</returns>
        public int GetPlayerID()
        {
            PlayerCharacter player = GetPlayer();
            return player != null ? player.player_id : 0;
        }

        /// <summary>
        /// 获取指定玩家 ID 的 CraftInfoPanel 实例
        /// </summary>
        /// <param name="player_id">玩家 ID</param>
        /// <returns>对应玩家 ID 的 CraftInfoPanel 实例，如果没有则返回 null</returns>
        public static CraftInfoPanel Get(int player_id = 0)
        {
            foreach (CraftInfoPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer(); // 获取面板上的玩家角色
                if (player != null && player.player_id == player_id)
                    return panel; // 返回对应玩家 ID 的面板
            }
            return null; // 如果没有找到则返回 null
        }

        /// <summary>
        /// 获取所有 CraftInfoPanel 实例
        /// </summary>
        /// <returns>所有 CraftInfoPanel 实例的列表</returns>
        public static List<CraftInfoPanel> GetAll()
        {
            return panel_list; // 返回所有 CraftInfoPanel 实例的列表
        }
    }

}
