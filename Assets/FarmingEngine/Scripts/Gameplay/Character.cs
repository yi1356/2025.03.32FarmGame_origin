using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// Characters是可以给予移动或执行动作命令的盟友或NPC。（Z：注意与PlayerCharacter的区别）
    /// Z：这段代码是游戏中 “角色行为总控制器”，负责管理角色的所有核心行为：移动、攻击、跟随、逃跑、避障、状态切换等
    /// </summary>

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Destructible))]
    [RequireComponent(typeof(UniqueID))]
    public class Character : Craftable
    {
        [Header("Character")]
        public CharacterData data;                           // 角色数据

        [Header("Move")]
        public bool move_enabled = true;                     // 是否启用移动
        public float move_speed = 2f;                        // 移动速度
        public float rotate_speed = 250f;                    // 旋转速度
        public float moving_threshold = 0.15f;               // 移动阈值，达到此速度则被认为在移动（触发动画等）
        [Header("Z：这个其实是控制是否要避障,需要手动设置")]
        public bool avoid_obstacles = true;                  // 避免障碍物，更高性能的替代方法，通过射线检测前方是否有障碍物，然后绕过它们
        public bool use_navmesh = false;                     // 使用真实的Unity导航网格(Z：可以手动开启，但是没有发现NavMeshAgent组件，不知道为什么)
                                                             //注意这里：这里只用到了unity导航功能大一部分，只是用来获取规划路径中的位置点而已，然后自己控制移动*************

        [Header("Ground/Falling")]
        public float fall_speed = 20f;                       // 下落速度
        public float slope_angle_max = 45f;                  // 最大可爬升角度（度数）
        public float ground_detect_dist = 0.1f;              // 地面检测距离，角色与地面之间的边缘距离，用于检测角色是否着陆
        public LayerMask ground_layer = ~0;                  // 什么被认为是地面？
        public float ground_refresh_rate = 0.2f;             // 地面检测刷新率，值越高性能越好，但精度越低，0 = 每帧都检测

        [Header("Attack")]
        public bool attack_enabled = true;                    // 是否启用攻击
        public int attack_damage = 10;                        // 攻击伤害
        public float attack_range = 1f;                       // 攻击范围
        public float attack_cooldown = 3f;                    // 攻击冷却时间
        public float attack_windup = 0.5f;                     // 攻击准备时间
        public float attack_duration = 1f;                     // 攻击持续时间
        public AudioClip attack_audio;                        // 攻击音效

        [Header("Attack Ranged")]
        public GameObject projectile_prefab;                  // 投射物预制体
        public Transform projectile_spawn;                    // 投射物生成点

        [Header("Action")]
        public float follow_distance = 3f;                    // 跟随距离

        public UnityAction onAttack;                          // 攻击时触发的事件
        public UnityAction onDamaged;                         // 受伤时触发的事件
        public UnityAction onDeath;                           // 死亡时触发的事件

        [HideInInspector]
        public bool was_spawned = false;                      // 如果为true，表示由玩家创建

        private Rigidbody rigid;                              // 刚体组件
        private Selectable selectable;                        // 可选择组件
        private Destructible destruct;                        // 可破坏组件(Z 其实就是攻击者character自己的Destruction)
        private Buildable buildable;                          // 可建造组件（可能为null）
        private UniqueID unique_id;                           // 唯一ID组件
        private Collider[] colliders;                         // 碰撞体数组
        private Vector3 bounds_extent;                        // 边界范围
        private Vector3 bounds_center_offset;                 // 中心偏移量
        private string current_scene;                         // 当前场景名

        private Vector3 moving;                               // 移动向量
        private Vector3 facing;                               // 面向方向

        private GameObject target = null;                     // 目标对象
        private Destructible attack_target = null;             // 攻击目标（可破坏对象）(Z 其实就是目标攻击对象)
        private PlayerCharacter attack_player = null;          // 攻击玩家角色

        [Header("移动目标点(Z：实时终点；注意和path_destination区别，最大区别在于跟踪会动物体时）")]
        private Vector3 move_target;                          // 移动目标点(Z：实时终点；注意和path_destination区别，最大区别在于跟踪会动物体时）
        [Header("避开障碍物的移动目标点（Z：“当前实际要走的点,路径上规划的一个个中间点）")]
        private Vector3 move_target_avoid;                    // 避开障碍物的移动目标点（Z：“当前实际要走的点,路径上规划的一个个中间点）
        private Vector3 move_average;                         // 移动平均值
        private Vector3 prev_pos;                             // 上一帧位置
        private float stuck_timer;                            // 卡住计时器

        private float attack_timer = 0f;                      // 攻击计时器
        private bool is_moving = false;                       // 是否在移动中
        private bool is_escaping = false;                     // 是否在逃跑中
        private bool is_attacking = false;                    // 是否在攻击中
        private bool is_stuck = false;                        // 是否卡住
        private bool attack_hit = false;                      // 攻击是否命中
        private bool direct_move = false;                     // 是否直接移动

        private bool is_grounded = false;                     // 是否着陆
        private bool is_fronted = false;                      // 是否前方有障碍
        private bool is_fronted_center = false;               // 是否中心前方有障碍
        private bool is_fronted_left = false;                 // 是否左侧前方有障碍
        private bool is_fronted_right = false;                // 是否右侧前方有障碍
        private float front_dist = 0f;                        // 前方障碍距离
        private Vector3 ground_normal = Vector3.up;           // 地面法线
        private float grounded_dist = 0f;                     // 着陆距离
        private float grounded_dist_average = 0f;             // 着陆距离的平均值
        private float avoid_angle = 0f;                       // 避让角度（Z：不开use_navmesh时，会通过判断有无障碍来设置avoid_angle值，最终影响是否避障）
        private float avoid_side = 1f;                        // 避让方向（左右）
        
        
        private Vector3[] nav_paths = new Vector3[0];         // 导航路径点数组 [Z：可以理解为调用CalculateNavmesh()方法，最终计算得出的路径点位置]
        [Header("导航路径目标点（Z：注意和move_target的区别，执行CalculateNavmesh()才会=move_target，且有0.5s间隔）")]
        private Vector3 path_destination;                     // 导航路径目标点（Z：注意和move_target的区别，执行CalculateNavmesh()才会=move_target，且有0.5s间隔）
        private int path_index = 0;                           // 导航路径索引
        private bool follow_path = false;                     // 是否跟随导航路径
        private bool calculating_path = false;                // 是否正在计算导航路径
        private float navmesh_timer = 0f;                     // 导航网格计时器
        private float ground_refesh_timer = 0f;               // 地面刷新计时器

        private static List<Character> character_list = new List<Character>();  // 角色列表

        protected override void Awake()
        {
            base.Awake();
            character_list.Add(this);
            rigid = GetComponent<Rigidbody>();
            selectable = GetComponent<Selectable>();
            destruct = GetComponent<Destructible>();
            buildable = GetComponent<Buildable>();
            unique_id = GetComponent<UniqueID>();
            colliders = GetComponentsInChildren<Collider>();
            avoid_side = Random.value < 0.5f ? 1f : -1f;
            facing = transform.forward;
            use_navmesh = move_enabled && use_navmesh;
            current_scene = SceneNav.GetCurrentScene();

            move_target = transform.position;
            move_target_avoid = transform.position;

            destruct.onDamaged += OnDamaged;
            destruct.onDeath += OnDeath;
            selectable.onDestroy += OnRemove;

            if(buildable != null)
                buildable.onBuild += OnBuild;

            foreach (Collider collide in colliders)
            {
                float size = collide.bounds.extents.magnitude;
                if (size > bounds_extent.magnitude)
                {
                    bounds_extent = collide.bounds.extents;
                    bounds_center_offset = collide.bounds.center - transform.position;
                }
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            character_list.Remove(this);
        }

        void Start()
        {
            if (!was_spawned && PlayerData.Get().IsObjectRemoved(GetUID())) {
                Destroy(gameObject);
                return;
            }

            //Set current position
            SceneObjectData sobj = PlayerData.Get().GetSceneObject(GetUID());
            if (sobj != null && sobj.scene == current_scene)
            {
                transform.position = sobj.pos;
                transform.rotation = sobj.rot;
            }

            DetectGrounded(); //Check grounded
        }

        //Z：处理计算路径+执行移动+计算进行朝向+检测是否卡住
        private void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())  // 如果游戏暂停
                return;

            if (!move_enabled || IsDead())  // 如果移动被禁用或者角色已死亡
                return;

            if (buildable && buildable.IsBuilding())  // 如果正在建造中
                return;

            // 根据导航网格和目标更新移动目标
            UpdateMoveTarget();

            // 查找移动方向
            Vector3 tmove = FindMoveDirection();

            // 应用移动
            moving = Vector3.Lerp(moving, tmove, 10f * Time.fixedDeltaTime);
            rigid.velocity = moving;

            // 查找面向方向
            if (IsMoving() && !IsDead())  // 如果正在移动且未死亡
            {
                Vector3 tface = new Vector3(moving.x, 0f, moving.z);
                facing = tface.normalized;
            }

            // 应用旋转（Z：先计算应该朝向，再缓动）
            Quaternion targ_rot = Quaternion.LookRotation(facing, Vector3.up);
            Quaternion nrot = Quaternion.RotateTowards(rigid.rotation, targ_rot, rotate_speed * Time.fixedDeltaTime);
            rigid.MoveRotation(nrot);

            // 地面距离平均值
            if (is_grounded)  // 如果着陆
                grounded_dist_average = Mathf.MoveTowards(grounded_dist_average, grounded_dist, 5f * Time.fixedDeltaTime);

            // 检查平均移动距离（用于检测角色是否卡住）
            float threshold = move_speed * Time.fixedDeltaTime * 0.25f;
            Vector3 last_frame_travel = transform.position - prev_pos;
            move_average = Vector3.MoveTowards(move_average, last_frame_travel, 2f * Time.fixedDeltaTime);
            prev_pos = transform.position;
            stuck_timer += (is_moving && move_average.magnitude < threshold) ? Time.fixedDeltaTime : -Time.fixedDeltaTime;
            stuck_timer = Mathf.Max(stuck_timer, 0f);
            is_stuck = is_moving && stuck_timer > 0.5f;
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())  // 如果游戏暂停
                return;

            if (IsDead())  // 如果角色已死亡
                return;

            if (buildable && buildable.IsBuilding())  // 如果正在建造中
                return;

            attack_timer += Time.deltaTime;  // 更新攻击计时器
            navmesh_timer += Time.deltaTime;  // 更新导航网格计时器
            ground_refesh_timer += Time.deltaTime;  // 更新地面刷新计时器

            // 检测障碍物和地面
            if (ground_refesh_timer > ground_refresh_rate)
            {
                ground_refesh_timer = Random.Range(-0.02f, 0.02f);  // 随机调整地面刷新计时器
                DetectGrounded();  // 检测着陆
                DetectFronted();  // 检测前方障碍物
            }

            // 保存位置信息
            PlayerData.Get().SetCharacterPosition(GetUID(), current_scene, transform.position, transform.rotation);

            // 停止移动
            if (is_moving && !HasTarget() && HasReachedMoveTarget(moving_threshold * 2f))
                Stop();

            // 攻击/跟随/逃跑行为更新
            UpdateFollowEscape();
            UpdateAttacking();

            // 检查避免障碍物
            CalculateAvoidObstacle();
        }

        #region 1、触发移动的 “指令方法”


        /// <summary>
        /// Z：触发移动的 “指令方法”：让角色移动到指定位置（比如玩家让村民去田里干活）。
        /// </summary>
        /// <param name="pos"></param>
        public void MoveTo(Vector3 pos)
        {
            move_target = pos;  // 移动目标位置
            move_target_avoid = pos;  // 避障后的移动目标位置
            target = null;  // 目标设为空
            attack_target = null;  // 攻击目标设为空
            attack_player = null;  // 攻击玩家设为空
            is_escaping = false;  // 不再逃跑
            is_moving = true;  // 开始移动
            stuck_timer = 0f;  // 重置卡住计时器
            direct_move = false;  // 非直接移动
            CalculateNavmesh();  // 计算寻路网格
        }


        /// <summary>
        /// Z：触发移动的 “指令方法”：让角色跟随另一个目标（比如村民跟着玩家走）
        /// </summary>
        /// <param name="target"></param>
        public void Follow(GameObject target)
        {
            if (target != null)
            {
                this.target = target.gameObject;  // 设置跟随目标
                this.attack_target = null;  // 攻击目标设为空
                this.attack_player = null;  // 攻击玩家设为空
                move_target = target.transform.position;  // 移动目标设置为目标位置
                is_escaping = false;  // 不再逃跑
                is_moving = true;  // 开始移动
                stuck_timer = 0f;  // 重置卡住计时器
                direct_move = false;  // 非直接移动
                CalculateNavmesh();  // 计算寻路网格


            }
        }

        /// <summary>
        /// Z：触发移动的 “指令方法”：让角色逃离目标（比如兔子躲开狐狸）[会计算 “与目标相反的方向” 作为move_target（比如狐狸在东边，兔子就往西跑），is_escaping设为true，角色朝着远离目标的方向移动。]
        /// </summary>
        /// <param name="target"> 一般情况下这个targrt是玩家 </param>
        public void Escape(GameObject target)
        {
            this.target = target;  // 设置逃跑目标
            this.attack_target = null;  // 攻击目标设为空
            this.attack_player = null;  // 攻击玩家设为空
            Vector3 dir = target.transform.position - transform.position;  // 目标方向向量（Z：这个方向向量是  自己指向target）
            move_target = transform.position - dir;  // 移动目标设置为逃离方向(Z：这里可以理解为计算得出逃向的点)
            is_escaping = true;  // 开始逃跑
            is_moving = true;  // 开始移动
            direct_move = false;  // 非直接移动
            stuck_timer = 0f;  // 重置卡住计时器
        }
        #endregion
        #region 2、计算移动的 “导航方法”


        /// <summary>
        /// Z：“实时更新要去的点”（类似导航软件刷新路线）？？？？？？？？？？？？？？？2025.08.19
        /// </summary>
        private void UpdateMoveTarget()
        {
            if (IsDead())  // 如果角色已死亡
                return;

            // 默认目标点设定
            move_target_avoid = move_target;

            //Z：导航网格模式（use_navmesh=true）：按提前算好的路径点（nav_paths）走，比如从 A→B→C 逐步到达目标
            // 使用导航网格并且正在跟随路径且非直接移动并且正在移动中且路径索引小于路径长度时
            if (use_navmesh && follow_path && !direct_move && is_moving && path_index < nav_paths.Length)
            {
                move_target_avoid = nav_paths[path_index];  // 使用路径中的目标点
                Vector3 dir_total = move_target_avoid - transform.position;
                dir_total.y = 0f;
                if (dir_total.magnitude < moving_threshold * 2f)
                    path_index++;  // 路径索引加一
            }
            //TODO:2025.08.19按计算路径移动的逻辑
            // 使用导航网格并且正在移动中并且非直接移动时（Z：用导航网格时，偏离方向就重新算路）
            if (use_navmesh && is_moving && !direct_move)
            {
                Vector3 path_dir = path_destination - transform.position;//Z：到最初终点的方向
                Vector3 nav_move_dir = move_target - transform.position;//Z：到现在最终目标方向（基本只有在追踪可移动目标时，才会有区别）
                float dot = Vector3.Dot(path_dir.normalized, nav_move_dir.normalized);//Z：到当前目标的方向
                if (dot < 0.7f)//Z：dot（点积）：判断两个方向是否一致（值越接近 1，方向越一致；小于 0.7 说明偏得很厉害），如果偏得很厉害就重新计算路径
                    CalculateNavmesh();  // 计算导航网格
            }

            //Z：普通模式：直接朝move_target走，遇到障碍就微调目标（move_target_avoid）；（不用导航网格时，遇到障碍就绕路）
            // 避免障碍物并且正在移动中并且非直接移动并且未到达移动目标点时（还没到达最终目标（距离大于 1 米））
            if (is_moving && !direct_move && avoid_obstacles && !use_navmesh && !HasReachedMoveTarget(1f))
                move_target_avoid = FindAvoidMoveTarget(move_target);  // 寻找避开障碍物的移动目标点（Z：生成一个 “绕开障碍的临时目标点”，赋值给 move_target_avoid。）
        }
        //Z：“这一步往哪迈”（计算每一步的方向和速度）。
        /// <summary>
        /// Z：最终得到的是速度向量
        /// </summary>
        /// <returns></returns>
        private Vector3 FindMoveDirection()
        {
            Vector3 tmove = Vector3.zero;  // 移动方向的向量初始化为零向量
            bool is_flying = fall_speed < 0.01f;  // 是否处于飞行状态（下落速度小于0.01）

            if (!IsDead())  // 如果角色未死亡
            {
                // 移动状态
                if (is_moving)
                {
                    Vector3 move_dir_total = move_target - transform.position;  // 目标总移动方向
                    Vector3 move_dir_next = move_target_avoid - transform.position;  // 避障后的移动方向
                    Vector3 move_dir = move_dir_next.normalized * Mathf.Min(move_dir_total.magnitude, 1f);  // 根据距离调整移动方向
                    tmove = move_dir.normalized * Mathf.Min(move_dir.magnitude, 1f) * move_speed;  // 根据速度调整移动向量（得到速度向量）
                }

                // 斜坡攀爬（Z：坡度超过一定限度就不爬了）
                float slope_angle = Vector3.Angle(ground_normal, Vector3.up);  // 地面法线与世界上方向的角度
                bool up_hill = Vector3.Dot(transform.forward, ground_normal) < -0.1f;  // 是否向上攀爬
                if (up_hill && !is_flying && slope_angle > slope_angle_max)
                    tmove = Vector3.zero;  // 斜坡角度太大，停止移动
            }

            // 下落状态（Z：模拟重力，让角色在空中时自然下落）
            if (!is_grounded && !is_flying)
                tmove += Vector3.down * fall_speed;  // 下落速度

            // 取消下落
            if (is_grounded && !is_flying && tmove.y < 0f)
                tmove.y = 0f;  // 着地后取消下落速度

            // 调整到斜坡（Z： 如果着地且没在飞，就把移动方向调整为沿斜坡表面（不会穿入或浮空））
            if (is_grounded && !is_flying)
                tmove = Vector3.ProjectOnPlane(tmove.normalized, ground_normal).normalized * tmove.magnitude;  // 根据地面法线调整移动向量方向

            return tmove;  // 返回计算后的移动向量（Z：移动速度向量）
        }
        #endregion


        private void UpdateFollowEscape()
        {
            if (target == null || is_attacking)  // 如果目标为空或者正在攻击中
                return;

            Vector3 targ_dir = (target.transform.position - transform.position);  // 目标方向向量
            targ_dir.y = 0f;  // 忽略垂直方向

            if (is_escaping)  // 如果正在逃跑
            {
                Vector3 targ_pos = transform.position - targ_dir.normalized * 4f;  // 逃跑目标位置
                move_target = targ_pos;  // 移动目标设置为逃跑位置
            }
            else if (is_moving)  // 如果正在移动
            {
                move_target = target.transform.position;  // 移动目标设置为目标位置

                // 停止跟随
                if ((attack_target != null || attack_player != null) && targ_dir.magnitude < GetAttackTargetHitRange() * 0.8f)
                {
                    move_target = transform.position;  // 停止移动
                    is_moving = false;  // 停止移动状态
                }

                // 停止跟随
                if (attack_target == null && attack_player == null && HasReachedMoveTarget(follow_distance))
                {
                    move_target = transform.position;  // 停止移动
                    is_moving = false;  // 停止移动状态
                }
            }
        }

        private void UpdateAttacking()
        {
            // 停止攻击
            if (is_attacking && !HasAttackTarget())
                Stop();

            if (!HasAttackTarget())
                return;

            Vector3 targ_dir = (target.transform.position - transform.position);  // 目标方向向量

            // 攻击之间的冷却时间
            if (!is_attacking)
            {
                if (!is_moving && targ_dir.magnitude > GetAttackTargetHitRange())
                {
                    Attack(attack_target);  // 开始移动
                }

                if (attack_timer > attack_cooldown)
                {
                    if (targ_dir.magnitude < GetAttackTargetHitRange())
                    {
                        StartAttackStrike();  // 开始攻击动作
                    }
                }
            }

            // 攻击进行中
            if (is_attacking)
            {
                move_target = transform.position;  // 移动目标设置为当前位置
                move_target_avoid = transform.position;  // 避障后的移动目标设置为当前位置
                FaceTorward(target.transform.position);  // 面向目标位置

                // 攻击动作之前
                if (!attack_hit && attack_timer > attack_windup)
                {
                    DoAttackStrike();  // 执行攻击动作
                }

                // 攻击动作结束后，重新开始跟随
                if (attack_timer > attack_duration)
                {
                    is_attacking = false;  // 攻击状态结束
                    attack_timer = 0f;  // 重置攻击计时器
                    is_moving = true;  // 开始移动

                    if (attack_target != null)
                        Attack(attack_target);  // 对攻击目标进行攻击
                    if (attack_player != null)
                        Attack(attack_player);  // 对攻击玩家进行攻击
                }
            }

            if (attack_target != null && attack_target.IsDead())
                Stop();  // 停止攻击

            if (attack_player != null && attack_player.IsDead())
                Stop();  // 停止攻击

            if (targ_dir.magnitude < GetAttackTargetHitRange() * 0.8f)
                StopMove();  // 停止移动
        }

        // 开始攻击准备
        private void StartAttackStrike()
        {
            is_attacking = true;  // 开始攻击状态
            is_moving = false;  // 停止移动状态
            attack_hit = false;  // 攻击未命中
            attack_timer = 0f;  // 重置攻击计时器

            if (onAttack != null)
                onAttack.Invoke();  // 触发攻击事件
        }

        // 执行攻击动作并造成伤害
        private void DoAttackStrike()
        {
            attack_hit = true;  // 攻击命中

            float range = (target.transform.position - transform.position).magnitude;  // 目标与自身位置的距离
            if (range < GetAttackTargetHitRange())
            {
                if (projectile_prefab != null)
                {
                    // 远程攻击
                    Vector3 pos = GetProjectileSpawnPos();  // 获取投射物生成位置
                    Vector3 tpos = target.transform.position + Vector3.up * 1.5f;  // 目标位置
                    Vector3 dir = tpos - pos;  // 方向向量
                    GameObject proj = Instantiate(projectile_prefab, pos, Quaternion.LookRotation(dir.normalized, Vector3.up));  // 实例化投射物
                    Projectile project = proj.GetComponent<Projectile>();  // 获取投射物组件
                    project.shooter = destruct;  // 设置发射者
                    project.dir = dir.normalized;  // 设置方向
                    project.damage = attack_damage;  // 设置伤害
                }
                else
                {
                    if (attack_target != null)
                        attack_target.TakeDamage(destruct, attack_damage);  // 对攻击目标造成伤害
                    if (attack_player != null)
                        attack_player.Combat.TakeDamage(attack_damage);  // 对攻击玩家造成伤害
                }
            }

            if (selectable.IsNearCamera(20f))
                TheAudio.Get().PlaySFX("character", attack_audio);  // 播放攻击音效
        }

        private Vector3 GetProjectileSpawnPos()
        {
            if (projectile_spawn != null)
                return projectile_spawn.position;  // 返回投射物生成位置
            return transform.position + Vector3.up * 2f;  // 默认返回当前位置向上2个单位
        }
        public void DirectMoveToward(Vector3 dir)
        {
            DirectMoveTo(transform.position + dir.normalized);  // 朝向方向进行直接移动
        }

        // 每帧调用，因此不使用寻路网格
        public void DirectMoveTo(Vector3 pos)
        {
            move_target = pos;  // 移动目标位置
            move_target_avoid = pos;  // 避障后的移动目标位置
            target = null;  // 目标设为空
            attack_target = null;  // 攻击目标设为空
            attack_player = null;  // 攻击玩家设为空
            is_escaping = false;  // 不再逃跑
            is_moving = true;  // 开始移动
            direct_move = true;  // 使用直接移动
            stuck_timer = 0f;  // 重置卡住计时器
        }
       
        public void Attack(Destructible target)
        {
            if (attack_enabled && target != null && target != destruct && target.CanBeAttacked())
            {
                this.target = target.gameObject;  // 设置攻击目标
                this.attack_target = target;  // 攻击目标设置为目标
                this.attack_player = null;  // 攻击玩家设为空
                move_target = target.transform.position;  // 移动目标设置为目标位置
                is_escaping = false;  // 不再逃跑
                is_moving = true;  // 开始移动
                stuck_timer = 0f;  // 重置卡住计时器
                direct_move = false;  // 非直接移动
                CalculateNavmesh();  // 计算寻路网格
            }
        }

        public void Attack(PlayerCharacter target)
        {
            if (attack_enabled && target != null && !target.IsDead())
            {
                this.target = target.gameObject;  // 设置攻击目标
                this.attack_target = null;  // 攻击目标设为空
                this.attack_player = target;  // 攻击玩家设置为目标
                move_target = target.transform.position;  // 移动目标设置为目标位置
                is_escaping = false;  // 不再逃跑
                is_moving = true;  // 开始移动
                stuck_timer = 0f;  // 重置卡住计时器
                direct_move = false;  // 非直接移动
                CalculateNavmesh();  // 计算寻路网格
            }
        }

        public void FaceTorward(Vector3 pos)
        {
            Vector3 face = (pos - transform.position);  // 面向目标位置的向量
            face.y = 0f;  // 忽略垂直方向
            if (face.magnitude > 0.01f)  // 如果向量长度大于0.01
            {
                facing = face.normalized;  // 设置面向方向为单位向量
            }
        }

        public void Stop()
        {
            target = null;  // 目标设为空
            attack_target = null;  // 攻击目标设为空
            attack_player = null;  // 攻击玩家设为空
            rigid.velocity = Vector3.zero;  // 刚体速度设为零向量
            moving = Vector3.zero;  // 移动向量设为零向量
            move_target = transform.position;  // 移动目标设为当前位置
            is_moving = false;  // 停止移动状态
            is_attacking = false;  // 停止攻击状态
            stuck_timer = 0f;  // 重置卡住计时器
            direct_move = false;  // 非直接移动
        }

        public void StopMove()
        {
            move_target = transform.position;  // 移动目标设为当前位置
            rigid.velocity = Vector3.zero;  // 刚体速度设为零向量
            moving = Vector3.zero;  // 移动向量设为零向量
            is_moving = false;  // 停止移动状态
        }

        public void Kill()
        {
            if (destruct != null)
                destruct.Kill();  // 执行销毁逻辑
            else
                selectable.Destroy();  // 执行销毁逻辑
        }

        public void Reset()
        {
            StopMove();  // 停止移动
            rigid.isKinematic = false;  // 刚体不是静态的
            destruct.Reset();  // 重置破坏物体
        }
        
        /// <summary>
        /// Z：每帧更新，检查是否有障碍（并设置avloid_angle值，最终影响是否避障）
        /// </summary>
        private void CalculateAvoidObstacle()
        {
            // 当避开障碍并且非直接移动时，增加偏移以逃离路径
            if (avoid_obstacles & !direct_move)
            {
                if (is_fronted_left && !is_fronted_right)
                    avoid_side = 1f;
                if (is_fronted_right && !is_fronted_left)
                    avoid_side = -1f;

                // 当四面都被挡住时，使用目标影响选择逃离方向
                if (is_fronted_center && is_fronted_left && is_fronted_right && target)
                {
                    Vector3 dir = target.transform.position - transform.position;
                    dir = dir * (is_escaping ? -1f : 1f);
                    float dot = Vector3.Dot(dir.normalized, transform.right);
                    if (Mathf.Abs(dot) > 0.5f)
                        avoid_side = Mathf.Sign(dot);
                }

                float angle = avoid_side * 90f;
                float far_val = is_fronted ? 1f - (front_dist / destruct.hit_range) : Mathf.Abs(angle) / 90f; // 1f = 靠近，0f = 远离
                float angle_speed = far_val * 150f + 50f;
                avoid_angle = Mathf.MoveTowards(avoid_angle, is_fronted ? angle : 0f, angle_speed * Time.deltaTime);
            }
        }

        //Z：当角色需要移动到某个目标点时，它会请求游戏引擎计算一条 “能走通的路径”，确保角色不会穿过墙壁、地形等障碍物*********************************************
        //Z：并且只有在执行这个方法时才执行path_destination = move_target;
        private void CalculateNavmesh()
        {
            //Z：满足这 3 个条件才会计算路径： 开启了 “导航网格模式”、当前没有正在计算的路径、距离上次计算路径已经过了 0.5 秒以上
            if (use_navmesh && !calculating_path && navmesh_timer > 0.5f)//Z：这个0.5S很有意思，最大区别了
            {
                calculating_path = true;
                path_index = 0;//Z：重置路径点索引
                NavMeshTool.CalculatePath(transform.position, move_target, 1 << 0, FinishCalculateNavmesh);//Z：计算路径
                path_destination = move_target;
                navmesh_timer = 0f;
            }
        }

        //Z：路径计算完成后要调用的方法，类似导航算完路后通知你 “路线已规划好”；最终得到的是nav_paths（Vector3数组）路径位置点的数组
        private void FinishCalculateNavmesh(NavMeshToolPath path)
        {
            calculating_path = false;
            follow_path = path.success;
            nav_paths = path.path;
            path_index = 0;
            navmesh_timer = 0f;
        }

        // 检查是否接触地面
        private void DetectGrounded()
        {
            float radius = (bounds_extent.x + bounds_extent.z) * 0.5f;
            float center_offset = bounds_extent.y;
            float hradius = center_offset + ground_detect_dist;

            Vector3 center = transform.position + bounds_center_offset;
            center.y = transform.position.y + center_offset;

            float gdist;
            Vector3 gnormal;
            is_grounded = PhysicsTool.DetectGround(transform, center, hradius, radius, ground_layer, out gdist, out gnormal);
            ground_normal = gnormal;
            grounded_dist = gdist;

            float slope_angle = Vector3.Angle(ground_normal, Vector3.up);
            is_grounded = is_grounded && slope_angle <= slope_angle_max;
        }

        // 检测角色前方是否有障碍物
        private void DetectFronted()
        {
            float radius = destruct.hit_range * 2f;

            Vector3 center = destruct.GetCenter();
            Vector3 dir = move_target_avoid - transform.position;
            Vector3 dirl = Quaternion.AngleAxis(-45f, Vector3.up) * dir.normalized;
            Vector3 dirr = Quaternion.AngleAxis(45f, Vector3.up) * dir.normalized;

            RaycastHit h, hl, hr;
            bool fc = PhysicsTool.RaycastCollision(center, dir.normalized * radius, out h);
            bool fl = PhysicsTool.RaycastCollision(center, dirl.normalized * radius, out hl);
            bool fr = PhysicsTool.RaycastCollision(center, dirr.normalized * radius, out hr);
            is_fronted_center = fc && (target == null || h.collider.gameObject != target);
            is_fronted_left = fl && (target == null || hl.collider.gameObject != target);
            is_fronted_right = fr && (target == null || hr.collider.gameObject != target);

            int front_count = (fc ? 1 : 0) + (fl ? 1 : 0) + (fr ? 1 : 0);
            front_dist = (fc ? h.distance : 0f) + (fl ? hl.distance : 0f) + (fr ? hr.distance : 0f);
            if (front_count > 0) front_dist = front_dist / (float)front_count;

            is_fronted = is_fronted_center || is_fronted_left || is_fronted_right;
        }

        private void OnBuild()
        {
            if (data != null)
            {
                TrainedCharacterData cdata = PlayerData.Get().AddCharacter(data.id, current_scene, transform.position, transform.rotation);
                unique_id.unique_id = cdata.uid;
            }
        }

        private void OnDamaged()
        {
            if (onDamaged != null)
                onDamaged.Invoke();
        }

        private void OnDeath()
        {
            rigid.velocity = Vector3.zero;
            moving = Vector3.zero;
            rigid.isKinematic = true;
            target = null;
            attack_target = null;
            attack_player = null;
            move_target = transform.position;
            is_moving = false;

            foreach (Collider coll in colliders)
                coll.enabled = false;

            if (onDeath != null)
                onDeath.Invoke();

            if (data != null)
            {
                foreach (PlayerCharacter character in PlayerCharacter.GetAll())
                    character.SaveData.AddKillCount(data.id); // 增加击杀计数
            }
        }

        private void OnRemove()
        {
            PlayerData.Get().RemoveCharacter(GetUID());
            if (!was_spawned)
                PlayerData.Get().RemoveObject(GetUID());
        }

        // 寻找新的移动目标，尝试避开障碍物（Z：生成一个 “绕开障碍的临时目标点”，赋值给 move_target_avoid。）
        //Z：发现为什么前方无障碍时方法仍可能执行？前方无障碍时，avoid_angle（旋转角度）会被其他逻辑（比如障碍物检测）设为 0 度，这样其实就是没有绕路
        private Vector3 FindAvoidMoveTarget(Vector3 target)
        {
            Vector3 targ_dir = (target - transform.position);
            targ_dir = Quaternion.AngleAxis(avoid_angle, Vector3.up) * targ_dir; // 如果前方有障碍物则旋转（Z：距离和原方向相同）
            return transform.position + targ_dir;
        }

        // 是否已到达目标位置？
        public bool HasReachedMoveTarget()
        {
            return HasReachedMoveTarget(moving_threshold * 2f); // 将阈值加倍以确保在到达目标前不停止移动
        }

        public bool HasReachedMoveTarget(float distance)
        {
            Vector3 diff = move_target - transform.position;
            return (diff.magnitude < distance); // 判断是否到达移动目标的距离
        }

        public bool HasTarget()
        {
            return target != null; // 判断是否有目标
        }

        public bool HasAttackTarget()
        {
            return attack_enabled && target != null && GetAttackTarget() != null; // 判断是否有攻击目标
        }

        public GameObject GetAttackTarget()
        {
            GameObject target = null;
            if (attack_player != null)
                target = attack_player.gameObject; // 获取玩家攻击目标的游戏对象
            else if (attack_target != null)
                target = attack_target.gameObject; // 获取攻击目标的游戏对象
            return target;
        }

        public float GetAttackTargetHitRange()
        {
            if (attack_target != null)
                return attack_range + attack_target.hit_range; // 获取攻击目标的打击范围
            return attack_range; // 返回默认攻击范围
        }

        public bool IsAttacking()
        {
            if (HasAttackTarget()) {
                Vector3 targ_dir = (target.transform.position - transform.position);
                return targ_dir.magnitude < GetAttackTargetHitRange(); // 判断是否正在攻击
            }
            return false;
        }

        public bool IsDead()
        {
            return destruct.IsDead(); // 判断角色是否已死亡
        }

        // 实际是否在移动
        public bool IsMoving()
        {
            Vector3 moveXZ = new Vector3(moving.x, 0f, moving.z);
            return is_moving && moveXZ.magnitude > moving_threshold; // 判断是否在移动
        }

        // 是否尝试移动
        public bool IsTryMoving()
        {
            return is_moving; // 判断是否正在尝试移动
        }

        public Vector3 GetMove()
        {
            return moving; // 获取移动向量
        }

        public Vector3 GetFacing()
        {
            return facing; // 获取面向方向向量
        }

        public bool IsGrounded()
        {
            return is_grounded; // 判断是否着陆
        }

        public bool IsFronted()
        {
            return is_fronted; // 判断是否被挡住
        }

        public bool IsFrontedLeft()
        {
            return is_fronted_left; // 判断是否左侧被挡住
        }

        public bool IsFrontedRight()
        {
            return is_fronted_right; // 判断是否右侧被挡住
        }

        public bool IsStuck()
        {
            return is_stuck; // 判断是否卡住
        }

        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id); // 判断是否有唯一标识符
        }

        public string GetUID()
        {
            return unique_id.unique_id; // 获取唯一标识符
        }

        public string GetSubUID(string tag)
        {
            return unique_id.GetSubUID(tag); // 获取子标识符
        }

        public bool HasGroup(GroupData group)
        {
            if (data != null)
                return data.HasGroup(group) || selectable.HasGroup(group);
            return selectable.HasGroup(group); // 判断是否拥有特定组
        }

        public Selectable GetSelectable()
        {
            return selectable; // 获取可选对象
        }

        public Destructible GetDestructible()
        {
            return destruct; // 获取可摧毁对象
        }

        public Buildable GetBuildable()
        {
            return buildable; // 获取可建造对象，可能为空
        }

        public TrainedCharacterData SaveData
        {
            get { return PlayerData.Get().GetCharacter(GetUID()); } // 获取保存的角色数据
        }

        public static new Character GetNearest(Vector3 pos, float range = 999f)
        {
            Character nearest = null;
            float min_dist = range;
            foreach (Character unit in character_list)
            {
                float dist = (unit.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = unit;
                }
            }
            return nearest; // 获取最近的角色对象
        }

        public static int CountInRange(Vector3 pos, float range)
        {
            int count = 0;
            foreach (Character character in GetAll())
            {
                float dist = (character.transform.position - pos).magnitude;
                if (dist < range && !character.IsDead())
                    count++;
            }
            return count; // 计算指定范围内的角色数量
        }

        public static int CountInRange(CharacterData data, Vector3 pos, float range)
        {
            int count = 0;
            foreach (Character character in GetAll())
            {
                if (character.data == data && !character.IsDead()) {
                    float dist = (character.transform.position - pos).magnitude;
                    if (dist < range)
                        count++;
                }
            }
            return count; // 计算指定数据在范围内的角色数量
        }

        public static Character GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Character unit in character_list)
                {
                    if (unit.GetUID() == uid)
                        return unit;
                }
            }
            return null; // 根据唯一标识符获取角色对象
        }

        public static List<Character> GetAllOf(CharacterData data)
        {
            List<Character> valid_list = new List<Character>();
            foreach (Character character in character_list)
            {
                if (character.data == data)
                    valid_list.Add(character);
            }
            return valid_list; // 获取指定数据的所有角色对象
        }

        public static new List<Character> GetAll()
        {
            return character_list; // 获取所有角色对象列表
        }

        // 生成一个已存在于保存文件中的角色（例如在加载后）
        public static Character Spawn(string uid, Transform parent = null)
        {
            // 获取保存文件中的训练角色数据
            TrainedCharacterData tcdata = PlayerData.Get().GetCharacter(uid);
            
            // 如果数据存在且处于当前场景中
            if (tcdata != null && tcdata.scene == SceneNav.GetCurrentScene())
            {
                // 获取角色数据
                CharacterData cdata = CharacterData.Get(tcdata.character_id);
                
                // 如果角色数据有效
                if (cdata != null)
                {
                    // 实例化角色预制体
                    GameObject cobj = Instantiate(cdata.character_prefab, tcdata.pos, tcdata.rot);
                    cobj.transform.parent = parent; // 设置父级对象
                    
                    // 获取角色组件
                    Character character = cobj.GetComponent<Character>();
                    character.data = cdata; // 设置角色数据
                    character.was_spawned = true; // 标记为已生成
                    character.unique_id.unique_id = uid; // 设置唯一标识符
                    return character; // 返回生成的角色对象
                }
            }
            return null; // 返回空，生成失败
        }

        // 创建一个全新的角色，该角色将会被添加到保存文件中，但只能在玩家建造后才能生效
        public static Character CreateBuildMode(CharacterData data, Vector3 pos)
        {
            // 实例化角色预制体
            GameObject build = Instantiate(data.character_prefab, pos, data.character_prefab.transform.rotation);
            
            // 获取角色组件
            Character character = build.GetComponent<Character>();
            character.data = data; // 设置角色数据
            character.was_spawned = true; // 标记为已生成
            return character; // 返回生成的角色对象
        }

        // 创建一个全新的角色，该角色将会被添加到保存文件中
        public static Character Create(CharacterData data, Vector3 pos)
        {
            Quaternion rot = Quaternion.Euler(0f, 180f, 0f);
            Character unit = Create(data, pos, rot); // 调用重载方法创建角色
            return unit;
        }

        // 创建一个全新的角色，该角色将会被添加到保存文件中
        public static Character Create(CharacterData data, Vector3 pos, Quaternion rot)
        {
            // 向玩家数据中添加角色数据并获取结果
            TrainedCharacterData ditem = PlayerData.Get().AddCharacter(data.id, SceneNav.GetCurrentScene(), pos, rot);
            
            // 实例化角色预制体
            GameObject build = Instantiate(data.character_prefab, pos, rot);
            
            // 获取角色组件
            Character unit = build.GetComponent<Character>();
            unit.data = data; // 设置角色数据
            unit.was_spawned = true; // 标记为已生成
            unit.unique_id.unique_id = ditem.uid; // 设置唯一标识符
            return unit; // 返回生成的角色对象
        }

    }

}