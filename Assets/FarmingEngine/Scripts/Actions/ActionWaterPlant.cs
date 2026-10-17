using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 使用浇水罐浇水植物的动作
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/WaterPlant", order = 50)]
    public class ActionWaterPlant : AAction
    {
        public GroupData required_item; // 所需物品组数据，用于检查玩家是否装备了正确的物品
        public float energy = 1f; // 执行动作消耗的能量

        //TODO：2025.08.14浇水动作（自动行为）
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            InventoryItemData item = character.EquipData.GetFirstItemInGroup(required_item); // 获取玩家装备中的第一个符合所需物品组的物品数据
            ItemData idata = ItemData.Get(item?.item_id); // 获取物品数据
            Plant plant = select.GetComponent<Plant>(); // 获取选择对象上的植物组件
            Soil soil = select.GetComponent<Soil>(); // 获取选择对象上的土壤组件
            if (idata != null && (plant != null || soil != null))
            {
                // 移除水量
                if (idata.durability_type == DurabilityType.UsageCount)//Z:其实就是将次数耐久度，当成了可浇水次数
                    item.durability -= 1f;
                else
                    character.Inventory.RemoveEquipItem(idata.equip_slot);

                string animation = character.Animation ? character.Animation.water_anim : ""; // 获取浇水动画名称
                character.TriggerAnim(animation, select.transform.position, 1f); // 触发浇水动画
                character.TriggerProgressBusy(1f, () =>//Z：AAction 应该是体现在这里，需要一定的时间
                {
                    // 添加水到植物或土壤
                    if(plant)
                        plant.Water();
                    if (soil)
                        soil.Water();

                    character.Attributes.AddAttribute(AttributeType.Energy, -energy); // 减少玩家的能量属性
                });
            }
        }

        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            Plant plant = select.GetComponent<Plant>(); // 获取选择对象上的植物组件
            Soil soil = select.GetComponent<Soil>(); // 获取选择对象上的土壤组件
            bool has_energy = character.Attributes.GetAttributeValue(AttributeType.Energy) >= energy; // 检查玩家是否有足够的能量执行动作
            return (plant != null || soil != null) && has_energy && character.EquipData.HasItemInGroup(required_item); // 返回是否可以执行浇水动作的条件：存在植物或土壤组件、有足够的能量、装备了所需的物品组
        }
    }

}
