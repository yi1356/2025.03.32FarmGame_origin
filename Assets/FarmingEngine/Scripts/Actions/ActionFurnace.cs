using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 在熔炉中熔化物品
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Furnace", order = 50)]
    public class ActionFurnace : MAction
    {
        public ItemData melt_item; // 熔化后的物品数据
        public int item_quantity_in = 1; // 输入的物品数量
        public int item_quantity_out = 1; // 输出的物品数量
        public float duration = 1f; // 游戏内时间，单位为小时

        // 执行合并操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            InventoryData inventory = slot.GetInventory(); // 获取物品槽对应的物品栏数据

            Furnace furnace = select.GetComponent<Furnace>(); // 获取选择对象的熔炉组件
            if (furnace != null && furnace.CountItemSpace() >= item_quantity_out) // 如果熔炉存在且有足够的空间容纳输出物品
            {
                furnace.PutItem(slot.GetItem(), melt_item, duration, item_quantity_out); // 放入物品进行熔化
                inventory.RemoveItemAt(slot.index, item_quantity_in); // 移除输入的物品
            }
        }

        // 判断是否可以进行操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            Furnace furnace = select.GetComponent<Furnace>(); // 获取选择对象的熔炉组件
            InventoryData inventory = slot.GetInventory(); // 获取物品槽对应的物品栏数据
            InventoryItemData iidata = inventory?.GetInventoryItem(slot.index); // 获取物品槽中的物品数据
            return furnace != null && iidata != null && furnace.CountItemSpace() >= item_quantity_out && iidata.quantity >= item_quantity_in; // 只有当熔炉存在、有足够的空间容纳输出物品、物品槽中有足够的输入物品时才能进行操作
        }
    }

}