using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 商店槽位，显示单个物品及其价格
    /// </summary>
    public class ShopSlot : UISlot
    {
        [Header("Item Slot")]
        public Image icon;        // 物品图标
        public Text quantity;    // 物品数量文本
        public Text title;       // 物品标题文本
        public Text cost;        // 物品价格文本
        public Image highlight;  // 高亮显示

        private Animator animator; // 动画控制器

        private ItemData item;     // 当前物品数据
        private bool is_sell;      // 是否是出售模式

        protected override void Start()
        {
            base.Start();

            animator = GetComponent<Animator>(); // 获取动画控制器组件

            // 初始化高亮和标题显示状态
            if (highlight)
                highlight.enabled = false;
            if (title)
                title.enabled = false;
        }

        protected override void Update()
        {
            base.Update();

            // 根据是否选中或键盘悬停状态更新高亮显示
            if (highlight != null)
                highlight.enabled = selected || key_hover;
            if (title != null)
                title.enabled = selected;
        }

        // 设置购买槽位
        public void SetBuySlot(ItemData item, int cost)
        {
            SetSlot(item, cost, 1, false);
        }

        // 设置出售槽位
        public void SetSellSlot(ItemData item, int cost, int quantity, bool active)
        {
            SetSlot(item, cost, quantity, true, active);
        }

        // 设置槽位的详细信息
        private void SetSlot(ItemData item, int cost, int quantity, bool sell, bool active=true)
        {
            if (item != null)
            {
                CraftData prev = this.item;
                icon.sprite = item.icon; // 设置物品图标
                icon.enabled = true;
                icon.color = new Color(icon.color.r, icon.color.g, icon.color.b, active ? 1f : 0.5f); // 根据活跃状态调整图标透明度
                this.quantity.text = quantity.ToString(); // 设置数量文本
                this.quantity.enabled = quantity > 1; // 仅当数量大于1时显示
                this.cost.text = cost.ToString(); // 设置价格文本
                this.cost.enabled = active; // 根据活跃状态决定是否显示价格
                this.item = item; // 设置当前物品
                this.is_sell = sell; // 设置是否为出售模式

                // 更新标题文本和显示状态
                if (title != null)
                {
                    title.enabled = selected;
                    title.text = item.title;
                }

                // 如果物品有所更改，触发获得物品动画
                if (prev != item)
                    AnimateGain();
            }
            else
            {
                // 如果没有物品，隐藏所有显示
                this.item = null;
                this.quantity.enabled = false;
                this.cost.enabled = false;
                icon.enabled = false;
                this.selected = false;

                if (highlight != null)
                    highlight.enabled = false;

                if (title != null)
                    title.enabled = false;
            }

            Show(); // 显示槽位
        }

        // 播放获得物品的动画
        public void AnimateGain()
        {
            if (animator != null)
                animator.SetTrigger("Gain");
        }

        // 获取可制作的物品数据
        public CraftData GetCraftable()
        {
            return item;
        }

        // 获取当前物品数据
        public ItemData GetItem()
        {
            if (item != null)
                return item.GetItem();
            return null;
        }

        /// <summary>
        /// Z：判断是否为出售模式
        /// </summary>
        /// <returns></returns>
        public bool IsSell()
        {
            return is_sell;
        }
    }
}
