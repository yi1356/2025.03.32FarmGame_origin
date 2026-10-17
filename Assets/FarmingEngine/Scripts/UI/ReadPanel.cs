using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 显示阅读面板的 UI 元件
    /// </summary>
    public class ReadPanel : UIPanel
    {
        public int panel_id = 0; // 面板的唯一标识符
        public Text title;       // 显示标题的 Text 组件
        public Text desc;        // 显示描述的 Text 组件
        public Image image;      // 显示图像的 Image 组件

        // 存储所有面板的字典，以 panel_id 为键
        private static Dictionary<int, ReadPanel> panel_list = new Dictionary<int, ReadPanel>();

        protected override void Awake()
        {
            base.Awake();
            // 将当前面板添加到面板列表中
            panel_list[panel_id] = this;
        }

        private void OnDestroy()
        {
            // 从面板列表中移除当前面板
            if (panel_list.ContainsKey(panel_id))
                panel_list.Remove(panel_id);
        }

        protected override void Update()
        {
            base.Update();
            // 在 Update 中可以添加面板更新的逻辑（如果需要）
        }

        /// <summary>
        /// 显示面板，使用标题和描述文本
        /// </summary>
        /// <param name="title">标题文本</param>
        /// <param name="desc">描述文本</param>
        public void ShowPanel(string title, string desc)
        {
            this.title.text = title; // 设置标题文本
            if (this.desc != null)
                this.desc.text = desc; // 设置描述文本
            if (this.image != null)
                image.enabled = false; // 隐藏图像
            
            Show(); // 显示面板
        }

        /// <summary>
        /// 显示面板，使用标题和图像
        /// </summary>
        /// <param name="title">标题文本</param>
        /// <param name="sprite">图像 Sprite</param>
        public void ShowPanel(string title, Sprite sprite)
        {
            this.title.text = title; // 设置标题文本
            if (this.desc != null)
                this.desc.text = ""; // 清空描述文本

            if (this.image != null)
            {
                image.enabled = true; // 显示图像
                image.sprite = sprite; // 设置图像 Sprite
            }

            Show(); // 显示面板
        }

        /// <summary>
        /// 点击确定按钮时隐藏面板
        /// </summary>
        public void ClickOK()
        {
            Hide(); // 隐藏面板
        }

        /// <summary>
        /// 根据 ID 获取对应的面板实例
        /// </summary>
        /// <param name="id">面板的 ID</param>
        /// <returns>对应的面板实例，如果不存在则返回 null</returns>
        public static ReadPanel Get(int id=0)
        {
            if (panel_list.ContainsKey(id))
                return panel_list[id];
            return null;
        }

        /// <summary>
        /// 检查是否有任何面板正在显示
        /// </summary>
        /// <returns>如果有任何面板可见则返回 true，否则返回 false</returns>
        public static bool IsAnyVisible()
        {
            foreach (KeyValuePair<int, ReadPanel> pair in panel_list)
            {
                if (pair.Value.IsVisible())
                    return true;
            }
            return false;
        }
    }

}
