using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    [RequireComponent(typeof(Selectable))]
    public class Ladder : MonoBehaviour
    {
        public Vector3 top_jump_offset; // 跳跃到顶部时的偏移量

        private Selectable select; // 可选择组件
        private Collider collide; // 碰撞体

        private bool front_blocked = false; // 前方是否被阻挡
        private bool back_blocked = false; // 后方是否被阻挡

        private void Awake()
        {
            select = GetComponent<Selectable>(); // 获取可选择组件
            collide = GetComponentInChildren<Collider>(); // 获取子物体中的碰撞体

            select.onUse += ClimbLadder; // 注册爬梯事件
        }

        private void Start()
        {
            // 检测前方和后方是否有阻挡物体
            front_blocked = PhysicsTool.RaycastCollision(transform.position + Vector3.up * 0.5f, transform.forward, out RaycastHit hit1);
            back_blocked = PhysicsTool.RaycastCollision(transform.position + Vector3.up * 0.5f, -transform.forward, out RaycastHit hit2);
        }

        // 爬梯方法，由玩家角色调用
        public void ClimbLadder(PlayerCharacter player)
        {
            if (player.Climbing != null)
                player.Climbing.Climb(this); // 调用爬梯动作
        }

        // 获取碰撞体的边界框
        public Bounds GetBounds()
        {
            return collide.bounds;
        }

        // 获取偏移方向，用于确定玩家的位置偏移
        public Vector3 GetOffsetDir(Vector3 player_pos)
        {
            Vector3 dir = player_pos - transform.position;
            return Vector3.Project(dir, transform.forward);
        }

        // 检测侧面是否被阻挡
        public bool IsSideBlocked(Vector3 dir)
        {
            float dot = Vector3.Dot(dir, transform.forward);
            if (dot > 0f) return IsFrontBlocked();
            else return IsBackBlocked();
        }

        // 前方是否被阻挡
        public bool IsFrontBlocked() { return front_blocked; }

        // 后方是否被阻挡
        public bool IsBackBlocked() { return back_blocked; }
    }
}