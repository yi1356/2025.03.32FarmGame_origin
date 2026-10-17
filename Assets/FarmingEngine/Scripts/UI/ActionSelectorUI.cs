using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 注意与ActionSelector[场景中的物体用]的区分， ActionSelectorUI[背包用] 类似于 ActionSelector，但用于 UI Canvas 中玩家背包中的物品[Z：其实就是右键点击格子会出现的小面板，来显示有什么行为可以用]。
    /// </summary>
    public class ActionSelectorUI : UISlotPanel
    {
        private Animator animator; // 动画控制器

        private PlayerUI parent_ui; // 父级 UI（玩家 UI）
        private ItemSlot slot; // 当前选择的物品槽

        private UISlotPanel prev_panel = null; // 之前的面板，用于恢复焦点

        private static List<ActionSelectorUI> selector_list = new List<ActionSelectorUI>(); // 存储所有 ActionSelectorUI 面板的列表

        protected override void Awake()
        {
            base.Awake();

            selector_list.Add(this); // 将当前面板添加到列表中
            animator = GetComponent<Animator>(); // 获取 Animator 组件
            parent_ui = GetComponentInParent<PlayerUI>(); // 获取父级 UI
            gameObject.SetActive(false); // 初始时面板不可见
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            selector_list.Remove(this); // 从列表中移除当前面板
        }

        protected override void Start()
        {
            base.Start();

            //PlayerControlsMouse.Get().onClick += OnMouseClick;
            PlayerControlsMouse.Get().onRightClick += OnMouseClick; // 注册右键点击事件

            onClickSlot += OnClick; // 注册点击槽位事件
            onPressAccept += OnAccept; // 注册接受操作事件
            onPressCancel += OnCancel; // 注册取消操作事件
            onPressUse += OnCancel; // 注册使用操作事件（与取消相同）
        }

        protected override void Update()
        {
            base.Update();

            // 自动聚焦
            UISlotPanel focus_panel = UISlotPanel.GetFocusedPanel();
            if (focus_panel != this && IsVisible() && PlayerControls.IsAnyGamePad())
            {
                prev_panel = focus_panel;
                Focus(); // 聚焦当前面板
            }
        }

        //Z：获取到这个格子的actions数据，并根据其刷新按钮
        private void RefreshSelector()
        {
            PlayerCharacter character = GetPlayer(); // 获取玩家角色

            foreach (ActionSelectorButton button in slots)
                button.Hide(); // 隐藏所有按钮

            if (slot != null)
            {
                int index = 0;
                foreach (SAction action in slot.GetItem().actions)
                {
                    if (index < slots.Length && !action.IsAuto() && action.CanDoAction(character, slot))
                    {
                        ActionSelectorButton button = (ActionSelectorButton) slots[index];
                        button.SetButton(action); // 设置按钮的操作
                        index++;
                    }
                }
            }
        }

        //Z 根据传入的 ItemSlot其中的Actions列表,显示ActionSelectorUI这个UI
        public void Show(ItemSlot slot)
        {
            PlayerCharacter character = GetPlayer(); // 获取玩家角色
            if (slot != null && character != null)
            {
                if (!IsVisible() || this.slot != slot)
                {
                    this.slot = slot;
                    RefreshSelector(); // 刷新面板上的按钮
                    //animator.SetTrigger("Show");
                    transform.position = slot.transform.position; // 设置面板的位置与物品槽一致
                    gameObject.SetActive(true); // 显示面板
                    animator.Rebind(); // 重新绑定动画
                    animator.SetBool("Solo", CountActiveSlots() == 1); // 如果只有一个按钮，则设置 Solo 标志
                    selection_index = 0;
                    Show(); // 显示面板
                }
            }
        }

        public override void Hide(bool instant = false)
        {
            if (IsVisible())
            {
                base.Hide(instant);
                animator.SetTrigger("Hide"); // 触发隐藏动画
            }
        }

        private void OnClick(UISlot islot)
        {
            ActionSelectorButton button = (ActionSelectorButton)islot;
            OnClickAction(button.GetAction()); // 执行按钮的操作
        }

        private void OnAccept(UISlot slot)
        {
            OnClick(slot); // 执行点击操作
            UISlotPanel.UnfocusAll(); // 取消所有面板的焦点
            if (prev_panel != null)
                prev_panel.Focus(); // 恢复之前面板的焦点
        }

        private void OnCancel(UISlot slot)
        {
            ItemSlotPanel.CancelSelectionAll(); // 取消所有物品槽的选择
            Hide(); // 隐藏面板
        }

        //TODO:2025.05.04
        //Z：点击一个按钮需要执行的行为（SAction）
        public void OnClickAction(SAction action)
        {
            if (IsVisible())
            {
                PlayerCharacter character = GetPlayer(); // 获取玩家角色
                if (action != null && slot != null && character != null)
                {
                    ItemSlot aslot = slot;

                    PlayerUI.Get(character.player_id)?.CancelSelection(); // 取消当前玩家的选择
                    Hide(); // 隐藏面板

                    if (action.CanDoAction(character, aslot))
                        action.DoAction(character, aslot); // 执行操作
                }
            }
        }

        private void OnMouseClick(Vector3 pos)
        {
            Hide(); // 点击时隐藏面板
        }

        public PlayerCharacter GetPlayer()
        {
            return parent_ui ? parent_ui.GetPlayer() : PlayerCharacter.GetFirst(); // 获取玩家角色，如果有父级 UI，则从父级 UI 获取角色
        }

        public static ActionSelectorUI Get(int player_id=0)
        {
            foreach (ActionSelectorUI panel in selector_list)
            {
                PlayerCharacter player = panel.GetPlayer();
                if (player != null && player.player_id == player_id)
                    return panel; // 返回对应玩家 ID 的面板
            }
            return null;
        }

        public static new List<ActionSelectorUI> GetAll()
        {
            return selector_list; // 返回所有 ActionSelectorUI 面板
        }
    }

}
