using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 将此脚本添加到玩家角色以便能够爬梯子
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterClimb : MonoBehaviour
    {
        public float climb_speed = 2f;        // 爬梯子速度
        public float climb_offset = 0.5f;     // 爬梯子偏移量

        private PlayerCharacter character;    // 玩家角色对象
        private Ladder climb_ladder = null;   // 当前爬的梯子
        private bool climbing = false;        // 是否正在爬梯子
        private Vector3 move_vect;            // 移动向量
        private Vector3 current_offset;       // 当前偏移量

        private bool auto_move = false;       // 是否自动移动
        private float auto_move_height = 0f;  // 自动移动的目标高度
        private float climb_timer = 0f;       // 爬梯子计时器

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();  // 获取玩家角色组件
        }

        private void Start()
        {
            PlayerControlsMouse controls = PlayerControlsMouse.Get();
            controls.onClickFloor += OnClickFloor;  // 监听鼠标点击地面事件
        }

        void Update()
        {
            climb_timer += Time.deltaTime;

            if (IsClimbing())
            {
                // 爬梯子上下移动
                PlayerControls controls = PlayerControls.Get();
                Vector3 cmove = controls.GetMove();
                move_vect = Vector3.zero;
                if (cmove.magnitude > 0.1f)
                {
                    transform.position += Vector3.up * cmove.z * climb_speed * Time.deltaTime;
                    move_vect = Vector3.up * cmove.z * climb_speed;
                    auto_move = false;
                }

                // 自动移动
                if (auto_move)
                {
                    Vector3 dir = Vector3.up * (auto_move_height - transform.position.y);
                    if (dir.magnitude > 0.1f)
                    {
                        transform.position += dir.normalized * climb_speed * Time.deltaTime;
                        move_vect = dir.normalized * climb_speed;
                    }
                }

                // 面向梯子
                character.FaceTorward(transform.position - current_offset);

                // 贴梯子位置
                if (climb_ladder != null)
                    transform.position = new Vector3(climb_ladder.transform.position.x, transform.position.y, climb_ladder.transform.position.z) + current_offset;

                // 按钮停止爬梯子
                if ((controls.IsPressAction() || controls.IsPressJump()) && climb_timer > 0.5f)
                    StopClimb();

                // 到达底部和顶部
                if (character.IsGrounded() && move_vect.y < -0.1f)
                    StopClimb();
                if (climb_ladder != null && character.transform.position.y < climb_ladder.GetBounds().min.y && move_vect.y < -0.1f)
                    StopClimb();
                if (climb_ladder != null && character.transform.position.y > climb_ladder.GetBounds().max.y && move_vect.y > 0.1f)
                    StopClimbTop();
                if (climb_ladder == null)
                    StopClimb();
            }
        }

        // 点击地面事件处理
        private void OnClickFloor(Vector3 pos)
        {
            if (IsClimbing())
            {
                auto_move = true;
                auto_move_height = pos.y;
                if (auto_move_height > transform.position.y)
                    auto_move_height += 1f;
                if (auto_move_height < transform.position.y)
                    auto_move_height -= 1f;
            }
        }

        // 开始爬梯子
        public void Climb(Ladder ladder)
        {
            if (ladder != null && !IsClimbing() && character.IsMovementEnabled() && climb_timer > 0.5f)
            {
                climb_ladder = ladder;
                character.DisableMovement();
                character.StopMove();
                Vector3 dir = ladder.GetOffsetDir(transform.position);
                if (ladder.IsSideBlocked(dir) && !ladder.IsSideBlocked(-dir))
                    dir = -dir; // 如果方向被阻挡则反向偏移
                current_offset = dir.normalized * climb_offset;
                transform.position = new Vector3(ladder.transform.position.x, transform.position.y, ladder.transform.position.z) + current_offset;
                transform.rotation = Quaternion.LookRotation(-current_offset, Vector3.up);
                auto_move = false;
                climbing = true;
                climb_timer = 0f;
            }
        }

        // 停止爬梯子
        public void StopClimb()
        {
            if (IsClimbing())
            {
                climb_ladder = null;
                character.EnableMovement();
                character.StopMove();
                character.FaceTorward(transform.position - current_offset);
                auto_move = false;
                climbing = false;
                climb_timer = 0f;
            }
        }

        // 到达顶部停止爬梯子
        public void StopClimbTop()
        {
            if (IsClimbing())
            {
                Vector3 dir = climb_ladder.GetOffsetDir(transform.position);
                Vector3 jump_offset = Quaternion.LookRotation(-dir, Vector3.up) * climb_ladder.top_jump_offset;

                StopClimb();

                transform.position += jump_offset;
            }
        }

        // 是否正在移动
        public bool IsMoving()
        {
            return move_vect.magnitude > 0.1f;
        }

        // 是否正在爬梯子
        public bool IsClimbing()
        {
            return climbing;
        }
    }
}
