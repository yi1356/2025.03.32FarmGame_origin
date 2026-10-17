using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace FarmingEngine
{
    [RequireComponent(typeof(UniqueID))]
    public class Zone : MonoBehaviour
    {
        [HideInInspector]
        public UnityAction<PlayerCharacter> onEnter;  // 进入区域时触发的事件
        [HideInInspector]
        public UnityAction<PlayerCharacter> onExit;   // 离开区域时触发的事件
        
        private Collider collide;                    // 碰撞体组件
        private Bounds bounds;                       // 区域边界
        private UniqueID unique_id;                  // 唯一标识组件

        private static List<Zone> zone_list = new List<Zone>(); // 区域列表

        private void Awake()
        {
            zone_list.Add(this);                    // 将当前区域添加到区域列表中
            unique_id = GetComponent<UniqueID>();   // 获取唯一标识组件
            collide = GetComponent<Collider>();     // 获取碰撞体组件
            if (collide != null)
                bounds = collide.bounds;            // 获取碰撞体的边界
        }
        
        private void OnDestroy()
        {
            zone_list.Remove(this);                // 当销毁时，从区域列表中移除
        }

        private void OnEnter(PlayerCharacter player)
        {
            onEnter?.Invoke(player);               // 触发进入区域事件
        }

        private void OnExit(PlayerCharacter player)
        {
            onExit?.Invoke(player);                // 触发离开区域事件
        }

        void OnTriggerEnter(Collider coll)
        {
            PlayerCharacter player = coll.GetComponent<PlayerCharacter>(); // 获取进入区域的玩家角色
            OnEnter(player);                      // 触发进入区域事件
        }

        void OnTriggerExit(Collider coll)
        {
            PlayerCharacter player = coll.GetComponent<PlayerCharacter>(); // 获取离开区域的玩家角色
            OnExit(player);                       // 触发离开区域事件
        }

        // 随机选择区域内的一个位置
        public Vector3 PickRandomPosition()
        {
            float x = Random.Range(bounds.min.x, bounds.max.x); // 在边界范围内随机选择x坐标
            float y = Random.Range(bounds.min.y, bounds.max.y); // 在边界范围内随机选择y坐标
            float z = Random.Range(bounds.min.z, bounds.max.z); // 在边界范围内随机选择z坐标
            return new Vector3(x, y, z);           // 返回随机位置向量
        }

        // 检查指定位置是否在区域内（考虑三维坐标）
        public bool IsInside(Vector3 position)
        {
            return (position.x > bounds.min.x && position.x < bounds.max.x 
                && position.y > bounds.min.y && position.y < bounds.max.y
                && position.z > bounds.min.z && position.z < bounds.max.z);
        }

        // 检查指定位置是否在区域内（仅考虑x和z坐标，忽略y坐标）
        public bool IsInsideXZ(Vector3 position)
        {
            return (position.x > bounds.min.x && position.z > bounds.min.z 
                && position.x < bounds.max.x && position.z < bounds.max.z);
        }

        // 获取离指定位置最近的区域
        public static Zone GetNearest(Vector3 pos, float range = 999f)
        {
            float min_dist = range;                // 最小距离初始化为指定范围
            Zone nearest = null;                   // 最近的区域初始化为空
            foreach (Zone zone in zone_list)        // 遍历所有区域
            {
                float dist = (pos - zone.transform.position).magnitude; // 计算到指定位置的距离
                if (dist < min_dist)                // 如果距离小于最小距离
                {
                    min_dist = dist;                // 更新最小距离
                    nearest = zone;                 // 更新最近的区域
                }
            }
            return nearest;                        // 返回最近的区域
        }

        // 根据唯一标识获取区域
        public static Zone Get(string uid)
        {
            foreach (Zone zone in zone_list)        // 遍历所有区域
            {
                if (zone.unique_id.unique_id == uid) // 如果找到指定唯一标识的区域
                {
                    return zone;                   // 返回该区域
                }
            }
            return null;                           // 如果未找到，返回空
        }

        // 获取所有区域列表
        public static List<Zone> GetAll()
        {
            return zone_list;                      // 返回所有区域列表
        }
    }
}
