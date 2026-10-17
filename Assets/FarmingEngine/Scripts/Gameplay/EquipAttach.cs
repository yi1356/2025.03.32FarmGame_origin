using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 角色身上用于装备附着的位置（如手、头部、脚等）
    /// </summary>
    public class EquipAttach : MonoBehaviour
    {
        public EquipSlot slot; // 装备槽位
        public EquipSide side; // 装备侧面（如左右手）
        public float scale = 1f; // 缩放比例

        private PlayerCharacter character; // 关联的玩家角色

        private void Awake()
        {
            character = GetComponentInParent<PlayerCharacter>(); // 在父级中获取玩家角色组件
        }

        /// <summary>
        /// 获取关联的玩家角色
        /// </summary>
        /// <returns>关联的玩家角色</returns>
        public PlayerCharacter GetCharacter()
        {
            return character;
        }
    }
}