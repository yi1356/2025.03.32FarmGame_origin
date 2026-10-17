using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 脚本用于允许玩家跳跃
    /// </summary>
    
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterJump : MonoBehaviour
    {
        public float jump_power = 10f; // 跳跃力量
        public float jump_duration = 0.2f; // 跳跃持续时间

        public UnityAction onJump; // 跳跃事件

        private PlayerCharacter character;

        private float jump_timer = 0f; // 跳跃计时器

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (character.IsDead())
                return;

            jump_timer -= Time.deltaTime;
        }

        public void Jump()
        {
            // 如果不在跳跃中，并且角色在地面上，并且没有忙碌，并且不在骑乘状态，并且不在游泳状态
            if (!IsJumping() && character.IsGrounded() && !character.IsBusy() && !character.IsRiding() && !character.IsSwimming())
            {
                character.SetFallVect(Vector3.up * jump_power); // 设置跳跃向量
                jump_timer = jump_duration; // 开始跳跃计时

                if (onJump != null)
                    onJump.Invoke(); // 触发跳跃事件
            }
        }

        public float GetJumpTimer()
        {
            return jump_timer; // 返回跳跃计时器的值
        }

        public bool IsJumping()
        {
            return jump_timer > 0f; // 判断是否在跳跃中
        }
    }
}