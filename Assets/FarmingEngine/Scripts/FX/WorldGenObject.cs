using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine.WorldGen
{
    /// <summary>
    /// 用于世界生成的物体预制件的属性
    /// </summary>

    public enum WorldGenObjectType {

        Default = 0, // 默认类型
        AvoidEdge = 10, // 避开边缘的类型
        NearEdge = 12, // 靠近边缘的类型

    }

    public class WorldGenObject : MonoBehaviour
    {
        public float size = 1f; // 与其他物体之间的最小距离
        public float size_group = 1f; // 同一组中的其他物体之间的最小距离

        public WorldGenObjectType type; // 物体的类型
        public float edge_dist = 0f; // 与生物群系边缘的最小（或最大）距离

    }

}