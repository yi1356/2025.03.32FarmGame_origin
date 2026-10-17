using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace FarmingEngine
{
    /// <summary>
    /// 用于购买/出售物品的 UI 面板（NPC 商店）
    /// </summary>
    public class ShopPanel : UISlotPanel
    {
        public Text shop_title;       // 商店标题文本
        public Text gold_value;       // 显示玩家金币的文本
        public ShopSlot[] buy_slots;  // 用于显示购买物品的槽位
        public ShopSlot[] sell_slots; // 用于显示出售物品的槽位
        public AudioClip buy_sell_audio; // 购买/出售的音效

        //Z：选中物品后显示的信息
        [Header("Description")]
        public Text title;            // 物品标题文本
        public Text desc;             // 物品描述文本
        public Text buy_cost;         // 物品价格文本
        public Button button;        // 购买/出售按钮
        public Text button_text;     // 按钮上的文本
        public GameObject desc_group; // 描述组（包含标题、描述和价格）

        //Z：数据存储
        private PlayerCharacter current_player; // 当前的玩家角色
        private List<ItemData> buy_items;       // 可购买的物品列表
        private GroupData sell_group;            // 可出售物品的分组
        private ShopSlot selected = null;        // 当前选中的槽位

        private static ShopPanel instance;       // ShopPanel 的单例实例

        protected override void Awake()
        {
            base.Awake();
            instance = this;

            // 初始化时隐藏所有槽位
            for (int i = 0; i < slots.Length; i++)
                ((ShopSlot)slots[i]).Hide();

            // 注册槽位点击事件
            onClickSlot += OnClickSlot;
            onRightClickSlot += OnRightClickSlot;
            onPressAccept += OnAccept;
            onPressCancel += OnCancel;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }

        protected override void RefreshPanel()//Z：由于继承UISlotPanel，所以会每0.1s 刷新一次
        {
            base.RefreshPanel();

            // 初始化金币显示
            gold_value.text = "0";

            // 隐藏所有购买和出售槽位
            foreach (ShopSlot slot in buy_slots)
                slot.Hide();

            foreach (ShopSlot slot in sell_slots)
                slot.Hide();

            if (current_player != null)
            {
                // 更新金币显示
                gold_value.text = current_player.SaveData.gold.ToString();

                // 显示购买物品
                int index = 0;
                foreach (ItemData item in buy_items)
                {
                    if (index < buy_slots.Length)
                    {
                        ShopSlot slot = buy_slots[index];
                        slot.SetBuySlot(item, item.buy_cost);
                        slot.SetSelected(selected == slot);
                    }
                    index++;
                }

                // 显示出售物品
                index = 0;
                foreach (KeyValuePair<int, InventoryItemData> pair in current_player.InventoryData.items)
                {
                    if (index < sell_slots.Length)
                    {
                        InventoryItemData item = pair.Value;
                        ItemData idata = ItemData.Get(item?.item_id);
                        bool can_sell = CanSell(idata);
                        ShopSlot slot = sell_slots[index];
                        slot.SetSellSlot(idata, idata.sell_cost, item.quantity, can_sell);
                        slot.SetSelected(selected == slot);
                    }
                    index++;
                }

                // 更新物品描述
                ItemData select_item = selected?.GetItem();
                desc_group.SetActive(select_item != null);
                if (select_item != null)
                {
                    title.text = select_item.title;
                    desc.text = select_item.desc;
                    bool sell = selected.IsSell();
                    int cost = (sell ? select_item.sell_cost : select_item.buy_cost);
                    buy_cost.text = cost.ToString();
                    button_text.text = sell ? "SELL" : "BUY";
                    button.interactable = (sell && cost > 0 && CanSell(select_item)) || (!sell && cost <= current_player.SaveData.gold); 
                }

                // 游戏手柄自动控制
                PlayerControls controls = PlayerControls.Get(current_player.player_id);
                UISlotPanel focus_panel = UISlotPanel.GetFocusedPanel();
                if (focus_panel != this && controls.IsGamePad())
                {
                    Focus(); // 如果当前面板未获得焦点，则自动聚焦
                }
            }
        }

        //Z：判断物品能不能卖（比如小麦属于 "农产品"，可以卖；剑不属于，不让卖）
        private bool CanSell(ItemData item)
        {
            // 判断物品是否可以出售
            return sell_group == null || item.HasGroup(sell_group);
        }

        /// <summary>
        /// Z：打开商店面板
        /// </summary>
        /// <param name="player"></param>
        /// <param name="title"></param>
        /// <param name="items"></param>
        /// <param name="sell_items"></param>
        public void ShowShop(PlayerCharacter player, string title, List<ItemData> items, GroupData sell_items)
        {
            // 显示商店面板
            current_player = player;
            buy_items = items;
            sell_group = sell_items;
            shop_title.text = title;
            selected = null;
            RefreshPanel();
            Show();
        }

        //Z：关闭商店时，清空当前玩家信息，面板消失
        public override void Hide(bool instant = false)
        {
            base.Hide(instant);
            current_player = null;
        }
        //Z：商店单个操作点击事件，监听方法
        private void OnClickSlot(UISlot islot)
        {
            // 处理槽位点击事件
            ShopSlot slot = (ShopSlot)islot;
            ItemData item = slot.GetItem();

            if (slot != null && item != null && selected != slot)
                selected = slot;
            else
                selected = null;
           
            RefreshPanel();
        }

        private void OnAccept(UISlot islot)
        {
            // 处理接受操作（购买或选择槽位）
            if (selected == islot)
                OnClickBuy();
            else
                OnClickSlot(islot);
        }

        private void OnCancel(UISlot islot)
        {
            // 处理取消操作
            if (selected != null)
                selected = null;
            else
                Hide();
        }

        /// <summary>
        /// Z：点击购买 / 出售按钮，处理操作的方法
        /// </summary>
        public void OnClickBuy()
        {
            // 处理购买或出售操作
            ShopSlot slot = selected;
            bool sell = slot.IsSell();
            ItemData item = slot.GetItem();

            //Z：判断商店类型格子是否出售状态（区分是卖还是买）
            if (sell)
            {
                if (current_player.InventoryData.HasItem(item.id, 1) && item.sell_cost > 0 && CanSell(item))
                {
                    // 出售物品
                    current_player.SaveData.gold += item.sell_cost;
                    current_player.InventoryData.RemoveItem(item.id, 1);

                    TheAudio.Get().PlaySFX("shop", buy_sell_audio);
                }
            }
            else
            {
                if (current_player.SaveData.gold >= item.buy_cost)
                {
                    // 购买物品
                    current_player.SaveData.gold -= item.buy_cost;
                    current_player.Inventory.GainItem(item, 1);

                    TheAudio.Get().PlaySFX("shop", buy_sell_audio);
                }
            }
            RefreshPanel();
        }

        private void OnRightClickSlot(UISlot islot)
        {
            // 右键点击槽位时的处理逻辑（目前为空）
        }

        public PlayerCharacter GetPlayer()
        {
            return current_player;
        }

        public static ShopPanel Get()
        {
            return instance;
        }

        public static bool IsAnyVisible()
        {
            if (instance)
                return instance.IsVisible();
            return false;
        }
    }
}
