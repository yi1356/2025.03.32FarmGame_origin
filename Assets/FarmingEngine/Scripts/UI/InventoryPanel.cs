using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 主库存面板，列出玩家库存中的所有物品
    /// </summary>
    public class InventoryPanel : ItemSlotPanel
    {
        private static List<InventoryPanel> panel_list = new List<InventoryPanel>(); // 存储所有 InventoryPanel 实例的列表

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this); // 将当前面板添加到面板列表中
            unfocus_when_out = true; // 当面板失去焦点时，将其隐藏

            // 为每个槽位添加快捷键按下的事件处理
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].onPressKey += OnPressShortcut;
            }

            Hide(true); // 初始化时隐藏面板
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            panel_list.Remove(this); // 从面板列表中移除当前面板
        }

        protected override void Start()
        {
            base.Start();
        }

        public override void InitPanel()
        {
            base.InitPanel();

            // 如果面板还没有设置库存,就为面板设置库存设置玩家等，并显示背包面板[Z：注意：这里依据的是这个面板有没有设置inventory数据，也就是这里的枚举InventoryType]
            if (!IsInventorySet())
            {
                PlayerCharacter player = GetPlayer();
                if (player != null)
                {
                    bool has_inventory = PlayerData.Get().HasInventory(player.player_id);
                    if (has_inventory)
                    {
                        // 设置面板的库存数据[Z：其实就是设置InventoryData的id等，是为了获取到inventoryData]
                        SetInventory(InventoryType.Inventory, player.InventoryData.uid, player.InventoryData.size);
                        SetPlayer(player);
                        Show(true); // 显示面板
                    }
                }
            }
        }

        //Z：键盘按键用
        private void OnPressShortcut(UISlot slot)
        {
            CancelSelection(); // 取消当前选择
            PressSlot(slot.index); // 按下槽位快捷键
        }

        public static InventoryPanel Get(int player_id = 0)
        {
            // 根据玩家 ID 获取对应的 InventoryPanel 实例
            foreach (InventoryPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel;
            }
            return null; // 如果没有找到匹配的面板，则返回 null
        }

        public static new List<InventoryPanel> GetAll()
        {
            // 返回所有 InventoryPanel 实例的列表
            return panel_list;
        }
    }
}
