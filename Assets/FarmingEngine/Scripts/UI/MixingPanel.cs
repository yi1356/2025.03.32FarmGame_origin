using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 主 UI 面板用于存储箱（如箱子）
    /// </summary>
    public class MixingPanel : ItemSlotPanel
    {
        public ItemSlot result_slot; // 结果槽，用于显示合成后的物品
        public Button mix_button; // 合成按钮

        private PlayerCharacter player; // 当前玩家
        private MixingPot mixing_pot; // 混合锅
        private ItemData crafed_item = null; // 当前合成的物品

        private static MixingPanel _instance; // MixingPanel 的单例实例

        protected override void Awake()
        {
            base.Awake();
            _instance = this; // 设置单例实例

            result_slot.onClick += OnClickResult; // 注册点击事件
        }

        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            result_slot.SetSlot(crafed_item, 1); // 更新结果槽

            mix_button.interactable = CanMix(); // 更新合成按钮的可交互状态

            // 如果玩家距离混合锅太远，则隐藏面板
            Selectable select = mixing_pot?.GetSelectable();
            if (IsVisible() && player != null && select != null)
            {
                float dist = (select.transform.position - player.transform.position).magnitude;
                if (dist > select.GetUseRange(player) * 1.2f)
                {
                    Hide(); // 隐藏面板
                }
            }
        }

        /// <summary>
        /// 显示混合面板
        /// </summary>
        /// <param name="player">当前玩家</param>
        /// <param name="pot">混合锅</param>
        /// <param name="uid">存储 UID</param>
        public void ShowMixing(PlayerCharacter player, MixingPot pot, string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                this.player = player; // 设置玩家
                this.mixing_pot = pot; // 设置混合锅
                SetInventory(InventoryType.Storage, uid, pot.max_items); // 设置背包
                SetPlayer(player); // 设置玩家
                RefreshPanel(); // 刷新面板
                Show(); // 显示面板
            }
        }

        /// <summary>
        /// 检查是否可以进行混合
        /// </summary>
        /// <returns>是否可以混合</returns>
        public bool CanMix()
        {
            bool at_least_one = false;
            foreach (ItemSlot slot in slots)
            {
                if (slot.GetItem() != null)
                    at_least_one = true; // 至少有一个物品
            }
            return mixing_pot != null && at_least_one && result_slot.GetItem() == null; // 检查混合锅和结果槽
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            SetInventory(InventoryType.Storage, "", 0); // 清空背包
            CancelSelection(); // 取消选择
        }

        /// <summary>
        /// 检查背包中是否有指定数量的物品
        /// </summary>
        /// <param name="item">物品数据</param>
        /// <param name="quantity">数量</param>
        /// <returns>是否拥有指定数量的物品</returns>
        public bool HasItem(ItemData item, int quantity)
        {
            int count = 0;
            foreach (ItemSlot slot in slots)
            {
                if (slot.GetItem() == item)
                    count += slot.GetQuantity(); // 累计物品数量
            }
            return count >= quantity; // 检查是否满足数量
        }

        /// <summary>
        /// 检查背包中是否有指定数量的物品组
        /// </summary>
        /// <param name="group">物品组数据</param>
        /// <param name="quantity">数量</param>
        /// <returns>是否拥有指定数量的物品组</returns>
        public bool HasItemInGroup(GroupData group, int quantity)
        {
            int count = 0;
            foreach (ItemSlot slot in slots)
            {
                if (slot.GetItem() != null && slot.GetItem().HasGroup(group))
                    count += slot.GetQuantity(); // 累计物品组数量
            }
            return count >= quantity; // 检查是否满足数量
        }

        /// <summary>
        /// 移除指定数量的物品
        /// </summary>
        /// <param name="item">物品数据</param>
        /// <param name="quantity">数量</param>
        public void RemoveItem(ItemData item, int quantity)
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot.GetItem() == item && quantity > 0)
                {
                    quantity -= slot.GetQuantity(); // 减少数量
                    UseItem(slot, slot.GetQuantity()); // 使用物品
                }
            }
        }

        /// <summary>
        /// 移除指定数量的物品组
        /// </summary>
        /// <param name="group">物品组数据</param>
        /// <param name="quantity">数量</param>
        public void RemoveItemInGroup(GroupData group, int quantity)
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot.GetItem() != null && slot.GetItem().HasGroup(group) && quantity > 0)
                {
                    quantity -= slot.GetQuantity(); // 减少数量
                    UseItem(slot, slot.GetQuantity()); // 使用物品
                }
            }
        }

        /// <summary>
        /// 移除背包中的所有物品
        /// </summary>
        public void RemoveAll()
        {
            foreach (ItemSlot slot in slots)
            {
                UseItem(slot, slot.GetQuantity()); // 使用所有物品
            }
        }

        /// <summary>
        /// 检查是否可以合成指定物品
        /// </summary>
        /// <param name="item">合成数据</param>
        /// <param name="skip_near">是否跳过附近检测</param>
        /// <returns>是否可以合成</returns>
        public bool CanCraft(CraftData item, bool skip_near = false)
        {
            if (item == null)
                return false;

            CraftCostData cost = item.GetCraftCost(); // 获取合成成本数据
            bool can_craft = true;

            Dictionary<GroupData, int> item_groups = new Dictionary<GroupData, int>(); // 存储物品组及其数量

            // 检查合成所需的物品
            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                AddCraftCostItemsGroups(item_groups, pair.Key, pair.Value);
                if (!HasItem(pair.Key, pair.Value))
                    can_craft = false; // 不拥有所需物品
            }

            // 检查合成所需的物品组
            foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
            {
                int value = pair.Value + CountCraftCostGroup(item_groups, pair.Key);
                if (!HasItemInGroup(pair.Key, value))
                    can_craft = false; // 不拥有所需物品组
            }

            return can_craft;
        }

        private void AddCraftCostItemsGroups(Dictionary<GroupData, int> item_groups, ItemData item, int quantity)
        {
            foreach (GroupData group in item.groups)
            {
                if (item_groups.ContainsKey(group))
                    item_groups[group] += quantity;
                else
                    item_groups[group] = quantity; // 更新物品组数量
            }
        }

        private int CountCraftCostGroup(Dictionary<GroupData, int> item_groups, GroupData group)
        {
            if (item_groups.ContainsKey(group))
                return item_groups[group];
            return 0;
        }

        /// <summary>
        /// 支付合成成本
        /// </summary>
        /// <param name="item">合成数据</param>
        public void PayCraftingCost(CraftData item)
        {
            CraftCostData cost = item.GetCraftCost(); // 获取合成成本数据
            foreach (KeyValuePair<ItemData, int> pair in cost.craft_items)
            {
                RemoveItem(pair.Key, pair.Value); // 移除合成所需物品
            }
            foreach (KeyValuePair<GroupData, int> pair in cost.craft_fillers)
            {
                RemoveItemInGroup(pair.Key, pair.Value); // 移除合成所需物品组
            }
        }

        /// <summary>
        /// 混合物品
        /// </summary>
        public void MixItems()
        {
            ItemData item = null;
            foreach (ItemData recipe in mixing_pot.recipes)
            {
                if (item == null && CanCraft(recipe))
                {
                    item = recipe;
                    PayCraftingCost(recipe); // 支付合成成本
                }
            }

            if (item != null)
            {
                crafed_item = item;
                result_slot.SetSlot(item, 1); // 设置结果槽

                if (mixing_pot.clear_on_mix)
                    RemoveAll(); // 清空背包
            }
        }

        /// <summary>
        /// 点击合成按钮的处理方法
        /// </summary>
        public void OnClickMix()
        {
            if (CanMix())
            {
                MixItems(); // 执行混合操作
            }
        }

        /// <summary>
        /// 点击结果槽的处理方法
        /// </summary>
        /// <param name="slot">点击的槽位</param>
        public void OnClickResult(UISlot slot)
        {
            if (player != null && result_slot.GetItem() != null)
            {
                player.Inventory.GainItem(result_slot.GetItem()); // 将合成物品添加到玩家背包
                result_slot.SetSlot(null, 0); // 清空结果槽
                crafed_item = null; // 清空当前合成物品
            }
        }

        /// <summary>
        /// 获取存储 UID
        /// </summary>
        /// <returns>存储 UID</returns>
        public string GetStorageUID()
        {
            return inventory_uid; // 返回存储 UID
        }

        /// <summary>
        /// 获取 MixingPanel 的单例实例
        /// </summary>
        /// <returns>MixingPanel 实例</returns>
        public static MixingPanel Get()
        {
            return _instance; // 返回单例实例
        }
    }

}
