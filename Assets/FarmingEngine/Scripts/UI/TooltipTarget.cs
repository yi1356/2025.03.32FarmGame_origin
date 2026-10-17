using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    // 枚举类型：提示目标类型
    public enum TooltipTargetType
    {
        Automatic = 0, // 自动类型
        Custom = 10,   // 自定义类型
    }

    [RequireComponent(typeof(Selectable))]
    public class TooltipTarget : MonoBehaviour
    {
        public TooltipTargetType type; // 提示目标类型

        [Header("Custom")]
        public string title; // 自定义提示的标题
        public Sprite icon; // 自定义提示的图标
        [TextArea(3, 5)]
        public string text; // 自定义提示的文本

        [Header("UI")]
        public int text_size = 22; // 提示文本的字体大小
        public int width = 400; // 提示框的宽度
        public int height = 200; // 提示框的高度

        private Selectable select; // 选择组件
        private Construction construct; // 建造组件
        private Plant plant; // 植物组件
        private Item item; // 物品组件
        private Character character; // 角色组件

        void Awake()
        {
            select = GetComponent<Selectable>(); // 获取选择组件

            // 获取其他相关组件
            construct = GetComponent<Construction>();
            plant = GetComponent<Plant>();
            item = GetComponent<Item>();
            character = GetComponent<Character>();
        }

        void Update()
        {
            // 如果 TooltipPanel 实例为空，或者是移动设备，则返回
            if (TooltipPanel.Get() == null)
                return;
            if (TheGame.IsMobile())
                return;

            PlayerControlsMouse mouse = PlayerControlsMouse.Get(); // 获取玩家鼠标控制
            // 如果选择项被悬停且鼠标未移动，并且 TooltipPanel 的目标不是当前选择项
            if (select.IsHovered() && !mouse.IsMovingMouse(0.25f) && TooltipPanel.Get().GetTarget() != select)
            {
                // 根据提示目标类型设置提示内容
                if (type == TooltipTargetType.Custom)
                {
                    // 自定义提示
                    SetTooltip(select, title, text, icon);
                }
                else
                {
                    // 自动提示，基于组件的数据
                    if (construct != null)
                        SetTooltip(select, construct.data); // 建造数据
                    else if (plant != null)
                        SetTooltip(select, plant.data); // 植物数据
                    else if (item != null)
                        SetTooltip(select, item.data); // 物品数据
                    else if (character != null)
                        SetTooltip(select, character.data); // 角色数据
                    else
                        SetTooltip(select, title, text, icon); // 默认提示
                }
            }
        }

        // 设置提示内容，使用标题、文本和图标
        private void SetTooltip(Selectable target, string title, string text, Sprite icon)
        {
            TooltipPanel.Get().Set(target, title, text, icon); // 设置提示面板
            TooltipPanel.Get().SetSize(width, height, text_size); // 设置提示面板的大小
        }

        // 设置提示内容，使用 CraftData
        private void SetTooltip(Selectable target, CraftData data)
        {
            TooltipPanel.Get().Set(target, data); // 设置提示面板
            TooltipPanel.Get().SetSize(width, height, text_size); // 设置提示面板的大小
        }
    }
}
