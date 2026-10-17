using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 植物可以播种（从种子开始），并且可以收获它们的果实。它们还可以有多个生长阶段。
    /// </summary>

    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Buildable))]
    [RequireComponent(typeof(UniqueID))]
    [RequireComponent(typeof(Destructible))]
    public class Plant : Craftable
    {
        [Header("Plant")]
        public PlantData data; // 植物数据
        public int growth_stage = 0; // 生长阶段

        [Header("Time")]
        public TimeType time_type = TimeType.GameDays; // 生长时间类型（天或小时）
        public float grow_time = 8f; // 生长时间（游戏小时或游戏天）
        public bool grow_require_water = true; // 是否需要水来生长？
        public bool regrow_on_death; // 如果为真，植物将重置到阶段1而不是被销毁
        public float soil_range = 1f; // 水浸的土壤可以离植物多远

        [Header("Harvest")]
        public ItemData fruit; // 水果物品数据
        public float fruit_grow_time = 0f; // 生长水果的时间（游戏小时或游戏天）
        public bool fruit_require_water = true; // 是否需要水来生长果实？
        public Transform fruit_model; // 水果模型
        public bool death_on_harvest; // 收获后是否死亡

        [Header("FX")]
        public GameObject gather_fx; // 收获特效
        public AudioClip gather_audio; // 收获音效

        [HideInInspector]
        public bool was_spawned = false; // 如果为真，表示植物是通过制作或从保存文件中加载的

        private Selectable selectable;
        private Buildable buildable;
        private Destructible destruct;
        private UniqueID unique_id;
        private Soil soil; // 土壤对象

        private int nb_stages = 1; // 阶段数量
        private bool has_fruit = false; // 是否有果实
        private float update_timer = 0f; // 更新计时器

        private static List<Plant> plant_list = new List<Plant>(); // 植物列表

        protected override void Awake()
        {
            base.Awake();
            plant_list.Add(this); // 添加到植物列表
            selectable = GetComponent<Selectable>();
            buildable = GetComponent<Buildable>();
            destruct = GetComponent<Destructible>();
            unique_id = GetComponent<UniqueID>();
            selectable.onDestroy += OnDeath; // 销毁时的事件
            buildable.onBuild += OnBuild; // 建造时的事件

            if(data != null)
                nb_stages = Mathf.Max(data.growth_stage_prefabs.Length, 1); // 确定阶段数量
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            plant_list.Remove(this); // 从植物列表移除
        }

        void Start()
        {
            if (!was_spawned && PlayerData.Get().IsObjectRemoved(GetUID()))
            {
                Destroy(gameObject);
                return;
            }

            //Soil
            if (!buildable.IsBuilding())
                soil = Soil.GetNearest(transform.position, soil_range); // 获取最近的土壤对象

            //Fruit
            if (PlayerData.Get().HasCustomInt(GetSubUID("fruit")))
                has_fruit = PlayerData.Get().GetCustomInt(GetSubUID("fruit")) > 0; // 是否有果实

            //Grow time
            if (!PlayerData.Get().HasCustomFloat(GetSubUID("grow_time")))
                ResetGrowTime(); // 重置生长时间

            RefreshFruitModel(); // 刷新果实模型显示状态
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (buildable.IsBuilding())
                return;

            update_timer += Time.deltaTime;
            if (update_timer > 0.5f)
            {
                update_timer = 0f;
                SlowUpdate(); // 慢更新
            }
        }

        private void SlowUpdate()
        {
            if (!IsFullyGrown() && HasUID())
            {
                bool can_grow = !grow_require_water || HasWater(); // 是否可以生长
                if (can_grow && GrowTimeFinished()) // 生长时间是否已完成
                {
                    GrowPlant(); // 生长植物
                    return;
                }
            }

            if (!has_fruit && fruit != null && HasUID())
            {
                bool can_grow = !fruit_require_water || HasWater(); // 是否可以生长果实
                if (can_grow && FruitGrowTimeFinished()) // 果实生长时间是否已完成
                {
                    GrowFruit(); // 生长果实
                    return;
                }
            }

            //Auto water
            if (!HasWater()) // 如果没有水
            {
                if (TheGame.Get().IsWeather(WeatherEffect.Rain)) // 如果是雨天
                    Water(); // 浇水
                Sprinkler nearest = Sprinkler.GetNearestInRange(transform.position); // 获取最近的洒水器
                if (nearest != null)
                    Water(); // 浇水
            }
        }

        public void GrowPlant()
        {
            if (!IsFullyGrown())
            {
                GrowPlant(growth_stage + 1); // 生长植物到下一阶段
            }
        }

        public void GrowPlant(int grow_stage)
        {
            if (data != null && grow_stage >= 0 && grow_stage < nb_stages)
            {
                SowedPlantData sdata = PlayerData.Get().GetSowedPlant(GetUID());
                if (sdata == null)
                {
                    //移除这个植物并创建一个新的（这个可能已经在场景中）
                    if (!was_spawned)
                        PlayerData.Get().RemoveObject(GetUID()); // 移除唯一标识符
                    sdata = PlayerData.Get().AddPlant(data.id, SceneNav.GetCurrentScene(), transform.position, transform.rotation, grow_stage); // 添加新的植物到玩家数据
                }
                else
                {
                    //从数据中生长当前植物
                    PlayerData.Get().GrowPlant(GetUID(), grow_stage);
                }

                ResetGrowTime(); // 重置生长时间
                RemoveWater(); // 移除水
                plant_list.Remove(this); // 从列表中移除，以便生成新的植物

                Spawn(sdata.uid); // 生成新植物
                Destroy(gameObject); // 销毁当前植物
            }
        }

        public void GrowFruit()
        {
            if (fruit != null && !has_fruit)
            {
                has_fruit = true;
                PlayerData.Get().SetCustomInt(GetSubUID("fruit"), 1); // 设置果实为存在
                RemoveWater(); // 移除水
                RefreshFruitModel(); // 刷新果实模型显示状态
            }
        }

        public void Harvest(PlayerCharacter character)
        {
            if (fruit != null && has_fruit && character.Inventory.CanTakeItem(fruit, 1)) // 如果有果实且角色可以收获
            {
                GameObject source = fruit_model != null ? fruit_model.gameObject : gameObject;
                character.Inventory.GainItem(fruit, 1, source.transform.position); // 收获果实

                RemoveFruit(); // 移除果实

                if (death_on_harvest && destruct != null)
                    destruct.Kill(); // 如果收获后死亡，杀死植物

                TheAudio.Get().PlaySFX("plant", gather_audio); // 播放收获音效

                if (gather_fx != null)
                    Instantiate(gather_fx, transform.position, Quaternion.identity); // 播放收获特效
            }
        }

        public void RemoveFruit()
        {
            if (has_fruit)
            {
                has_fruit = false;
                ResetGrowTime(); // 重置生长时间
                PlayerData.Get().SetCustomInt(GetSubUID("fruit"), 0); // 设置果实为不存在
                RefreshFruitModel(); // 刷新果实模型显示状态
            }
        }

        public void Water()
        {
            if (!HasWater()) // 如果没有水
            {
                if (soil != null)
                    soil.Water(); // 浇水
                PlayerData.Get().SetCustomInt(GetSubUID("water"), 1); // 设置为有水状态
                ResetGrowTime(); // 重置生长时间
            }
        }

        public void RemoveWater()
        {
            if (HasWater()) // 如果有水
            {
                PlayerData.Get().SetCustomInt(GetSubUID("water"), 0); // 设置为无水状态
                if (soil != null)
                    soil.RemoveWater(); // 移除土壤的水
            }
        }

        private void RefreshFruitModel()
        {
            if (fruit_model != null && has_fruit != fruit_model.gameObject.activeSelf)
                fruit_model.gameObject.SetActive(has_fruit); // 根据果实状态设置果实模型的显示状态
        }

        private void ResetGrowTime()
        {
            if(time_type == TimeType.GameDays)
                PlayerData.Get().SetCustomFloat(GetSubUID("grow_time"), PlayerData.Get().day); // 设置生长时间为当前天数
            if(time_type == TimeType.GameHours)
                PlayerData.Get().SetCustomFloat(GetSubUID("grow_time"), PlayerData.Get().GetTotalTime()); // 设置生长时间为游戏总时间
        }

        private bool GrowTimeFinished()
        {
            float last_grow_time = PlayerData.Get().GetCustomFloat(GetSubUID("grow_time")); // 上次生长时间
            if (time_type == TimeType.GameDays && HasUID()) // 如果是按天计算且有唯一标识符
                return PlayerData.Get().day >= Mathf.RoundToInt(last_grow_time + grow_time); // 生长时间是否已结束
            if (time_type == TimeType.GameHours && HasUID()) // 如果是按小时计算且有唯一标识符
                return PlayerData.Get().GetTotalTime() > last_grow_time + grow_time; // 生长时间是否已结束
            return false;
        }

        private bool FruitGrowTimeFinished()
        {
            float last_grow_time = PlayerData.Get().GetCustomFloat(GetSubUID("grow_time")); // 上次生长时间
            if (time_type == TimeType.GameDays && HasUID()) // 如果是按天计算且有唯一标识符
                return PlayerData.Get().day >= Mathf.RoundToInt(last_grow_time + fruit_grow_time); // 果实生长时间是否已结束
            if (time_type == TimeType.GameHours && HasUID()) // 如果是按小时计算且有唯一标识符
                return PlayerData.Get().GetTotalTime() > last_grow_time + fruit_grow_time; // 果实生长时间是否已结束
            return false;
        }

        public void Kill()
        {
            destruct.Kill(); // 杀死植物
        }

        public void KillNoLoot()
        {
            destruct.KillNoLoot(); // 杀死植物，不生成战利品
        }

        private void OnBuild()
        {
            if (data != null)
            {
                SowedPlantData splant = PlayerData.Get().AddPlant(data.id, SceneNav.GetCurrentScene(), transform.position, transform.rotation, growth_stage); // 添加植物到玩家数据
                unique_id.unique_id = splant.uid; // 设置唯一标识符
                soil = Soil.GetNearest(transform.position, soil_range); // 获取最近的土壤对象
                ResetGrowTime(); // 重置生长时间
            }
        }

        private void OnDeath()
        {
            if (data != null)
            {
                foreach (PlayerCharacter character in PlayerCharacter.GetAll())
                    character.SaveData.AddKillCount(data.id); // 增加击杀计数
            }

            PlayerData.Get().RemovePlant(GetUID()); // 从玩家数据中移除植物
            if (!was_spawned)
                PlayerData.Get().RemoveObject(GetUID()); // 如果不是生成的，移除唯一标识符

            if (HasFruit())
                Item.Create(fruit, transform.position, 1); // 生成果实物品

            if (data != null && regrow_on_death)
            {
                SowedPlantData sdata = PlayerData.Get().GetSowedPlant(GetUID()); // 获取种植的植物数据
                Create(data, transform.position, transform.rotation, 0); // 重新生成植物
            }
        }

        public bool HasFruit()
        {
            return has_fruit; // 是否有果实
        }

        public bool HasWater()
        {
            bool wplant = PlayerData.Get().GetCustomInt(GetSubUID("water")) > 0; // 植物是否有水
            bool wsoil = soil != null ? soil.IsWatered() : false; // 土壤是否有水
            return wplant || wsoil; // 是否有水
        }

        public bool IsFullyGrown()
        {
            return (growth_stage + 1) >= nb_stages; // 是否完全成长
        }

        public bool IsBuilt()
        {
            return !IsDead() && !buildable.IsBuilding(); // 是否已建造完成
        }

        public bool IsDead()
        {
            return destruct.IsDead(); // 是否已死亡
        }

        public float GetGrowTime()
        {
            return PlayerData.Get().GetCustomFloat(GetSubUID("grow_time")); // 获取生长时间
        }

        public bool HasUID()
        {
            return !string.IsNullOrEmpty(unique_id.unique_id); // 是否有唯一标识符
        }

        public string GetUID()
        {
            return unique_id.unique_id; // 获取唯一标识符
        }

        public string GetSubUID(string tag)
        {
            return unique_id.GetSubUID(tag); // 获取子唯一标识符
        }

        public bool HasGroup(GroupData group)
        {
            if (data != null)
                return data.HasGroup(group) || selectable.HasGroup(group); // 是否属于某个组
            return selectable.HasGroup(group); // 是否属于某个组
        }

        public Selectable GetSelectable()
        {
            return selectable; // 获取可选择对象
        }

        public Destructible GetDestructible()
        {
            return destruct; // 获取可破坏对象
        }

        public Buildable GetBuildable()
        {
            return buildable; // 获取可建造对象
        }

        public SowedPlantData SaveData
        {
            get { return PlayerData.Get().GetSowedPlant(GetUID()); }  // 获取保存的数据（如果没有播种或生成，可能为空）
        }

        public static new Plant GetNearest(Vector3 pos, float range = 999f)
        {
            Plant nearest = null;
            float min_dist = range;
            foreach (Plant plant in plant_list)
            {
                float dist = (plant.transform.position - pos).magnitude; // 计算与指定位置的距离
                if (dist < min_dist && plant.IsBuilt()) // 如果距离更近且已建造完成
                {
                    min_dist = dist;
                    nearest = plant; // 更新最近的植物
                }
            }
            return nearest; // 返回最近的植物
        }

        public static int CountInRange(Vector3 pos, float range)
        {
            int count = 0;
            foreach (Plant plant in GetAll())
            {
                float dist = (plant.transform.position - pos).magnitude; // 计算与指定位置的距离
                if (dist < range && plant.IsBuilt()) // 如果在范围内且已建造完成
                    count++; // 计数加一
            }
            return count; // 返回计数
        }

        public static int CountInRange(PlantData data, Vector3 pos, float range)
        {
            int count = 0;
            foreach (Plant plant in GetAll())
            {
                if (plant.data == data && plant.IsBuilt()) // 如果是指定植物数据并且已建造完成
                {
                    float dist = (plant.transform.position - pos).magnitude; // 计算与指定位置的距离
                    if (dist < range)
                        count++; // 计数加一
                }
            }
            return count; // 返回计数
        }

        public static Plant GetByUID(string uid)
        {
            if (!string.IsNullOrEmpty(uid))
            {
                foreach (Plant plant in plant_list)
                {
                    if (plant.GetUID() == uid)
                        return plant; // 返回指定UID的植物
                }
            }
            return null; // 没有找到返回空
        }

        public static List<Plant> GetAllOf(PlantData data)
        {
            List<Plant> valid_list = new List<Plant>();
            foreach (Plant plant in plant_list)
            {
                if (plant.data == data)
                    valid_list.Add(plant); // 添加符合指定植物数据的植物到列表
            }
            return valid_list; // 返回列表
        }

        public static new List<Plant> GetAll()
        {
            return plant_list; // 返回所有植物列表
        }

        // 生成已保存的植物实例（例如加载后）
        public static Plant Spawn(string uid, Transform parent = null)
        {
            SowedPlantData sdata = PlayerData.Get().GetSowedPlant(uid); // 获取已播种的植物数据
            if (sdata != null && sdata.scene == SceneNav.GetCurrentScene()) // 如果有数据并且场景一致
            {
                PlantData pdata = PlantData.Get(sdata.plant_id); // 获取植物数据
                if (pdata != null)
                {
                    GameObject prefab = pdata.GetStagePrefab(sdata.growth_stage); // 获取指定生长阶段的预制体
                    GameObject build = Instantiate(prefab, sdata.pos, sdata.rot); // 实例化植物对象
                    build.transform.parent = parent; // 设置父对象

                    Plant plant = build.GetComponent<Plant>(); // 获取植物组件
                    plant.data = pdata; // 设置植物数据
                    plant.growth_stage = sdata.growth_stage; // 设置生长阶段
                    plant.was_spawned = true; // 设置为已生成
                    plant.unique_id.unique_id = uid; // 设置唯一标识符
                    return plant; // 返回生成的植物
                }
            }
            return null; // 未找到返回空
        }

        // 创建玩家可以放置的全新实例，将在调用FinishBuild()后保存到保存文件中，-1 = 最大阶段
        public static Plant CreateBuildMode(PlantData data, Vector3 pos, int stage)
        {
            GameObject prefab = data.GetStagePrefab(stage); // 获取指定阶段的预制体
            GameObject build = Instantiate(prefab, pos, prefab.transform.rotation); // 实例化植物对象
            Plant plant = build.GetComponent<Plant>(); // 获取植物组件
            plant.data = data; // 设置植物数据
            plant.was_spawned = true; // 设置为已生成

            if(stage >= 0 && stage < data.growth_stage_prefabs.Length)
                plant.growth_stage = stage; // 设置生长阶段
            
            return plant; // 返回生成的植物
        }

        // 创建全新实例，并添加到保存文件中，已经放置
        public static Plant Create(PlantData data, Vector3 pos, int stage)
        {
            Plant plant = CreateBuildMode(data, pos, stage); // 创建放置模式的植物
            plant.buildable.FinishBuild(); // 完成建造
            return plant; // 返回生成的植物
        }

        public static Plant Create(PlantData data, Vector3 pos, Quaternion rot, int stage)
        {
            Plant plant = CreateBuildMode(data, pos, stage); // 创建放置模式的植物
            plant.transform.rotation = rot; // 设置旋转
            plant.buildable.FinishBuild(); // 完成建造
            return plant; // 返回生成的植物
        }
    }

}
