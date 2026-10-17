using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 通用插槽类，适用于任何类型的插槽（物品等）
    /// </summary>
    public class UISlot : MonoBehaviour
    {
        [Header("导航")] // 如果为空，则使用默认导航
        public UISlot top;   // 上方插槽
        public UISlot down;  // 下方插槽
        public UISlot left;  // 左侧插槽
        public UISlot right; // 右侧插槽

        [HideInInspector]
        public int index = -1; // 插槽的索引

        public UnityAction<UISlot> onClick;        // 单击插槽时触发的事件
        public UnityAction<UISlot> onClickRight;   // 右键单击插槽时触发的事件
        public UnityAction<UISlot> onClickLong;    // 长按插槽时触发的事件
        public UnityAction<UISlot> onClickDouble;  // 双击插槽时触发的事件

        public UnityAction<UISlot> onDragStart;    // 开始拖动插槽时触发的事件
        public UnityAction<UISlot> onDragEnd;      // 拖动结束时释放插槽时触发的事件
        public UnityAction<UISlot, UISlot> onDragTo; // 拖动插槽并释放到另一个插槽上时触发的事件

        public UnityAction<UISlot> onPressKey;     // 数字键按下时触发的事件
        public UnityAction<UISlot> onPressAccept;  // 接受操作时触发的事件
        public UnityAction<UISlot> onPressCancel;  // 取消操作时触发的事件
        public UnityAction<UISlot> onPressUse;     // 使用操作时触发的事件

        protected Button button;                  // 按钮组件
        protected EventTrigger evt_trigger;       // 事件触发器组件
        protected RectTransform rect;             // RectTransform 组件
        protected UISlotPanel parent;              // 父级 UISlotPanel

        protected bool active = true;             // 插槽是否激活
        protected bool selected = false;          // 插槽是否被选中
        protected bool key_hover = false;         // 键盘焦点状态

        private bool is_holding = false;          // 是否正在长按
        private bool is_dragging = false;         // 是否正在拖动
        private bool can_click = false;           // 是否可以点击
        private float holding_timer = 0f;         // 长按计时器
        private float double_timer = 0f;          // 双击计时器

        private static List<UISlot> slot_list = new List<UISlot>(); // 所有插槽的列表

        protected virtual void Awake()
        {
            slot_list.Add(this); // 将当前插槽添加到列表中
            parent = GetComponentInParent<UISlotPanel>(); // 获取父级 UISlotPanel
            rect = GetComponent<RectTransform>(); // 获取 RectTransform 组件
            evt_trigger = GetComponent<EventTrigger>(); // 获取 EventTrigger 组件
            button = GetComponent<Button>(); // 获取 Button 组件
        }

        protected virtual void OnDestroy()
        {
            slot_list.Remove(this); // 从列表中移除当前插槽
        }

        protected virtual void Start()
        {
            if (evt_trigger != null)
            {
                // 添加点击事件
                EventTrigger.Entry entry1 = new EventTrigger.Entry();
                entry1.eventID = EventTriggerType.PointerClick;
                entry1.callback.AddListener((BaseEventData eventData) => { OnClick(eventData); });
                evt_trigger.triggers.Add(entry1);

                // 添加按下事件
                EventTrigger.Entry entry2 = new EventTrigger.Entry();
                entry2.eventID = EventTriggerType.PointerDown;
                entry2.callback.AddListener((BaseEventData eventData) => { OnDown(eventData); });
                evt_trigger.triggers.Add(entry2);

                // 添加抬起事件
                EventTrigger.Entry entry3 = new EventTrigger.Entry();
                entry3.eventID = EventTriggerType.PointerUp;
                entry3.callback.AddListener((BaseEventData eventData) => { OnUp(eventData); });
                evt_trigger.triggers.Add(entry3);

                // 添加退出事件
                EventTrigger.Entry entry4 = new EventTrigger.Entry();
                entry4.eventID = EventTriggerType.PointerExit;
                entry4.callback.AddListener((BaseEventData eventData) => { OnExit(eventData); });
                evt_trigger.triggers.Add(entry4);
            }

            if (button != null)
            {
                button.onClick.AddListener(ClickSlot); // 添加点击按钮的监听器
            }
        }

        protected virtual void Update()
        {
            // 双击计时器
            if (double_timer < 1f)
                double_timer += Time.deltaTime;

            // 长按操作
            if (is_holding)
            {
                holding_timer += Time.deltaTime;
                if (holding_timer > 0.5f)
                {
                    can_click = false;
                    is_holding = false;

                    if (onClickLong != null)
                        onClickLong.Invoke(this);
                }
            }

            // 键盘快捷键
            int key_index = (index + 1);
            if (key_index == 10)
                key_index = 0;
            if (key_index < 10 && PlayerControls.Get().IsPressedByName(key_index.ToString()))
            {
                if (onPressKey != null)
                    onPressKey.Invoke(this);
            }

            bool use_mouse = PlayerControlsMouse.Get().IsUsingMouse();
            key_hover = false;
            foreach (KeyControlsUI kcontrols in KeyControlsUI.GetAll())
            {
                bool hover = !use_mouse && kcontrols != null && kcontrols.GetFocusedPanel() == parent
                    && index >= 0 && kcontrols.GetSelectedIndex() == index;
                key_hover = key_hover || hover;
            }

            // 控制插槽的显示状态
            if (!active)
                gameObject.SetActive(false);
        }

        // 选择插槽
        public void SelectSlot()
        {
            selected = true;
        }

        // 取消选择插槽
        public void UnselectSlot()
        {
            selected = false;
        }

        // 设置插槽的选择状态
        public void SetSelected(bool sel)
        {
            selected = sel;
        }

        // 判断插槽是否被选中
        public bool IsSelected()
        {
            return selected;
        }

        // 显示插槽
        public void Show()
        {
            active = true;
            if (active != gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        // 隐藏插槽
        public void Hide()
        {
            active = false;
        }

        // 单击插槽
        public void ClickSlot()
        {
            if (onClick != null)
                onClick.Invoke(this);
        }

        // 右键单击插槽
        public void ClickRightSlot()
        {
            if (onClickRight != null)
                onClickRight.Invoke(this);
        }

        // 按下接受键
        public void KeyPressAccept()
        {
            Debug.Log("测试按下了UI");
            if (onPressAccept != null)
                onPressAccept.Invoke(this);
        }

        // 按下取消键
        public void KeyPressCancel()
        {
            if (onPressCancel != null)
                onPressCancel.Invoke(this);
        }

        // 按下使用键
        public void KeyPressUse()
        {
            if (onPressUse != null)
                onPressUse.Invoke(this);
        }

        // 点击事件处理
        void OnClick(BaseEventData eventData)
        {
            if (can_click)
            {
                // 可以在此处理点击事件
            }
        }

        // 按下事件处理
        void OnDown(BaseEventData eventData)
        {
            is_holding = true;
            is_dragging = false;
            can_click = true;
            holding_timer = 0f;

            PointerEventData pEventData = eventData as PointerEventData;

            if (pEventData.button == PointerEventData.InputButton.Right)
            {
                if (onClickRight != null)
                    onClickRight.Invoke(this);
            }
            else if (pEventData.button == PointerEventData.InputButton.Left)
            {
                if (double_timer < 0f)
                {
                    double_timer = 0f;
                    if (onClickDouble != null)
                        onClickDouble.Invoke(this);
                }
                else
                {
                    double_timer = -0.3f;
                    if (onClick != null)
                        onClick.Invoke(this);
                }
            }
        }

        // 松开事件处理
        void OnUp(BaseEventData eventData)
        {
            is_holding = false;

            // 拖放操作
            if (is_dragging)
            {
                is_dragging = false;
                onDragEnd?.Invoke(this);
                Vector3 anchor_pos = TheUI.Get().ScreenPointToCanvasPos(PlayerControlsMouse.Get().GetMousePosition());
                UISlot target = UISlot.GetNearestActive(anchor_pos, 50f);//根据鼠标松开时的画布坐标 `anchor_pos`，在**50 像素范围**内，查找所有激活的插槽，返回**离这个坐标最近的那个 UISlot**
                if (target != null && target != this)
                    onDragTo?.Invoke(this, target);
            }
        }

        // 退出事件处理
        void OnExit(BaseEventData eventData)
        {
            bool hold = PlayerControlsMouse.Get().IsMouseHoldUI();
            if (is_holding && hold)
            {
                is_holding = false;
                is_dragging = true;
                onDragStart?.Invoke(this);
            }
        }

        // 判断插槽是否可见
        public bool IsVisible()
        {
            return gameObject.activeSelf && (parent == null || parent.IsVisible());
        }

        // 判断是否正在拖动
        public bool IsDrag()
        {
            return is_dragging;
        }

        // 获取 RectTransform 组件
        public RectTransform GetRect()
        {
            return rect;
        }

        // 获取父级 UISlotPanel
        public UISlotPanel GetParent()
        {
            return parent;
        }

        // 获取当前正在拖动的插槽
        public static UISlot GetDrag()
        {
            foreach (UISlot slot in slot_list)
            {
                if (slot.IsDrag())
                    return slot;
            }
            return null;
        }

        // 获取距离给定位置最近的激活插槽
        public static UISlot GetNearestActive(Vector2 anchor_pos, float range = 999f)
        {
            UISlot nearest = null;
            float min_dist = range;
            foreach (UISlot slot in slot_list)
            {
                Vector2 canvas_pos = TheUI.Get().WorldToCanvasPos(slot.transform.position);//把一个物体的世界坐标，转换成 Canvas 画布空间下的坐标
                float dist = (canvas_pos - anchor_pos).magnitude;
                if (dist < min_dist && slot.gameObject.activeInHierarchy)
                {
                    min_dist = dist;
                    nearest = slot;
                }
            }
            return nearest;
        }
    }
}
