using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 用于制作类别的按钮槽位
    /// </summary>
    public class CategorySlot : UISlot
    {
        public GroupData group; // 与此槽位关联的类别数据
        public Image icon; // 显示类别图标的 UI 元素
        public Image highlight; // 高亮显示的 UI 元素

        protected override void Start()
        {
            base.Start();

            // 如果类别数据和图标不为空，设置图标
            if (group != null && group.icon != null)
                icon.sprite = group.icon;

            // 初始化时隐藏高亮
            if (highlight)
                highlight.enabled = false;
        }

        protected override void Update()
        {
            base.Update();

            // 如果高亮元素存在，则根据选择状态或悬停状态来显示高亮
            if (highlight != null)
                highlight.enabled = selected || key_hover;
        }

        /// <summary>
        /// 设置槽位的类别数据
        /// </summary>
        /// <param name="group">要设置的类别数据</param>
        public void SetSlot(GroupData group)
        {
            this.group = group; // 设置类别数据
            icon.sprite = group.icon; // 更新图标
            Show(); // 显示槽位
        }
    }
}