using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 使用铲子挖掘，在可选择对象上放置以在左键点击时自动挖掘
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/DigAuto", order = 50)]
    public class ActionDigAuto : AAction
    {
        public GroupData required_item; // 所需物品组
        public float energy = 1f; // 执行挖掘消耗的能量

        // 执行自动挖掘操作
        public override void DoAction(PlayerCharacter character, Selectable select)
        {
            DigSpot spot = select.GetComponent<DigSpot>(); // 获取可选择对象的挖掘点组件
            if (spot != null)
            {
                string animation = character.Animation ? character.Animation.dig_anim : ""; // 获取角色的挖掘动画名称
                character.TriggerAnim(animation, spot.transform.position); // 触发角色的挖掘动画，并传入挖掘点位置
                character.TriggerProgressBusy(1.5f, () =>
                {
                    spot.Dig(); // 执行挖掘操作

                    character.Attributes.AddAttribute(AttributeType.Energy, -energy); // 扣除角色的能量属性

                    InventoryItemData ivdata = character.EquipData.GetFirstItemInGroup(required_item); // 获取所需物品组的第一个物品数据
                    if (ivdata != null)
                        ivdata.durability -= 1; // 减少物品耐久度
                });
            }
        }

        // 判断是否可以进行自动挖掘操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, Selectable select)
        {
            return character.EquipData.HasItemInGroup(required_item) && character.Attributes.GetAttributeValue(AttributeType.Energy) >= energy; // 只有当角色装备中有所需物品组的物品并且角色的能量属性大于等于消耗能量时才能执行自动挖掘操作
        }
    }

}