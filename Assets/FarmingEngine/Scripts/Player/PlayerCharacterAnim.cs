using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 管理所有角色动画
    /// </summary>
    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterAnim : MonoBehaviour
    {
        public string move_anim = "Move";           // 移动动画参数名
        public string move_side_x = "MoveX";        // 横向移动动画参数名（X轴）
        public string move_side_z = "MoveZ";        // 横向移动动画参数名（Z轴）
        public string attack_anim = "Attack";       // 攻击动画参数名
        public string attack_speed = "AttackSpeed"; // 攻击速度动画参数名
        public string take_anim = "Take";           // 拾取物品动画参数名
        public string craft_anim = "Craft";         // 制作物品动画参数名
        public string build_anim = "Build";         // 建造动画参数名
        public string use_anim = "Use";             // 使用动画参数名
        public string damaged_anim = "Damaged";     // 受伤动画参数名
        public string death_anim = "Death";         // 死亡动画参数名
        public string sleep_anim = "Sleep";         // 睡觉动画参数名
        public string fish_anim = "Fish";           // 钓鱼动画参数名
        public string dig_anim = "Dig";             // 挖掘动画参数名
        public string water_anim = "Water";         // 浇水动画参数名
        public string hoe_anim = "Hoe";             // 锄地动画参数名
        public string ride_anim = "Ride";           // 骑乘动画参数名
        public string swim_anim = "Swim";           // 游泳动画参数名
        public string climb_anim = "Climb";         // 攀爬动画参数名

        private PlayerCharacter character;           // 角色控制组件
        private Animator animator;                   // 动画控制器组件

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();  // 获取角色控制组件
            animator = GetComponentInChildren<Animator>(); // 获取子对象中的动画控制器组件

            if (animator == null)
                enabled = false;  // 如果没有找到动画控制器组件，则禁用脚本
        }

        private void Start()
        {
            // 注册事件监听
            character.Inventory.onTakeItem += OnTake;
            character.Inventory.onDropItem += OnDrop;
            character.Crafting.onCraft += OnCraft;
            character.Crafting.onBuild += OnBuild;
            character.Combat.onAttack += OnAttack;
            character.Combat.onAttackHit += OnAttackHit;
            character.Combat.onDamaged += OnDamaged;
            character.Combat.onDeath += OnDeath;
            character.onTriggerAnim += OnTriggerAnim;

            // 如果角色有跳跃组件，则注册跳跃事件
            if (character.Jumping)
                character.Jumping.onJump += OnJump;
        }

        // Z 在这里，是根据目前角色状态播放动画，也可以通过上面添加监听来实现播放对应动画
        void Update()
        {
            bool player_paused = TheGame.Get().IsPausedByPlayer();     // 玩家暂停状态
            bool gameplay_paused = TheGame.Get().IsPausedByScript();   // 游戏脚本暂停状态
            animator.enabled = !player_paused;                         // 根据玩家暂停状态设置动画播放状态

            if (animator.enabled)
            {
                // 设置动画布尔参数（ Z 感觉这里的动画参数应该是是不会冲突的,比如Sleep 和 Fishing不会都是 true）
                SetAnimBool(move_anim, !gameplay_paused && character.IsMoving());
                SetAnimBool(craft_anim, !gameplay_paused && character.Crafting.IsCrafting());
                SetAnimBool(sleep_anim, character.IsSleeping());
                SetAnimBool(fish_anim, character.IsFishing());
                SetAnimBool(ride_anim, character.IsRiding());
                SetAnimBool(swim_anim, character.IsSwimming());
                SetAnimBool(climb_anim, character.IsClimbing());

                // 计算横向移动的动画参数值
                Vector3 move_vect = character.GetMoveNormalized();
                float mangle = Vector3.SignedAngle(character.GetFacing(), move_vect, Vector3.up);
                Vector3 move_side = new Vector3(Mathf.Sin(mangle * Mathf.Deg2Rad), 0f, Mathf.Cos(mangle * Mathf.Deg2Rad));
                move_side = move_side * move_vect.magnitude;
                SetAnimFloat(move_side_x, move_side.x);
                SetAnimFloat(move_side_z, move_side.z);
            }
        }

        // 设置动画布尔参数
        public void SetAnimBool(string id, bool value)
        {
            if (!string.IsNullOrEmpty(id))
                animator.SetBool(id, value);
        }

        // 设置动画浮点参数
        public void SetAnimFloat(string id, float value)
        {
            if (!string.IsNullOrEmpty(id))
                animator.SetFloat(id, value);
        }

        // 设置动画触发器
        public void SetAnimTrigger(string id)
        {
            if (!string.IsNullOrEmpty(id))
                animator.SetTrigger(id);
        }

        // 拾取物品事件处理
        private void OnTake(Item item)
        {
            SetAnimTrigger(take_anim);  // 触发拾取动画
        }

        // 丢弃物品事件处理
        private void OnDrop(Item item)
        {
            // 可以在这里添加丢弃动画
        }

        // 制作物品事件处理
        private void OnCraft(CraftData cdata)
        {
            // 可以在这里添加制作动画
        }

        // 建造事件处理
        private void OnBuild(Buildable construction)
        {
            SetAnimTrigger(build_anim);  // 触发建造动画
        }

        // 跳跃事件处理
        private void OnJump()
        {
            // 可以在这里添加跳跃动画
        }

        // 受伤事件处理
        private void OnDamaged()
        {
            SetAnimTrigger(damaged_anim);  // 触发受伤动画
        }

        // 死亡事件处理
        private void OnDeath()
        {
            SetAnimTrigger(death_anim);    // 触发死亡动画
        }

        // 攻击事件处理
        private void OnAttack(Destructible target, bool ranged)
        {
            string anim = attack_anim;               // 默认攻击动画参数名
            float anim_speed = character.Combat.GetAttackAnimSpeed(); // 获取攻击动画速度

            // 根据当前装备的武器替换动画
            EquipItem equip = character.Inventory.GetEquippedWeaponMesh();
            if (equip != null)
            {
                if (!ranged && !string.IsNullOrEmpty(equip.attack_melee_anim))
                    anim = equip.attack_melee_anim;
                if (ranged && !string.IsNullOrEmpty(equip.attack_ranged_anim))
                    anim = equip.attack_ranged_anim;
            }

            SetAnimFloat(attack_speed, anim_speed); // 设置攻击速度参数
            SetAnimTrigger(anim);                   // 触发攻击动画
        }

        // 攻击命中事件处理
        private void OnAttackHit(Destructible target)
        {
            // 这里可以添加攻击命中时的动画处理
        }

        // 触发动画事件处理
        private void OnTriggerAnim(string anim, float duration)
        {
            SetAnimTrigger(anim);  // 触发指定动画
        }
    }
}
