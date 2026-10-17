using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    public class TowerAttack : MonoBehaviour
    {
        public int attack_damage = 10;       // 攻击基础伤害
        public float attack_range = 20f;     // 攻击范围
        public float attack_cooldown = 2f;   // 攻击间隔时间（秒）

        public Transform shoot_root;         // 发射点根节点
        public GameObject projectile_prefab; // 投射物预制体

        private Buildable buildable;         // 可建造物组件
        private Destructible destruct;       // 可摧毁物组件
        private float timer = 0f;            // 计时器

        private void Awake()
        {
            buildable = GetComponent<Buildable>();     // 获取可建造物组件
            destruct = GetComponent<Destructible>();   // 获取可摧毁物组件
        }

        private void Update()
        {
            if (TheGame.Get().IsPaused())   // 如果游戏暂停
                return;

            if (buildable != null && buildable.IsBuilding())   // 如果正在建造中
                return;

            timer += Time.deltaTime;    // 更新计时器
            if (timer > attack_cooldown)    // 如果超过攻击间隔时间
            {
                timer = 0f;     // 重置计时器
                ShootNearestEnemy();    // 发射至最近的敌人
            }
        }

        // 发射至最近的敌人
        public void ShootNearestEnemy()
        {
            Destructible nearest = Destructible.GetNearestAttack(AttackTeam.Enemy, transform.position, attack_range); // 获取最近的敌方可摧毁物
            Shoot(nearest); // 发射至目标
        }

        // 发射
        public void Shoot(Destructible target)
        {
            if (target != null && projectile_prefab != null)   // 如果目标和投射物预制体不为空
            {
                int damage = attack_damage; // 设置伤害值
                Vector3 pos = GetShootPos(); // 获取发射位置
                Vector3 dir = target.GetCenter() - pos; // 计算方向向量
                GameObject proj = Instantiate(projectile_prefab, pos, Quaternion.LookRotation(dir.normalized, Vector3.up)); // 实例化投射物
                Projectile project = proj.GetComponent<Projectile>(); // 获取投射物组件
                project.shooter = destruct; // 设置发射者
                project.dir = dir.normalized; // 设置方向
                project.damage = damage; // 设置伤害
            }
        }

        // 获取发射位置
        public Vector3 GetShootPos()
        {
            if (shoot_root != null)
                return shoot_root.position; // 返回发射点位置
            return transform.position + Vector3.up * 2f; // 返回默认位置（当前位置向上偏移2个单位）
        }
    }
}
