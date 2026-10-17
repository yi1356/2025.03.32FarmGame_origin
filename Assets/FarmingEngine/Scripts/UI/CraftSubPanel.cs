using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 第二级制作面板，包含一个类别下的所有物品（根据从一级建造面板传入的建造组别，遍历里面的可建造数据，更新槽位）
    /// </summary>
    public class CraftSubPanel : UISlotPanel
    {
        [Header("Craft Sub Panel")]
        public Text title; // 面板标题文本
        public Animator animator; // 动画控制器

        private PlayerUI parent_ui; // 父级 UI
        private UISlot prev_slot; // 上一个槽位

        private GroupData current_category; // 当前类别

        private static List<CraftSubPanel> panel_list = new List<CraftSubPanel>(); // 存储所有 CraftSubPanel 实例的列表

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this); // 将当前实例添加到列表中
            parent_ui = GetComponentInParent<PlayerUI>(); // 获取父级 UI

            if (animator != null)
                animator.SetBool("Visible", IsVisible()); // 设置动画状态
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            panel_list.Remove(this); // 从列表中移除当前实例
        }

        protected override void Start()
        {
            base.Start();

            // 设置槽位点击、确认和取消事件的处理方法
            onClickSlot += OnClick;
            onPressAccept += OnAccept;
            onPressCancel += OnCancel;
        }

        protected override void Update()
        {
            base.Update();
        }

        //Z这里看不懂有什么作用？？？
        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            // 游戏手柄自动控制
            PlayerCharacter player = GetPlayer(); // 获取玩家角色
            CraftInfoPanel info_panel = CraftInfoPanel.Get(GetPlayerID()); // 获取制作信息面板
            if (UISlotPanel.GetFocusedPanel() == this)
            {
                selection_index = Mathf.Clamp(selection_index, 0, CountActiveSlots() - 1); // 限制选择索引范围

                UISlot slot = GetSelectSlot(); // 获取当前选中的槽位
                if (player != null && !player.Crafting.IsBuildMode())
                {
                    if (prev_slot != slot || !info_panel.IsVisible())
                    {
                        OnClick(slot); // 点击当前槽位
                        prev_slot = slot;
                    }
                }
            }
        }

        //Z真正实现刷新二级建造界面的逻辑
        public void RefreshCraftPanel()
        {
            // 隐藏所有物品槽位
            foreach (ItemSlot slot in slots)
                slot.Hide();

            if (current_category == null || !IsVisible())
                return;

            // 显示类别下的所有物品
            PlayerCharacter player = GetPlayer(); // 获取玩家角色
            if (player != null)
            {
                List<CraftData> items = CraftData.GetAllCraftableInGroup(GetPlayer(), current_category); // 获取该类别中的所有可制作数据（Z：根据从一级建造面板传入的建造组别，遍历里面的可建造数据，更新槽位）

                // 对列表进行排序
                items.Sort((p1, p2) =>
                {
                    return (p1.craft_sort_order == p2.craft_sort_order)
                        ? p1.title.CompareTo(p2.title) : p1.craft_sort_order.CompareTo(p2.craft_sort_order);
                });

                // 更新槽位
                for (int i = 0; i < items.Count; i++)
                {
                    if (i < slots.Length)
                    {
                        CraftData item = items[i];
                        ItemSlot slot = (ItemSlot)slots[i];
                        slot.SetSlot(item, 1, false); // 设置槽位
                        slot.AnimateGain(); // 播放动画
                    }
                }
            }
        }

        //Z这个是在CraftPanel中调用的，更新当前选中类别并显示二级建造面板
        public void ShowCategory(GroupData group)
        {
            Hide(true); // 立即隐藏以进行显示动画

            current_category = group; // 设置当前类别
            title.text = group.title; // 更新标题文本
            
            Show(); // 显示面板
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);

            ShowAnim(true); // 显示动画
            RefreshCraftPanel(); // 刷新制作面板
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);

            current_category = null; // 清除当前类别
            CraftInfoPanel.Get(GetPlayerID())?.Hide(); // 隐藏制作信息面板
            ShowAnim(false); // 隐藏动画

            if (instant && animator != null)
                animator.Rebind(); // 立即重新绑定动画
        }

        private void ShowAnim(bool visible)
        {
            SetVisible(visible); // 设置面板可见性
            if (animator != null)
                animator.SetBool("Visible", IsVisible()); // 更新动画状态
        }

        private void OnClick(UISlot uislot)
        {
            int slot = uislot.index; // 获取槽位索引
            ItemSlot islot = (ItemSlot)uislot;
            CraftData item = islot.GetCraftable(); // 获取可制作数据

            // 取消所有槽位选择
            foreach (ItemSlot aslot in slots)
                aslot.UnselectSlot();

            CraftInfoPanel info_panel = CraftInfoPanel.Get(GetPlayerID()); // 获取制作信息面板
            if (info_panel) {
                if (item == info_panel.GetData())
                {
                    info_panel.Hide(); // 隐藏制作信息面板
                }
                else
                {
                    parent_ui?.CancelSelection(); // 取消父级 UI 的选择
                    slots[slot].SelectSlot(); // 选择当前槽位
                    info_panel.ShowData(item); // 显示制作数据
                }
            }
        }

        private void OnAccept(UISlot slot)
        {
            PlayerCharacter player = PlayerCharacter.Get(GetPlayerID()); // 获取玩家角色
            CraftInfoPanel.Get(GetPlayerID())?.OnClickCraft(); // 执行制作操作
            if (player.Crafting.IsBuildMode())
                UISlotPanel.UnfocusAll(); // 取消所有面板的聚焦
        }

        private void OnCancel(UISlot slot)
        {
            CancelSelection(); // 取消选择
            CraftInfoPanel.Get(GetPlayerID())?.Hide(); // 隐藏制作信息面板
            CraftPanel.Get(GetPlayerID())?.Focus(); // 聚焦 CraftPanel
        }

        public void CancelSelection()
        {
            // 取消所有槽位选择
            for (int i = 0; i < slots.Length; i++)
                slots[i].UnselectSlot();
            CraftInfoPanel.Get(GetPlayerID())?.Hide(); // 隐藏制作信息面板
        }

        public GroupData GetCurrentCategory()
        {
            return current_category; // 获取当前类别
        }

        public PlayerCharacter GetPlayer()
        {
            return parent_ui ? parent_ui.GetPlayer() : PlayerCharacter.GetFirst(); // 获取玩家角色
        }

        public int GetPlayerID()
        {
            PlayerCharacter player = GetPlayer();
            return player != null ? player.player_id : 0; // 获取玩家 ID
        }

        public static CraftSubPanel Get(int player_id = 0)
        {
            foreach (CraftSubPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer(); // 获取面板上的玩家角色
                if (player != null && player.player_id == player_id)
                    return panel; // 返回对应玩家 ID 的面板
            }
            return null; // 如果没有找到则返回 null
        }

        public static new List<CraftSubPanel> GetAll()
        {
            return panel_list; // 返回所有 CraftSubPanel 实例的列表
        }
    }

}
