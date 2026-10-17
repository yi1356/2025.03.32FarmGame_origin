using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace FarmingEngine
{
    // 实现了 IPointerEnterHandler 和 IPointerExitHandler 接口的 Tooltip 目标 UI 组件
    public class TooltipTargetUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public TooltipTargetType type; // 提示目标类型

        [Header("Custom")]
        public string title; // 自定义提示的标题
        [TextArea(5, 7)]
        public string desc; // 自定义提示的描述文本
        public Sprite icon; // 自定义提示的图标

        [Header("UI")]
        public float delay = 0.5f; // 显示提示的延迟时间
        public int text_size = 22; // 提示文本的字体大小
        public int width = 400; // 提示框的宽度
        public int height = 200; // 提示框的高度

        private ItemSlot slot; // 物品槽组件
        private Canvas canvas; // 画布组件
        private RectTransform rect; // 画布的 RectTransform
        private float timer = 0f; // 计时器
        private bool hover = false; // 是否悬停标志

        void Awake()
        {
            canvas = GetComponentInParent<Canvas>(); // 获取父级画布组件
            slot = GetComponent<ItemSlot>(); // 获取物品槽组件
            rect = canvas.GetComponent<RectTransform>(); // 获取画布的 RectTransform
        }

        void Start()
        {
            // 在此可以进行其他初始化操作
        }

        void Update()
        {
            if (TooltipPanel.Get() == null)
                return;

            // 如果在桌面平台且处于悬停状态
            if (hover && !TheGame.IsMobile())
            {
                timer += Time.deltaTime; // 更新计时器
                if (timer > delay) // 如果计时器超过延迟时间
                {
                    if (type == TooltipTargetType.Custom)
                    {
                        // 设置自定义提示
                        SetTooltip(title, desc, icon);
                    }
                    else if (slot != null)
                    {
                        // 设置基于 CraftData 的提示
                        CraftData data = slot.GetCraftable();
                        SetTooltip(data);
                    }
                }
            }
        }

        // 设置自定义提示内容
        private void SetTooltip(string title, string text, Sprite icon)
        {
            TooltipPanel.Get().Set(title, text, icon); // 设置提示面板的内容
            TooltipPanel.Get().SetSize(width, height, text_size); // 设置提示面板的大小
        }

        // 设置基于 CraftData 的提示内容
        private void SetTooltip(CraftData data)
        {
            TooltipPanel.Get().Set(data); // 设置提示面板的内容
            TooltipPanel.Get().SetSize(width, height, text_size); // 设置提示面板的大小
        }

        // 当指针进入 UI 元素时调用
        public void OnPointerEnter(PointerEventData eventData)
        {
            timer = 0f; // 重置计时器
            hover = true; // 设置悬停标志为 true
        }

        // 当指针离开 UI 元素时调用
        public void OnPointerExit(PointerEventData eventData)
        {
            timer = 0f; // 重置计时器
            hover = false; // 设置悬停标志为 false
        }

        // 在组件禁用时调用
        void OnDisable()
        {
            hover = false; // 设置悬停标志为 false
        }

        // 获取画布组件
        public Canvas GetCanvas()
        {
            return canvas;
        }

        // 获取画布的 RectTransform
        public RectTransform GetRect()
        {
            return rect;
        }

        // 获取是否处于悬停状态
        public bool IsHover()
        {
            return hover;
        }
    }
}
