using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    public enum PetState
    {
        Idle = 0,        // 空闲状态
        Follow = 2,      // 跟随主人状态
        Attack = 5,      // 攻击状态
        Dig = 8,         // 挖掘状态
        Pet = 10,        // 宠物状态
        MoveTo = 15,     // 移动到指定位置状态
        Dead = 20,       // 死亡状态
    }

    /// <summary>
    /// Pet behavior script for following player, attacking enemies, and digging
    /// 宠物行为脚本，用于跟随玩家、攻击敌人和挖掘
    /// </summary>

    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Destructible))]
    [RequireComponent(typeof(Character))]
    [RequireComponent(typeof(UniqueID))]
    public class Pet : MonoBehaviour
    {
        [Header("Actions")]
        public float follow_range = 3f;           // 跟随范围
        public float detect_range = 5f;           // 检测范围
        public float wander_range = 4f;           // 游荡范围
        public float action_duration = 10f;       // 行动持续时间
        public bool can_attack = false;           // 是否能攻击
        public bool can_dig = false;              // 是否能挖掘

        public UnityAction onAttack;              // 攻击事件
        public UnityAction onDamaged;             // 受伤事件
        public UnityAction onDeath;               // 死亡事件
        public UnityAction onPet;                 // 宠物事件

        private Character character;              // 角色组件
        private Destructible destruct;            // 可毁坏组件
        private UniqueID unique_id;               // 唯一标识组件

        private PetState state;                   // 当前状态
        private Vector3 start_pos;                // 初始位置
        private Animator animator;                // 动画控制器

        private Destructible attack_target = null;    // 攻击目标
        private GameObject action_target = null;      // 行动目标（挖掘目标）
        private Vector3 wander_target;                // 游荡目标

        private int master_player = -1;          // 主人玩家ID
        private bool follow = false;             // 是否在跟随状态
        private float state_timer = 0f;          // 状态计时器
        private bool force_action = false;       // 是否强制执行行动

        void Awake()
        {
            character = GetComponent<Character>();           // 获取角色组件
            destruct = GetComponent<Destructible>();         // 获取可毁坏组件
            unique_id = GetComponent<UniqueID>();             // 获取唯一标识组件
            animator = GetComponentInChildren<Animator>();   // 获取动画控制器
            start_pos = transform.position;                  // 记录初始位置
            wander_target = transform.position;              // 初始游荡目标为当前位置

            character.onAttack += OnAttack;           // 注册攻击事件监听
            destruct.onDamaged += OnTakeDamage;       // 注册受伤事件监听
            destruct.onDeath += OnKill;               // 注册死亡事件监听

            transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f); // 随机设置初始旋转角度
        }

        private void Start()
        {
            if (PlayerData.Get().IsObjectRemoved(GetUID()))
            {
                Destroy(gameObject);
                return;
            }

            if (PlayerData.Get().HasCustomInt(unique_id.GetSubUID("master")))
            {
                master_player = PlayerData.Get().GetCustomInt(unique_id.GetSubUID("master"));
                Follow(); // 跟随主人
            }
        }

        private void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (state == PetState.Dead)
                return;

            state_timer += Time.deltaTime;

            // 根据状态执行相应逻辑
            if (state == PetState.Idle)
            {
                if (follow && state_timer > 2f && HasMaster())
                    ChangeState(PetState.Follow); // 切换至跟随状态

                if (state_timer > 5f && wander_range > 0.1f)
                {
                    state_timer = Random.Range(-1f, 1f);
                    FindWanderTarget(); // 寻找游荡目标
                    character.MoveTo(wander_target); // 移动至游荡目标
                }

                // 检测角色是否被卡住
                if (character.IsStuck())
                    character.Stop(); // 停止移动
            }

            if (state == PetState.Follow)
            {
                if (HasMaster() && !IsMoving() && PlayerIsFar(follow_range))
                    character.Follow(GetMaster().gameObject); // 跟随主人

                if (!follow)
                    ChangeState(PetState.Idle); // 切换至空闲状态

                DetectAction(); // 检测可执行的动作
            }

            if (state == PetState.Dig)
            {
                if (action_target == null)
                {
                    StopAction(); // 停止当前动作
                    return;
                }

                Vector3 dir = action_target.transform.position - transform.position;
                if (dir.magnitude < 1f)
                {
                    character.Stop(); // 停止移动
                    character.FaceTorward(action_target.transform.position); // 面向挖掘目标

                    if (animator != null)
                        animator.SetTrigger("Dig"); // 播放挖掘动画

                    StartCoroutine(DigRoutine()); // 开始挖掘协程
                }

                if (state_timer > 10f)
                {
                    StopAction(); // 超过挖掘时间限制，停止动作
                }
            }

            if (state == PetState.Attack)
            {
                if (attack_target == null || attack_target.IsDead())
                {
                    StopAction(); // 停止当前动作
                    return;
                }

                Vector3 targ_dir = attack_target.transform.position - transform.position;
                if (!force_action && state_timer > action_duration)
                {
                    if (targ_dir.magnitude > detect_range || PlayerIsFar(detect_range * 2f))
                    {
                        StopAction(); // 超过攻击范围或主人距离过远，停止攻击
                    }
                }

                if (targ_dir.y > 10f)
                    StopAction(); // 鸟类目标过高，停止攻击
            }

            if (state == PetState.Pet)
            {
                if (state_timer > 2f)
                {
                    if (HasMaster())
                        ChangeState(PetState.Follow); // 切换至跟随状态
                    else
                        ChangeState(PetState.Idle); // 切换至空闲状态
                }
            }

            if (state == PetState.MoveTo)
            {
                if (character.HasReachedMoveTarget())
                    StopAction(); // 停止当前动作
            }

            if (animator != null)
            {
                animator.SetBool("Move", IsMoving()); // 设置动画参数，表示是否在移动中
            }
        }

        // 挖掘协程
        private IEnumerator DigRoutine()
        {
            yield return new WaitForSeconds(1f);

            if (action_target != null)
            {
                DigSpot dig = action_target.GetComponent<DigSpot>();
                if (dig != null)
                    dig.Dig(); // 执行挖掘操作
            }

            StopAction(); // 停止当前动作
        }

        // 检测可执行的动作（攻击或挖掘）
        private void DetectAction()
        {
            if (PlayerIsFar(detect_range))
                return;

            foreach (Selectable selectable in Selectable.GetAllActive())
            {
                if (selectable.gameObject != gameObject)
                {
                    Vector3 dir = (selectable.transform.position - transform.position);
                    if (dir.magnitude < detect_range)
                    {
                        DigSpot dig = selectable.GetComponent<DigSpot>();
                        Destructible destruct = selectable.GetComponent<Destructible>();

                        if (can_attack && destruct && destruct.target_team == AttackTeam.Enemy && destruct.required_item == null)
                        {
                            attack_target = destruct; // 设置攻击目标
                            action_target = null;
                            character.Attack(destruct); // 执行攻击
                            ChangeState(PetState.Attack); // 切换至攻击状态
                            return;
                        }

                        else if (can_dig && dig != null)
                        {
                            attack_target = null;
                            action_target = dig.gameObject; // 设置挖掘目标
                            ChangeState(PetState.Dig); // 切换至挖掘状态
                            character.MoveTo(dig.transform.position); // 移动至挖掘目标
                            return;
                        }
                    }
                }
            }
        }

        // 宠物互动
        public void PetPet()
        {
            StopAction(); // 停止当前动作
            ChangeState(PetState.Pet); // 切换至宠物状态
            if (animator != null)
                animator.SetTrigger("Pet"); // 播放宠物动画
        }

        // 驯服宠物
        public void TamePet(PlayerCharacter player)
        {
            if (player != null && character.data != null && !HasMaster() && unique_id.HasUID())
            {
                PetPet(); // 宠物互动
                follow = true; // 设置为跟随状态

                // 创建新角色，使宠物可以切换场景
                string prev_uid = unique_id.unique_id;
                TrainedCharacterData prev_cdata = PlayerData.Get().GetCharacter(prev_uid);
                if (prev_cdata == null)
                {
                    TrainedCharacterData cdata = PlayerData.Get().AddCharacter(character.data.id, SceneNav.GetCurrentScene(), transform.position, transform.rotation);
                    unique_id.SetUID(cdata.uid); // 设置唯一标识符
                    PlayerData.Get().RemoveObject(prev_uid); // 移除之前的对象数据
                }

                // 设置主人
                master_player = player.player_id;
                player.SaveData.AddPet(unique_id.unique_id, character.data.id); // 添加宠物到玩家数据
                PlayerData.Get().SetCustomInt(unique_id.GetSubUID("master"), player.player_id); // 设置主人的自定义整数
            }
        }

        // 解除驯服
        public void UntamePet()
        {
            if (HasMaster() && unique_id.HasUID())
            {
                StopAction(); // 停止当前动作

                // 移除主人
                PlayerCharacter master = GetMaster();
                master.SaveData.RemovePet(unique_id.unique_id); // 从玩家数据中移除宠物
                master_player = -1;
                PlayerData.Get().SetCustomInt(unique_id.GetSubUID("master"), -1); // 设置主人的自定义整数
            }
        }

        // 开始跟随
        public void Follow()
        {
            if (HasMaster())
            {
                StopAction(); // 停止当前动作
                follow = true; // 设置为跟随状态
                ChangeState(PetState.Follow); // 切换至跟随状态
            }
        }

        // 停止跟随
        public void StopFollow()
        {
            StopAction(); // 停止当前动作
            follow = false; // 取消跟随状态
            ChangeState(PetState.Idle); // 切换至空闲状态
        }

        // 攻击目标
        public void AttackTarget(Destructible target)
        {
            if (target != null)
            {
                attack_target = target; // 设置攻击目标
                action_target = null;
                force_action = true;
                character.Attack(target); // 执行攻击
                ChangeState(PetState.Attack); // 切换至攻击状态
            }
        }

        // 移动至目标位置
        public void MoveToTarget(Vector3 pos)
        {
            force_action = true;
            attack_target = null;
            action_target = null;
            ChangeState(PetState.MoveTo); // 切换至移动到指定位置状态
            character.MoveTo(pos); // 移动至目标位置
        }

        // 停止当前动作
        public void StopAction()
        {
            character.Stop(); // 停止角色动作
            attack_target = null; // 清空攻击目标
            action_target = null; // 清空行动目标
            force_action = false; // 取消强制行动状态
            follow = false; // 取消跟随状态
            ChangeState(PetState.Idle); // 切换至空闲状态
        }

        // 寻找游荡目标
        private void FindWanderTarget()
        {
            float range = Random.Range(0f, wander_range);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * range;
            wander_target = pos; // 设置游荡目标
        }

        // 切换状态
        public void ChangeState(PetState state)
        {
            this.state = state; // 设置当前状态
            state_timer = 0f; // 重置状态计时器
        }

        // 攻击事件处理
        private void OnAttack()
        {
            if (animator != null)
                animator.SetTrigger("Attack"); // 播放攻击动画

            if (onAttack != null)
                onAttack.Invoke(); // 触发攻击事件
        }

        // 受伤事件处理
        private void OnTakeDamage()
        {
            if (IsDead())
                return;

            if (onDamaged != null)
                onDamaged.Invoke(); // 触发受伤事件
        }

        // 死亡事件处理
        private void OnKill()
        {
            state = PetState.Dead; // 设置死亡状态

            if (animator != null)
                animator.SetTrigger("Death"); // 播放死亡动画

            if (onDeath != null)
                onDeath.Invoke(); // 触发死亡事件
        }

        // 主人是否距离过远
        public bool PlayerIsFar(float distance)
        {
            if (HasMaster())
            {
                PlayerCharacter master = GetMaster();
                Vector3 dir = master.transform.position - transform.position;
                return dir.magnitude > distance; // 返回主人与宠物之间的距离是否超过设定距离
            }
            return false;
        }

        // 获取主人角色
        public PlayerCharacter GetMaster()
        {
            return PlayerCharacter.Get(master_player); // 根据主人玩家ID获取主人角色
        }

        // 是否有主人
        public bool HasMaster()
        {
            return master_player >= 0; // 判断是否有主人
        }

        // 是否在跟随状态
        public bool IsFollow()
        {
            return follow; // 返回是否在跟随状态
        }

        // 是否已死亡
        public bool IsDead()
        {
            return character.IsDead(); // 返回宠物角色是否已死亡
        }

        // 是否在移动中
        public bool IsMoving()
        {
            return character.IsMoving(); // 返回宠物角色是否在移动中
        }

        // 获取唯一标识符
        public string GetUID()
        {
            return character.GetUID(); // 获取宠物角色的唯一标识符
        }
    }

}
