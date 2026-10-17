using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// ActionSelector 中的一个按钮
    /// </summary>
    public class ActionSelectorButton : UISlot
    {
        [Header("Selector Button")]
        public Text title; // 按钮上的标题文本
        public Image highlight; // 高亮显示的图像

        private SAction action; // 按钮对应的操作

        protected override void Awake()
        {
            base.Awake();

            if (highlight != null)
                highlight.enabled = false; // 初始化时高亮图像不可见
        }

        protected override void Update()
        {
            base.Update();

            if (highlight != null)
                highlight.enabled = key_hover; // 当鼠标悬停时显示高亮图像
        }

        public void SetButton(SAction action)
        {
            this.action = action; // 设置按钮对应的操作
            title.text = action.title; // 更新按钮标题
            Show(); // 显示按钮
        }

        public SAction GetAction()
        {
            return action; // 获取按钮对应的操作
        }
    }

}