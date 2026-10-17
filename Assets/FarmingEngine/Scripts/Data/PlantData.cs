using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 植物数据文件[Z：存储不同生长状态的模型]
    /// </summary>

    [CreateAssetMenu(fileName = "PlantData", menuName = "FarmingEngine/PlantData", order = 5)]
    public class PlantData : CraftData
    {
        [Header("--- PlantData ------------------")]

        [Header("Prefab")]
        public GameObject plant_prefab; // 默认的植物预制体
        public GameObject[] growth_stage_prefabs; // 每个生长阶段的预制体数组（索引 0 是像幼苗一样的第一个阶段）

        private static List<PlantData> plant_data = new List<PlantData>();

        /// <summary>
        /// 获取指定生长阶段的预制体
        /// </summary>
        /// <param name="stage">生长阶段索引</param>
        /// <returns>对应阶段的预制体</returns>
        public GameObject GetStagePrefab(int stage)
        {
            if (stage >= 0 && stage < growth_stage_prefabs.Length)
                return growth_stage_prefabs[stage];
            return plant_prefab;
        }

        public static new void Load(string folder = "")
        {
            plant_data.Clear();
            plant_data.AddRange(Resources.LoadAll<PlantData>(folder));
        }

        public new static PlantData Get(string construction_id)
        {
            foreach (PlantData item in plant_data)
            {
                if (item.id == construction_id)
                    return item;
            }
            return null;
        }

        public new static List<PlantData> GetAll()
        {
            return plant_data;
        }
    }

}