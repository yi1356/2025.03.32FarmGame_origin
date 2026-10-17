using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmingEngine
{

    /// <summary>
    /// ActionSelector 是一个面板，注意与ActionSelectorUI[背包用]区分，当点击可选择的对象时弹出，允许选择一个操作。
    /// </summary>
    public class ActionSelector : UISlotPanel
    {
        private Animator animator; // 动画控制器

        private PlayerCharacter character; // 当前控制的玩家角色
        private Selectable select; // 当前选择的对象
        private Vector3 interact_pos; // 与对象交互的位置

        private static List<ActionSelector> selector_list = new List<ActionSelector>(); // 存储所有 ActionSelector 面板的列表

        protected override void Awake()
        {
            base.Awake();

            selector_list.Add(this); // 将当前面板添加到列表中
            animator = GetComponent<Animator>(); // 获取 Animator 组件
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

            if (!IsVisible()) // 如果面板不可见，直接返回
                return;

            if (character != null && select != null)
            {
                // 计算玩家与交互位置的距离，如果超过范围则隐藏面板
                float dist = (interact_pos - character.transform.position).magnitude;
                if (dist > select.GetUseRange(character) * 1.2f)
                {
                    Hide();
                }
            }

            // 获取相机的前向方向并设置面板的旋转
            Vector3 dir = TheCamera.Get().GetFacingFront();
            transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

            if (select == null) // 如果没有选择对象，则隐藏面板
                Hide();

            // 自动聚焦
            TheCamera cam = TheCamera.Get();
            bool gamepad = PlayerControls.IsAnyGamePad();
            UISlotPanel focus_panel = UISlotPanel.GetFocusedPanel();
            if (focus_panel != this && gamepad && !cam.IsFreeRotation())
                Focus();

            // 游戏手柄瞄准
            PlayerControls controls = PlayerControls.Get();
            PlayerControlsMouse mcontrols = PlayerControlsMouse.Get();
            if (gamepad && cam.IsFreeRotation())
            {
                if (controls.IsPressAction())
                {
                    Vector3 mpos = mcontrols.GetCursorPosition();
                    List<RaycastResult> results = TheUI.RaycastAllUI(mpos);
                    List<GameObject> obj = new List<GameObject>();
                    foreach (RaycastResult res in results)
                    {
                        obj.Add(res.gameObject);
                    }
                    foreach (ActionSelectorButton button in slots)
                    {
                        if (button != null && obj.Contains(button.gameObject))
                            button.ClickSlot();
                    }
                }
            }
        }
        /// <summary>
        /// Z：根据Show中传入的Sleletable中的Actions列表，生成对应的按钮
        /// </summary>
        private void RefreshSelector()
        {
            foreach (ActionSelectorButton button in slots)
                button.Hide(); // 隐藏所有按钮

            if (select != null)
            {
                int index = 0;
                foreach (SAction action in select.actions)
                {
                    if (index < slots.Length && !action.IsAuto() && action.CanDoAction(character, select))
                    {
                        ActionSelectorButton button = (ActionSelectorButton) slots[index];
                        button.SetButton(action); // 设置按钮的操作
                        index++;
                    }
                }
            }
        }

        public void Show(PlayerCharacter character, Selectable select, Vector3 pos)
        {
            if (select != null && character != null)
            {
                if (!IsVisible() || this.select != select || this.character != character)
                {
                    this.select = select;
                    this.character = character;
                    RefreshSelector(); // 刷新面板上的按钮
                    animator.Rebind(); // 重新绑定动画
                    //animator.SetTrigger("Show");
                    transform.position = pos;
                    interact_pos = pos;
                    gameObject.SetActive(true); // 显示面板
                    selection_index = 0;
                    Show();
                }
            }
        }

        public override void Hide(bool instant = false)
        {
            if (IsVisible())
            {
                base.Hide(instant);
                select = null; // 清空选择对象
                character = null; // 清空角色
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
            OnClick(slot);
            UISlotPanel.UnfocusAll(); // 取消所有面板的焦点
        }

        private void OnCancel(UISlot slot) {
            Hide(); // 取消操作时隐藏面板
        }

        public void OnClickAction(SAction action)
        {
            if (IsVisible())
            {
                if (action != null && select != null && character != null)
                {
                    character.FaceTorward(interact_pos); // 让角色面向交互位置

                    if (action.CanDoAction(character, select))
                        action.DoAction(character, select); // 执行操作

                    Hide();
                }
            }
        }

        private void OnMouseClick(Vector3 pos)
        {
            Hide(); // 点击时隐藏面板
        }

        public Selectable GetSelectable()
        {
            return select; // 获取当前选择的对象
        }

        public PlayerCharacter GetPlayer()
        {
            return character; // 获取当前控制的玩家角色
        }

        public static ActionSelector Get(int player_id=0)
        {
            foreach (ActionSelector panel in selector_list)
            {
                if (panel.character == null)
                {
                    panel.character = PlayerCharacter.Get(player_id); // 分配角色
                }

                if (panel.character != null && panel.character.player_id == player_id)
                    return panel; // 返回对应玩家 ID 的面板
            }
            return null;
        }

        public static new List<ActionSelector> GetAll()
        {
            return selector_list; // 返回所有 ActionSelector 面板
        }
    }

}
