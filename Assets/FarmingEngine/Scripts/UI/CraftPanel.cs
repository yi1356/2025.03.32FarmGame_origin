using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 顶级制作面板，包含所有制作类别（Z：根据配置在PlayerCharacterCraft.craft_groups中的可制作组数据，来生成一级建造面板Slot，而且基本只在Show时刷新一次）
    /// </summary>
    public class CraftPanel : UISlotPanel
    {
        [Header("Craft Panel")]
        public Animator animator; // 动画控制器

        private PlayerUI parent_ui; // 父级 UI

        private CraftStation current_staton = null; // 当前工艺站
        private int selected_slot = -1; // 选中的槽位索引
        private UISlot prev_slot; // 上一个槽位

        private List<GroupData> default_categories = new List<GroupData>(); // 默认的分类列表

        private static List<CraftPanel> panel_list = new List<CraftPanel>(); // 存储所有 CraftPanel 实例的列表

        protected override void Awake()
        {
            base.Awake();
            panel_list.Add(this); // 将当前实例添加到列表中
            parent_ui = GetComponentInParent<PlayerUI>(); // 获取父级 UI

            // 将所有类别槽位的默认类别添加到列表中
            for (int i = 0; i < slots.Length; i++)
            {
                CategorySlot cslot = (CategorySlot)slots[i];
                if (cslot.group)
                    default_categories.Add(cslot.group);
            }

            // 设置动画状态
            if (animator != null)
                animator.SetBool("Visible", IsVisible());
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            panel_list.Remove(this); // 从列表中移除当前实例
        }

        protected override void Start()
        {
            base.Start();

            // 设置鼠标点击和右键点击的事件(哪里都可以吗？还是要在哪一个UI上？？？)
            PlayerControlsMouse.Get().onClick += (Vector3) => { CancelSubSelection(); };
            PlayerControlsMouse.Get().onRightClick += (Vector3) => { CancelSelection(); };

            // 设置槽位点击、确认和取消事件的处理方法
            onClickSlot += OnClick;
            //TODU:2025.04.23
            onPressAccept += OnAccept;
            onPressCancel += OnCancel;

            RefreshCategories(); // 刷新类别面板
        }

        protected override void Update()
        {
            base.Update();

            PlayerControls controls = PlayerControls.Get(GetPlayerID());

            // 如果不是游戏手柄控制
            if (!controls.IsGamePad())
            {
                // 按下动作键或攻击键时取消子选择
                if (controls.IsPressAction() || controls.IsPressAttack())
                    CancelSubSelection();
            }
        }



        protected override void RefreshPanel()
        {
            //Debug.Log("测试CraftPanel.RefreshPanel()000000000000000000000000000000000");能执行到
            base.RefreshPanel();
            PlayerCharacter player = GetPlayer(); // 获取玩家角色
            if (player != null)
            {
                //Debug.Log("测试CraftPanel.RefreshPanel()11111111111111111111111111111");能执行到
                CraftStation station = player.Crafting.GetCraftStation(); // 获取当前工艺站
                if (current_staton != station)
                {
                    current_staton = station;//TODO:2025.04.26
                    //Debug.Log("测试CraftPanel.RefreshPanel()22222222222222222222222222222");执行不到
                    RefreshCategories(); // 刷新类别面板
                }
            }
            else
            {
                //Debug.Log("测试CraftPanel.RefreshPanel()3333333333333333333333333333333333");
            }

            // 游戏手柄自动控制
            PlayerControls controls = PlayerControls.Get(GetPlayerID());    
            CraftSubPanel sub_panel = CraftSubPanel.Get(GetPlayerID());
            UISlotPanel focus_panel = UISlotPanel.GetFocusedPanel(); // 获取当前聚焦的面板   
            if (focus_panel != this && focus_panel != sub_panel && !TheUI.Get().IsBlockingPanelOpened()
                && controls.IsGamePad() && player != null && !player.Crafting.IsBuildMode())
            {
                Focus(); // 聚焦当前面板
                CraftInfoPanel.Get(GetPlayerID())?.Hide(); // 隐藏制作信息面板
            }
            if (focus_panel == this)
            {
                selection_index = Mathf.Clamp(selection_index, 0, CountActiveSlots() - 1); // 限制选择索引范围

                UISlot slot = GetSelectSlot(); // 获取当前选中的槽位
                if (prev_slot != slot || !sub_panel.IsVisible())
                {
                    OnClick(slot); // 点击当前槽位
                    sub_panel.selection_index = 0; // 重置子面板选择索引
                    prev_slot = slot;  
                }
            }
        }


        //Z：这个可以理解为，根据可制作物品数据，设置一级制作面板Slot（Z：注意只是一级，基本可理解为只在开始时刷新一次）
        private void RefreshCategories()
        {
            //Debug.Log("测试CraftPanel.RefreshCategories()");
            // 隐藏所有类别槽位
            foreach (CategorySlot slot in slots)
                slot.Hide();

            PlayerCharacter player = GetPlayer(); // 获取玩家角色
            if (player != null)
            {
                int index = 0;
                List<GroupData> groups = player.Crafting.GetCraftGroups(); // 获取制作组数据(Z：注意，这里获得的是配置在PlayerCharacterCraft.craft_groups中的)

                // 设置制作组槽位
                foreach (GroupData group in groups)
                {
                    if (index < slots.Length)
                    {
                        CategorySlot slot = (CategorySlot)slots[index];
                        List<CraftData> items = CraftData.GetAllCraftableInGroup(GetPlayer(), group); // 获取该组中的所有可制作数据
                        if (items.Count > 0)
                        {
                            slot.SetSlot(group); // 设置槽位（Z：也是一级面板的）
                            index++;
                        }
                    }
                }

                CraftSubPanel.Get(GetPlayerID())?.Hide(); // 隐藏子面板
            }
        }

        public override void Show(bool instant = false)
        {
            base.Show(instant);

            CancelSelection(); // 取消选择
            if (animator != null)
                animator.SetBool("Visible", IsVisible()); // 更新动画状态
            CraftSubPanel.Get(GetPlayerID())?.Hide(); // 隐藏子面板

            RefreshCategories(); // 刷新类别面板
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);

            CancelSelection(); // 取消选择
            if (animator != null)
                animator.SetBool("Visible", IsVisible()); // 更新动画状态
            CraftSubPanel.Get(GetPlayerID())?.Hide(); // 隐藏子面板
        }

        //Z点击委托中的方法（Z：其实就是传入可制作组数据group,调用二级建造面板的CraftSubPanel.ShowCategory(GroupData group)打开二级建造面板）
        private void OnClick(UISlot uislot)
        {
            if (uislot != null)
            {
                CategorySlot cslot = (CategorySlot)uislot;

                // 取消所有槽位选择
                for (int i = 0; i < slots.Length; i++)
                    slots[i].UnselectSlot();

                // 如果点击的是当前子面板的类别，隐藏子面板（Z根据包括的数据判断）
                if (cslot.group == CraftSubPanel.Get(GetPlayerID())?.GetCurrentCategory())
                {
                    CraftSubPanel.Get(GetPlayerID())?.Hide();//Z则隐藏这个建造子面板
                }
                else
                {
                    selected_slot = uislot.index; // 设置选中的槽位
                    uislot.SelectSlot(); // 选择当前槽位
                    CraftSubPanel.Get(GetPlayerID())?.ShowCategory(cslot.group); // 显示子面板类别（相当于如果是显示状态则直接更新）（Z会传入类别，根据类别找到有是这一类的物品，如果有则显示/更新二级建造面板）
                }
            }
        }

        //Z这个OnAccept 和  OnCancel不知道是干什么用的
        private void OnAccept(UISlot slot)
        {
            //TUDO2025.04.24
            CraftSubPanel.Get(GetPlayerID())?.Focus(); // 聚焦子面板
        }

        private void OnCancel(UISlot slot)
        {
            Toggle(); // 切换面板显示状态
            CraftSubPanel.Get(GetPlayerID())?.Hide(); // 隐藏子面板
            UISlotPanel.UnfocusAll(); // 取消所有面板的聚焦
        }


        //Z:这个好像是取消二级建造界面所有选项（同时会隐藏三级面板）
        public void CancelSubSelection()
        {
            CraftSubPanel.Get(GetPlayerID())?.CancelSelection(); // 取消子面板选择
        }

        //Z:这个可以理解为 取消一级建造界面所有选项
        public void CancelSelection()
        {
            selected_slot = -1; // 重置选中的槽位
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] != null)
                    slots[i].UnselectSlot(); // 取消槽位选择
            }
            CancelSubSelection(); // 取消子面板选择
        }

        public int GetSelected()
        {
            return selected_slot; // 获取选中的槽位
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

        public static CraftPanel Get(int player_id = 0)
        {
            foreach (CraftPanel panel in panel_list)
            {
                PlayerCharacter player = panel.GetPlayer(); // 获取面板上的玩家角色
                if (player != null && player.player_id == player_id)
                    return panel; // 返回对应玩家 ID 的面板
            }
            return null; // 如果没有找到则返回 null
        }

        public static new List<CraftPanel> GetAll()
        {
            return panel_list; // 返回所有 CraftPanel 实例的列表
        }
    }

}
