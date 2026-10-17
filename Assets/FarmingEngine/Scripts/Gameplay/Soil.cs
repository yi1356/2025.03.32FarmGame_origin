using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 可以浇水的土壤
    /// </summary>

    [RequireComponent(typeof(UniqueID))]
    public class Soil : MonoBehaviour
    {
        public MeshRenderer mesh; // 土壤的网格渲染器
        public Material watered_mat; // 浇水后的材质

        private UniqueID unique_id; // 唯一标识组件
        private Material original_mat; // 原始材质
        private bool watered = false; // 是否已浇水
        private float update_timer = 0f; // 更新计时器

        private static List<Soil> soil_list = new List<Soil>(); // 土壤列表

        void Awake()
        {
            soil_list.Add(this);
            unique_id = GetComponent<UniqueID>(); // 获取唯一标识组件
            if(mesh != null)
                original_mat = mesh.material; // 获取原始材质
        }

        private void OnDestroy()
        {
            soil_list.Remove(this);
        }

        private void Update()
        {
            bool now_watered = IsWatered(); // 当前是否已浇水
            // 如果状态改变且有网格渲染器和浇水后的材质
            if (now_watered != watered && mesh != null && watered_mat != null)
            {
                mesh.material = now_watered ? watered_mat : original_mat; // 切换材质
            }
            watered = now_watered; // 更新浇水状态

            update_timer += Time.deltaTime;
            if (update_timer > 0.5f)
            {
                update_timer = 0f;
                SlowUpdate(); // 慢更新
            }
        }

        private void SlowUpdate()
        {
            // 自动浇水
            if (!watered)
            {
                if (TheGame.Get().IsWeather(WeatherEffect.Rain)) // 如果是下雨天
                    Water(); // 浇水
                Sprinkler nearest = Sprinkler.GetNearestInRange(transform.position); // 获取最近的洒水器
                if (nearest != null)
                    Water(); // 浇水
            }
        }

        // 浇水
        public void Water()
        {
            PlayerData.Get().SetCustomInt(GetSubUID("water"), 1); // 设置玩家数据，标记为浇水状态
        }

        // 移除水
        public void RemoveWater()
        {
            PlayerData.Get().SetCustomInt(GetSubUID("water"), 0); // 设置玩家数据，移除浇水状态
        }
		
		// 浇水植物
		public void WaterPlant()
		{
			Plant plant = Plant.GetNearest(transform.position, 1f); // 获取最近的植物
			if(plant != null)
				plant.Water(); // 植物浇水
		}

        // 判断是否已浇水
        public bool IsWatered()
        {
            return PlayerData.Get().GetCustomInt(GetSubUID("water")) > 0; // 获取玩家数据，判断是否浇水
        }

        // 获取子UID
        public string GetSubUID(string tag)
        {
            return unique_id.GetSubUID(tag); // 获取唯一标识的子标识
        }

        // 获取最近的土壤
        public static Soil GetNearest(Vector3 pos, float range=999f)
        {
            float min_dist = range;
            Soil nearest = null;
            foreach (Soil soil in soil_list)
            {
                float dist = (pos - soil.transform.position).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = soil;
                }
            }
            return nearest;
        }

        // 获取所有土壤
        public static List<Soil> GetAll(){
            return soil_list; // 返回所有土壤列表
        }
    }
}
