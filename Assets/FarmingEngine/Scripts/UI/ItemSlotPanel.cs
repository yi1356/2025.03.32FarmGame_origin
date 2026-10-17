using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 通用的 UI 面板父类，管理多个物品槽位（库存/装备/储存等）
    /// 处理 “点一下格子、拖动物品到另一个格子、合并同类物品” 等操作
    /// 控制格子显示（物品图标、数量、耐久度）
    /// 记录哪个格子被选中、右键点击要干啥
    /// </summary>
    public class ItemSlotPanel : UISlotPanel
    {
        public bool limit_one_item = false; // 如果为 true，每个槽位只能放一个物品

        public UnityAction<ItemSlot> onSelectSlot; // 选择槽位的事件
        public UnityAction<ItemSlot, ItemSlot> onMergeSlot; // 合并槽位的事件

        protected PlayerCharacter current_player = null; // 当前玩家
        //Z:用它们（可以理解为索引）来得到真正的数据
        protected InventoryType inventory_type; // 库存类型
        protected string inventory_uid; // 库存唯一标识符（Z获取数据 和 标识这个面板用）
        protected int inventory_size = 99; // 库存大小[Z：这个似乎和格子数量有关,这里 player.InventoryData.size设置的是15]

        protected int selected_slot = -1; // 选择的槽位索引(Z这个面板比较有意思，它slot的选中状态有两种原因，左键/右键)
        protected int selected_right_slot = -1; // 右键选择的槽位索引

        private static List<ItemSlotPanel> slot_panels = new List<ItemSlotPanel>(); // 所有面板的列表

        protected override void Awake()
        {
            base.Awake();
            slot_panels.Add(this); // 添加到面板列表

            for (int i = 0; i < slots.Length; i++)
                ((ItemSlot)slots[i]).Hide(); // 隐藏所有槽位
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            slot_panels.Remove(this); // 从面板列表中移除
        }

        protected override void Start()
        {
            base.Start();

            PlayerControlsMouse.Get().onRightClick += (Vector3) => { CancelSelection(); }; // 右键点击时取消选择

            onClickSlot += OnClick; // 注册点击事件
            onRightClickSlot += OnClickRight; // 注册右键点击事件
            onDoubleClickSlot += OnClickRight; // 注册双击事件
            onLongClickSlot += OnClickRight; // 注册长按事件
            onDragStart += OnDragStart; // 注册拖动开始事件
            onDragEnd += OnDragEnd; // 注册拖动结束事件
            onDragTo += OnDragTo; // 注册拖动到目标事件
            onPressAccept += OnClick; // 注册接受按键事件
            onPressUse += OnClickRight; // 注册使用按键事件
            onPressCancel += OnCancel; // 注册取消按键事件

            InitPanel(); // 初始化面板
        }

        protected override void Update()
        {
            base.Update();

            InitPanel(); // 尝试初始化面板，如果尚未初始化
        }
        //关键功能 
        //1、“初始化面板”：加载背包数据
        /// <summary>
        /// 初始化面板
        /// </summary>
        public virtual void InitPanel()
        {
            if (!IsPlayerSet()) // 如果玩家未设置
            {
                PlayerUI player_ui = GetComponentInParent<PlayerUI>();
                PlayerCharacter player = player_ui ? player_ui.GetPlayer() : PlayerCharacter.GetFirst();
                if (player != null && current_player == null)
                    current_player = player; // 设置默认玩家
            }
        }

        //2、“刷新面板”：同步物品状态（ 读取背包数据，刷线背包格子UI ）
        protected override void RefreshPanel()
        {
            base.RefreshPanel();

            InventoryData inventory = GetInventory();

            if (inventory != null)
            {
                for (int i = 0; i < slots.Length; i++)
                {
                    InventoryItemData invdata = inventory.GetInventoryItem(i);
                    ItemData idata = ItemData.Get(invdata?.item_id);
                    ItemSlot slot = (ItemSlot)slots[i];
                    //格子数据不为空
                    if (invdata != null && idata != null)
                    {
                        slot.SetSlot(idata, invdata.quantity, selected_slot == slot.index || selected_right_slot == slot.index); // 设置槽位
                        slot.SetDurability(idata.GetDurabilityPercent(invdata.durability), ShouldShowDurability(idata, invdata.durability)); // 设置耐久度
                        slot.SetFilter(GetFilterLevel(idata, invdata.durability)); // 设置过滤器（Z：也就是设置耐久度UI）
                    }
                    //格子数据为空
                    else if (i < inventory_size)//Z：遍历的槽位 少于 数据数量时
                    {
                        slot.SetSlot(null, 0, false); // 清空槽位
                    }
                    else//Z遍历的槽位 多于 数据数量时 
                    {
                        slot.Hide(); // 隐藏超出库存大小的槽位
                    }
                }

                ItemSlot sslot = GetSelectedSlot();
                if (sslot != null && sslot.GetItem() == null)
                    CancelSelection(); // 如果选中的槽位没有物品，取消选择
            }
        }

        /// <summary>
        /// 判断是否应该显示物品耐久度
        /// </summary>
        /// <param name="idata">物品数据</param>
        /// <param name="durability">耐久度</param>
        /// <returns>是否显示耐久度</returns>
        protected bool ShouldShowDurability(ItemData idata, float durability)
        {
            int durabi = idata.GetDurabilityPercent(durability);
            return idata.HasDurability() && durabi < 100 && (idata.durability_type != DurabilityType.Spoilage || durabi <= 50);
        }

        /// <summary>
        /// 获取物品过滤器级别（Z可以理解为获取耐久度下降到了什么级别）
        /// </summary>
        /// <param name="idata">物品数据</param>
        /// <param name="durability">耐久度</param>
        /// <returns>过滤器级别</returns>
        protected int GetFilterLevel(ItemData idata, float durability)
        {
            int durabi = idata.GetDurabilityPercent(durability);
            if (idata.HasDurability() && durabi <= 40 && idata.durability_type == DurabilityType.Spoilage)
            {
                return durabi <= 20 ? 2 : 1;
            }
            return 0;
        }

        //TODO:2025.05.03（Z OnClick(UISlot uislot)，这里面的Action和数据部分暂时还看不懂）
        //3、“点击槽位”：处理交互逻辑
        /// <summary>
        /// 点击槽位的处理函数
        /// </summary>
        /// <param name="uislot">点击的槽位</param>
        private void OnClick(UISlot uislot)
        {
            if (uislot != null)
            {
                Debug.Log("测试槽位点击ItemSlotPanel.OnClick已经执行");
                // 取消右键点击和动作选择器(Z在这里，处理点击这个Slot后会取消右键选中的Slot)
                int previous_right_select = selected_right_slot;
                ActionSelectorUI.Get(GetPlayerID()).Hide();
                selected_right_slot = -1;

                int slot = uislot.index;
                ItemSlot selslot = GetSelectedSlotInAllPanels();//Z这里是用来找到所有面板中,上一个被选中的Slot

                // 取消动作选择器(Z这里的情况应该是 符合 当在一次右击这个Slot后，接着单击这个Slot)
                if (slot == previous_right_select)
                {
                    CancelSelection();
                    return;
                }

                // 合并两个槽位
                ItemSlot islot = uislot as ItemSlot;
                if (islot != null && selslot != null)
                {
                    MergeSlots(selslot, islot);//TODO:ItemSlotPanel的合并槽位这里面的逻辑看不懂？？？？？？
                    onMergeSlot?.Invoke(selslot, islot); // 触发合并槽位事件
                }
                // 选择槽位
                else if (islot.GetCraftable() != null)
                {
                    CancelSelectionAll(); // 取消所有选择
                    //5、状态管理”：记录选中的格子
                    selected_slot = slot;//Z点击槽位后，更新选中槽位索引 

                    ItemData idata = islot?.GetItem();
                    AAction aaction = idata?.FindAutoAction(GetPlayer(), islot);
                    aaction?.DoSelectAction(GetPlayer(), islot); // 执行选择动作

                    onSelectSlot?.Invoke(islot); // 触发选择槽位事件
                }
            }
        }

        /// <summary>
        /// 右键点击槽位的处理函数(Z 同样里面的数据和 Action看不懂)
        /// </summary>
        /// <param name="uislot">右键点击的槽位</param>
        private void OnClickRight(UISlot uislot)
        {
            // 取消选择
            selected_slot = -1; 
            selected_right_slot = -1;
            ActionSelectorUI.Get(GetPlayerID()).Hide(); // 隐藏动作选择器

            // 执行自动动作
            ItemSlot islot = uislot as ItemSlot;
            ItemData idata = islot?.GetItem();
            AAction aaction = idata?.FindAutoAction(GetPlayer(), islot);
            aaction?.DoAction(GetPlayer(), islot); // 执行动作

            // 显示动作选择器
            if (idata != null && islot?.GetInventoryItem() != null && idata.actions.Length > 0)
            {
                selected_right_slot = islot.index;
                ActionSelectorUI.Get(GetPlayerID()).Show(islot); // 显示动作选择器
            }
        }

        //4、拖动物品”：实现物品移动
        /// <summary>
        /// 拖动开始时的处理函数
        /// </summary>
        /// <param name="slot">拖动的槽位</param>
        private void OnDragStart(UISlot slot)
        {
            CancelSelection(); // 取消选择
        }

        /// <summary>
        /// 拖动结束时的处理函数（Z 看不懂）
        /// </summary>
        /// <param name="aslot">拖动的槽位</param>
        private void OnDragEnd(UISlot aslot)
        {
            PlayerControlsMouse mouse = PlayerControlsMouse.Get();
            if (mouse.IsMouseOverUI())
                return;

            // 拖动到可选择对象
            ItemSlot slot = aslot as ItemSlot;
            if (slot != null && slot.GetItem() != null)
            {
                PlayerCharacter player = GetPlayer();
                Selectable select = mouse.GetNearestRaycastList(mouse.GetPointingPos());
                MAction maction = slot.GetItem().FindMergeAction(select);
                if (player != null && maction != null
                    && select.IsInUseRange(player)
                    && maction.CanDoAction(player, slot, select))
                {
                    maction.DoAction(player, slot, select); // 执行合并动作
                }
            }
        }

        /// <summary>
        /// 拖动到目标槽位的处理函数
        /// </summary>
        /// <param name="slot">源槽位</param>
        /// <param name="target">目标槽位</param>
        private void OnDragTo(UISlot slot, UISlot target)
        {
            if (slot != null && target != null)
            {
                ItemSlot islot = slot as ItemSlot;
                ItemSlot itarget = target as ItemSlot;
                MergeSlots(islot, itarget); // 合并槽位
                onMergeSlot?.Invoke(islot, itarget); // 触发合并槽位事件
            }
        }

        /// <summary>
        /// 取消槽位选择
        /// </summary>
        /// <param name="slot">取消的槽位</param>
        private void OnCancel(UISlot slot)
        {
            ItemSlotPanel.CancelSelectionAll(); // 取消所有选择
            UISlotPanel.UnfocusAll(); // 取消所有焦点
        }

        /// <summary>
        /// 设置库存（Z：通过设置这个脚本中InventoryType等，获取背包数据时自动就是这个类型的背包数据了）
        /// </summary>
        /// <param name="type">库存类型</param>
        /// <param name="uid">库存唯一标识符</param>
        /// <param name="size">库存大小</param>
        public void SetInventory(InventoryType type, string uid, int size)
        {
            inventory_type = type;
            inventory_uid = uid;
            inventory_size = size;

            InventoryData idata = InventoryData.Get(type, uid);
            if (idata != null)
                idata.size = size;
        }

        /// <summary>
        /// 设置玩家
        /// </summary>
        /// <param name="player">玩家</param>
        public void SetPlayer(PlayerCharacter player)
        {
            current_player = player;
        }

        /// <summary>
        /// 获取玩家 ID
        /// </summary>
        /// <returns>玩家 ID</returns>
        public int GetPlayerID()
        {
            return current_player ? current_player.player_id : 0;
        }

        /// <summary>
        /// 合并两个槽位(Z??????)
        /// </summary>
        /// <param name="selected_slot">选择的槽位</param>
        /// <param name="clicked_slot">点击的槽位</param>
        public void MergeSlots(ItemSlot selected_slot, ItemSlot clicked_slot)
        {
            Debug.Log("测试合并两个槽位ItemSlotPanel.MergeSlots()已经执行");
            if (selected_slot != null && clicked_slot != null && current_player != null)
            {
                ItemSlot slot1 = selected_slot;
                ItemSlot slot2 = clicked_slot;
                ItemData item1 = slot1.GetItem();
                ItemData item2 = slot2.GetItem();

                if (slot1 == slot2)
                {
                    CancelSelection();
                    return;
                }

                // 检查合并动作
                if (item1 != null && item2 != null)
                {
                    MAction action1 = item1.FindMergeAction(item2);
                    MAction action2 = item2.FindMergeAction(item1);

                    if (action1 != null && action1.CanDoAction(current_player, slot1, slot2))
                    {
                        DoMergeAction(action1, slot1, slot2); // 执行合并动作
                        return;
                    }

                    else if (action2 != null && action2.CanDoAction(current_player, slot2, slot1))
                    {
                        DoMergeAction(action2, slot2, slot1); // 执行合并动作
                        return;
                    }
                }

                // 移动物品（Z应该是发现前后点击槽位的物品不可以合并时，槽位就交换物品）
                MoveItem(slot1, slot2);
            }
        }

        private void DoMergeAction(MAction action, ItemSlot slot_action, ItemSlot slot_other)
        {
            Debug.Log("测试合并两个槽位ItemSlotPanel.DoMergeAction()已经执行");
            if (slot_action == null || slot_other == null || current_player == null)
                return;

            action.DoAction(current_player, slot_action, slot_other); // 执行合并动作

            CancelPlayerSelection(); // 取消玩家选择
        }

        /// <summary>
        /// 移动物品（从一个格子移动到另一个格子）
        /// </summary>
        /// <param name="slot1">源槽位</param>
        /// <param name="slot2">目标槽位</param>
        public void MoveItem(ItemSlot slot1, ItemSlot slot2)
        {
            Debug.Log("测试移动槽位ItemSlotPanel. MoveItem()已经执行");
            ItemData item1 = slot1.GetItem();
            if (item1 == null || current_player == null)
                return;

            current_player.Inventory.MoveItem(slot1, slot2, limit_one_item); // 移动物品(Z 这里好像处理了装备面板不能放其他item的逻辑)
            CancelPlayerSelection(); // 取消玩家选择
        }

        /// <summary>
        /// 使用物品
        /// </summary>
        /// <param name="slot">槽位</param>
        /// <param name="quantity">数量</param>
        public void UseItem(ItemSlot slot, int quantity = 1)
        {
            InventoryData inventory1 = slot.GetInventory();
            if (current_player != null && inventory1 != null)
                inventory1.RemoveItemAt(slot.index, quantity); // 从库存中移除物品
        }

        /// <summary>
        /// 取消选择
        /// </summary>
        public void CancelSelection()
        {
            selected_slot = -1;
            selected_right_slot = -1;
        }

        /// <summary>
        /// 取消玩家选择
        /// </summary>
        public void CancelPlayerSelection()
        {
            CancelSelection();
            if (current_player != null)
            {
                PlayerUI player_ui = PlayerUI.Get(current_player.player_id);
                if (player_ui != null)
                    player_ui.CancelSelection(); // 取消玩家 UI 选择
            }
        }

        /// <summary>
        /// 是否有槽位被选中
        /// </summary>
        /// <returns>是否有槽位被选中</returns>
        public bool HasSlotSelected()
        {
            return selected_slot >= 0;
        }

        /// <summary>
        /// 获取选择的槽位索引
        /// </summary>
        /// <returns>选择的槽位索引</returns>
        public int GetSelectedSlotIndex()
        {
            return selected_slot;
        }

        /// <summary>
        /// 根据索引获取槽位
        /// </summary>
        /// <param name="slot_index">槽位索引</param>
        /// <returns>槽位</returns>
        public ItemSlot GetSlotByIndex(int slot_index)
        {
            foreach (ItemSlot slot in slots)
            {
                if (slot.index == slot_index)
                    return slot;
            }
            return null;
        }

        /// <summary>
        /// 获取选择的槽位
        /// </summary>
        /// <returns>选择的槽位</returns>
        public ItemSlot GetSelectedSlot()
        {
            return GetSlotByIndex(selected_slot);
        }

        /// <summary>
        /// 获取槽位的世界位置
        /// </summary>
        /// <param name="slot">槽位索引</param>
        /// <returns>世界位置</returns>
        public Vector3 GetSlotWorldPosition(int slot)
        {
            ItemSlot islot = GetSlotByIndex(slot);
            if (islot != null)
            {
                RectTransform slotRect = islot.GetRect();
                return slotRect.position;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// 获取库存唯一标识符
        /// </summary>
        /// <returns>库存唯一标识符</returns>
        public string GetInventoryUID()
        {
            return inventory_uid;
        }

        /// <summary>
        /// 获取库存数据
        /// </summary>
        /// <returns>库存数据</returns>
        public InventoryData GetInventory()
        {
            return InventoryData.Get(inventory_type, inventory_uid);
        }

        /// <summary>
        /// 是否设置了库存
        /// </summary>
        /// <returns>是否设置了库存</returns>
        public bool IsInventorySet()
        {
            return inventory_type != InventoryType.None;
        }

        /// <summary>
        /// 是否设置了玩家
        /// </summary>
        /// <returns>是否设置了玩家</returns>
        public bool IsPlayerSet()
        {
            return current_player != null;
        }

        /// <summary>
        /// 获取当前玩家
        /// </summary>
        /// <returns>玩家</returns>
        public PlayerCharacter GetPlayer()
        {
            return current_player;
        }

        /// <summary>
        /// 取消所有面板的选择
        /// </summary>
        public static void CancelSelectionAll()
        {
            foreach (ItemSlotPanel panel in slot_panels)
                panel.CancelSelection();
        }

        /// <summary>
        /// 获取所有面板中选择的槽位（Z鼠标UI时很有用）
        /// </summary>
        /// <returns>选择的槽位</returns>
        public static ItemSlot GetSelectedSlotInAllPanels()
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                ItemSlot slot = panel.GetSelectedSlot();
                if (slot != null)
                    return slot;
            }
            return null;
        }

        /// <summary>
        /// 获取所有面板中拖动的槽位
        /// </summary>
        /// <returns>拖动的槽位</returns>
        public static ItemSlot GetDragSlotInAllPanels()
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                ItemSlot slot = panel.GetDragSlot();
                if (slot != null)
                    return slot;
            }
            return null;
        }

        /// <summary>
        /// 根据库存类型获取面板
        /// </summary>
        /// <param name="type">库存类型</param>
        /// <returns>面板</returns>
        public static ItemSlotPanel Get(InventoryType type)
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                if (panel != null && panel.inventory_type == type)
                    return panel;
            }
            return null;
        }

        /// <summary>
        /// 根据库存唯一标识符获取面板
        /// </summary>
        /// <param name="inventory_uid">库存唯一标识符</param>
        /// <returns>面板</returns>
        public static ItemSlotPanel Get(string inventory_uid)
        {
            foreach (ItemSlotPanel panel in slot_panels)
            {
                if (panel != null && panel.inventory_uid == inventory_uid)
                    return panel;
            }
            return null;
        }

        /// <summary>
        /// 获取所有面板
        /// </summary>
        /// <returns>面板列表</returns>
        public static new List<ItemSlotPanel> GetAll()
        {
            return slot_panels;
        }
    }
}
