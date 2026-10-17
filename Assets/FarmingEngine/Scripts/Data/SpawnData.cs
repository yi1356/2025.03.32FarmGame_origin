using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// SData是此引擎的基本可编写对象数据
    /// </summary>
    [System.Serializable]
    public abstract class SData : ScriptableObject { }

    /// <summary>
    /// IdData为类添加了一个ID
    /// </summary>
    [System.Serializable]
    public abstract class IdData : SData { public string id; }

    /// <summary>
    /// 这是一个通用的生成数据，用于生成非构造、物品、植物或角色的任何通用预制体(Z 就是生成物体，比如砍树得到蜂巢[暂时还没有找到用处])
    /// Spawn()在加载过程中自动调用以重新生成保存的所有内容，使用Create()来创建新对象
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnData", menuName = "FarmingEngine/SpawnData", order = 5)]
    public class SpawnData : IdData
    {
        public GameObject prefab; // 预制体对象

        private static List<SpawnData> spawn_data = new List<SpawnData>(); // 存储所有SpawnData对象的静态列表

        /// <summary>
        /// 加载指定文件夹中的SpawnData对象（Z 发现在调用时folder = ""，这就相当于在Resources文件夹中直接搜索SpawnData类Scripe）
        /// </summary>
        /// <param name="folder">指定的文件夹名称</param>
        public static void Load(string folder = "")
        {
            spawn_data.Clear();
            spawn_data.AddRange(Resources.LoadAll<SpawnData>(folder));//Z 这里相当于在在Resources文件夹中筛选SpawnData并，加到spawn_data中（这里只有一个SpawnData）
        }

        /// <summary>
        /// 根据ID获取SpawnData对象
        /// </summary>
        /// <param name="id">要获取的SpawnData的ID</param>
        /// <returns>匹配的SpawnData对象，如果未找到则返回null</returns>
        public static SpawnData Get(string id)
        {
            foreach (SpawnData data in spawn_data)
            {
                if (data.id == id)
                    return data;
            }
            return null;
        }

        /// <summary>
        /// 获取所有SpawnData对象的列表
        /// </summary>
        /// <returns>包含所有SpawnData对象的列表</returns>
        public static List<SpawnData> GetAll()
        {
            return spawn_data;
        }
    }
}