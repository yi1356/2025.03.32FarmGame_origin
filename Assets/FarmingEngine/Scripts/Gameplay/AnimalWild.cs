using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    // 动物的状态
    public enum AnimalState
    {
        Wander = 0,     // 游荡
        Alerted = 2,    // 警戒
        Escape = 4,     // 逃跑
        Attack = 6,     // 攻击
        MoveTo = 10,    // 移动到指定位置
        Dead = 20,      // 死亡
    }

    // 动物的行为（Z：动物性格，遇到人后，第一反应是什么）
    public enum AnimalBehavior
    {
        None = 0,           // 无行为，由其他脚本定义
        Escape = 5,         // 看到就逃跑
        PassiveEscape = 10, // 被攻击时逃跑
        PassiveDefense = 15,// 被攻击时反击
        Aggressive = 20,    // 看到就攻击，一段时间后返回(Z：看到玩家就攻击，但追一会儿会回去)
        VeryAggressive = 25,// 看到就攻击，一直追击
    }

    // 游荡行为
    public enum WanderBehavior
    {
        None = 0,       // 不游荡
        WanderNear = 10,// 在附近游荡
        WanderFar = 20, // 超出初始位置游荡
    }

    /// <summary>
    /// 动物的行为脚本，用于游荡、逃跑或追击玩家(Z：野生动物行为的脚本)
    /// </summary>

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Destructible))]
    [RequireComponent(typeof(Character))]
    public class AnimalWild : MonoBehaviour
    {
        [Header("行为设置")]//Z：需要在unity编辑器中配置不同动物的行为等
        public WanderBehavior wander = WanderBehavior.WanderNear;    // 游荡行为
        public AnimalBehavior behavior = AnimalBehavior.PassiveEscape;   // 动物行为

        [Header("移动设置")]
        public float wander_speed = 2f;     // 游荡速度
        public float run_speed = 5f;        // 奔跑速度（Z：逃跑速度）
        public float wander_range = 10f;    // 游荡范围
        public float wander_interval = 10f; // 游荡间隔

        [Header("视觉设置")]
        public float detect_range = 5f;     // 检测范围
        public float detect_angle = 360f;   // 检测角度
        public float detect_360_range = 1f; // 360°检测范围（Z：也就是说在这个范围内就不用管角度了，只要在这个范围内就会被发现）
        public float reaction_time = 0.5f;  // 反应时间，发现威胁的速度

        [Header("动作设置")]
        public float action_duration = 10f; // 攻击/逃跑持续时间

        public UnityAction onAttack;    // 攻击事件
        public UnityAction onDamaged;   // 受伤事件
        public UnityAction onDeath;     // 死亡事件

        private AnimalState state;      // 当前状态
        private Character character;    // 角色组件（Z：为了控制动物移动、攻击等基础动作）
        private Selectable selectable;  // 选择组件
        private Destructible destruct;  // 可摧毁组件
        private Animator animator;      // 动画控制器

        private Vector3 start_pos;      // 初始位置

        private PlayerCharacter player_target = null;   // 玩家目标
        private Destructible attack_target = null;       // 攻击目标
        private Vector3 wander_target;  // 游荡目标

        private bool is_running = false; // 是否奔跑中
        [Header("Z：当前状态已经持续时间")]
        private float state_timer = 0f;  // 状态计时器
        private bool is_active = false;  // 是否激活（Z：当动物离玩家太远（超出active_range）时，会 “休眠）

        private float lure_interest = 8f;    // 诱饵兴趣
        private bool force_action = false;   // 强制动作
        private float update_timer = 0f;     // 更新计时器

        void Awake()
        {
            character = GetComponent<Character>();
            destruct = GetComponent<Destructible>();
            selectable = GetComponent<Selectable>();
            animator = GetComponentInChildren<Animator>();
            start_pos = transform.position;
            state_timer = 99f; // 立即寻找游荡目标
            update_timer = Random.Range(-1f, 1f);

            if (wander != WanderBehavior.None)
                transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        }

        void Start()
        {
            character.onAttack += OnAttack;
            destruct.onDamaged += OnDamaged;
            destruct.onDamagedBy += OnDamagedBy;
            destruct.onDamagedByPlayer += OnDamagedPlayer;
            destruct.onDeath += OnDeath;
        }

        void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
                return;

            // 优化，如果距离太远则不运行（Z：这里会进行性能优化）
            float dist = (TheCamera.Get().GetTargetPos() - transform.position).magnitude;
            float active_range = Mathf.Max(detect_range * 2f, selectable.active_range * 0.8f);
            is_active = (state != AnimalState.Wander && state != AnimalState.Dead) || character.IsMoving() || dist < active_range;
        }

        private void Update()
        {
            // 动画
            bool paused = TheGame.Get().IsPaused();
            if (animator != null)
                animator.enabled = !paused;

            if (TheGame.Get().IsPaused())
                return;

            if (state == AnimalState.Dead || behavior == AnimalBehavior.None || !is_active)
                return;

            state_timer += Time.deltaTime;

            if (state != AnimalState.MoveTo)
                is_running = (state == AnimalState.Escape || state == AnimalState.Attack);

            character.move_speed = is_running ? run_speed : wander_speed;

            // 状态处理
            if (state == AnimalState.Wander)
            {
                if (state_timer > wander_interval && wander != WanderBehavior.None)
                {
                    state_timer = Random.Range(-1f, 1f);
                    FindWanderTarget();
                    character.MoveTo(wander_target);
                }

                // 角色被卡住
                if (character.IsStuck())
                    character.Stop();
            }
            //Z：当动物进入 Alerted（警戒）状态后，Update 方法会每帧检查以下内容：
            if (state == AnimalState.Alerted)
            {
                GameObject target = GetTarget();
                if (target == null)
                {
                    character.Stop();
                    ChangeState(AnimalState.Wander);
                    return;
                }

                character.FaceTorward(target.transform.position);

                if (state_timer > reaction_time)
                {
                    ReactToThreat();
                }
            }

            if (state == AnimalState.Escape)
            {
                GameObject target = GetTarget();
                if (target == null)
                {
                    StopAction();
                    return;
                }

                if (!force_action && state_timer > action_duration)
                {
                    Vector3 targ_dir = (target.transform.position - transform.position);
                    targ_dir.y = 0f;

                    if (targ_dir.magnitude > detect_range)
                    {
                        StopAction();
                    }
                }

            }

            if (state == AnimalState.Attack)
            {
                GameObject target = GetTarget();
                if (target == null)
                {
                    StopAction();
                    return;
                }

                // 非常攻击性不会停止追踪
                if (!force_action && behavior != AnimalBehavior.VeryAggressive && state_timer > action_duration)
                {
                    Vector3 targ_dir = target.transform.position - transform.position;
                    Vector3 start_dir = start_pos - transform.position;

                    float follow_range = detect_range * 2f;
                    bool cant_see = targ_dir.magnitude > follow_range;
                    bool too_far = wander == WanderBehavior.WanderNear && start_dir.magnitude > Mathf.Max(wander_range, follow_range);
                    if (cant_see || too_far)
                    {
                        StopAction();
                        MoveToTarget(wander_target, false);
                    }
                }
            }

            if (state == AnimalState.MoveTo)
            {
                if (character.HasReachedMoveTarget())
                    StopAction();
            }

            update_timer += Time.deltaTime;
            if (update_timer > 0.5f)
            {
                update_timer = Random.Range(-0.1f, 0.1f);
                SlowUpdate(); // 优化
            }

            // 动画
            if (animator != null && animator.enabled)
            {
                animator.SetBool("Move", IsMoving() && IsActive());
                animator.SetBool("Run", IsRunning() && IsActive());
            }
        }

        //Z：优化，每0.5s 执行一次（闲逛时，定期检查周围有没有危险、攻击时，定期检查自己是不是卡住了，要不要调整攻击目标）
        private void SlowUpdate(){
            if (state == AnimalState.Wander)
            {
                //These behavior trigger a reaction on sight, while the "Defense" behavior only trigger a reaction when attacked
                //Z：检查周围有没有危险
                //Z：只有“主动攻击型”“见人就跑型”动物才需要定期检查（防御型动物只在被打时反应）
                if (behavior == AnimalBehavior.Aggressive || behavior == AnimalBehavior.VeryAggressive || behavior == AnimalBehavior.Escape)
                {
                    DetectThreat(detect_range);

                    if (GetTarget() != null)//Z：如果扫到了威胁（比如玩家进入视野）
                    {
                        character.Stop();
                        ChangeState(AnimalState.Alerted); // Z：切换到“警戒状态”（准备逃跑或攻击）；之后Update中会检测到进入警戒状态，执行后面各自的逻辑*********
                    }
                }
            }

            //Z：当动物处于 “攻击状态（Attack）”，这部分代码负责：解决攻击时的 “卡壳问题”，让动物不会因为卡住而一直发呆。
            if (state == AnimalState.Attack)
            {
            //Z：三个条件同时满足：1.动物被卡住了；2.没在攻击动作中；3.攻击已经持续1秒以上
                if (character.IsStuck() && !character.IsAttacking() && state_timer > 1f)
                {
                    DetectThreat(detect_range);// 重新扫描目标位置
                    ReactToThreat();// 根据最新位置调整行为（比如重新冲向目标）
                }
            }
        }

        // 检测玩家是否在视野范围内（Z：核心检测逻辑）
        private void DetectThreat(float range)
        {
            Vector3 pos = transform.position;

            // 检测玩家
            float min_dist = range;
            foreach (PlayerCharacter player in PlayerCharacter.GetAll())
            {
                Vector3 char_dir = (player.transform.position - pos);
                float dist = char_dir.magnitude;
                if (dist < min_dist && !player.IsDead())
                {
                    float dangle = detect_angle / 2f; // /2 每侧角度
                    float angle = Vector3.Angle(transform.forward, char_dir.normalized);
                    if (angle < dangle || char_dir.magnitude < detect_360_range)
                    {
                        player_target = player;
                        attack_target = null;
                        min_dist = dist;
                    }
                }
            }

            // 检测其他角色/可摧毁物体（Z：其实这里处理的是环境互动逻辑，比如狼遇到兔子是不是要攻击........）
            foreach (Selectable selectable in Selectable.GetAllActive())
            {
                if (selectable.gameObject != gameObject)
                {
                    Vector3 dir = (selectable.transform.position - pos);
                    if (dir.magnitude < min_dist)
                    {
                        float dangle = detect_angle / 2f; // /2 每侧角度
                        float angle = Vector3.Angle(transform.forward, dir.normalized);
                        if (angle < dangle || dir.magnitude < detect_360_range)
                        {
                            // 找到可摧毁物体进行攻击
                            if (HasAttackBehavior())
                            {
                                Destructible destruct = selectable.Destructible;
                                if (destruct && !destruct.IsDead() && (destruct.target_team == AttackTeam.Ally || destruct.target_team == AttackTeam.Enemy)) // 默认攻击（非中立）
                                {
                                    if (destruct.target_team == AttackTeam.Ally || destruct.target_group != this.destruct.target_group) // 不在同一个队伍
                                    {
                                        attack_target = destruct;
                                        player_target = null;
                                        min_dist = dir.magnitude;
                                    }
                                }
                            }

                            // 找到角色进行逃跑
                            if (HasEscapeBehavior())
                            {
                                Character character = selectable.Character;
                                if (character && character.attack_enabled) // 只有角色能攻击时才害怕
                                {
                                    if (character.GetDestructible().target_group != this.destruct.target_group) // 不在同一个队伍不害怕
                                    {
                                        attack_target = destruct;
                                        player_target = null;
                                        min_dist = dir.magnitude;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        // 如果被动物看见玩家则进行反应
        private void ReactToThreat()
        {
            GameObject target = GetTarget();

            if (target == null || IsDead())
                return;

            if (HasEscapeBehavior())
            {
                ChangeState(AnimalState.Escape);
                character.Escape(target);
            }
            else if (HasAttackBehavior())
            {
                ChangeState(AnimalState.Attack);
                if (player_target)
                    character.Attack(player_target);
                else if (attack_target)
                    character.Attack(attack_target);
            }
        }

        //Z：获取到目标（有可能为空）
        private GameObject GetTarget()
        {
            if (player_target != null && !player_target.IsDead())
                return player_target.gameObject;
            else if (attack_target != null && !attack_target.IsDead())
                return attack_target.gameObject;
            return null;
        }

        /// <summary>
        /// Z：给动物找一个 “闲逛的目标位置”
        /// </summary>
        private void FindWanderTarget()
        {
            //Z：正常闲逛：随机找个地方走（以目前位置/出生点为起点，随机一个距离+方向得到目标位置点）
            float range = Random.Range(0f, wander_range);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 spos = wander == WanderBehavior.WanderFar ? transform.position : start_pos;
            Vector3 pos = spos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * range;
            wander_target = pos;

            //Z：被诱饵吸引：向诱饵方向偏一点
            Lure lure = Lure.GetNearestInRange(transform.position);
            if (lure != null)
            {
                Vector3 dir = lure.transform.position - transform.position;
                dir.y = 0f;

                Vector3 center = transform.position + dir.normalized * dir.magnitude * 0.5f;
                if (lure_interest < 4f)
                    center = lure.transform.position;

                float range2 = Mathf.Clamp(lure_interest, 1f, wander_range);
                Vector3 pos2 = center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * range2;
                wander_target = pos2;

                lure_interest = lure_interest * 0.5f;
                if (lure_interest <= 0.2f)
                    lure_interest = 8f;
            }
        }

        public void AttackTarget(PlayerCharacter target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Attack);
                this.player_target = target;
                this.attack_target = null;
                force_action = true;
                character.Attack(target);
            }
        }

        public void EscapeTarget(PlayerCharacter target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Escape);
                this.player_target = target;
                this.attack_target = null;
                force_action = true;
                character.Escape(target.gameObject);
            }
        }

        public void AttackTarget(Destructible target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Attack);
                this.attack_target = target;
                this.player_target = null;
                force_action = true;
                character.Attack(target);
            }
        }

        public void EscapeTarget(Destructible target)
        {
            if (target != null)
            {
                ChangeState(AnimalState.Escape);
                this.attack_target = target;
                this.player_target = null;
                force_action = true;
                character.Escape(target.gameObject);
            }
        }

        public void MoveToTarget(Vector3 pos, bool run)
        {
            is_running = run;
            force_action = true;
            ChangeState(AnimalState.MoveTo);
            character.MoveTo(pos);
        }

        public void StopAction()
        {
            character.Stop();
            is_running = false;
            force_action = false;
            player_target = null;
            attack_target = null;
            ChangeState(AnimalState.Wander);
        }

        public void ChangeState(AnimalState state)
        {
            this.state = state;
            state_timer = 0f;
            lure_interest = 8f;
        }

        public void Reset()
        {
            StopAction();
            character.Reset();
            animator.Rebind();
        }

        private void OnDamaged()
        {
            if (IsDead())
                return;

            if (onDamaged != null)
                onDamaged.Invoke();

            if (animator != null)
                animator.SetTrigger("Damaged");
        }

        private void OnDamagedPlayer(PlayerCharacter player)
        {
            if (IsDead() || state_timer < 2f)
                return;

            player_target = player;
            attack_target = null;
            ReactToThreat();
        }

        private void OnDamagedBy(Destructible attacker)
        {
            if (IsDead() || state_timer < 2f)
                return;

            player_target = null;
            attack_target = attacker;
            ReactToThreat();
        }

        private void OnDeath()
        {
            state = AnimalState.Dead;

            if (onDeath != null)
                onDeath.Invoke();

            if (animator != null)
                animator.SetTrigger("Death");
        }

        void OnAttack()
        {
            if (animator != null)
                animator.SetTrigger("Attack");
        }

        public bool HasAttackBehavior()
        {
            return behavior == AnimalBehavior.Aggressive || behavior == AnimalBehavior.VeryAggressive || behavior == AnimalBehavior.PassiveDefense;
        }

        public bool HasEscapeBehavior()
        {
            return behavior == AnimalBehavior.Escape || behavior == AnimalBehavior.PassiveEscape;
        }

        public bool IsDead()
        {
            return character.IsDead();
        }

        public bool IsActive()
        {
            return is_active;
        }

        public bool IsMoving()
        {
            return character.IsMoving();
        }

        public bool IsRunning()
        {
            return character.IsMoving() && is_running;
        }

        public string GetUID()
        {
            return character.GetUID();
        }
    }
}