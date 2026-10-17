using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 在火堆上烹饪物品（如生肉）
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Cook", order = 50)]
    public class ActionCook : MAction
    {
        public ItemData cooked_item; // 烹饪后的物品数据
        public float duration = 0.5f; // 烹饪持续时间
        public float energy = 1f; // 执行烹饪消耗的能量

        //TODO：2025.08.15火烤肉制作食物MAction
        // 合并操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            string anim = character.Animation ? character.Animation.use_anim : ""; // 获取角色的使用动画名称
            character.TriggerAnim(anim, select.transform.position); // 触发角色的使用动画，并传入选择对象的位置
            character.TriggerProgressBusy(duration, () =>
            {
                InventoryData inventory = slot.GetInventory(); // 获取物品栏数据
                inventory.RemoveItemAt(slot.index, 1); // 从物品栏移除一个物品
                character.Inventory.GainItem(cooked_item, 1); // 角色获得烹饪后的物品
                character.Attributes.AddAttribute(AttributeType.Energy, -energy); // 扣除角色的能量属性
            });
        }

        // 判断是否可以进行烹饪操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot, Selectable select)
        {
            return character.Attributes.GetAttributeValue(AttributeType.Energy) >= energy; // 只有当角色的能量属性大于等于消耗能量时才能执行烹饪操作
        }
    }

}