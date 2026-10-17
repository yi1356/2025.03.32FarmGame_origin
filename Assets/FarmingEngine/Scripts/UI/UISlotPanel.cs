using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 基本面板类，包含可以选择的插槽，给 UI 面板加 “插槽交互” 能力（Z：其实这个还是偏抽象一些，真正的逻辑其实在其子类ItemSlotPanel中）
    /// </summary>
    public class UISlotPanel : UIPanel
    {
        //关键功能 
        //1、“插槽数组” 和 “交互事件”
        [Header("插槽面板")]
        public float refresh_rate = 0.1f; // 刷新频率，设置为0f时每帧刷新
        public int slots_per_row = 99; // 每行插槽数量，用于游戏手柄控制（知道行/列的设置）
        public UISlot[] slots; // 插槽数组

        public UnityAction<UISlot> onClickSlot;      // 单击插槽时触发的事件（Z可以这么理解，单击这个面板上的Slot触发这个onClickSlot委托事件；
                                                     // 这个onClickSlot委托会分别在其子类中添加不同方法，最终实现不同面板逻辑；比如建造面板会打开新的下一级建造面板，背包面板会显示选中并悬浮鼠标UI）


        public UnityAction<UISlot> onRightClickSlot; // 右键单击插槽时触发的事件
        public UnityAction<UISlot> onLongClickSlot;  // 长按插槽时触发的事件
        public UnityAction<UISlot> onDoubleClickSlot;// 双击插槽时触发的事件

        public UnityAction<UISlot> onDragStart;    // 开始拖动插槽时触发的事件
        public UnityAction<UISlot> onDragEnd;      // 拖动结束时释放插槽时触发的事件
        public UnityAction<UISlot, UISlot> onDragTo; // 拖动插槽并释放到另一个插槽上时触发的事件（Z：比如拖动UI来进行装备时）

        public UnityAction<UISlot> onPressAccept;  // 按下接受键时触发的事件
        public UnityAction<UISlot> onPressCancel;  // 按下取消键时触发的事件
        public UnityAction<UISlot> onPressUse;     // 按下使用键时触发的事件

        [HideInInspector]
        public int selection_index = 0; // 当前选择的插槽索引，用于游戏手柄控制

        [HideInInspector]
        public bool unfocus_when_out = false; // 当面板失去焦点时是否自动取消焦点

        //[HideInInspector]
        public bool focused = false; // 是否聚焦的面板(Z：暂时没有发现这个在建造时有什么用，且点击Slot时并没有发现它是Ture)

        private float timer = 0f; // 计时器

        private static List<UISlotPanel> slot_panels = new List<UISlotPanel>(); // 存储所有插槽面板的列表

        protected override void Awake()
        {
            base.Awake();
            slot_panels.Add(this); // 将当前面板添加到列表中

            // 为每个插槽设置事件
            for (int i = 0; i < slots.Length; i++)
            {
                int index = i; // 重要：在循环中拷贝索引，避免被覆盖
                slots[i].index = index;
                slots[i].onClick += OnClickSlot;
                slots[i].onClickRight += OnClickSlotRight;
                slots[i].onClickLong += OnClickSlotLong;
                slots[i].onClickDouble += OnClickSlotDouble;

                slots[i].onDragStart += OnDragStart;
                slots[i].onDragEnd += OnDragEnd;
                slots[i].onDragTo += OnDragTo;

                slots[i].onPressAccept += OnPressAccept;
                slots[i].onPressCancel += OnPressCancel;
                slots[i].onPressUse += OnPressUse;
            }
        }

        protected virtual void OnDestroy()
        {
            slot_panels.Remove(this); // 从列表中移除当前面板
        }
        //Z：增加了缓慢（0.1s）更新面板的逻辑
        //2、“定时刷新” 机制
        protected override void Update()
        {
            base.Update();

            timer += Time.deltaTime;
            if (IsVisible())
            {
                if (timer > refresh_rate)
                {
                    timer = 0f;
                    SlowUpdate(); // 执行缓慢更新
                }
            }
        }

        //Z这个方法会定时约0.1s执行一次(在这里是为了会 自动刷新面板)
        private void SlowUpdate()
        {
            RefreshPanel(); // 刷新面板
        }

        /// <summary>
        /// 刷新面板，固定频率0.1s
        /// </summary>
        protected virtual void RefreshPanel()
        {
            // 子类实现具体的面板刷新逻辑
        }

        // 聚焦当前面板
        public void Focus()
        {
            UnfocusAll(); // 取消所有面板的焦点
            focused = true; // 设置当前面板为聚焦状态
            UISlot slot = GetSelectSlot(); // 获取当前选择的插槽
            if (slot == null && slots.Length > 0)
                selection_index = slots[0].index; // 如果没有选择的插槽，选择第一个插槽
        }

        // 按下指定索引的插槽(Z：好像是用在手柄中)
        public void PressSlot(int index)
        {
            UISlot slot = GetSlot(index);
            if (slot != null && onPressAccept != null)
                onPressAccept.Invoke(slot);
        }

        // 按下接受键事件处理（Z好像是和键盘有关，但经过我测试发现Enter键无效）
        private void OnPressAccept(UISlot slot)
        {
            if (onPressAccept != null)
                onPressAccept.Invoke(slot);
        }

        // 按下取消键事件处理
        private void OnPressCancel(UISlot slot)
        {
            if (onPressCancel != null)
                onPressCancel.Invoke(slot);
        }

        // 按下使用键事件处理
        private void OnPressUse(UISlot slot)
        {
            if (onPressUse != null)
                onPressUse.Invoke(slot);
        }

        // 单击插槽事件处理
        private void OnClickSlot(UISlot islot)
        {
            if (onClickSlot != null)
                onClickSlot.Invoke(islot);
        }

        // 右键单击插槽事件处理
        private void OnClickSlotRight(UISlot islot)
        {
            if (onRightClickSlot != null)
                onRightClickSlot.Invoke(islot);
        }

        // 长按插槽事件处理
        private void OnClickSlotLong(UISlot islot)
        {
            if (onLongClickSlot != null)
                onLongClickSlot.Invoke(islot);
        }

        // 双击插槽事件处理
        private void OnClickSlotDouble(UISlot islot)
        {
            if (onDoubleClickSlot != null)
                onDoubleClickSlot.Invoke(islot);
        }

        // 开始拖动插槽事件处理
        private void OnDragStart(UISlot islot)
        {
            if (onDragStart != null)
                onDragStart.Invoke(islot);
        }

        // 拖动结束事件处理
        private void OnDragEnd(UISlot islot)
        {
            if (onDragEnd != null)
                onDragEnd.Invoke(islot);
        }

        // 拖动到另一个插槽事件处理（Z：这个感觉可以重点看下）
        private void OnDragTo(UISlot islot, UISlot target)
        {
            if (onDragTo != null)
                onDragTo.Invoke(islot, target);
        }

        // 计算当前激活的插槽数量
        public int CountActiveSlots()
        {
            int count = 0;
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i].gameObject.activeSelf)
                    count++;
            }
            return count;
        }

        // 根据索引获取插槽
        //3、“插槽工具方法”（按索引找插槽，很方便）
        public UISlot GetSlot(int index)
        {
            foreach (UISlot slot in slots)
            {
                if (slot.index == index)
                    return slot;
            }
            return null;
        }

        // 获取当前选择的插槽
        public UISlot GetSelectSlot()
        {
            return GetSlot(selection_index);
        }

        // 获取当前拖动的插槽
        public ItemSlot GetDragSlot()
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot.IsDrag())
                    return slot;
            }
            return null;
        }

        // 判断当前选择的插槽是否不可见
        public bool IsSelectedInvisible()
        {
            UISlot slot = GetSelectSlot();
            return slot != null && !slot.IsVisible();
        }

        // 判断当前选择的插槽是否有效
        public bool IsSelectedValid()
        {
            UISlot slot = GetSelectSlot();
            return slot != null && slot.IsVisible();
        }

        // 取消所有面板的焦点
        public static void UnfocusAll()
        {
            foreach (UISlotPanel panel in slot_panels)
                panel.focused = false;
        }

        // 获取当前聚焦的面板
        public static UISlotPanel GetFocusedPanel()
        {
            foreach (UISlotPanel panel in slot_panels)
            {
                if (panel.focused)
                    return panel;
            }
            return null;
        }

        // 获取所有插槽面板
        public static List<UISlotPanel> GetAll()
        {
            return slot_panels;
        }
    }
}
