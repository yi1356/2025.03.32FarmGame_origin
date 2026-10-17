using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 可以进食和产生产品的动物
    /// </summary>

    public enum LivestockState
    {
        Wander = 0,     // 游荡状态
        MoveTo = 10,    // 移动到目标状态
        FindFood = 20,  // 寻找食物状态
        Eat = 25,       // 进食状态
        Dead = 50,      // 死亡状态
    }

    public enum LivestockProduceType
    {
        DropFloor = 0,      // 掉落到地面上
        CollectAction = 10  // 需要手动收集
    }

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Destructible))]
    [RequireComponent(typeof(Character))]
    public class AnimalLivestock : MonoBehaviour
    {
        [Header("Move/Wander")]
        public WanderBehavior wander = WanderBehavior.WanderNear;  // 游荡行为
        public float wander_range = 10f;  // 游荡范围
        public float wander_interval = 10f;  // 游荡间隔
        public float detect_range = 5f;  // 检测范围

        [Header("Time")]
        public TimeType time_type = TimeType.GameHours;  // 用于产出/成长时间的时间类型（天或小时）

        [Header("Eat")]
        public bool eat = true;  // 是否可以进食
        public GroupData eat_food_group;  // 可以食用的食物组
        public float eat_range = 1f;  // 进食范围
        public float eat_interval_time = 12f;  // 间隔多长时间可以再次进食（游戏小时或游戏天）

        [Header("Produce")]
        public ItemData item_produce;  // 产出的物品数据
        public LivestockProduceType item_collect_type;  // 物品如何产出（掉落或手动收集）
        public int item_eat_count = 1;  // 需要进食多少次才能产出物品
        public float item_produce_time = 24f;  // 产出物品所需时间（游戏小时或游戏天）
        public int item_max = 1;  // 地面上同时存在的最大产出物品数量
        public float item_max_range = 10f;  // 计算最大产出物品范围

        [Header("Growth")]
        public CharacterData grow_to;  // 成长到的角色数据
        public int grow_eat_count = 4;  // 需要进食多少次才能成长
        public float grow_time = 48f;  // 成长所需时间（游戏小时或游戏天）

        public UnityAction onAttack;  // 攻击时的事件
        public UnityAction onDamaged;  // 受伤时的事件
        public UnityAction onDeath;  // 死亡时的事件

        private LivestockState state;  // 当前状态
        private Character character;  // 角色组件
        private Selectable selectable;  // 可选择组件
        private Destructible destruct;  // 可破坏组件
        private Animator animator;  // 动画控制器

        private Vector3 start_pos;  // 初始位置

        private AnimalFood target_food;  // 目标食物
        private Vector3 wander_target;  // 游荡目标点

        private bool is_running = false;  // 是否正在奔跑
        private bool is_active = false;  // 是否处于活动状态

        private float state_timer = 0f;  // 状态计时器
        private float find_timer = 0f;  // 寻找计时器
        private float update_timer = 0f;  // 更新计时器

        private float last_eat_time = 0f;  // 上次进食时间
        private float last_grow_time = 0f;  // 上次成长时间

        void Awake()
        {
            character = GetComponent<Character>();  // 获取角色组件
            destruct = GetComponent<Destructible>();  // 获取可破坏组件
            selectable = GetComponent<Selectable>();  // 获取可选择组件
            animator = GetComponentInChildren<Animator>();  // 获取动画控制器
            start_pos = transform.position;  // 记录初始位置
            state_timer = 99f;  // 初始化状态计时器，用于立即开始游荡
            update_timer = Random.Range(-1f, 1f);  // 初始化更新计时器

            if (wander != WanderBehavior.None)
                transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);  // 如果有游荡行为，则随机旋转朝向
        }

        void Start()
        {
            character.onAttack += OnAttack;  // 订阅角色的攻击事件
            destruct.onDamaged += OnDamaged;  // 订阅可破坏组件的受伤事件
            destruct.onDeath += OnDeath;  // 订阅可破坏组件的死亡事件

            last_eat_time = PlayerData.Get().GetCustomFloat(GetSubUID("eat_time"));  // 获取上次进食时间
            last_grow_time = PlayerData.Get().GetCustomFloat(GetSubUID("grow_time"));  // 获取上次成长时间

            if (last_eat_time < 0.01f)
                ResetEatTime();  // 如果上次进食时间过短，重置进食时间
            if (last_grow_time < 0.01f)
                ResetGrowTime();  // 如果上次成长时间过短，重置成长时间
        }

        void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())  // 如果游戏暂停，则返回
                return;

            // 优化，如果距离太远则不运行
            float dist = (TheCamera.Get().GetTargetPos() - transform.position).magnitude;
            float active_range = Mathf.Max(detect_range * 2f, selectable.active_range * 0.8f);
            is_active = (state != LivestockState.Wander && state != LivestockState.Dead) || character.IsMoving() || dist < active_range;
        }

        private void Update()
        {
            // 动画控制
            bool paused = TheGame.Get().IsPaused();
            if (animator != null)
                animator.enabled = !paused;

            if (TheGame.Get().IsPaused())
                return;

            if (state == LivestockState.Dead || !is_active)
                return;

            state_timer += Time.deltaTime;
            find_timer += Time.deltaTime;

            // 状态处理
            if (state == LivestockState.Wander)
            {
                if (state_timer > wander_interval && wander != WanderBehavior.None)
                {
                    state_timer = Random.Range(-1f, 1f);  // 随机设置计时器，以实现间隔
                    FindWanderTarget();  // 寻找游荡目标点
                    character.MoveTo(wander_target);  // 移动到游荡目标点
                }

                // 处理角色卡住的情况
                if (character.IsStuck())
                    character.Stop();
            }

            if (state == LivestockState.MoveTo)
            {
                if (character.HasReachedMoveTarget())
                    StopAction();  // 如果已经到达目标点，则停止动作
            }

            if (state == LivestockState.FindFood)
            {
                if (target_food == null)
                {
                    ChangeState(LivestockState.Wander);  // 如果目标食物为空，则改为游荡状态
                    return;
                }

                if (!character.IsTryMoving())
                    character.MoveTo(target_food.transform.position);  // 如果没有尝试移动，则移动到目标食物位置

                float dist = (target_food.transform.position - transform.position).magnitude;
                if (dist < eat_range)
                {
                    StartEat();  // 如果距离小于进食范围，则开始进食
                    return;
                }

                if (state_timer > 2f && character.IsStuck())
                    StopFindFood();  // 如果状态计时器超过2秒且角色卡住，则停止寻找食物

                if (state_timer > 10f)
                    StopFindFood();  // 如果状态计时器超过10秒，则停止寻找食物
            }

            if (state == LivestockState.Eat)
            {
                if (target_food == null)
                {
                    ChangeState(LivestockState.Wander);  // 如果目标食物为空，则改为游荡状态
                    return;
                }

                if (state_timer > 2f)
                {
                    FinishEat(target_food);  // 如果状态计时器超过2秒，则完成进食
                }
            }

            update_timer += Time.deltaTime;
            if (update_timer > 0.5f)
            {
                update_timer = Random.Range(-0.1f, 0.1f);
                SlowUpdate();  // 进行慢速更新，用于优化
            }

            // 动画状态更新
            if (animator != null && animator.enabled)
            {
                animator.SetBool("Move", IsMoving() && IsActive());  // 设置移动动画状态
                animator.SetBool("Run", IsRunning() && IsActive());  // 设置奔跑动画状态
            }
        }

        private void SlowUpdate()
        {
            if (state == LivestockState.Wander)
            {
                // 如果可以进食且需要进食且在寻找计时器内
                if (eat && IsHungry() && find_timer > 0f)
                {
                    target_food = AnimalFood.GetNearest(eat_food_group, transform.position, detect_range);  // 获取最近的食物
                    if (target_food != null)
                    {
                        ChangeState(LivestockState.FindFood);  // 改为寻找食物状态
                        character.Stop();  // 停止移动
                    }
                }

                // 如果可以成长且成长时间已到且进食次数足够
                if (grow_to != null && GrowTimeFinished() && GetEatCount() >= grow_eat_count)
                {
                    Grow();  // 角色成长
                }

                // 如果可以产出物品且产出时间已到且进食次数足够
                else if (item_produce != null && ProduceTimeFinished() && GetEatCount() >= item_eat_count)
                {
                    ProduceItem();  // 产出物品
                }
            }
        }

        private void StartEat()
        {
            character.Stop();  // 停止移动
            character.FaceTorward(target_food.transform.position);  // 面向目标食物位置
            ChangeState(LivestockState.Eat);  // 改为进食状态
            if (animator != null)
                animator.SetTrigger("Eat");  // 播放进食动画
        }

        private void FinishEat(AnimalFood food)
        {
            if (food != null)
            {
                state_timer = 0f;  // 重置状态计时器
                food.EatFood();  // 进食食物
                ResetEatTime();  // 重置进食时间
                PlayerData.Get().SetCustomInt(GetSubUID("eat"), GetEatCount() + 1);  // 设置进食次数
                ChangeState(LivestockState.Wander);  // 改为游荡状态
            }
        }

        private void StopFindFood()
        {
            find_timer = -5f;  // 5秒内不再寻找食物
            ChangeState(LivestockState.Wander);  // 改为游荡状态
            FindWanderTarget();  // 寻找游荡目标点
            character.MoveTo(wander_target);  // 移动到游荡目标点
        }

        private void ProduceItem()
        {
            if (item_produce != null)
            {
                ResetGrowTime();  // 重置成长时间
                PlayerData.Get().SetCustomInt(GetSubUID("eat"), 0);  // 重置进食次数

                // 如果产出类型为掉落到地面
                if (item_collect_type == LivestockProduceType.DropFloor)
                {
                    int count_animal = Character.CountSceneObjects(character.data, transform.position, item_max_range);  // 统计场景中同类角色的数量
                    int count_item = Item.CountSceneObjects(item_produce, transform.position, item_max_range);  // 统计场景中产出物品的数量

                    if (count_animal * item_max > count_item)
                    {
                        Item.Create(item_produce, transform.position);  // 创建物品
                    }
                }

                // 如果产出类型为需要手动收集
                if (item_collect_type == LivestockProduceType.CollectAction)
                {
                    int nb = GetProductCount();  // 获取当前物品数量
                    if (nb < item_max)
                    {
                        PlayerData.Get().SetCustomInt(GetSubUID("product"), nb + 1);  // 增加物品数量
                    }
                }
            }
        }

        private void Grow()
        {
            if (grow_to != null)
            {
                PlayerData.Get().RemoveCharacter(GetUID());  // 从玩家数据中移除角色
                PlayerData.Get().RemoveObject(GetUID());  // 从玩家数据中移除对象
                PlayerData.Get().SetCustomInt(GetSubUID("eat"), 0);  // 重置进食次数
                ResetGrowTime();  // 重置成长时间
                Destroy(gameObject);  // 销毁当前物体

                Character.Create(grow_to, transform.position, transform.rotation);  // 创建新的角色
            }
        }

        private void FindWanderTarget()
        {
            float range = Random.Range(0f, wander_range);  // 随机游荡范围
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;  // 随机角度
            Vector3 spos = wander == WanderBehavior.WanderFar ? transform.position : start_pos;  // 起始位置
            Vector3 pos = spos + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * range;  // 计算目标位置
            wander_target = pos;  // 设置游荡目标点
        }

        public void CollectProduct(PlayerCharacter player)
        {
            int nb = GetProductCount();  // 获取当前物品数量
            if (nb > 0)
            {
                PlayerData.Get().SetCustomInt(GetSubUID("product"), nb - 1);  // 减少物品数量
                player.Inventory.GainItem(item_produce, 1);  // 玩家获取物品
            }
        }

        public void MoveToTarget(Vector3 pos, bool run)
        {
            is_running = run;  // 设置奔跑状态
            ChangeState(LivestockState.MoveTo);  // 改为移动到目标状态
            character.MoveTo(pos);  // 移动到目标位置
        }

        public void StopAction()
        {
            character.Stop();  // 停止角色动作
            is_running = false;  // 停止奔跑状态
            target_food = null;  // 清空目标食物
            ChangeState(LivestockState.Wander);  // 改为游荡状态
        }

        public void ChangeState(LivestockState state)
        {
            this.state = state;  // 改变状态
            state_timer = 0f;  // 重置状态计时器
        }

        public void Reset()
        {
            StopAction();  // 停止动作
            character.Reset();  // 重置角色
            animator.Rebind();  // 重新绑定动画
        }

        private void ResetEatTime()
        {
            if (time_type == TimeType.GameDays)
                PlayerData.Get().SetCustomFloat(GetSubUID("eat_time"), PlayerData.Get().day);  // 设置进食时间为当前天数
            if (time_type == TimeType.GameHours)
                PlayerData.Get().SetCustomFloat(GetSubUID("eat_time"), PlayerData.Get().GetTotalTime());  // 设置进食时间为当前总时间
        }

        private void ResetGrowTime()
        {
            if (time_type == TimeType.GameDays)
                PlayerData.Get().SetCustomFloat(GetSubUID("grow_time"), PlayerData.Get().day);  // 设置成长时间为当前天数
            if (time_type == TimeType.GameHours)
                PlayerData.Get().SetCustomFloat(GetSubUID("grow_time"), Mathf.RoundToInt(PlayerData.Get().GetTotalTime()));  // 设置成长时间为当前总时间（四舍五入）
        }

        private bool EatTimeFinished()
        {
            float last_eat_time = PlayerData.Get().GetCustomFloat(GetSubUID("eat_time"));  // 获取上次进食时间
            if (time_type == TimeType.GameDays && HasUID())
                return PlayerData.Get().day >= Mathf.RoundToInt(last_eat_time + eat_interval_time);  // 判断是否达到进食时间间隔
            if (time_type == TimeType.GameHours && HasUID())
                return PlayerData.Get().GetTotalTime() > last_eat_time + eat_interval_time;  // 判断是否达到进食时间间隔
            return false;
        }

        private bool GrowTimeFinished()
        {
            float last_grow_time = PlayerData.Get().GetCustomFloat(GetSubUID("grow_time"));  // 获取上次成长时间
            if (time_type == TimeType.GameDays && HasUID())
                return PlayerData.Get().day >= Mathf.RoundToInt(last_grow_time + grow_time);  // 判断是否达到成长时间间隔
            if (time_type == TimeType.GameHours && HasUID())
                return PlayerData.Get().GetTotalTime() > last_grow_time + grow_time;  // 判断是否达到成长时间间隔
            return false;
        }

        private bool ProduceTimeFinished()
        {
            float last_grow_time = PlayerData.Get().GetCustomFloat(GetSubUID("grow_time"));  // 获取上次成长时间
            if (time_type == TimeType.GameDays && HasUID())
                return PlayerData.Get().day >= Mathf.RoundToInt(last_grow_time + item_produce_time);  // 判断是否达到产出时间间隔
            if (time_type == TimeType.GameHours && HasUID())
                return PlayerData.Get().GetTotalTime() > last_grow_time + item_produce_time;  // 判断是否达到产出时间间隔
            return false;
        }

        private void OnDamaged()
        {
            if (IsDead())
                return;

            if (onDamaged != null)
                onDamaged.Invoke();  // 触发受伤事件

            if (animator != null)
                animator.SetTrigger("Damaged");  // 播放受伤动画
        }

        private void OnDeath()
        {
            state = LivestockState.Dead;  // 改为死亡状态

            if (onDeath != null)
                onDeath.Invoke();  // 触发死亡事件

            if (animator != null)
                animator.SetTrigger("Death");  // 播放死亡动画
        }

        void OnAttack()
        {
            if (animator != null)
                animator.SetTrigger("Attack");  // 播放攻击动画
        }

        public bool IsHungry()
        {
            return eat && EatTimeFinished();  // 判断是否饥饿状态
        }

        public bool IsDead()
        {
            return character.IsDead();  // 判断是否死亡
        }

        public bool IsActive()
        {
            return is_active;  // 判断是否激活状态
        }

        public bool IsMoving()
        {
            return character.IsMoving();  // 判断是否移动状态
        }

        public bool IsRunning()
        {
            return character.IsMoving() && is_running;  // 判断是否奔跑状态
        }

        public bool HasProduct()
        {
            return PlayerData.Get().GetCustomInt(GetSubUID("product")) > 0;  // 判断是否有产出物品
        }

        public int GetProductCount()
        {
            return PlayerData.Get().GetCustomInt(GetSubUID("product"));  // 获取产出物品数量
        }

        public int GetEatCount()
        {
            return PlayerData.Get().GetCustomInt(GetSubUID("eat"));  // 获取进食次数
        }

        public bool HasUID()
        {
            return character.HasUID();  // 判断是否有UID
        }

        public string GetUID()
        {
            return character.GetUID();  // 获取UID
        }

        public string GetSubUID(string tag)
        {
            return character.GetSubUID(tag);  // 获取子UID
        }
    }
}
