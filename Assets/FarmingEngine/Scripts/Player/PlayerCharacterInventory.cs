using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    //TODO:2025.05.26

    /// <summary>
    /// 管理角色背包和相关操作的类（Z 目前可知与物品拾取丢弃、使用+物品装备到身上、卸下有关）
    /// 还负责生成/销毁装备的视觉附加效果
    /// 玩家物品管理的总控制器” ，简单说就是玩家的 “随身物品管家”，负责处理所有和 “物品” 相关的操作 —— 从捡东西、放东西，到穿装备、用道具，全由它管。
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterInventory : MonoBehaviour
    {
        public int inventory_size = 15; //If you change this, make sure to change the UI（Z：这里好像就是 下面背包面板中的格子数量）
        public ItemData[] starting_items;

        public UnityAction<Item> onTakeItem;
        public UnityAction<Item> onDropItem;
        public UnityAction<ItemData> onGainItem;

        private PlayerCharacter character;

        private EquipAttach[] equip_attachments;

        private Dictionary<string, EquipItem> equipped_items = new Dictionary<string, EquipItem>();//Z：这里应该是存了已经装备上的物品（而且可以作为条件，防止重复装备）

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            equip_attachments = GetComponentsInChildren<EquipAttach>();
        }

        //核心功能：
        //1、初始化：给玩家 “初始家当”
        private void Start()
        {
            bool has_inventory = PlayerData.Get().HasInventory(character.player_id);//Z 测试这里是true，也就是我们能得到我们的InventoryData（也就是背包数据）

            InventoryData.size = inventory_size; //This will also create the inventory[Z：这里可以理解为设置背包的容量吧]
            EquipData.size = 99; //Create the inventory, size doesnt matter for equip

            //If new game, add starting items(这里应该是在新游戏开始时，默认包含的物品)
            if (!has_inventory)
            {
                InventoryData invdata = InventoryData.Get(InventoryType.Inventory, character.player_id);//[Z：这里其实就是判断没有背包数据后，创建一个空的，下面为其添加了一些默认数据]
                foreach (ItemData item in starting_items)//TODO：2025.07.15
                {
                    invdata.AddItem(item.id, 1, item.durability, UniqueID.GenerateUniqueID());
                }
            }
        }

        void Update()
        {
            HashSet<string> equipped_data = new HashSet<string>();// Z：注意这里，这样是会每帧清空的
            List<string> remove_list = new List<string>();

            //Equip unequip（Z：只需在别处中像EquipData.items添加要装备的物品，这里就会自动装备***************************************）
            foreach (KeyValuePair<int, InventoryItemData> item in EquipData.items)
            {
                if (item.Value != null)
                {
                    equipped_data.Add(item.Value.item_id);
                    if (!equipped_items.ContainsKey(item.Value.item_id))
                        EquipAddedItem(item.Value.item_id);
                }
            }

            //Create remove list（Z：这里的逻辑就是将要卸下的装备，存到removelist中，后面会自动根据这个列表来卸载装备）
            foreach (KeyValuePair<string, EquipItem> item in equipped_items)
            {
                if (!equipped_data.Contains(item.Key))
                    remove_list.Add(item.Key);
            }

            //Remove
            foreach (string item_id in remove_list)
            {
                UnequipRemovedItem(item_id);
            }
        }

        //------- Items ----------（Z与拾取、丢弃、使用物品相关）

        //Take an Item on the floor
        //2、拾取物品：把地上的东西放进背包（将数据加入背包中）
        public void TakeItem(Item item)
        {
            if (BagData != null && !InventoryData.CanTakeItem(item.data.id, item.quantity) && !item.data.IsBag())
            {
                TakeItem(BagData, item); //Take into bag
            }
            else
            {
                TakeItem(InventoryData, item); //Take into main inventory
            }
        }
        /// <summary>
        /// Z：向背包中增加数据（其实就是拾取）
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="item">要拾取的物品，其中包含itemData</param>
        public void TakeItem(InventoryData inventory, Item item)
        {
            if (item != null && !character.IsBusy() && inventory.CanTakeItem(item.data.id, item.quantity))
            {
                character.FaceTorward(item.transform.position);

                if (onTakeItem != null)
                    onTakeItem.Invoke(item);

                character.TriggerBusy(0.4f, () =>
                {
                    //Make sure wasnt destroyed during the 0.4 sec
                    if (item != null && inventory.CanTakeItem(item.data.id, item.quantity))
                    {
                        PlayerData pdata = PlayerData.Get();
                        DroppedItemData dropped_item = pdata.GetDroppedItem(item.GetUID());
                        float durability = dropped_item != null ? dropped_item.durability : item.data.durability;
                        int slot = inventory.AddItem(item.data.id, item.quantity, durability, item.GetUID()); //Add to inventory（Z：将物品数据添加到背包数据中）

                        ItemTakeFX.DoTakeFX(item.transform.position, item.data, inventory.type, slot);//Z：拾取特效，包括图片飞

                        item.TakeItem(); //Destroy item
                    }
                });
            }
        }

        //Auto take item, without animation, facing, action
        public void AutoTakeItem(Item item)
        {
            if (BagData != null && !InventoryData.CanTakeItem(item.data.id, item.quantity) && !item.data.IsBag())
            {
                AutoTakeItem(BagData, item); //Take into bag
            }
            else
            {
                AutoTakeItem(InventoryData, item); //Take into main inventory
            }
        }

        public void AutoTakeItem(InventoryData inventory, Item item)
        {
            if (item != null && !character.IsBusy() && inventory.CanTakeItem(item.data.id, item.quantity))
            {
                PlayerData pdata = PlayerData.Get();
                DroppedItemData dropped_item = pdata.GetDroppedItem(item.GetUID());
                float durability = dropped_item != null ? dropped_item.durability : item.data.durability;
                int slot = inventory.AddItem(item.data.id, item.quantity, durability, item.GetUID()); //Add to inventory

                ItemTakeFX.DoTakeFX(item.transform.position, item.data, inventory.type, slot);

                item.TakeItem(); //Destroy item
            }
        }

        //Gain an new item directly to inventory
        public void GainItem(ItemData item, int quantity=1)
        {
            GainItem(item, quantity, transform.position);
        }

        public void GainItem(ItemData item, int quantity, Vector3 source_pos)
        {
            if (BagData != null && !InventoryData.CanTakeItem(item.id, quantity) && !item.IsBag())
            {
                GainItem(BagData, item, quantity, source_pos); //Gain into bag
            }
            else
            {
                GainItem(InventoryData, item, quantity, source_pos); //Gain into main inventory
            }
        }

        //Gain into specified inventory
        public void GainItem(InventoryData inventory, ItemData item, int quantity=1)
        {
            GainItem(inventory, item, quantity, transform.position);
        }

        public void GainItem(InventoryData inventory, ItemData item, int quantity, Vector3 source_pos)
        {
            if (item != null)
            {
                if (inventory.CanTakeItem(item.id, quantity))
                {
                    if (inventory.type == InventoryType.Equipment)
                    {
                        EquipData.EquipItem(item.equip_slot, item.id, item.durability, UniqueID.GenerateUniqueID());
                        ItemTakeFX.DoTakeFX(source_pos, item, inventory.type, (int)item.equip_slot);
                    }
                    else
                    {
                        int islot = inventory.AddItem(item.id, quantity, item.durability, UniqueID.GenerateUniqueID());
                        ItemTakeFX.DoTakeFX(source_pos, item, inventory.type, islot);
                    }
                }
                else
                {
                    Item.Create(item, character.GetPosition(), quantity);
                }
            }
        }

        //Use an item in your inventory and build it immediately on the map (skipping build-mode)
        public void BuildItem(int slot)
        {
            BuildItem(InventoryData, slot);
        }

        public void BuildItem(InventoryData inventory, int slot)
        {
            character.Crafting.BuildItem(inventory, slot);
            PlayerUI.Get(character.player_id)?.CancelSelection();
        }

        //Eat item and gain its attributes
        
        public void EatItem(int slot)
        {
            EatItem(InventoryData, slot);
        }

        //4.2吃食物(扣除指定格子物品+为角色加hp和血量)
        public void EatItem(InventoryData inventory, int slot)
        {
            InventoryItemData idata = inventory.GetInventoryItem(slot);
            ItemData item = ItemData.Get(idata?.item_id);
            if (item != null && item.type == ItemType.Consumable)
            {
                if (inventory.IsItemIn(item.id, slot))
                {
                    inventory.RemoveItemAt(slot, 1);
                    if (item.container_data)
                        inventory.AddItem(item.container_data.id, 1, item.container_data.durability, UniqueID.GenerateUniqueID());

                    character.StopSleep();
                    character.Attributes.AddAttribute(AttributeType.Health, item.eat_hp);
                    character.Attributes.AddAttribute(AttributeType.Energy, item.eat_energy);
                    character.Attributes.AddAttribute(AttributeType.Hunger, item.eat_hunger);
                    character.Attributes.AddAttribute(AttributeType.Thirst, item.eat_thirst);
                    character.Attributes.AddAttribute(AttributeType.Happiness, item.eat_happiness);

                    foreach (BonusEffectData bonus in item.eat_bonus)
                    {
                        character.SaveData.AddTimedBonus(bonus.type, bonus.value, item.eat_bonus_duration);
                    }
                }
            }
        }

        //Drop item on the floor
        public void DropItem(int slot)
        {
            DropItem(InventoryData, slot);
        }

        //3、丢弃物品：把背包里的东西扔到地上
        /// <summary>
        /// Z：从背包丢弃物体[移除数据+在角色当前位置创建模型]（比如点击背包中物体，再点击地面这种丢弃方式）
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="slot"></param>
        public void DropItem(InventoryData inventory, int slot)
        {
            InventoryItemData invdata = inventory?.GetInventoryItem(slot);
            ItemData idata = ItemData.Get(invdata?.item_id);
            if (invdata != null && idata != null && invdata.quantity > 0)
            {
                if (idata.CanBeDropped())
                {
                    inventory.RemoveItemAt(slot, invdata.quantity);
                    Item iitem = Item.Create(idata, character.GetPosition(), invdata.quantity, invdata.durability, invdata.uid);

                    PlayerUI.Get(character.player_id)?.CancelSelection();

                    if (onDropItem != null)
                        onDropItem.Invoke(iitem);
                }
                else if (idata.CanBeBuilt())
                {
                    BuildItem(inventory, slot);
                }
            }
        }
        //4.1、使用物品（比如建造时扣除材料）
        //Remove item directly from inventory, keeping its container
        /// <summary>
        /// 从背包，装备，bag中找要使用的物体（比如建造时扣除材料）
        /// </summary>
        /// <param name="item"></param>
        /// <param name="quantity"></param>
        public void UseItem(ItemData item, int quantity = 1)
        {
            if (item != null)
            {
                for (int i = 0; i < quantity; i++)
                {
                    if (InventoryData.HasItem(item.id, 1))
                        UseItem(InventoryData, item, 1);
                    else if (EquipData.HasItem(item.id, 1))
                        UseItem(EquipData, item, 1);
                    else if (BagData != null && BagData.HasItem(item.id, 1))
                        UseItem(BagData, item, 1);
                }
            }
        }
       /// <summary>
       /// Z：从指定的inventory中扣除物品数据
       /// </summary>
       /// <param name="inventory"></param>
       /// <param name="item"></param>
       /// <param name="quantity"></param>
        //Remove item in one inventory, keeping its container
        public void UseItem(InventoryData inventory, ItemData item, int quantity = 1)
        {
            if (item != null)
            {
                inventory.RemoveItem(item.id, quantity);
                if (item.container_data)
                    inventory.AddItem(item.container_data.id, quantity, item.container_data.durability, UniqueID.GenerateUniqueID());
            }
        }

        //Remove items of group directly from inventory, keeping its container
        public void UseItemInGroup(GroupData group, int quantity = 1)
        {
            if (group != null)
            {
                for (int i = 0; i < quantity; i++)
                {
                    if (InventoryData.HasItemInGroup(group, 1))
                        UseItemInGroup(InventoryData, group, 1);
                    else if (EquipData.HasItemInGroup(group, 1))
                        UseItemInGroup(EquipData, group, 1);
                    else if (BagData != null && BagData.HasItemInGroup(group, 1))
                        UseItemInGroup(BagData, group, 1);
                }
            }
        }

        //Remove items of group in one inventory, keeping its container
        public void UseItemInGroup(InventoryData inventory, GroupData group, int quantity = 1)
        {
            if (group != null)
            {
                //Find which items should be used (by group)
                Dictionary<ItemData, int> remove_list = new Dictionary<ItemData, int>(); //Item, Quantity
                foreach (KeyValuePair<int, InventoryItemData> pair in inventory.items)
                {
                    ItemData idata = ItemData.Get(pair.Value?.item_id);
                    if (idata != null && idata.HasGroup(group) && pair.Value.quantity > 0 && quantity > 0)
                    {
                        int remove = Mathf.Min(quantity, pair.Value.quantity);
                        remove_list.Add(idata, remove);
                        quantity -= remove;
                    }
                }

                //Use those specific items
                foreach (KeyValuePair<ItemData, int> pair in remove_list)
                {
                    UseItem(inventory, pair.Key, pair.Value);
                }
            }
        }

        //Compatibility with older versions
        public void RemoveItem(ItemData item, int quantity = 1)
        {
            UseItem(item, quantity);
        }

        public void RemoveAll(InventoryData inventory)
        {
            inventory.RemoveAll();
        }

        // ---- Equipments -----（装备与卸下物品相关）

        //Equip inventory item
        public void EquipItem(int islot)
        {
            InventoryItemData item = InventoryData.GetInventoryItem(islot);
            ItemData idata = ItemData.Get(item?.item_id);
            if (idata != null && idata.type == ItemType.Equipment)
            {
                EquipItemTo(islot, idata.equip_slot);
            }
        }

        /// <summary>
        /// Z 与装备界面处理槽位有关，将背包某个格子中的数据 移到 装备数据中
        /// </summary>
        /// <param name="inventory">这个应该是inventory背包数据</param>
        /// <param name="islot"></param>
        public void EquipItem(InventoryData inventory, int islot)
        {
            InventoryItemData item = inventory.GetInventoryItem(islot);
            ItemData idata = ItemData.Get(item?.item_id);
            if (idata != null && idata.type == ItemType.Equipment)//Z：这里会判断是否是装备，是就装备到对应的装备格子（比如左手，这个数据是存储在ItemData中）
            {
                EquipItemTo(inventory, islot, idata.equip_slot);
            }
        }

        public void UnequipItem(EquipSlot eslot)
        {
            InventoryItemData invdata = EquipData.GetEquippedItem(eslot);
            ItemData idata = ItemData.Get(invdata?.item_id);

            if (invdata != null && InventoryData.CanTakeItem(invdata.item_id, 1))
            {
                EquipData.UnequipItem(eslot);
                InventoryData.AddItem(invdata.item_id, 1, invdata.durability, invdata.uid);
            }
            else if (invdata != null && BagData != null && BagData.CanTakeItem(invdata.item_id, 1) && !idata.IsBag())
            {
                EquipData.UnequipItem(eslot);
                BagData.AddItem(invdata.item_id, 1, invdata.durability, invdata.uid);
            }
        }

        public void RemoveEquipItem(EquipSlot eslot)
        {
            InventoryItemData invtem = EquipData.GetEquippedItem(eslot);
            ItemData idata = ItemData.Get(invtem?.item_id);
            if (idata != null)
            {
                EquipData.UnequipItem(eslot);
                if (idata.container_data)
                    EquipData.EquipItem(eslot, idata.container_data.id, idata.container_data.durability, UniqueID.GenerateUniqueID());
            }
        }

        public void EquipItemTo(int islot, EquipSlot eslot)
        {
            EquipItemTo(InventoryData, islot, eslot);
        }


        //5、装备 / 卸下物品：穿盔甲、拿武器
        /// <summary>
        /// Z：这里是真正用来处理装备物品的逻辑（其实是操作下方背包库存数据 与 装备背包数据（面板会自动刷新不必担心），而且会自动装备到对应的装备格子中）
        /// </summary>
        /// <param name="inventory">这个应该是inventoryData也就是背包数据</param>
        /// <param name="islot"></param>
        /// <param name="eslot">枚举，这个指明是装备数据中哪一个格子（目标）</param>
        public void EquipItemTo(InventoryData inventory, int islot, EquipSlot eslot)
        {
            InventoryItemData invt_slot = inventory.GetInventoryItem(islot);
            InventoryItemData invt_equip = EquipData.GetEquippedItem(eslot);
            ItemData idata = ItemData.Get(invt_slot?.item_id);
            ItemData edata = ItemData.Get(invt_equip?.item_id);

            if (invt_slot != null && inventory != EquipData && invt_slot.quantity > 0 && idata != null && eslot > 0)
            {
                if (edata == null)
                {
                    //Equip only
                    EquipData.EquipItem(eslot, idata.id, invt_slot.durability, invt_slot.uid);
                    inventory.RemoveItemAt(islot, 1);
                }
                else if (invt_slot.quantity == 1 && idata.type == ItemType.Equipment)
                {
                    //Swap
                    inventory.RemoveItemAt(islot, 1);
                    EquipData.UnequipItem(eslot);
                    EquipData.EquipItem(eslot, idata.id, invt_slot.durability, invt_slot.uid);
                    inventory.AddItemAt(edata.id, islot, 1, invt_equip.durability, invt_equip.uid);
                }
            }
        }

        public void UnequipItemTo(EquipSlot eslot, int islot)
        {
            UnequipItemTo(InventoryData, eslot, islot);
        }
        //5、装备 / 卸下物品：穿盔甲、拿武器
        /// <summary>
        /// Z：真正用来处理卸下装备的逻辑
        /// </summary>
        /// <param name="inventory"></param>
        /// <param name="eslot"></param>
        /// <param name="islot"></param>
        public void UnequipItemTo(InventoryData inventory, EquipSlot eslot, int islot)
        {
            InventoryItemData invt_slot = inventory.GetInventoryItem(islot);
            InventoryItemData invt_equip = EquipData.GetEquippedItem(eslot);
            ItemData idata = ItemData.Get(invt_slot?.item_id);
            ItemData edata = ItemData.Get(invt_equip?.item_id);
            bool both_bag = inventory.type == InventoryType.Bag && edata.IsBag();

            if (edata != null && inventory != EquipData && !both_bag)
            {
                bool same_item = idata != null && invt_slot != null && invt_slot.quantity > 0 && idata.id == edata.id && invt_slot.quantity < idata.inventory_max;
                bool slot_empty = invt_slot == null || invt_slot.quantity <= 0;
                if (same_item || slot_empty)
                {
                    //Unequip
                    EquipData.UnequipItem(eslot);
                    inventory.AddItemAt(edata.id, islot, 1, invt_equip.durability, invt_equip.uid);
                }
                else if (idata != null && invt_slot != null && !same_item && idata.type == ItemType.Equipment && idata.equip_slot == edata.equip_slot && invt_slot.quantity == 1)
                {
                    //swap
                    inventory.RemoveItemAt(islot, 1);
                    EquipData.UnequipItem(eslot);
                    EquipData.EquipItem(eslot, idata.id, invt_slot.durability, invt_slot.uid);
                    inventory.AddItemAt(edata.id, islot, 1, invt_equip.durability, invt_equip.uid);
                }
            }
        }

        public void UpdateAllEquippedItemsDurability(bool weapon, float value)
        {
            //Durability
            foreach (KeyValuePair<int, InventoryItemData> pair in EquipData.items)
            {
                InventoryItemData invdata = pair.Value;
                ItemData idata = ItemData.Get(invdata?.item_id);
                if (idata != null && invdata != null && idata.IsWeapon() == weapon && idata.durability_type == DurabilityType.UsageCount)
                    invdata.durability += value;
            }
        }
		
		public void UpdateDurability(EquipSlot eslot, float value)
        {
            InventoryItemData inv = GetEquippedItem(eslot);
            if (inv != null)
                inv.durability += value;
        }

        public InventoryItemData GetEquippedItem(EquipSlot eslot)
        {
            InventoryItemData invt_equip = EquipData.GetEquippedItem(eslot);
            return invt_equip;
        }

        public ItemData GetEquippedItemData(EquipSlot eslot)
        {
            InventoryItemData invt_equip = EquipData.GetEquippedItem(eslot);
            return invt_equip != null ? ItemData.Get(invt_equip.item_id) : null;
        }

        public bool HasEquippedItem(EquipSlot eslot)
        {
            return GetEquippedItem(eslot) != null;
        }

        //---- Swaps/Combine items slot1=selected, slot2=clicked -----
        /// <summary>
        /// Z：应该是与移动格子中的物体有关？？？？？？？？？？？？？？
        /// </summary>
        /// <param name="islot1"></param>
        /// <param name="islot2"></param>
        /// <param name="limit_one_item"></param>

        public void MoveItem(ItemSlot islot1, ItemSlot islot2, bool limit_one_item = false)
        {
            //Debug.Log("测试移动槽位PlayerCharacterInventoryPanel. MoveItem()已经执行");
            InventoryData inventory1 = islot1.GetInventory();
            InventoryData inventory2 = islot2.GetInventory();

            if (inventory1 == null || inventory2 == null)
                return;

            InventoryItemData iitem1 = islot1.GetInventoryItem();
            InventoryItemData iitem2 = islot2.GetInventoryItem();
            ItemData item1 = islot1.GetItem();
            ItemData item2 = islot2.GetItem();

            if (inventory2.type == InventoryType.Equipment)//Z：通过判断传图的第一个格子/第二个格子 是装备面板的来决定是装备/卸载
            {
                EquipItem(inventory1, islot1.index);//TODO:Z:只能确定这里与EquipPanel不能交换格子的有关，其他的就看不懂了
            }
            else if (inventory1.type == InventoryType.Equipment)
            {
                EquipSlot eslot = (EquipSlot)islot1.index;
                UnequipItemTo(inventory2, eslot, islot2.index);
            }
            else if (item1 != null && item1 == item2 && !limit_one_item)
            {
                CombineItems(inventory1, islot1.index, inventory2, islot2.index);
            }
            else if (item1 != item2)
            {
                //Swap
                int quant1 = iitem1 != null ? iitem1.quantity : 0;
                int quant2 = iitem2 != null ? iitem2.quantity : 0;
                bool quantity_is_1 = quant1 <= 1 && quant2 <= 1;
                bool can_swap = !limit_one_item || quantity_is_1 || item2 == null;
                if (can_swap)
                {
                    SwapItems(inventory1, islot1.index, inventory2, islot2.index);
                }
            }
        }

        public void SwapItems(InventoryData inventory_data1, int slot1, InventoryData inventory_data2, int slot2)
        {
            PlayerData.Get().SwapInventoryItems(inventory_data1, slot1, inventory_data2, slot2);
        }

        public void CombineItems(InventoryData inventory_data1, int slot1, InventoryData inventory_data2, int slot2)
        {
            InventoryItemData invdata1 = inventory_data1?.GetInventoryItem(slot1);
            InventoryItemData invdata2 = inventory_data2?.GetInventoryItem(slot2);
            ItemData idata1 = ItemData.Get(invdata1?.item_id);
            if (idata1 != null && invdata1.item_id == invdata2.item_id && (invdata1.quantity + invdata2.quantity) <= idata1.inventory_max)
                PlayerData.Get().CombineInventoryItems(inventory_data1, slot1, inventory_data2, slot2);
        }

        //---- Getters (Multi inventory) ------

        //Has item in any player inventory
        public bool HasItem(ItemData item, int quantity = 1)
        {
            int nb = CountItem(item);
            return nb >= quantity;
        }

        public int CountItem(ItemData item)
        {
            return InventoryData.CountItem(item.id) 
                + EquipData.CountItem(item.id) 
                + (BagData != null ? BagData.CountItem(item.id) : 0);
        }

        //Check inventory / equip /bag
        public bool HasItemInGroup(GroupData group, int quantity = 1)
        {
            return CountItemInGroup(group) >= quantity;
        }

        public int CountItemInGroup(GroupData group)
        {
            return InventoryData.CountItemInGroup(group)
                + EquipData.CountItemInGroup(group)
                + (BagData != null ? BagData.CountItemInGroup(group) : 0);
        }

        //Check inventory / bag
        public bool HasEmptySlot()
        {
            return InventoryData.HasEmptySlot()
                || (BagData != null && BagData.HasEmptySlot());
        }

        public InventoryItemData GetFirstItemInGroup(GroupData group)
        {
            if (InventoryData.HasItemInGroup(group))
                return InventoryData.GetFirstItemInGroup(group);
            if (EquipData.HasItemInGroup(group))
                return EquipData.GetFirstItemInGroup(group);
            if (BagData != null && BagData.HasItemInGroup(group))
                return BagData.GetFirstItemInGroup(group);
            return null;
        }

        //Check both bag and inventory
        public bool CanTakeItem(ItemData item, int quantity = 1)
        {
            return item != null && (InventoryData.CanTakeItem(item.id, quantity) 
                || (BagData != null && BagData.CanTakeItem(item.id, quantity)));
        }

        //Return the best equipped bag (bag is an item that can contain other items)
        public InventoryItemData GetBestEquippedBag()
        {
            int best_size = 0;
            InventoryItemData bag = null;
            foreach (KeyValuePair<int, InventoryItemData> invdata in EquipData.items)
            {
                ItemData idata = invdata.Value?.GetItem();
                if(idata != null && idata.bag_size > best_size && !string.IsNullOrEmpty(invdata.Value.uid))
                {
                    best_size = idata.bag_size;
                    bag = invdata.Value;
                }
            }
            return bag;
        }

        //Return all equipped bags (bag is an item that can contain other items)
        public List<InventoryItemData> GetAllEquippedBags()
        {
            List<InventoryItemData> bags = new List<InventoryItemData>();
            foreach (KeyValuePair<int, InventoryItemData> invdata in EquipData.items)
            {
                ItemData idata = invdata.Value?.GetItem();
                if (idata != null && idata.IsBag() && !string.IsNullOrEmpty(invdata.Value.uid))
                {
                    bags.Add(invdata.Value);
                }
            }
            return bags;
        }

        //Return inventory that can take item (main one first, then bag)
        public InventoryData GetValidInventory(ItemData item, int quantity)
        {
            if (InventoryData.CanTakeItem(item.id, quantity))
                return InventoryData;
            else if(BagData != null && BagData.CanTakeItem(item.id, quantity))
                return BagData;
            return null;
        }

        //--- Equip Attachments

        //7、装备模型显示：让角色 “真的穿上” 装备（生成模型，获取挂载点，挂载上去,加入已装备字典中）
        private void EquipAddedItem(string item_id)//Z 这里是装备物品到玩家身上的用法
        {
            ItemData idata = ItemData.Get(item_id);
            if (idata != null && idata.equipped_prefab != null)
            {
                GameObject equip_obj = Instantiate(idata.equipped_prefab, transform.position, Quaternion.identity);
                EquipItem eitem = equip_obj.GetComponent<EquipItem>();
                if (eitem != null)
                {
                    eitem.data = idata;
                    eitem.target = GetEquipAttachment(idata.equip_slot, idata.equip_side);//Z：获取到装备挂载点
                    if (eitem.child_left != null)//Z：左右手挂载点，这里用不到
                        eitem.target_left = GetEquipAttachment(idata.equip_slot, EquipSide.Left);
                    if (eitem.child_right != null)
                        eitem.target_right = GetEquipAttachment(idata.equip_slot, EquipSide.Right);
                }
                equipped_items.Add(item_id, eitem);
            }
            else
            {
                equipped_items.Add(item_id, null);
            }
        }

        private void UnequipRemovedItem(string item_id)//Z 这里是将装备物品从玩家身上 卸下的用法
        {
            if (equipped_items.ContainsKey(item_id))
            {
                EquipItem eitem = equipped_items[item_id];
                equipped_items.Remove(item_id);
                if (eitem != null)
                    Destroy(eitem.gameObject);
            }
        }

        //Get the EquipAttach on the character (positions on the body where the equipments can spawn)
        /// <summary>
        /// Z：给装备找一个 “挂载点”
        /// </summary>
        /// <param name="slot"></param>
        /// <param name="side"></param>
        /// <returns></returns>
        public EquipAttach GetEquipAttachment(EquipSlot slot, EquipSide side)
        {
            if (slot == EquipSlot.None)
                return null;

            foreach (EquipAttach attach in equip_attachments)
            {
                if (attach.slot == slot)
                {
                    if (attach.side == EquipSide.Default || side == EquipSide.Default || attach.side == side)
                        return attach;
                }
            }
            return null;
        }

        //Get the EquipItem (spawned model for equipment items), of the first equipped weapon
        public EquipItem GetEquippedWeaponMesh()
        {
            InventoryItemData invdata = EquipData.GetEquippedWeapon();
            ItemData equipped = ItemData.Get(invdata?.item_id);
            if (equipped != null)
            {
                foreach (KeyValuePair<string, EquipItem> item in equipped_items)
                {
                    if (item.Key == equipped.id)
                        return item.Value;
                }
            }
            return null;
        }

        //Get the EquipItem (spawned model for equipment items)
        public EquipItem GetEquippedItemMesh(EquipSlot slot)
        {
            InventoryItemData invdata = EquipData.GetEquippedItem(slot);
            ItemData equipped = ItemData.Get(invdata?.item_id);
            if (equipped != null)
            {
                foreach (KeyValuePair<string, EquipItem> item in equipped_items)
                {
                    if (item.Key == equipped.id)
                        return item.Value;
                }
            }
            return null;
        }

        //---- Shortcuts -----（Z这里似乎是为了方便获取数据）
        //6、 管理 “多库存”：背包、装备栏、袋子的分工

        //背包数据
        public InventoryData InventoryData  //Z这里似乎是用来获取 InventoryData 也就是背包数据的
        {
            get { return InventoryData.Get(InventoryType.Inventory, character.player_id); }
        }

        /// <summary>
        /// Z:其实这个本质上是用来得到装备背包数据
        /// </summary>
        public InventoryData EquipData
        {
            get { return InventoryData.GetEquip(InventoryType.Equipment, character.player_id); }
        }
        //装备面板的小背包
        public InventoryData BagData //Can be null if no bag(Z 这里好像是用来获取背包数据用的)
        {
            get {
                InventoryItemData bag = GetBestEquippedBag();
                return (bag != null) ? InventoryData.Get(InventoryType.Bag, bag.uid) : null;
            }
        }

        public PlayerCharacter GetCharacter()
        {
            return character;
        }
    }

}
