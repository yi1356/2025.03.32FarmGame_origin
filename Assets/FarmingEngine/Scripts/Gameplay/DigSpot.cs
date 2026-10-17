using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 可以用铲子挖掘的点，将从可销毁对象中获得战利品
    /// </summary>

    [RequireComponent(typeof(Destructible))]
    public class DigSpot : MonoBehaviour
    {
        private Destructible destruct; // 可销毁对象引用

        private static List<DigSpot> dig_list = new List<DigSpot>(); // 挖掘点列表

        void Awake()
        {
            dig_list.Add(this); // 添加到挖掘点列表
            destruct = GetComponent<Destructible>(); // 获取可销毁对象组件
        }

        void OnDestroy()
        {
            dig_list.Remove(this); // 从挖掘点列表移除
        }

        public void Dig()
        {
            destruct.Kill(); // 挖掘操作，销毁可销毁对象
        }

        public static DigSpot GetNearest(Vector3 pos, float range = 999f)
        {
            DigSpot nearest = null;
            float min_dist = range;
            foreach (DigSpot spot in dig_list)
            {
                float dist = (spot.transform.position - pos).magnitude;
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = spot;
                }
            }
            return nearest;
        }

        public static List<DigSpot> GetAll()
        {
            return dig_list; // 获取所有挖掘点列表
        }
    }
}