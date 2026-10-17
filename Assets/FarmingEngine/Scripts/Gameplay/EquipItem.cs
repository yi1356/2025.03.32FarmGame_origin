using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 玩家身上显示已装备物品的物体。将附着到 EquipAttach 上。
    /// </summary>
    public class EquipItem : MonoBehaviour
    {
        public ItemData data; // 物品数据

        [Header("武器动画")]
        public string attack_melee_anim = "Attack"; // 近战攻击动画
        public string attack_ranged_anim = "Shoot"; // 远程攻击动画

        [Header("武器时间设置")]
        public bool override_timing = false; // 如果为真，角色的默认起始和结束动作将被以下值覆盖
        public float attack_windup = 0.7f; // 攻击起始时间
        public float attack_windout = 0.4f; // 攻击结束时间

        [Header("子物体网格")]
        public GameObject child_left; // 左侧子物体
        public GameObject child_right; // 右侧子物体

        [HideInInspector]
        public EquipAttach target; // 目标装备附着点
        [HideInInspector]
        public EquipAttach target_left; // 左侧目标装备附着点
        [HideInInspector]
        public EquipAttach target_right; // 右侧目标装备附着点

        private Vector3 start_scale; // 初始缩放

        void Start()
        {
            start_scale = transform.localScale; // 记录初始缩放
        }

        void LateUpdate()
        {
            if (target == null)
            {
                Destroy(gameObject); // 如果没有目标装备附着点，销毁物体
                return;
            }

            transform.position = target.transform.position; // 设置位置与目标一致
            transform.rotation = target.transform.rotation; // 设置旋转与目标一致

            if (child_left == null && child_right == null)
            {
                transform.localScale = start_scale * target.scale; // 如果没有子物体，则设置缩放
            }

            if (child_right != null && target_right != null)
            {
                child_right.transform.position = target_right.transform.position; // 设置右侧子物体位置
                child_right.transform.rotation = target_right.transform.rotation; // 设置右侧子物体旋转
                child_right.transform.localScale = start_scale * target_right.scale; // 设置右侧子物体缩放
            }

            if (child_left != null && target_left != null)
            {
                child_left.transform.position = target_left.transform.position; // 设置左侧子物体位置
                child_left.transform.rotation = target_left.transform.rotation; // 设置左侧子物体旋转
                child_left.transform.localScale = start_scale * target_left.scale; // 设置左侧子物体缩放
            }
        }

        /// <summary>
        /// 获取关联的玩家角色
        /// </summary>
        /// <returns>关联的玩家角色，如果没有目标装备附着点则返回 null</returns>
        public PlayerCharacter GetCharacter()
        {
            if (target != null)
                return target.GetCharacter(); // 返回目标装备附着点的关联玩家角色
            return null;
        }
    }
}
