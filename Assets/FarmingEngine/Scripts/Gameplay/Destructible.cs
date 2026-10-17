using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    public enum AttackTeam
    {
        Neutral=0, // 中立：任何人都可以攻击，但不会自动受到攻击，适用于资源
        Ally=10, // 盟友：会被野生动物自动攻击，玩家除非拥有所需物品否则无法攻击
        Enemy=20, // 敌人：会被盟友宠物和野生动物自动攻击（除非在同一队伍组内），可以被任何人攻击
        CantAttack =50, // 无法攻击
    }

    /// <summary>
    /// 可被销毁的对象，具有HP并且可以受到玩家或动物的伤害。（Z 也会记录弓箭的伤害）并生成战利品
    /// 在被销毁或杀死时通常会生成战利品物品。
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(UniqueID))]
    public class Destructible : MonoBehaviour
    {
        [Header("Stats")]
        public int hp = 100; // 生命值
        public int armor = 0; // 护甲：每次攻击减少的伤害值
        public float hp_regen = 0f; // 每游戏小时的生命值恢复量

        [Header("Targeting")]
        public AttackTeam target_team; // 攻击目标队伍类型
        public GroupData target_group; // 同一组的敌人不会互相攻击
        public GroupData required_item; // 攻击所需的物品（只有玩家受此影响）
        public bool attack_melee_only = true; // 如果为true，则只能用近战武器攻击此对象
        public float hit_range = 1f; // 可以攻击的范围

        [Header("Loot")]
        public int xp = 0; // 经验值
        public string xp_type; // 经验类型
        public SData[] loots; // 战利品物品数据

        [Header("FX")]
        public bool shake_on_hit = true; // 被击中时是否产生震动动画
        public float destroy_delay = 0f; // 销毁延迟时间，用于在对象消失之前播放死亡动画
        public GameObject attack_center; // 用于投射物的攻击中心点，因为大多数对象的枢轴点直接在地面上，有时我们不希望投射物瞄准枢轴点而是瞄准这个位置

        public GameObject hp_bar; // 生命值条的预制件
        public GameObject hit_fx; // 被击中时的特效预制件
        public GameObject death_fx; // 死亡时的特效预制件
        public AudioClip hit_sound; // 被击中时的音效
        public AudioClip death_sound; // 死亡时的音效

        // 事件
        public UnityAction<Destructible> onDamagedBy; // 受到伤害事件（由谁造成的伤害）
        public UnityAction<PlayerCharacter> onDamagedByPlayer; // 受到玩家伤害事件
        public UnityAction onDamaged; // 受到伤害事件
        public UnityAction onDeath; // 死亡事件

        private bool hit_by_player = false; // 是否被玩家击中过
        private bool dead = false; // 是否已死亡

        private Selectable select; // 可选择组件
        private Collider[] colliders; // 所有碰撞器
        private UniqueID unique_id; // 唯一ID
        private Vector3 shake_center; // 震动中心点
        private Vector3 shake_vector = Vector3.zero; // 震动向量
        private bool is_shaking = false; // 是否正在震动
        private float shake_timer = 0f; // 震动计时器
        private float shake_intensity = 1f; // 震动强度
        private int max_hp; // 最大生命值
        private float hp_regen_val; // 生命值恢复值
        private HPBar hbar = null; // 生命值条

        void Awake()
        {
            shake_center = transform.position;
            unique_id = GetComponent<UniqueID>();
            select = GetComponent<Selectable>();
            colliders = GetComponentsInChildren<Collider>();
            max_hp = hp; // 记录初始生命值
        }

        private void Start()
        {
            // 如果对象在玩家数据中被移除，则销毁对象
            if (PlayerData.Get().IsObjectRemoved(GetUID()))
            {
                Destroy(gameObject);
                return;
            }

            // 如果对象有唯一ID并且在玩家数据中有自定义整数，则设置生命值
            if (HasUID() && PlayerData.Get().HasCustomInt(GetHpUID()))
            {
                hp = PlayerData.Get().GetCustomInt(GetHpUID());
            }
        }

        void Update()
        {
            // 震动效果
            if (is_shaking)
            {
                shake_timer -= Time.deltaTime;

                if (shake_timer > 0f)
                {
                    shake_vector = new Vector3(Mathf.Cos(shake_timer * Mathf.PI * 16f) * 0.02f, 0f, Mathf.Sin(shake_timer * Mathf.PI * 8f) * 0.01f);
                    transform.position += shake_vector * shake_intensity;
                }
                else if (shake_timer > -0.5f)
                {
                    transform.position = Vector3.Lerp(transform.position, shake_center, 4f * Time.deltaTime);
                }
                else
                {
                    is_shaking = false;
                }
            }

            // 生成生命值条
            if (hp > 0 && hp < max_hp && hbar == null && hp_bar != null)
            {
                GameObject hp_obj = Instantiate(hp_bar, transform);
                hbar = hp_obj.GetComponent<HPBar>();
                hbar.target = this;
            }

            // 生命值恢复
            if (!dead && hp_regen > 0.01f && hp < max_hp)
            {
                float game_speed = TheGame.Get().GetGameTimeSpeedPerSec();
                hp_regen_val += game_speed * hp_regen * Time.deltaTime;
                if (hp_regen_val >= 1f)
                {
                    hp_regen_val -= 1f;
                    hp += 1;
                }
            }
        }

        // 从角色处接收伤害（Z  感觉这个就是记录下攻击自己的角色（比如别的动物，弓箭等））
        public void TakeDamage(Destructible attacker, int damage)
        {
            if (!dead)
            {
                ApplyDamage(damage);

                onDamagedBy?.Invoke(attacker);

                if (hp <= 0)
                    Kill();
            }
        }


        /// <summary>
        /// Z：从玩家处接收伤害（Z 记录是玩家造成的伤害）
        /// </summary>
        /// <param name="attacker"></param>
        /// <param name="damage"></param>
        public void TakeDamage(PlayerCharacter attacker, int damage)
        {
            if (!dead)
            {
                ApplyDamage(damage);

                hit_by_player = true;
                onDamagedByPlayer?.Invoke(attacker);

                if (hp <= 0)
                    Kill();
            }
        }

        // 没有来源的伤害（例如陷阱）
        public void TakeDamage(int damage)
        {
            if (!dead)
            {
                ApplyDamage(damage);

                if (hp <= 0)
                    Kill();
            }
        }

        // 对可被销毁对象造成伤害，如果HP降至0则将其杀死
        private void ApplyDamage(int damage)
        {
            if (!dead)
            {
                int adamage = Mathf.Max(damage - armor, 1);
                hp -= adamage;

                PlayerData.Get().SetCustomInt(GetHpUID(), hp);

                if (shake_on_hit)
                    ShakeFX();

                if (select.IsActive() && select.IsNearCamera(20f))
                {
                    if (hit_fx != null)
                        Instantiate(hit_fx, transform.position, Quaternion.identity);

                    TheAudio.Get().PlaySFX("destruct", hit_sound);
                }

                onDamaged?.Invoke();
            }
        }

        // 恢复生命值
        public void Heal(int value)
        {
            if (!dead)
            {
                hp += value;
                hp = Mathf.Min(hp, max_hp);

                PlayerData.Get().SetCustomInt(GetHpUID(), hp);
            }
        }

        // 杀死可销毁对象
        public void Kill()
        {
            if (!dead)
            {
                SpawnLoots();
                GiveXPLoot();
                DropStorage();
                KillFX();
                KillNoLoot();
            }
        }

        // 杀死可销毁对象，不生成战利品
        public void KillNoLoot()
        {
            if (!dead)
            {
                dead = true;
                hp = 0;

                foreach (Collider collide in colliders)
                    collide.enabled = false;

                PlayerData.Get().RemoveObject(GetUID()); // 如果对象在初始场景中，则移除对象
                PlayerData.Get().RemoveSpawnedObject(GetUID()); // 如果对象是被生成的，则移除对象
                PlayerData.Get().RemoveCustomInt(GetHpUID()); // 移除HP的自定义值

                if (onDeath != null)
                    onDeath.Invoke();

                select.Destroy(destroy_delay);
            }
        }

        // 播放杀死效果
        private void KillFX()
        {
            // 特效
            if (select.IsActive() && select.IsNearCamera(20f))
            {
                if (death_fx != null)
                    Instantiate(death_fx, transform.position, Quaternion.identity);

                TheAudio.Get().PlaySFX("destruct", death_sound);
            }
        }

        // 给予经验值战利品
        public void GiveXPLoot()
        {
            if (!hit_by_player)
                return; // 如果没有受到玩家至少一次攻击，则不给予任何经验值

            foreach (PlayerCharacter player in PlayerCharacter.GetAll())
            {
                float range = (player.transform.position - transform.position).magnitude;
                if (range < 20f) // 20可以作为参数添加
                    player.Attributes.GainXP(xp_type, xp);
            }
        }

        // 丢弃存储物品
        public void DropStorage()
        {
            // 丢弃存储的物品
            InventoryData sdata = InventoryData.Get(InventoryType.Storage, GetUID());
            if (sdata != null)
            {
                foreach (KeyValuePair<int, InventoryItemData> item in sdata.items)
                {
                    ItemData idata = ItemData.Get(item.Value.item_id);
                    if (idata != null && item.Value.quantity > 0)
                    {
                        Item.Create(idata, GetLootRandomPos(), item.Value.quantity, item.Value.durability, item.Value.uid);
                    }
                }
            }
        }

        // 生成战利品
        public void SpawnLoots()
        {
            foreach (SData item in loots)
            {
                SpawnLoot(item);
            }
        }

        // 生成特定战利品
        public void SpawnLoot(SData item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return;

            Vector3 pos = GetLootRandomPos();
            if (item is ItemData)
            {
                ItemData aitem = (ItemData)item;
                Item.Create(aitem, pos, quantity);
            }
            if (item is ConstructionData)
            {
                ConstructionData construct_data = (ConstructionData)item;
                Construction.Create(construct_data, pos);
            }
            if (item is PlantData)
            {
                PlantData plant_data = (PlantData)item;
                Plant.Create(plant_data, pos, 0);
            }
            if (item is SpawnData)
            {
                SpawnData spawn_data = (SpawnData)item;
                Spawnable.Create(spawn_data, pos);
            }
            if (item is LootData)
            {
                LootData loot = (LootData)item;
                if (Random.value <= loot.probability)
                {
                    SpawnLoot(loot.item, loot.quantity);
                }
            }
        }

        // 获取随机战利品位置
        private Vector3 GetLootRandomPos()
        {
            float radius = Random.Range(0.5f, 1f);
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            return transform.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
        }

        // 延迟杀死对象（在攻击者进行动画攻击之前有用）
        public void KillIn(float delay)
        {
            StartCoroutine(KillInRun(delay));
        }

        private IEnumerator KillInRun(float delay)
        {
            yield return new WaitForSeconds(delay);
            Kill();
        }

        // 重置对象状态
        public void Reset()
        {
            hit_by_player = false;
            dead = false;
            hp = max_hp;

            foreach (Collider collide in colliders)
                collide.enabled = true;
        }

        // 震动效果
        public void ShakeFX(float intensity = 1f, float duration = 0.2f)
        {
            is_shaking = true;
            shake_center = transform.position;
            shake_intensity = intensity;
            shake_timer = duration;
        }

        // 是否有唯一ID
        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id);
        }

        // 获取唯一ID
        public string GetUID()
        {
            return unique_id.unique_id;
        }

        // 获取HP的唯一ID
        public string GetHpUID()
        {
            if (HasUID())
                return unique_id.unique_id + "_hp";
            return "";
        }

        // 是否已死亡
        public bool IsDead()
        {
            return dead;
        }

        // 获取攻击中心位置
        public Vector3 GetCenter()
        {
            if (attack_center != null)
                return attack_center.transform.position;
            return transform.position + Vector3.up * 0.1f; // 稍高于地面
        }

       
        /// <summary>
        /// Z：是否可以被攻击（根据是否已经死亡等条件）
        /// </summary>
        /// <returns></returns>
        public bool CanBeAttacked()
        {
            return target_team != AttackTeam.CantAttack && !dead;
        }

        // 是否可以远程攻击
        public bool CanAttackRanged()
        {
            return CanBeAttacked() && !attack_melee_only;
        }
        
        public int GetMaxHP()
        {
            return max_hp;
        }

        public Selectable Selectable
        {
            get { return select; }
        }


        // 获取最近的自动攻击目标（玩家）
        public static Destructible GetNearestAutoAttack(PlayerCharacter character, Vector3 pos, float range = 999f)
        {
            Destructible nearest = null;
            float min_dist = range;
            foreach (Selectable selectable in Selectable.GetAllActive()) // 仅循环活动的可选择对象以优化性能
            {
                Destructible destruct = selectable.Destructible;
                if (destruct != null && character.Combat.CanAutoAttack(destruct))
                {
                    float dist = (destruct.transform.position - pos).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = destruct;
                    }
                }
            }
            return nearest;
        }

        // 获取最近的攻击目标（指定队伍）
        public static Destructible GetNearestAttack(AttackTeam team, Vector3 pos, float range = 999f)
        {
            Destructible nearest = null;
            float min_dist = range;
            foreach (Selectable selectable in Selectable.GetAllActive()) // 仅循环活动的可选择对象以优化性能
            {
                Destructible destruct = selectable.Destructible;
                if (destruct != null && selectable.IsActive() && destruct.CanBeAttacked() && destruct.target_team == team)
                {
                    float dist = (destruct.transform.position - pos).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = destruct;
                    }
                }
            }
            return nearest;
        }

        // 获取最近的可选择的可销毁对象
        public static Destructible GetNearest(Vector3 pos, float range = 999f)
        {
            Destructible nearest = null;
            float min_dist = range;
            foreach (Selectable selectable in Selectable.GetAllActive()) // 仅循环活动的可选择对象以优化性能
            {
                Destructible destruct = selectable.Destructible;
                if (destruct != null && selectable.IsActive())
                {
                    float dist = (destruct.transform.position - pos).magnitude;
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        nearest = destruct;
                    }
                }
            }
            return nearest;
        }
    }
}
