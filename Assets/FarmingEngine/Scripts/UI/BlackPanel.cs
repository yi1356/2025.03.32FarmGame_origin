using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 黑色面板，显示文本信息的 UI 面板
    /// </summary>
    public class BlackPanel : UIPanel
    {
        public Text text; // 用于显示文本的 UI 元素

        private static BlackPanel _instance; // 单例实例

        protected override void Awake()
        {
            base.Awake();
            _instance = this; // 设置单例实例
            if (this.text != null)
                this.text.text = ""; // 初始化时文本为空
        }

        /// <summary>
        /// 显示文本信息
        /// </summary>
        /// <param name="text">要显示的文本</param>
        /// <param name="instant">是否立即显示</param>
        public void ShowText(string text, bool instant = false)
        {
            if (this.text != null)
                this.text.text = text; // 设置要显示的文本
            Show(instant); // 显示面板
        }

        /// <summary>
        /// 隐藏面板
        /// </summary>
        /// <param name="instant">是否立即隐藏</param>
        public override void Hide(bool instant = false)
        {
            base.Hide(instant); // 调用基类隐藏方法
            if (this.text != null)
                this.text.text = ""; // 隐藏时清空文本
        }

        /// <summary>
        /// 获取黑色面板的单例实例
        /// </summary>
        /// <returns>黑色面板的单例实例</returns>
        public static BlackPanel Get()
        {
            return _instance; // 返回单例实例
        }
    }

}