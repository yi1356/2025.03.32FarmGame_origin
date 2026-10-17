using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 建筑是可以由玩家放置在地图上的对象（通过制作或使用物品）
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Buildable))]
    [RequireComponent(typeof(UniqueID))]
    public class Construction : Craftable
    {
        [Header("Construction")]
        public ConstructionData data; // 建筑的数据

        [HideInInspector]
        public bool was_spawned = false; // 如果为真，表示它是通过制作或从保存文件加载的

        private Selectable selectable; // 可选的，可以为空
        private Destructible destruct; // 可破坏的，可以为空
        private Buildable buildable; // 可建造的
        private UniqueID unique_id; // 唯一标识符

        private static List<Construction> construct_list = new List<Construction>(); // 建筑物列表

        protected override void Awake()
        {
            base.Awake();
            construct_list.Add(this);
            selectable = GetComponent<Selectable>();
            buildable = GetComponent<Buildable>();
            destruct = GetComponent<Destructible>();
            unique_id = GetComponent<UniqueID>();

            buildable.onBuild += OnBuild;

            if (selectable != null)
            {
                selectable.onDestroy += OnDeath;
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            construct_list.Remove(this);
        }

        void Start()
        {
            // 如果不是通过生成或加载移除的对象，销毁它
            if (!was_spawned && PlayerData.Get().IsObjectRemoved(GetUID()))
            {
                Destroy(gameObject);
                return;
            }
        }

        // 摧毁建筑
        public void Kill()
        {
            if (destruct != null)
                destruct.Kill();
            else if (selectable != null)
                selectable.Destroy();
            else
                Destroy(gameObject);
        }

        // 当建造完成时调用
        private void OnBuild()
        {
            if (data != null)
            {
                // 将建筑数据添加到玩家数据中
                BuiltConstructionData cdata = PlayerData.Get().AddConstruction(data.id, SceneNav.GetCurrentScene(), transform.position, transform.rotation, data.durability);
                unique_id.unique_id = cdata.uid; // 设置唯一标识符
            }
        }

        // 当建筑被摧毁时调用
        private void OnDeath()
        {
            if (data != null)
            {
                // 增加杀敌数到所有玩家角色的保存数据中
                foreach (PlayerCharacter character in PlayerCharacter.GetAll())
                    character.SaveData.AddKillCount(data.id); // 增加杀敌数
            }

            // 从玩家数据中移除建筑数据
            PlayerData.Get().RemoveConstruction(GetUID());

            // 如果不是通过生成或加载移除的对象，还需从玩家数据中移除对象
            if (!was_spawned)
                PlayerData.Get().RemoveObject(GetUID());
        }

        // 是否已建造完成
        public bool IsBuilt()
        {
            return !IsDead() && !buildable.IsBuilding();
        }

        // 是否已摧毁
        public bool IsDead()
        {
            if (destruct)
                return destruct.IsDead();
            return false;
        }

        // 是否有唯一标识符
        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id);
        }

        // 获取唯一标识符
        public string GetUID()
        {
            return unique_id.unique_id;
        }

        // 是否拥有特定组
        public bool HasGroup(GroupData group)
        {
            if (data != null)
                return data.HasGroup(group) || selectable.HasGroup(group);
            return selectable.HasGroup(group);
        }

        // 获取可选的组件
        public Selectable GetSelectable()
        {
            return selectable; // 可能为空
        }

        // 获取可破坏的组件
        public Destructible GetDestructible()
        {
            return destruct; // 可能为空
        }

        // 获取可建造的组件
        public Buildable GetBuildable()
        {
            return buildable;
        }

        // 获取保存的建筑数据
        public BuiltConstructionData SaveData
        {
            get { return PlayerData.Get().GetConstructed(GetUID()); }  // 如果未建造或生成，则可能为空
        }

        // 获取最近的建筑
        public static new Construction GetNearest(Vector3 pos, float range = 999f)
        {
            Construction nearest = null;
            float min_dist = range;
            foreach (Construction construction in construct_list)
            {
                float dist = (construction.transform.position - pos).magnitude;
                if (dist < min_dist && construction.IsBuilt())
                {
                    min_dist = dist;
                    nearest = construction;
                }
            }
            return nearest;
        }

        // 在指定范围内计算建筑数量
        public static int CountInRange(Vector3 pos, float range)
        {
            int count = 0;
            foreach (Construction construct in GetAll())
            {
                float dist = (construct.transform.position - pos).magnitude;
                if (dist < range && construct.IsBuilt())
                    count++;
            }
            return count;
        }

        // 在指定范围内计算特定建筑数据的数量
        public static int CountInRange(ConstructionData data, Vector3 pos, float range)
        {
            int count = 0;
            foreach (Construction construct in GetAll())
            {
                if (construct.data == data && construct.IsBuilt())
                {
                    float dist = (construct.transform.position - pos).magnitude;
                    if (dist < range)
                        count++;
                }
            }
            return count;
        }

        // 根据唯一标识符获取建筑
        public static Construction GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Construction construct in construct_list)
                {
                    if (construct.GetUID() == uid)
                        return construct;
                }
            }
            return null;
        }

        // 获取所有特定建筑数据的建筑列表
        public static List<Construction> GetAllOf(ConstructionData data)
        {
            List<Construction> valid_list = new List<Construction>();
            foreach (Construction construct in construct_list)
            {
                if (construct.data == data)
                    valid_list.Add(construct);
            }
            return valid_list;
        }

        // 获取所有建筑列表
        public static new List<Construction> GetAll()
        {
            return construct_list;
        }

        // 生成一个已存在于保存文件中的建筑（例如在加载后）
        public static Construction Spawn(string uid, Transform parent = null)
        {
            BuiltConstructionData bdata = PlayerData.Get().GetConstructed(uid);
            if (bdata != null && bdata.scene == SceneNav.GetCurrentScene())
            {
                ConstructionData cdata = ConstructionData.Get(bdata.construction_id);
                if (cdata != null)
                {
                    // 实例化建筑预制体
                    GameObject build = Instantiate(cdata.construction_prefab, bdata.pos, bdata.rot);
                    build.transform.parent = parent; // 设置父级对象

                    // 获取建筑组件
                    Construction construct = build.GetComponent<Construction>();
                    construct.data = cdata; // 设置建筑数据
                    construct.was_spawned = true; // 标记为已生成
                    construct.unique_id.unique_id = uid; // 设置唯一标识符
                    return construct; // 返回生成的建筑对象
                }
            }
            return null; // 返回空，生成失败
        }

        // 创建一个全新的建筑，在玩家建造后才会添加到保存文件中
        public static Construction CreateBuildMode(ConstructionData data, Vector3 pos)
        {
            // 实例化建筑预制体
            GameObject build = Instantiate(data.construction_prefab, pos, data.construction_prefab.transform.rotation);
            
            // 获取建筑组件
            Construction construct = build.GetComponent<Construction>();
            construct.data = data; // 设置建筑数据
            construct.was_spawned = true; // 标记为已生成
            return construct; // 返回生成的建筑对象
        }

        // 创建一个全新的建筑，并立即添加到保存文件中
        public static Construction Create(ConstructionData data, Vector3 pos)
        {
            // 使用默认旋转创建建筑
            Construction construct = CreateBuildMode(data, pos);
            construct.buildable.FinishBuild(); // 完成建造
            return construct; // 返回生成的建筑对象
        }

        // 创建一个全新的建筑，并立即添加到保存文件中
        public static Construction Create(ConstructionData data, Vector3 pos, Quaternion rot)
        {
            // 使用指定旋转创建建筑
            Construction construct = CreateBuildMode(data, pos);
            construct.transform.rotation = rot; // 设置旋转
            construct.buildable.FinishBuild(); // 完成建造
            return construct; // 返回生成的建筑对象
        }
    }
}
