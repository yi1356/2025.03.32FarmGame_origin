using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine {

    [RequireComponent(typeof(CanvasGroup))]
    public class TooltipPanel : UIPanel
    {
        public RectTransform box; // 用于显示提示的框的 RectTransform
        public GameObject icon_group; // 包含图标的 GameObject
        public GameObject text_only_group; // 仅包含文本的 GameObject

        public Text title; // 提示标题的文本
        public Image icon; // 提示图标的 Image
        public Text desc; // 提示描述的文本

        public Text title2; // 备用提示标题的文本
        public Text desc2; // 备用提示描述的文本

        private RectTransform rect; // 当前 Tooltip 面板的 RectTransform
        private Selectable target = null; // 当前显示提示的目标

        private int start_width; // 初始宽度
        private int start_height; // 初始高度
        private int start_text_size; // 初始文本字体大小

        private static TooltipPanel _instance; // 单例实例

        protected override void Awake()
        {
            base.Awake();
            _instance = this; // 设置单例实例
            rect = GetComponent<RectTransform>(); // 获取 RectTransform 组件
            start_width = Mathf.RoundToInt(rect.sizeDelta.x); // 获取初始宽度
            start_height = Mathf.RoundToInt(rect.sizeDelta.y); // 获取初始高度
            start_text_size = desc.fontSize; // 获取初始文本字体大小
        }

        protected override void Start()
        {
            base.Start();
            // 可以在这里添加其他初始化代码
        }

        protected override void Update()
        {
            base.Update();

            RefreshTooltip(); // 刷新提示面板的位置和显示

            if (target == null) // 如果目标为 null，则隐藏提示
                Hide();
        }

        void RefreshTooltip()
        {
            PlayerControlsMouse mouse = PlayerControlsMouse.Get(); // 获取玩家鼠标控制
            rect.anchoredPosition = TheUI.Get().ScreenPointToCanvasPos(mouse.GetMousePosition()); // 更新提示面板的位置

            if (target != null)
            {
                // 如果目标不再被悬停或者鼠标正在移动，隐藏提示
                if (!target.IsHovered() || mouse.IsMovingMouse())
                    Hide();
            }
        }

        private void UpdateAnchoring()
        {
            if (box != null)
            {
                PlayerControlsMouse mouse = PlayerControlsMouse.Get(); // 获取玩家鼠标控制
                Vector2 pos = TheUI.Get().ScreenPointToCanvasPos(mouse.GetMousePosition()); // 获取鼠标在画布上的位置
                Vector2 csize = TheUI.Get().GetCanvasSize() * 0.5f; // 获取画布的中心位置
                // 计算提示框的 pivot
                float pivotX = Mathf.Sign(pos.x - csize.x * 0.5f) * 0.5f + 0.5f;
                float pivotY = Mathf.Sign(pos.y + csize.y * 0.5f) * 0.5f + 0.5f;
                box.pivot = new Vector2(pivotX, pivotY); // 设置提示框的 pivot
                box.anchoredPosition = Vector2.zero; // 设置提示框的位置为零
                rect.anchoredPosition = pos; // 更新提示面板的位置
            }
        }

        public void Set(CraftData data)
        {
            Set(null, data); // 使用 CraftData 设置提示
        }

        public void Set(string atitle, string adesc, Sprite aicon)
        {
            Set(null, atitle, adesc, aicon); // 使用标题、描述和图标设置提示
        }

        public void Set(Selectable target, CraftData data)
        {
            if (data == null)
                return;

            this.target = target; // 设置目标

            // 更新提示的标题、图标和描述
            if (title != null)
                title.text = data.title;
            if (icon != null)
                icon.sprite = data.icon;
            if (desc != null)
                desc.text = data.desc;

            if(title2 != null)
                title2.text = data.title;
            if (desc2 != null)
                desc2.text = data.desc;

            // 根据是否有图标更新显示组
            if (text_only_group != null)
                text_only_group.SetActive(data.icon == null);
            if (icon_group != null)
                icon_group.SetActive(data.icon != null);

            Show(); // 显示提示面板
            UpdateAnchoring(); // 更新提示面板的位置
            RefreshTooltip(); // 刷新提示内容
        }

        public void Set(Selectable target, string atitle, string adesc, Sprite aicon)
        {
            this.target = target; // 设置目标

            // 更新提示的标题、图标和描述
            if (title != null)
                title.text = atitle;
            if (icon != null)
                icon.sprite = aicon;
            if (desc != null)
                desc.text = adesc;

            if (title2 != null)
                title2.text = atitle;
            if (desc2 != null)
                desc2.text = adesc;

            // 根据是否有图标更新显示组
            if (text_only_group != null)
                text_only_group.SetActive(aicon == null);
            if (icon_group != null)
                icon_group.SetActive(aicon != null);

            Show(); // 显示提示面板
            UpdateAnchoring(); // 更新提示面板的位置
            RefreshTooltip(); // 刷新提示内容
        }

        public void SetSize(int width, int height, int text)
        {
            box.sizeDelta = new Vector2(width, height); // 设置提示框的大小
            desc.fontSize = text; // 设置描述文本的字体大小
        }

        public void ResetSize()
        {
            box.sizeDelta = new Vector2(start_width, start_height); // 重置提示框的大小
            desc.fontSize = start_text_size; // 重置描述文本的字体大小
        }

        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            target = null; // 隐藏提示时，清空目标
        }

        public Selectable GetTarget()
        {
            return target; // 获取当前提示的目标
        }

        public static TooltipPanel Get()
        {
            return _instance; // 获取单例实例
        }
    }

}
