using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 存储箱的主UI面板（如箱子）
    /// </summary>
    public class StoragePanel : ItemSlotPanel
    {
        private static List<StoragePanel> panel_list = new List<StoragePanel>(); // 存储所有存储面板的列表

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this); // 将当前面板添加到面板列表中
            unfocus_when_out = true; // 离开面板时自动取消焦点

            // 注册槽位选择、合并和取消操作的回调
            onSelectSlot += OnSelectSlot;
            onMergeSlot += OnMergeSlot;
            onPressCancel += OnCancel;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            panel_list.Remove(this); // 从面板列表中移除当前面板
        }

        protected override void Update()
        {
            base.Update();

            PlayerControls controls = PlayerControls.Get();
            // 如果面板可见并且按下了取消菜单的按钮，则隐藏面板
            if (IsVisible() && controls.IsPressMenuCancel())
                Hide();
        }

        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            // 如果距离过远，则隐藏面板
            Selectable select = Selectable.GetByUID(inventory_uid);
            PlayerCharacter player = GetPlayer();
            if (IsVisible() && player != null && select != null)
            {
                float dist = (select.transform.position - player.transform.position).magnitude; // 计算玩家与存储箱的距离
                if (dist > select.GetUseRange(player) * 1.2f)
                {
                    Hide(); // 隐藏面板
                }
            }
        }

        // 显示存储箱面板
        public void ShowStorage(PlayerCharacter player, string uid, int max)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                SetInventory(InventoryType.Storage, uid, max); // 设置存储箱的库存
                SetPlayer(player); // 设置玩家
                RefreshPanel(); // 刷新面板显示
                Show(); // 显示面板
            }
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            SetInventory(InventoryType.Storage, "", 0); // 清空存储箱的库存
            CancelSelection(); // 取消选择
        }

        private void OnSelectSlot(ItemSlot islot)
        {
            // 在此处理槽位选择的逻辑（暂未实现）
        }

        private void OnMergeSlot(ItemSlot clicked_slot, ItemSlot selected_slot)
        {
            // 在此处理槽位合并的逻辑（暂未实现）
        }

        private void OnCancel(UISlot slot)
        {
            Hide(); // 取消操作时隐藏面板
        }

        // 获取当前存储箱的UID
        public string GetStorageUID()
        {
            return inventory_uid;
        }

        // 根据玩家ID获取对应的存储面板
        public static StoragePanel Get(int player_id = 0)
        {
            foreach (StoragePanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel;
            }
            return null;
        }

        // 检查是否有任何存储面板可见
        public static bool IsAnyVisible()
        {
            foreach (StoragePanel panel in panel_list)
            {
                if (panel.IsVisible())
                    return true;
            }
            return false;
        }

        // 获取所有存储面板
        public static new List<StoragePanel> GetAll()
        {
            return panel_list;
        }
    }
}
