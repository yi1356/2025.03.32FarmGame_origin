using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 用于射程武器射出的投射物
    /// A projectile shot with a ranged weapon
    /// </summary>

    public class Projectile : MonoBehaviour
    {
        public float speed = 10f; // 投射物速度
        public float duration = 10f; // 存活时间
        public float gravity = 0.2f; // 重力影响

        public AudioClip shoot_sound; // 射击音效

        [HideInInspector]
        public int damage = 0; // 将被武器的伤害替代

        [HideInInspector]
        public Vector3 dir; // 射击方向

        [HideInInspector]
        public PlayerCharacter player_shooter; // 射击的玩家角色

        [HideInInspector]
        public Destructible shooter; // 射击者

        private Vector3 curve_dir = Vector3.zero; // 曲线方向
        private float curve_dist = 0f; // 曲线距离
        private float timer = 0f; // 计时器

        void Start()
        {
            TheAudio.Get().PlaySFX("projectile", shoot_sound); // 播放射击音效
        }

        void Update()
        {
            if (TheGame.Get().IsPaused()) // 如果游戏暂停
                return;

            if (curve_dist > 0.01f && (timer * speed) < curve_dist)
            {
                // 在自由视角模式下的初始曲线方向
                float value = Mathf.Clamp01(timer * speed / curve_dist);
                Vector3 cdir = (1f - value) * curve_dir + value * dir;
                transform.position += cdir * speed * Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(cdir.normalized, Vector2.up);
            }
            else
            {
                // 常规方向
                transform.position += dir * speed * Time.deltaTime;
                dir += gravity * Vector3.down * Time.deltaTime;
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector2.up);
            }

            timer += Time.deltaTime;
            if (timer > duration)
                Destroy(gameObject); // 超过存活时间销毁投射物
        }

        public void SetInitialCurve(Vector3 dir, float dist = 10f)
        {
            curve_dir = dir;
            curve_dist = dist * 1.25f; // 增加偏移量以提高精度
        }

        private void OnTriggerEnter(Collider collision)
        {
            Destructible destruct = collision.GetComponent<Destructible>();
            if (destruct != null && !destruct.attack_melee_only)
            {
                if (player_shooter != null)
                    destruct.TakeDamage(player_shooter, damage); // 由玩家角色造成伤害
                else if (shooter != null)
                    destruct.TakeDamage(shooter, damage); // 由射击者造成伤害
                else
                    destruct.TakeDamage(damage); // 一般伤害
                Destroy(gameObject); // 销毁投射物
            }

            PlayerCharacterCombat player = collision.GetComponent<PlayerCharacterCombat>();
            if (player != null && (player_shooter == null || player_shooter.Combat != player))
            {
                player.TakeDamage(damage); // 玩家角色承受伤害
                Destroy(gameObject); // 销毁投射物
            }
        }
    }
}
