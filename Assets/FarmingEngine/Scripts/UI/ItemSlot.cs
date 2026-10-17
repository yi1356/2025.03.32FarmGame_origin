using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 显示单个物品的槽位，用于库存或装备栏
    /// </summary>
    public class ItemSlot : UISlot
    {
        [Header("Item Slot")]
        public Image icon; // 显示物品图标的 UI 元素
        public Text value; // 显示物品数量的 UI 元素
        public Text title; // 显示物品标题的 UI 元素
        public Text dura; // 显示物品耐久度的 UI 元素

        [Header("Extra")]
        public Image default_icon; // 默认图标 UI 元素
        public Image highlight; // 高亮显示 UI 元素
        public Image filter; // 过滤器 UI 元素

        private Animator animator; // 动画控制器
        [Tooltip("当前物品数据")]
        public CraftData item; // 当前物品数据
        private int quantity; // 当前物品数量
        private float durability; // 当前物品耐久度

        private float highlight_opacity = 1f; // 高亮的透明度

        protected override void Start()
        {
            base.Start();

            animator = GetComponent<Animator>(); // 获取动画控制器

            if (highlight)
            {
                highlight.enabled = false; // 默认不显示高亮
                highlight_opacity = highlight.color.a; // 保存高亮的透明度
            }

            if (dura)
                dura.enabled = false; // 默认不显示耐久度
        }

        protected override void Update()
        {
            base.Update();

            if (highlight != null)
            {
                highlight.enabled = selected || key_hover; // 高亮显示条件
                float alpha = selected ? highlight_opacity : (highlight_opacity * 0.8f); // 根据选择状态调整透明度
                highlight.color = new Color(highlight.color.r, highlight.color.g, highlight.color.b, alpha);
            }
        }
        //TODO:2025.04.28

        /// <summary>
        /// 设置槽位的数据（Z更新数据并显示本槽位）
        /// </summary>
        /// <param name="item">物品数据</param>
        /// <param name="quantity">物品数量</param>
        /// <param name="selected">是否被选中</param>
        public void SetSlot(CraftData item, int quantity, bool selected = false)
        {
            if (item != null)
            {
                CraftData prev = this.item;
                int prevq = this.quantity;
                this.item = item;
                this.quantity = quantity;
                this.durability = 0f;
                icon.sprite = item.icon; // 设置物品图标
                icon.enabled = true;
                value.text = quantity.ToString(); // 设置物品数量
                value.enabled = quantity > 1;
                this.selected = selected;

                if (title != null)
                {
                    title.enabled = selected;
                    title.text = item.title; // 设置物品标题
                }

                if (default_icon != null)
                    default_icon.enabled = false; // 隐藏默认图标

                if (dura != null)
                    dura.enabled = false; // 隐藏耐久度

                if (filter != null)
                    filter.enabled = false; // 隐藏过滤器

                if (prev != item || prevq != quantity)
                    AnimateGain(); // 如果物品或数量改变，播放获得动画
            }
            else
            {
                // 如果没有物品(Z会将本ItemSlot设置为某种状态)
                this.item = null;
                this.quantity = 0;
                this.durability = 0f;
                icon.enabled = false;
                value.enabled = false;
                this.selected = false;

                if (dura != null)
                    dura.enabled = false;

                if (filter != null)
                    filter.enabled = false;

                if (title != null)
                    title.enabled = false;

                if (default_icon != null)
                    default_icon.enabled = true; // 显示默认图标
            }

            Show(); // 显示槽位(也就是显示为true)
        }

        /// <summary>
        /// 使用自定义图标和标题设置槽位的数据（Z会将原本槽位的数据）
        /// </summary>
        /// <param name="sicon">自定义图标</param>
        /// <param name="title">标题</param>
        /// <param name="quantity">物品数量</param>
        /// <param name="selected">是否被选中</param>
        public void SetSlotCustom(Sprite sicon, string title, int quantity, bool selected = false)
        {
            this.item = null;
            this.quantity = quantity;
            this.durability = 0f;
            icon.enabled = sicon != null; // 只有在图标不为空时显示图标
            icon.sprite = sicon;
            value.text = quantity.ToString(); // 设置物品数量
            value.enabled = quantity > 1;
            this.selected = selected;

            if (this.title != null)
            {
                this.title.enabled = selected;
                this.title.text = title; // 设置标题
            }

            if (dura != null)
                dura.enabled = false;

            if (filter != null)
                filter.enabled = false;

            if (default_icon != null)
                default_icon.enabled = false;

            Show(); // 显示槽位
        }

        /// <summary>
        /// 显示物品标题
        /// </summary>
        public void ShowTitle()
        {
            if (this.title != null)
                this.title.enabled = true; // 显示标题
        }

        /// <summary>
        /// 设置物品耐久度
        /// </summary>
        /// <param name="durability">耐久度值</param>
        /// <param name="show_value">是否显示耐久度</param>
        public void SetDurability(int durability, bool show_value)
        {
            this.durability = durability;

            if (dura != null)
            {
                dura.enabled = show_value;
                dura.text = durability.ToString() + "%"; // 设置耐久度文本
            }
        }

        /// <summary>
        /// 设置物品过滤器（Z根据传入的耐久度级别，隐藏/显示不同耐久UI）
        /// </summary>
        /// <param name="filter_level">过滤器级别</param>
        public void SetFilter(int filter_level)
        {
            if (filter != null)
            {
                filter.enabled = filter_level > 0; // 根据过滤器级别显示或隐藏
                filter.color = filter_level >= 2 ? TheUI.Get().filter_red : TheUI.Get().filter_yellow; // 设置过滤器颜色
            }
        }

        /// <summary>
        /// 选择该槽位
        /// </summary>
        public void Select()
        {
            this.selected = true;
            if (this.title != null)
                this.title.enabled = true; // 显示标题
        }

        /// <summary>
        /// 取消选择该槽位
        /// </summary>
        public void Unselect()
        {
            this.selected = false;
            if (this.title != null)
                this.title.enabled = false; // 隐藏标题
        }

        /// <summary>
        /// 播放获得物品的动画
        /// </summary>
        public void AnimateGain()
        {
            if (animator != null)
                animator.SetTrigger("Gain"); // 触发动画
        }

        /// <summary>
        /// 获取当前的 CraftData 物品数据
        /// </summary>
        /// <returns>物品数据</returns>
        public CraftData GetCraftable()
        {
            return item;
        }

        /// <summary>
        /// 获取当前的 ItemData 物品数据
        /// </summary>
        /// <returns>物品数据</returns>
        public ItemData GetItem()
        {
            if (item != null)
                return item.GetItem();
            return null;
        }

        /// <summary>
        /// 获取当前物品的数量
        /// </summary>
        /// <returns>物品数量</returns>
        public int GetQuantity()
        {
            return quantity;
        }

        /// <summary>
        /// 获取当前物品的耐久度（显示值）
        /// </summary>
        /// <returns>物品耐久度</returns>
        public float GetDurability()
        {
            return durability; // 返回显示的耐久度值（百分比），而不是实际耐久度
        }

        /// <summary>
        /// 获取库存的唯一标识符
        /// </summary>
        /// <returns>库存唯一标识符</returns>
        public string GetInventoryUID()
        {
            ItemSlotPanel parent_item = parent as ItemSlotPanel;
            return parent_item?.GetInventoryUID();
        }

        /// <summary>
        /// 获取库存数据
        /// </summary>
        /// <returns>库存数据</returns>
        public InventoryData GetInventory()
        {
            ItemSlotPanel parent_item = parent as ItemSlotPanel;
            return parent_item?.GetInventory();
        }

        /// <summary>
        /// 获取当前槽位的库存物品数据
        /// </summary>
        /// <returns>库存物品数据</returns>
        public InventoryItemData GetInventoryItem()
        {
            InventoryData inventory = GetInventory();
            if (inventory != null)
                return inventory.GetInventoryItem(index);
            return null;
        }
    }
}
