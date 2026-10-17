using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 允许为 EquipPanel 设置装备槽位
    /// </summary>
    public class EquipSlotUI : ItemSlot
    {
        [Header("Equip Slot")]
        public EquipSlot equip_slot; // 装备槽位类型

        protected override void Start()
        {
            base.Start();
            index = (int)equip_slot; // 将装备槽位枚举值转换为整数，并设置为槽位索引
        }
    }
}