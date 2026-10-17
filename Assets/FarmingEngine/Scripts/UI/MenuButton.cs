using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine {

    /// <summary>
    /// 通用菜单按钮脚本（用于处理键盘/游戏手柄的操作）
    /// </summary>
    public class MenuButton : UISlot
    {
        [Header("菜单按钮")]
        public string group; // 按钮所在的组
        public GameObject menu_arrow; // 菜单箭头对象
        public bool starting = false; // 是否为组的起始按钮

        private RectTransform arrow; // 箭头的 RectTransform

        private bool is_leader = false; // 是否为组的领导者
        private int selection = 0; // 当前选择的按钮索引
        private float height; // 按钮的高度
        private int start_index = 0; // 组的起始索引
        private List<MenuButton> group_list = new List<MenuButton>(); // 当前组的按钮列表

        private static List<MenuButton> button_list = new List<MenuButton>(); // 所有按钮的列表

        protected override void Awake()
        {
            base.Awake();

            // 如果该组中没有按钮，设置为组的领导者
            if (GetGroup(group).Count == 0)
                is_leader = true;

            // 将当前按钮添加到按钮列表中
            button_list.Add(this);
            button = GetComponent<Button>(); // 获取按钮组件
            rect = GetComponent<RectTransform>(); // 获取 RectTransform 组件
            height = rect.anchoredPosition.y; // 获取按钮的高度

            // 如果有箭头对象，实例化并设置其位置
            if (menu_arrow != null)
            {
                GameObject arro = Instantiate(menu_arrow, transform); // 实例化箭头对象
                arrow = arro.GetComponent<RectTransform>(); // 获取箭头的 RectTransform 组件
                arrow.anchoredPosition = Vector2.left * (rect.sizeDelta.x * 0.5f + arrow.sizeDelta.x * 0.5f); // 设置箭头位置
                arrow.gameObject.SetActive(false); // 默认隐藏箭头
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            button_list.Remove(this); // 从按钮列表中移除当前按钮
        }

        protected override void Start()
        {
            base.Start();

            if (is_leader)
            {
                // 如果是组的领导者，初始化组按钮列表并排序
                group_list = GetGroup(group);
                group_list.Sort((p1, p2) =>
                {
                    return p2.height.CompareTo(p1.height); // 按高度降序排序
                });

                // 设置每个按钮的索引，并确定起始选择
                for (int i = 0; i < group_list.Count; i++) {
                    group_list[i].index = i;
                    if (group_list[i].starting)
                        selection = i;
                }

                MenuButton start = GetGroupStart(group); // 获取组的起始按钮
                start_index = start ? start.index : 0; // 设置起始索引
            }
        }

        protected override void Update()
        {
            base.Update();

            // 如果没有游戏手柄，则退出更新
            if (!PlayerControls.IsAnyGamePad())
                return;

            if (is_leader)
            {
                if (IsVisible())
                {
                    foreach (PlayerControls controls in PlayerControls.GetAll())
                    {
                        // 根据游戏手柄输入调整选择的按钮索引
                        if (controls.IsMenuPressUp())
                        {
                            selection--;
                            selection = Mathf.Clamp(selection, 0, group_list.Count - 1); // 限制索引范围
                        }

                        if (controls.IsMenuPressDown())
                        {
                            selection++;
                            selection = Mathf.Clamp(selection, 0, group_list.Count - 1); // 限制索引范围
                        }

                        // 按下确认按钮时，点击当前选中的按钮
                        if (controls.IsPressMenuAccept())
                        {
                            if (selection >= 0 && selection < group_list.Count)
                            {
                                MenuButton button = group_list[selection];
                                button.Click();
                            }
                        }
                    }

                    // 更新箭头的可见性
                    foreach (MenuButton button in group_list)
                    {
                        button.SetArrow(selection == button.index);
                    }
                }
                else
                {
                    selection = start_index; // 如果按钮不可见，恢复到起始索引
                }
            }
        }

        /// <summary>
        /// 模拟点击按钮
        /// </summary>
        public void Click()
        {
            if(button.enabled && button.interactable)
                button.onClick.Invoke(); // 触发按钮的点击事件
        }

        /// <summary>
        /// 设置箭头的可见性
        /// </summary>
        /// <param name="visible">箭头是否可见</param>
        public void SetArrow(bool visible)
        {
            if (arrow != null)
                arrow.gameObject.SetActive(visible); // 设置箭头的可见性
        }

        /// <summary>
        /// 获取指定组的起始按钮
        /// </summary>
        /// <param name="group">组名</param>
        /// <returns>起始按钮</returns>
        public static MenuButton GetGroupStart(string group)
        {
            foreach (MenuButton button in button_list)
            {
                if (button.group == group && button.starting)
                    return button;
            }
            return null; // 如果没有找到起始按钮，返回 null
        }

        /// <summary>
        /// 获取指定组的所有按钮
        /// </summary>
        /// <param name="group">组名</param>
        /// <returns>指定组的按钮列表</returns>
        public static List<MenuButton> GetGroup(string group)
        {
            List<MenuButton> valid_button = new List<MenuButton>();
            foreach (MenuButton button in button_list)
            {
                if (button.group == group)
                    valid_button.Add(button);
            }
            return valid_button; // 返回指定组的按钮列表
        }
    }
}
