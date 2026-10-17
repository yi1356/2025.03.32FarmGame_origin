using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 火坑可以通过木材或其他材料添加燃料。燃料耗尽前会保持点火状态。
    /// </summary>
    [RequireComponent(typeof(Selectable))]
    [RequireComponent(typeof(Construction))]
    public class Firepit : MonoBehaviour
    {
        public GroupData fire_group; // 火焰组，用于选择火坑对象
        public GameObject fire_fx; // 火焰特效对象
        public GameObject fuel_model; // 燃料模型对象

        public float start_fuel = 10f; // 初始燃料量
        public float max_fuel = 50f; // 最大燃料量
        public float fuel_per_hour = 1f; // 每游戏小时消耗的燃料量
        public float wood_add_fuel = 2f; // 每单位木材添加的燃料量

        private Selectable select; // 可选择对象组件
        private Construction construction; // 建造组件
        private Buildable buildable; // 可建造组件
        private UniqueID unique_id; // 唯一标识组件

        private bool is_on = false; // 是否点火状态
        private float fuel = 0f; // 当前燃料量

        private static List<Firepit> firepit_list = new List<Firepit>(); // 所有火坑对象的列表

        void Awake()
        {
            firepit_list.Add(this); // 添加到火坑列表
            select = GetComponent<Selectable>(); // 获取可选择组件
            construction = GetComponent<Construction>(); // 获取建造组件
            buildable = GetComponent<Buildable>(); // 获取可建造组件
            unique_id = GetComponent<UniqueID>(); // 获取唯一标识组件

            if (fire_fx)
                fire_fx.SetActive(false); // 初始时关闭火焰特效
            if (fuel_model)
                fuel_model.SetActive(false); // 初始时关闭燃料模型显示
        }

        private void OnDestroy()
        {
            firepit_list.Remove(this); // 从火坑列表移除
        }

        private void Start()
        {
            select.RemoveGroup(fire_group); // 移除火焰组选择
            buildable.onBuild += OnBuild; // 注册建造事件

            // 如果没有被生成过且不在建造中，则初始化燃料为起始燃料量
            if (!construction.was_spawned && !buildable.IsBuilding())
                fuel = start_fuel;

            // 如果玩家数据中有自定义燃料值，则设置当前燃料为玩家数据中的值
            if (PlayerData.Get().HasCustomFloat(GetFireUID()))
                fuel = PlayerData.Get().GetCustomFloat(GetFireUID());
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return; // 如果游戏暂停，则返回

            if (is_on)
            {
                float game_speed = TheGame.Get().GetGameTimeSpeedPerSec(); // 获取游戏时间速度
                fuel -= fuel_per_hour * game_speed * Time.deltaTime; // 消耗燃料

                PlayerData.Get().SetCustomFloat(GetFireUID(), fuel); // 更新玩家数据中的燃料值
            }

            is_on = fuel > 0f; // 更新火坑点火状态
            if (fire_fx)
                fire_fx.SetActive(is_on); // 设置火焰特效显示状态
            if (fuel_model)
                fuel_model.SetActive(fuel > 0f); // 设置燃料模型显示状态

            if (is_on)
                select.AddGroup(fire_group); // 如果点火状态，则加入火焰组
            else
                select.RemoveGroup(fire_group); // 否则移除火焰组
        }

        /// <summary>
        /// 添加燃料到火坑
        /// </summary>
        /// <param name="value">要添加的燃料量</param>
        public void AddFuel(float value)
        {
            fuel += value; // 增加燃料量
            is_on = fuel > 0f; // 更新点火状态

            PlayerData.Get().SetCustomFloat(GetFireUID(), fuel); // 更新玩家数据中的燃料值
        }

        private void OnBuild()
        {
            fuel = start_fuel; // 建造完成后重置燃料为起始燃料量
        }

        /// <summary>
        /// 获取火坑的唯一标识
        /// </summary>
        /// <returns>火坑的唯一标识</returns>
        public string GetFireUID()
        {
            if (!string.IsNullOrEmpty(unique_id.unique_id))
                return unique_id.unique_id + "_fire"; // 返回火坑的唯一标识
            return "";
        }

        /// <summary>
        /// 检查火坑是否处于点火状态
        /// </summary>
        /// <returns>如果火坑处于点火状态则返回true，否则返回false</returns>
        public bool IsOn()
        {
            return is_on; // 返回火坑的点火状态
        }

        /// <summary>
        /// 获取最近的火坑对象
        /// </summary>
        /// <param name="pos">参考位置</param>
        /// <param name="range">搜索范围</param>
        /// <returns>最近的火坑对象，如果没有找到则返回null</returns>
        public static Firepit GetNearest(Vector3 pos, float range = 999f)
        {
            float min_dist = range;
            Firepit nearest = null;
            foreach (Firepit fire in firepit_list)
            {
                float dist = (pos - fire.transform.position).magnitude; // 计算距离
                if (dist < min_dist)
                {
                    min_dist = dist;
                    nearest = fire; // 更新最近的火坑对象
                }
            }
            return nearest; // 返回最近的火坑对象
        }

        /// <summary>
        /// 获取所有火坑对象的列表
        /// </summary>
        /// <returns>所有火坑对象的列表</returns>
        public static List<Firepit> GetAll()
        {
            return firepit_list; // 返回所有火坑对象的列表
        }
    }
}
