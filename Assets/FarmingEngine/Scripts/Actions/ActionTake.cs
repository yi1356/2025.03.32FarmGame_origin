using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 用于获取具有物品变体（诱饵/陷阱）的建筑物[Z：这个好像 还不是普通的收集，好像是收集特殊角色(小黄鸡)、和特殊建筑的]
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Take", order = 50)]
    public class ActionTake : SAction
    {
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            PlayerData pdata = PlayerData.Get(); // 获取玩家数据
            Construction construction = select.GetComponent<Construction>(); // 获取选择对象上的建筑物组件
            if (construction != null && construction.data != null)
            {
                ItemData take_item = construction.data.take_item_data; // 获取可以获取的物品数据
                InventoryData inv_data = character.Inventory.GetValidInventory(take_item, 1); // 获取玩家角色有效的背包
                if (take_item != null && inv_data != null)
                {
                    BuiltConstructionData bdata = pdata.GetConstructed(construction.GetUID()); // 获取建筑物的数据
                    float durability = bdata != null && bdata.durability > 0.01f ? bdata.durability : take_item.durability; // 计算物品的耐久度

                    inv_data.AddItem(take_item.id, 1, durability, select.GetUID()); // 将物品添加到背包中
                    select.Destroy(); // 销毁选择对象（建筑物）
                }
            }

            Character acharacter = select.GetComponent<Character>(); // 获取选择对象上的角色组件
            if (acharacter != null)
            {
                ItemData take_item = acharacter.data.take_item_data; // 获取可以获取的物品数据[Z:其实就是获取物品的itemData数据,为了存入背包]
                InventoryData inv_data = character.Inventory.GetValidInventory(take_item, 1); // 获取玩家角色有效的背包[Z：获取到背包]
                if (take_item != null && inv_data != null)
                {
                    TrainedCharacterData cdata = pdata.GetCharacter(acharacter.GetUID()); // 获取角色的数据
                    inv_data.AddItem(take_item.id, 1, take_item.durability, select.GetUID()); // 将物品添加到背包中
                    select.Destroy(); // 销毁选择对象（角色）
                }
            }
        }
    }

}
