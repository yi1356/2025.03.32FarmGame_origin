using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// Birds are alternate version of the animal script but with flying!
    /// 鸟类是动物脚本的另一版本，但具备飞行能力！
    /// </summary>

    public enum BirdState
    {
        Sit = 0,
        Fly = 2,
        FlyDown = 4,
        Alerted = 5,
        Dead = 10,
    }

    [RequireComponent(typeof(Character))]
    public class Bird : MonoBehaviour
    {
        [Header("Fly")]
        public float wander_radius = 10f;        // 飞行范围半径
        public float fly_duration = 20f;         // 飞行持续时间
        public float sit_duration = 20f;         // 静止持续时间

        [Header("Vision")]
        public float detect_range = 5f;          // 检测范围
        public float detect_angle = 360f;        // 检测角度
        public float detect_360_range = 1f;      // 360度检测范围
        public float reaction_time = 0.2f;       // 反应时间

        [Header("Models")]
        public Animator sit_model;               // 静止模型的动画控制器
        public Animator fly_model;               // 飞行模型的动画控制器

        private Character character;             // 角色控制器
        private Destructible destruct;           // 可破坏对象
        private Collider[] colliders;            // 所有碰撞体
        private BirdState state = BirdState.Sit; // 当前状态
        private float state_timer = 0f;          // 状态计时器
        private Vector3 start_pos;               // 初始位置
        private Vector3 target_pos;              // 目标位置
        private float update_timer = 0f;         // 更新计时器

        private void Awake()
        {
            character = GetComponent<Character>();  // 获取角色控制器组件
            destruct = GetComponent<Destructible>(); // 获取可破坏对象组件
            colliders = GetComponentsInChildren<Collider>(); // 获取所有碰撞体组件
            start_pos = transform.position; // 记录初始位置
            target_pos = transform.position; // 初始化目标位置为当前位置
            destruct.onDeath += OnDeath;    // 注册死亡事件监听器
            state_timer = 99f;              // 设置状态计时器，用于立即飞行
            update_timer = Random.Range(-1f, 1f); // 初始化更新计时器

            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f); // 随机设置初始旋转角度
        }

        private void Start()
        {
            StopFly(); // 开始时停止飞行
        }

        void Update()
        {
            if (TheGame.Get().IsPaused()) // 如果游戏暂停则返回
                return;

            state_timer += Time.deltaTime; // 更新状态计时器

            // 根据不同状态执行相应行为
            if (state == BirdState.Sit)
            {
                if (state_timer > sit_duration)
                {
                    FlyAway(); // 超过静止持续时间后飞行
                }
            }

            if (state == BirdState.Alerted)
            {
                if (state_timer > reaction_time)
                {
                    FlyAway(); // 超过反应时间后飞行
                }
            }

            if (state == BirdState.Fly)
            {
                // 飞行时隐藏飞行模型，如果已到达目标位置则停止飞行
                if (fly_model.gameObject.activeSelf && character.HasReachedMoveTarget())
                    fly_model.gameObject.SetActive(false);

                if (state_timer > fly_duration)
                {
                    StopFly(); // 超过飞行持续时间后停止飞行
                }
            }

            if (state == BirdState.FlyDown)
            {
                if (character.HasReachedMoveTarget())
                {
                    Land(); // 如果已到达目标位置则降落
                }
            }

            update_timer += Time.deltaTime;
            if (update_timer > 0.5f)
            {
                update_timer = Random.Range(-0.1f, 0.1f);
                SlowUpdate(); // 优化：定时执行慢速更新
            }
        }

        private void SlowUpdate()
        {
            if (state == BirdState.Sit)
            {
                DetectThreat(); // 在静止状态下检测威胁
            }
        }

        // 飞离当前位置
        public void FlyAway()
        {
            state_timer = 0f; // 重置状态计时器
            FindFlyPosition(transform.position, wander_radius, out target_pos); // 找到飞行目标位置
            state = BirdState.Fly; // 切换状态为飞行
            sit_model.gameObject.SetActive(false); // 隐藏静止模型
            fly_model.gameObject.SetActive(true);  // 显示飞行模型
            character.MoveTo(target_pos);           // 角色移动到目标位置

            foreach (Collider collide in colliders)
                collide.enabled = false; // 禁用所有碰撞体
        }

        // 停止飞行
        public void StopFly()
        {
            state_timer = 0f; // 重置状态计时器
            Vector3 npos;
            bool success = FindGroundPosition(start_pos, wander_radius, out npos); // 找到降落位置
            if (success)
            {
                state = BirdState.FlyDown; // 切换状态为降落
                target_pos = npos;         // 设置目标位置为降落位置
                fly_model.gameObject.SetActive(true);  // 显示飞行模型
                sit_model.gameObject.SetActive(false); // 隐藏静止模型
                character.MoveTo(target_pos);           // 角色移动到目标位置

                foreach (Collider collide in colliders)
                    collide.enabled = false; // 禁用所有碰撞体
            }
        }

        // 降落到地面
        private void Land()
        {
            state_timer = Random.Range(-1f, 1f); // 随机设置状态计时器
            state = BirdState.Sit; // 切换状态为静止
            sit_model.gameObject.SetActive(true);  // 显示静止模型
            fly_model.gameObject.SetActive(false); // 隐藏飞行模型

            foreach (Collider collide in colliders)
                collide.enabled = true; // 启用所有碰撞体
        }

        // 死亡处理
        private void OnDeath()
        {
            StopMoving(); // 停止移动
            state = BirdState.Dead; // 切换状态为死亡
            state_timer = 0f;       // 重置状态计时器
            sit_model.gameObject.SetActive(true);  // 显示静止模型
            fly_model.gameObject.SetActive(false); // 隐藏飞行模型
            sit_model.SetTrigger("Death"); // 播放死亡动画
        }

        // 找到飞行目标位置
        private bool FindFlyPosition(Vector3 pos, float radius, out Vector3 fly_pos)
        {
            Vector3 offset = new Vector3(Random.Range(-radius, radius), 0f, Random.Range(-radius, radius));
            fly_pos = pos + offset;
            fly_pos.y = start_pos.y + 20f; // 设置飞行高度
            return true;
        }

        // 找到降落位置，确保不会降落在障碍物上
        private bool FindGroundPosition(Vector3 pos, float radius, out Vector3 ground_pos)
        {
            Vector3 offset = new Vector3(Random.Range(-radius, radius), 0f, Random.Range(-radius, radius));
            Vector3 center = pos + offset;
            bool found = PhysicsTool.FindGroundPosition(center, 50f, character.ground_layer.value, out ground_pos); // 使用物理工具找到地面位置
            return found;
        }

        // 检测威胁，即玩家是否在视野范围内
        private void DetectThreat()
        {
            Vector3 pos = transform.position;

            // 检测玩家
            foreach (PlayerCharacter player in PlayerCharacter.GetAll())
            {
                Vector3 player_dir = (player.transform.position - pos);
                if (player_dir.magnitude < detect_range)
                {
                    float half_angle = detect_angle / 2f; // 计算半角度
                    float angle = Vector3.Angle(transform.forward, player_dir.normalized);
                    if (angle < half_angle || player_dir.magnitude < detect_360_range)
                    {
                        state = BirdState.Alerted; // 切换状态为警戒
                        state_timer = 0f;          // 重置状态计时器
                        StopMoving();              // 停止移动
                        return;
                    }
                }
            }

            // 检测其他角色
            foreach (Selectable selectable in Selectable.GetAllActive())
            {
                if (selectable.gameObject != gameObject)
                {
                    Vector3 dir = (selectable.transform.position - pos);
                    if (dir.magnitude < detect_range)
                    {
                        Character charac = selectable.Character;
                        if (charac && charac.attack_enabled) // 只有角色能攻击时才害怕
                        {
                            if (charac.GetDestructible().target_group != destruct.target_group)
                            {
                                state = BirdState.Alerted; // 切换状态为警戒
                                state_timer = 0f;          // 重置状态计时器
                                StopMoving();              // 停止移动
                                return;
                            }
                        }
                    }
                }
            }
        }

        // 停止移动
        public void StopMoving()
        {
            target_pos = transform.position;
            state_timer = 0f;
            character.Stop(); // 停止角色移动
        }
    }
}
