using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 耕地，以便种植植物
    /// </summary>

    [CreateAssetMenu(fileName = "Action", menuName = "FarmingEngine/Actions/Hoe", order = 50)]
    public class ActionHoe : SAction
    {
        public float hoe_range = 1f; // 耕地的范围

        // 执行操作
        public override void DoAction(PlayerCharacter character, ItemSlot slot)
        {
            Vector3 pos = character.transform.position + character.GetFacing() * hoe_range; // 计算耕地的位置

            PlayerCharacterHoe hoe = character.GetComponent<PlayerCharacterHoe>(); // 获取角色的耕地组件
            hoe?.HoeGround(pos); // 调用耕地方法耕地

            InventoryItemData ivdata = character.EquipData.GetInventoryItem(slot.index); // 获取装备物品的数据
            if (ivdata != null)
                ivdata.durability -= 1; // 减少装备耐久度
        }

        // 判断是否可以执行操作的条件方法
        public override bool CanDoAction(PlayerCharacter character, ItemSlot slot)
        {
            return slot is EquipSlotUI; // 只有装备槽可以进行耕地操作
        }
    }

}