using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 允许使用键盘/游戏手柄控制 UI
    /// </summary>
    public class KeyControlsUI : MonoBehaviour
    {
        public int player_id; // 玩家 ID

        public UISlotPanel default_top; // 默认的向上面板
        public UISlotPanel default_down; // 默认的向下面板
        public UISlotPanel default_left; // 默认的向左面板
        public UISlotPanel default_right; // 默认的向右面板

        private static List<KeyControlsUI> controls_ui_list = new List<KeyControlsUI>(); // 所有 KeyControlsUI 实例的列表

        void Awake()
        {
            controls_ui_list.Add(this); // 将当前实例添加到列表中
        }

        private void OnDestroy()
        {
            controls_ui_list.Remove(this); // 从列表中移除当前实例
        }

        void Update()
        {
            PlayerControls controls = PlayerControls.Get(player_id); // 获取玩家控制

            if (!controls.IsGamePad())
                return; // 如果不是游戏手柄控制，退出更新

            // 根据玩家的输入方向进行导航
            if (controls.IsUIPressLeft())
                Navigate(Vector2.left);
            else if (controls.IsUIPressRight())
                Navigate(Vector2.right);
            else if(controls.IsUIPressUp())
                Navigate(Vector2.up);
            else if (controls.IsUIPressDown())
                Navigate(Vector2.down);

            // 如果当前面板失去焦点，则停止导航
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel();
            if (selected_panel != null && !selected_panel.IsVisible())
                StopNavigate();
            else if (selected_panel != null && selected_panel.GetSelectSlot() != null && !selected_panel.GetSelectSlot().IsVisible())
                StopNavigate();

            // 处理 UI 控制输入
            if (controls.IsPressUISelect())
                OnPressSelect();

            if (controls.IsPressUIUse())
                OnPressUse();

            if (controls.IsPressUICancel())
                OnPressCancel();

            if (controls.IsPressAttack())
                OnPressAttack();
        }

        /// <summary>
        /// 根据方向进行导航
        /// </summary>
        /// <param name="dir">导航方向</param>
        public void Navigate(Vector2 dir)
        {
            UISlotPanel selected_panel = GetFocusedPanel(); // 获取当前焦点面板
            Navigate(selected_panel, dir); // 导航到指定方向
        }

        /// <summary>
        /// 在指定面板中根据方向进行导航
        /// </summary>
        /// <param name="panel">目标面板</param>
        /// <param name="dir">导航方向</param>
        public void Navigate(UISlotPanel panel, Vector2 dir)
        {
            UISlot current = panel?.GetSelectSlot(); // 获取当前选中的槽位
            if (panel == null || current == null)
            {
                // 如果面板或当前槽位为空，根据方向选择默认面板并聚焦
                if (IsLeft(dir))
                    panel = default_left;
                else if (IsRight(dir))
                    panel = default_right;
                else if (IsUp(dir))
                    panel = default_top;
                else if (IsDown(dir))
                    panel = default_down;
                panel.Focus();
            }
            else
            {
                // 根据方向进行导航到相邻的槽位
                if (IsLeft(dir) && current.left)
                    NavigateTo(current.left, dir);
                else if (IsRight(dir) && current.right)
                    NavigateTo(current.right, dir);
                else if (IsUp(dir) && current.top)
                    NavigateTo(current.top, dir);
                else if (IsDown(dir) && current.down)
                    NavigateTo(current.down, dir);
                else
                    NavigateAuto(panel, dir);
            }
        }

        /// <summary>
        /// 在面板中自动进行导航
        /// </summary>
        /// <param name="panel">目标面板</param>
        /// <param name="dir">导航方向</param>
        public void NavigateAuto(UISlotPanel panel, Vector2 dir)
        {
            if (panel != null)
            {
                int slots_per_row = panel.slots_per_row; // 每行槽位数
                int prev_select = panel.selection_index; // 记录之前的选中索引

                // 根据方向更新选中索引
                if (IsLeft(dir))
                    panel.selection_index--;
                else if (IsRight(dir))
                    panel.selection_index++;
                else if (IsUp(dir))
                    panel.selection_index -= slots_per_row;
                else if (IsDown(dir))
                    panel.selection_index += slots_per_row;

                // 如果当前选择不可见，则继续相同方向的导航
                if (panel.IsSelectedInvisible())
                    Navigate(panel, dir);
                if (!panel.unfocus_when_out && !panel.IsSelectedValid())
                    panel.selection_index = prev_select; // 如果不自动取消焦点且选择无效，则恢复之前的索引
                if (panel.unfocus_when_out && !panel.IsSelectedValid())
                    UISlotPanel.UnfocusAll(); // 如果自动取消焦点且选择无效，则取消所有焦点
            }
        }

        /// <summary>
        /// 导航到指定槽位
        /// </summary>
        /// <param name="slot">目标槽位</param>
        /// <param name="dir">导航方向</param>
        public void NavigateTo(UISlot slot, Vector2 dir)
        {
            UISlotPanel panel = slot?.GetParent(); // 获取槽位所在的面板
            if (panel != null && panel.IsVisible())
            {
                panel.Focus(); // 聚焦面板
                panel.selection_index = slot.index; // 设置选择索引
            }
            else
            {
                Navigate(panel, dir); // 如果面板不可见，则进行默认导航
            }
        }

        /// <summary>
        /// 停止导航
        /// </summary>
        public void StopNavigate()
        {
            ActionSelector.Get(player_id)?.Hide(); // 隐藏动作选择器
            ActionSelectorUI.Get(player_id)?.Hide(); // 隐藏动作选择器 UI
            UISlotPanel.UnfocusAll(); // 取消所有面板的焦点
        }

        private void OnPressSelect()
        {
            Debug.Log("测试+KeyControlsUI.OnPressSelect方法已经执行");
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel(); // 获取当前焦点面板
            UISlot selected_slot = selected_panel?.GetSelectSlot(); // 获取当前选中的槽位
            if (selected_slot != null)
            {
                selected_slot.KeyPressAccept(); // 处理选择操作
            }

            // 如果读取面板完全可见，则隐藏它
            if (ReadPanel.Get().IsFullyVisible())
                ReadPanel.Get().Hide();
        }

        private void OnPressUse()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel(); // 获取当前焦点面板
            UISlot selected_slot = selected_panel?.GetSelectSlot(); // 获取当前选中的槽位
            if (selected_slot != null)
            {
                selected_slot.KeyPressUse(); // 处理使用操作
            }
        }

        private void OnPressCancel()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel(); // 获取当前焦点面板
            UISlot selected_slot = selected_panel?.GetSelectSlot(); // 获取当前选中的槽位
            if (selected_slot != null)
            {
                selected_slot.KeyPressCancel(); // 处理取消操作
            }

            // 如果读取面板可见，则隐藏它
            if (ReadPanel.Get().IsVisible())
                ReadPanel.Get().Hide();
        }

        private void OnPressAttack()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel(); // 获取当前焦点面板
            // 如果当前面板是 InventoryPanel 或 EquipPanel，取消所有选择并取消焦点
            if (selected_panel is InventoryPanel || selected_panel is EquipPanel)
            {
                ItemSlotPanel.CancelSelectionAll();
                UISlotPanel.UnfocusAll();
            }
        }

        /// <summary>
        /// 获取选中的槽位
        /// </summary>
        /// <returns>选中的槽位</returns>
        public UISlot GetSelectedSlot()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel(); // 获取当前焦点面板
            UISlot selected_slot = selected_panel?.GetSelectSlot(); // 获取当前选中的槽位
            return selected_slot;
        }

        /// <summary>
        /// 获取当前焦点面板
        /// </summary>
        /// <returns>当前焦点面板</returns>
        public UISlotPanel GetFocusedPanel()
        {
            return UISlotPanel.GetFocusedPanel(); // 获取当前焦点面板
        }

        /// <summary>
        /// 获取当前选中的索引
        /// </summary>
        /// <returns>当前选中的索引</returns>
        public int GetSelectedIndex()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel(); // 获取当前焦点面板
            if (selected_panel != null)
                return selected_panel.selection_index; // 返回当前选中的索引
            return -1; // 如果没有选中的面板，返回 -1
        }

        /*public bool IsCraftPanelFocus()
        {
            if (selected_panel == null)
                return false;
            return selected_panel == CraftPanel.Get(player_id) || selected_panel == CraftSubPanel.Get(player_id) || selected_panel == CraftInfoPanel.Get(player_id);
        }*/

        /// <summary>
        /// 判断是否是动作选择器
        /// </summary>
        /// <returns>是否是动作选择器</returns>
        public bool IsActionSelector()
        {
            return !ActionSelector.Get().IsFullyHidden() || !ActionSelectorUI.Get().IsFullyHidden();
        }

        /// <summary>
        /// 判断是否有面板处于焦点
        /// </summary>
        /// <returns>是否有面板处于焦点</returns>
        public bool IsPanelFocus()
        {
            return GetFocusedPanel() != null || IsActionSelector();
        }

        /// <summary>
        /// 判断当前焦点是否是面板上的物品槽
        /// </summary>
        /// <returns>是否是物品槽</returns>
        public bool IsPanelFocusItem()
        {
            UISlotPanel selected_panel = UISlotPanel.GetFocusedPanel(); // 获取当前焦点面板
            UISlot slot = selected_panel?.GetSelectSlot(); // 获取当前选中的槽位
            ItemSlot islot = (slot != null && slot is ItemSlot) ? (ItemSlot)slot : null; // 判断槽位是否为物品槽
            return islot != null && islot.GetItem() != null; // 返回是否有物品
        }

        /// <summary>
        /// 判断是否使用游戏手柄
        /// </summary>
        /// <returns>是否使用游戏手柄</returns>
        public bool IsGamePad()
        {
            PlayerControls controls = PlayerControls.Get(player_id); // 获取玩家控制
            return controls ? controls.IsGamePad() : false; // 判断是否使用游戏手柄
        }

        // 判断方向
        public bool IsLeft(Vector2 dir) { return dir.x < -0.1f; }
        public bool IsRight(Vector2 dir) { return dir.x > 0.1f; }
        public bool IsDown(Vector2 dir) { return dir.y < -0.1f; }
        public bool IsUp(Vector2 dir) { return dir.y > 0.1f; }

        /// <summary>
        /// 根据玩家 ID 获取 KeyControlsUI 实例
        /// </summary>
        /// <param name="player_id">玩家 ID</param>
        /// <returns>对应的 KeyControlsUI 实例</returns>
        public static KeyControlsUI Get(int player_id=0)
        {
            foreach (KeyControlsUI panel in controls_ui_list)
            {
                if (panel.player_id == player_id)
                    return panel;
            }
            return null; // 如果没有找到对应的实例，返回 null
        }

        /// <summary>
        /// 获取所有 KeyControlsUI 实例
        /// </summary>
        /// <returns>所有 KeyControlsUI 实例</returns>
        public static List<KeyControlsUI> GetAll()
        {
            return controls_ui_list;
        }
    }
}
