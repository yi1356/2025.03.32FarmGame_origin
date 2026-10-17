using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 使用铲子挖掘，移除植物或挖掘埋藏的物品
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Dig", order = 50)]
    public class ActionDig : SAction
    {
        public float dig_range = 2f; // 挖掘范围
        public float energy = 1f; // 执行挖掘消耗的能量

        // 执行挖掘操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            DigSpot spot = DigSpot.GetNearest(character.transform.position, dig_range); // 获取最近的挖掘点
            Plant plant = Plant.GetNearest(character.transform.position, dig_range); // 获取最近的植物

            Vector3 pos = plant != null ? plant.transform.position : character.transform.position; // 确定动画播放位置，默认为角色位置
            if (spot != null)
                pos = spot.transform.position; // 如果存在挖掘点，则设置播放位置为挖掘点位置

            string animation = character.Animation ? character.Animation.dig_anim : ""; // 获取角色的挖掘动画名称
            character.TriggerAnim(animation, pos); // 触发角色的挖掘动画，并传入播放位置
            character.TriggerProgressBusy(1.5f, () =>
            {
                if (spot != null)
                    spot.Dig(); // 如果存在挖掘点，则进行挖掘操作
                else if (plant != null)
                    plant.Kill(); // 如果存在植物，则移除植物

                character.Attributes.AddAttribute(AttributeType.Energy, -energy); // 扣除角色的能量属性

                InventoryItemData ivdata = character.EquipData.GetInventoryItem(slot.index); // 获取装备物品数据
                if (ivdata != null)
                    ivdata.durability -= 1; // 减少物品耐久度
            });
        }

        // 判断是否可以进行挖掘操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return slot is EquipSlotUI && character.Attributes.GetAttributeValue(AttributeType.Energy) >= energy; // 只有当物品槽是装备槽并且角色的能量属性大于等于消耗能量时才能执行挖掘操作
        }
    }

}