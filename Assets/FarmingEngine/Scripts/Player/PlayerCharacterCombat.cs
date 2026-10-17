using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    public enum PlayerAttackBehavior
    {
        AutoAttack = 0, // 点击目标后将持续进行攻击
        ClickToHit = 10, // 点击物体后只进行一次攻击
        NoAttack = 20, // 角色无法攻击
    }

    /// <summary>
    /// 管理玩家角色的攻击、生命值和死亡的类
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterCombat : MonoBehaviour
    {
        [Header("Combat")]
        public PlayerAttackBehavior attack_type; // 攻击类型
        public int hand_damage = 5; // 手持武器的基础伤害
        public int base_armor = 0; // 基础护甲
        public float attack_range = 1.2f; // 攻击范围（近战）
        public float attack_cooldown = 1f; // 攻击间隔
        public float attack_windup = 0.7f; // 攻击预备时间
        public float attack_windout = 0.4f; // 攻击结束后的冷却时间
        public float attack_energy = 1f; // 攻击消耗的能量

        [Header("FX")]
        public GameObject hit_fx; // 攻击特效
        public GameObject death_fx; // 死亡特效
        public AudioClip hit_sound; // 攻击音效
        public AudioClip death_sound; // 死亡音效

        public UnityAction<Destructible, bool> onAttack; // 攻击事件（目标，是否远程攻击）
        public UnityAction<Destructible> onAttackHit; // 攻击命中事件（目标）
        public UnityAction onDamaged; // 受伤事件
        public UnityAction onDeath; // 死亡事件

        private PlayerCharacter character; // 玩家角色实例
        private PlayerCharacterAttribute character_attr; // 玩家角色属性

        private Coroutine attack_routine = null; // 攻击协程
        private float attack_timer = 0f; // 攻击计时器

        //Z 状态标识，而且我觉得有必要的脚本都要设置下
        private bool is_dead = false; // 是否已死亡
        private bool is_attacking = false; // 是否正在进行攻击

        private void Awake()
        {
            character = GetComponent<PlayerCharacter>();
            character_attr = GetComponent<PlayerCharacterAttribute>();
        }

        void Start()
        {

        }

        //Z 总结下，这里的逻辑就是，判断是否符合攻击条件，符合则攻击
        void Update()
        {
            if (TheGame.Get().IsPaused()) // 如果游戏暂停了，则返回
                return;

            if (IsDead()) // 如果已死亡，则返回
                return;

            attack_timer += Time.deltaTime; // 更新攻击计时器

            // 在目标范围内时进行攻击
            Destructible auto_move_attack = character.GetAutoAttackTarget();
            if (auto_move_attack != null && !character.IsBusy() && IsAttackTargetInRange(auto_move_attack))
            {
                character.FaceTorward(auto_move_attack.transform.position); // 面向目标
                character.PauseAutoMove(); // 到达目标后停止自动移动

                if (attack_timer > GetAttackCooldown())
                {
                    Attack(auto_move_attack); // 发动攻击(Z：这里应该是每一次攻击执行一次，比如砍树，每砍一次执行一次，这样做便于中断行为)
                }
            }
        }

        // 受到伤害（Z 其实这里也是，先判断是否符合条件，如果符合则再进行操作）
        public void TakeDamage(int damage)
        {
            if (is_dead) // 如果已死亡，则返回
                return;

            if (character.Attributes.GetBonusEffectTotal(BonusType.Invulnerable) > 0.5f) // 如果有无敌效果，则返回
                return;

            int dam = damage - GetArmor();
            dam = Mathf.Max(dam, 1); // 最少造成1点伤害

            int invuln = Mathf.RoundToInt(dam * character.Attributes.GetBonusEffectTotal(BonusType.Invulnerable));
            dam = dam - invuln;

            if (dam <= 0) // 如果伤害小于等于0，则返回
                return;

            character_attr.AddAttribute(AttributeType.Health, -dam); // 扣除生命值

            // 耐久度
            character.Inventory.UpdateAllEquippedItemsDurability(false, -1f);

            character.StopSleep(); // 停止睡眠状态

            TheCamera.Get().Shake(); // 震动摄像机
            TheAudio.Get().PlaySFX("player", hit_sound); // 播放受击音效
            if (hit_fx != null)
                Instantiate(hit_fx, transform.position, Quaternion.identity); // 播放攻击特效

            if (onDamaged != null)
                onDamaged.Invoke(); // 触发受伤事件
        }

        // 死亡
        public void Kill()
        {
            if (is_dead) // 如果已死亡，则返回
                return;

            character.StopMove(); // 停止移动
            is_dead = true; // 标记为已死亡

            TheAudio.Get().PlaySFX("player", death_sound); // 播放死亡音效
            if (death_fx != null)
                Instantiate(death_fx, transform.position, Quaternion.identity); // 播放死亡特效

            if (onDeath != null)
                onDeath.Invoke(); // 触发死亡事件
        }

        // 发动攻击
        /// <summary>
        /// Z：这里应该是每一次攻击执行一次，比如砍树，每砍一次执行一次，这样做便于中断行为
        /// </summary>
        /// <param name="target"></param>
        public void Attack(Destructible target)
        {
            DoAttack(target);
        }

        // 发动攻击（无目标）
        public void Attack()
        {
            DoAttackNoTarget();
        }

        /// <summary>
        /// Z：执行一次攻击
        /// </summary>
        /// <param name="target"></param>
        private void DoAttack(Destructible target)
        {
            if (!character.IsBusy() && attack_timer > GetAttackCooldown())
            {
                attack_timer = -10f;
                character_attr.AddAttribute(AttributeType.Energy, -attack_energy); // 扣除能量消耗
                attack_routine = StartCoroutine(AttackRun(target)); // 开始攻击协程
            }
        }

        // 执行一次攻击（无目标）
        private void DoAttackNoTarget()
        {
            if (!character.IsBusy() && attack_timer > GetAttackCooldown())
            {
                attack_timer = -10f;
                character_attr.AddAttribute(AttributeType.Energy, -attack_energy); // 扣除能量消耗
                attack_routine = StartCoroutine(AttackRunNoTarget()); // 开始攻击协程（无目标）
            }
        }

        // 进行攻击的协程（针对一个目标）
        private IEnumerator AttackRun(Destructible target)
        {
            character.SetBusy(true); // 设置角色为忙碌状态
            is_attacking = true; // 标记为正在攻击

            bool is_ranged = target != null && CanWeaponAttackRanged(target); // 是否为远程攻击

            // 开始动画
            if (onAttack != null)
                onAttack.Invoke(target, is_ranged);

            // 面向目标
            character.FaceTorward(target.transform.position);

            // 等待攻击预备时间
            float windup = GetAttackWindup();
            yield return new WaitForSeconds(windup);

            // 耐久度(武器)
            character.Inventory.UpdateAllEquippedItemsDurability(true, -1f);

            int nb_strikes = GetAttackStrikes(target); // 获取攻击次数（Z：要攻击多少次，才会摧毁物体）
            float strike_interval = GetAttackStikesInterval(target); // 获取攻击间隔

            while (nb_strikes > 0)//Z 这里通过一个循环，实现多次攻击（注意砍树不是多次这个攻击，砍树只有一次，砍树的多次逻辑在Update中）
            {
                DoAttackStrike(target, is_ranged); // 执行攻击击打
                yield return new WaitForSeconds(strike_interval); // 等待攻击间隔
                nb_strikes--;
            }

            // 重置计时器
            attack_timer = 0f;

            // 等待攻击结束后，角色可以再次移动
            float windout = GetAttackWindout();
            yield return new WaitForSeconds(windout);

            character.SetBusy(false); // 设置角色为非忙碌状态
            is_attacking = false; // 标记为非攻击状态

            if (attack_type == PlayerAttackBehavior.ClickToHit)
                character.StopAutoMove(); // 停止自动移动
        }

        // 进行攻击的协程（无目标）
        private IEnumerator AttackRunNoTarget()
        {
            character.SetBusy(true); // 设置角色为忙碌状态
            is_attacking = true; // 标记为正在攻击

            // 面向前方
            bool freerotate = TheCamera.Get().IsFreeRotation();
            if (freerotate)
                character.FaceFront();

            // 开始动画
            if (onAttack != null)
                onAttack.Invoke(null, true);

            // 等待攻击预备时间
            float windup = GetAttackWindup();
            yield return new WaitForSeconds(windup);

            // 耐久度
            character.Inventory.UpdateAllEquippedItemsDurability(true, -1f);

            int nb_strikes = GetAttackStrikes(); // 获取攻击次数
            float strike_interval = GetAttackStikesInterval(); // 获取攻击间隔

            while (nb_strikes > 0)
            {
                DoAttackStrikeNoTarget(); // 执行攻击击打
                yield return new WaitForSeconds(strike_interval); // 等待攻击间隔
                nb_strikes--;
            }

            // 重置计时器
            attack_timer = 0f;

            // 等待攻击结束后，角色可以再次移动
            float windout = GetAttackWindout();
            yield return new WaitForSeconds(windout);

            character.SetBusy(false); // 设置角色为非忙碌状态
            is_attacking = false; // 标记为非攻击状态
        }

        /// <summary>
        /// Z：执行攻击击打（针对一个目标）
        /// </summary>
        /// <param name="target"></param>
        /// <param name="is_ranged"></param>
        private void DoAttackStrike(Destructible target, bool is_ranged)
        {
            // 远程攻击
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (target != null && is_ranged && equipped != null)
            {
                InventoryItemData projectile_inv = character.Inventory.GetFirstItemInGroup(equipped.projectile_group);
                ItemData projectile = ItemData.Get(projectile_inv?.item_id);
                if (projectile != null && CanWeaponAttackRanged(target))
                {
                    Vector3 pos = GetProjectileSpawnPos();
                    Vector3 dir = target.GetCenter() - pos;
                    GameObject proj = Instantiate(projectile.projectile_prefab, pos, Quaternion.LookRotation(dir.normalized, Vector3.up));
                    Projectile project = proj.GetComponent<Projectile>();
                    project.player_shooter = character;
                    project.dir = dir.normalized;
                    project.damage = equipped.damage + projectile.damage;
                    character.Inventory.UseItem(projectile, 1);
                }
            }

            // 近战攻击
            else if (IsAttackTargetInRange(target))
            {
                target.TakeDamage(character, GetAttackDamage(target)); // 造成伤害

                if (onAttackHit != null)
                    onAttackHit.Invoke(target); // 触发攻击命中事件
            }
        }

        // 执行攻击击打（无目标）
        private void DoAttackStrikeNoTarget()
        {
            // 远程攻击
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && equipped.IsRangedWeapon())
            {
                InventoryItemData projectile_inv = character.Inventory.GetFirstItemInGroup(equipped.projectile_group);
                ItemData projectile = ItemData.Get(projectile_inv?.item_id);
                if (projectile != null)
                {
                    character.Inventory.UseItem(projectile, 1);
                    Vector3 pos = GetProjectileSpawnPos();
                    Vector3 dir = transform.forward;
                    bool freerotate = TheCamera.Get().IsFreeRotation();
                    if (freerotate)
                        dir = TheCamera.Get().GetFacingDir();

                    GameObject proj = Instantiate(projectile.projectile_prefab, pos, Quaternion.LookRotation(dir.normalized, Vector3.up));
                    Projectile project = proj.GetComponent<Projectile>();
                    project.player_shooter = character;
                    project.dir = dir.normalized;
                    project.damage = equipped.damage + projectile.damage;

                    if (freerotate)
                        project.SetInitialCurve(GetAimDir());
                }
            }
            else
            {
                Destructible destruct = Destructible.GetNearestAutoAttack(character, character.GetInteractCenter(), 10f);
                if (destruct != null && IsAttackTargetInRange(destruct))
                {
                    destruct.TakeDamage(character, GetAttackDamage(destruct));

                    if (onAttackHit != null)
                        onAttackHit.Invoke(destruct); // 触发攻击命中事件
                }
            }
        }

        // 取消当前攻击
        public void CancelAttack()
        {
            if (is_attacking)
            {
                is_attacking = false; // 标记为非攻击状态
                attack_timer = 0f; // 重置计时器
                character.SetBusy(false); // 设置角色为非忙碌状态
                character.StopAutoMove(); // 停止自动移动
                if (attack_routine != null)
                    StopCoroutine(attack_routine); // 停止攻击协程
            }
        }

        // 是否正在攻击
        public bool IsAttacking()
        {
            return is_attacking;
        }

        // 是否能够攻击
        public bool CanAttack()
        {
            return attack_type != PlayerAttackBehavior.NoAttack;
        }

        // 攻击是否优先于其他行动
        public bool CanAutoAttack(Destructible target)
        {
            bool has_required_item = target != null && target.required_item != null && character.EquipData.HasItemInGroup(target.required_item); // 是否有必需的物品
            return CanAttack(target) && (has_required_item || target.target_team == AttackTeam.Enemy || !target.Selectable.CanAutoInteract()); // 返回是否能自动攻击
        }

        // 是否能够攻击
        public bool CanAttack(Destructible target)
        {
            return attack_type != PlayerAttackBehavior.NoAttack && target != null && target.CanBeAttacked()
                && (target.required_item != null || target.target_team != AttackTeam.Ally) // 不能攻击盟友，除非有必需的物品
                && (target.required_item == null || character.EquipData.HasItemInGroup(target.required_item)); // 有装备物品才能攻击
        }

        // 获取攻击伤害
        public int GetAttackDamage(Destructible target)
        {
            int damage = hand_damage;

            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && CanWeaponHitTarget(target))
                damage = equipped.damage;

            float mult = 1f + character.Attributes.GetBonusEffectTotal(BonusType.AttackBoost, target.Selectable.groups);
            damage = Mathf.RoundToInt(damage * mult);

            return damage;
        }

        // 获取攻击范围
        public float GetAttackRange()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && CanWeaponAttack())
                return Mathf.Max(equipped.range, attack_range);
            return attack_range;
        }

        // 获取攻击范围（针对一个目标）
        public float GetAttackRange(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && CanWeaponHitTarget(target))
                return Mathf.Max(equipped.range, attack_range);
            return attack_range;
        }

        // 获取攻击次数
        public int GetAttackStrikes(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && CanWeaponHitTarget(target))
                return Mathf.Max(equipped.strike_per_attack, 1);
            return 1;
        }

        // 获取攻击间隔
        public float GetAttackStikesInterval(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && CanWeaponHitTarget(target))
                return Mathf.Max(equipped.strike_interval, 0.01f);
            return 0.01f;
        }

        // 获取攻击冷却时间
        public float GetAttackCooldown()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null)
                return equipped.attack_cooldown / character.Attributes.GetAttackMult();
            return attack_cooldown / character.Attributes.GetAttackMult();
        }

        // 获取攻击次数
        public int GetAttackStrikes()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null)
                return Mathf.Max(equipped.strike_per_attack, 1);
            return 1;
        }

        // 获取攻击间隔
        public float GetAttackStikesInterval()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null)
                return Mathf.Max(equipped.strike_interval, 0.01f);
            return 0.01f;
        }

        // 获取攻击预备时间
        public float GetAttackWindup()
        {
            EquipItem item_equip = character.Inventory.GetEquippedWeaponMesh();
            if (item_equip != null && item_equip.override_timing)
                return item_equip.attack_windup / GetAttackAnimSpeed();
            return attack_windup / GetAttackAnimSpeed();
        }

        // 获取攻击结束时间
        public float GetAttackWindout()
        {
            EquipItem item_equip = character.Inventory.GetEquippedWeaponMesh();
            if (item_equip != null && item_equip.override_timing)
                return item_equip.attack_windout / GetAttackAnimSpeed();
            return attack_windout / GetAttackAnimSpeed();
        }

        // 获取攻击动画速度
        public float GetAttackAnimSpeed()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && equipped.attack_speed > 0.01f)
                return equipped.attack_speed * character.Attributes.GetAttackMult();
            return 1f * character.Attributes.GetAttackMult();
        }

        // 获取投射物生成位置
        public Vector3 GetProjectileSpawnPos()
        {
            ItemData weapon = character.EquipData.GetEquippedWeaponData();
            EquipAttach attach = character.Inventory.GetEquipAttachment(weapon.equip_slot, weapon.equip_side);
            if (attach != null)
                return attach.transform.position;
            return transform.position + Vector3.up;
        }

        // 获取射击投影的方向
        public Vector3 GetAimDir(float distance = 10f)
        {
            Vector3 cam_pos = TheCamera.Get().transform.position; 
            Vector3 cam_dir = TheCamera.Get().GetFacingDir();
            Vector3 far = cam_pos + cam_dir * distance;
            Vector3 aim = far - character.GetColliderCenter();
            return aim.normalized;
        }

        // 确保当前装备的武器可以击中目标，并且有足够的子弹
        public bool CanWeaponHitTarget(Destructible target)
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            bool valid_ranged = equipped != null && equipped.IsRangedWeapon() && CanWeaponAttackRanged(target);
            bool valid_melee = equipped != null && equipped.IsMeleeWeapon();
            return valid_melee || valid_ranged;
        }

        // 检查目标是否适合远程攻击，并且是否有足够的子弹（Z 这里是检查远程攻击吗？？？范围+子弹？？？）
        public bool CanWeaponAttackRanged(Destructible destruct)
        {
            if (destruct == null)
                return false;

            return destruct.CanAttackRanged() && HasRangedProjectile();
        }

        // 是否能够攻击
        public bool CanWeaponAttack()
        {
            return !HasRangedWeapon() || HasRangedProjectile();
        }

        // 是否拥有远程武器
        public bool HasRangedWeapon()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            return (equipped != null && equipped.IsRangedWeapon());
        }

        // 是否拥有远程投射物
        public bool HasRangedProjectile()
        {
            ItemData equipped = character.EquipData.GetEquippedWeaponData();
            if (equipped != null && equipped.IsRangedWeapon())
            {
                InventoryItemData invdata = character.Inventory.GetFirstItemInGroup(equipped.projectile_group);
                ItemData projectile = ItemData.Get(invdata?.item_id);
                return projectile != null && character.Inventory.HasItem(projectile);
            }
            return false;
        }

        // 获取目标攻击范围
        public float GetTargetAttackRange(Destructible target)
        {
            return GetAttackRange(target) + target.hit_range;
        }

        // 目标是否在攻击范围内
        public bool IsAttackTargetInRange(Destructible target)
        {
            if (target != null)
            {
                float dist = (target.transform.position - character.GetInteractCenter()).magnitude;
                return dist < GetTargetAttackRange(target);
            }
            return false;
        }

        // 获取护甲值
        public int GetArmor()
        {
            int armor = base_armor;
            foreach (KeyValuePair<int, InventoryItemData> pair in character.EquipData.items)
            {
                ItemData idata = ItemData.Get(pair.Value?.item_id);
                if (idata != null)
                    armor += idata.armor;
            }

            armor += Mathf.RoundToInt(armor * character.Attributes.GetBonusEffectTotal(BonusType.ArmorBoost));

            return armor;
        }

        // 计算总共击败的数量
        public int CountTotalKilled(CraftData craftable)
        {
            if (craftable != null)
                return character.SaveData.GetKillCount(craftable.id);
            return 0;
        }

        // 重置特定物体的击败计数
        public void ResetKillCount(CraftData craftable)
        {
            if (craftable != null)
                character.SaveData.ResetKillCount(craftable.id);
        }

        // 重置所有物体的击败计数
        public void ResetKillCount()
        {
            character.SaveData.ResetKillCount();
        }

        // 是否已死亡
        public bool IsDead()
        {
            return is_dead;
        }

        // 获取角色实例
        public PlayerCharacter GetCharacter()
        {
            return character;
        }
    }
}
