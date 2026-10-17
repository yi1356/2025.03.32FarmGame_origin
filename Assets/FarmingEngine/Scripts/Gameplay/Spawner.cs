using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 在场景中以一定间隔生成随机预制体。
    /// </summary>

    public class Spawner : MonoBehaviour
    {
        public float spawn_interval = 8f; // 生成间隔，以游戏小时为单位
        public float spawn_radius = 10f; // 生成区域的圆形半径，保持足够大以便能够追踪已生成的对象。
        public int spawn_max = 1; // 在半径内如果已经有超过这个数量的对象，则停止生成。
        public float spawn_max_radius = 10f; // 如果在这个半径内已经有超过这个数量的对象，则停止生成。
        public LayerMask valid_floor_layer = (1 << 9); // 可以生成的地板层
        public CraftData[] spawn_data; // 要生成的对象

        private float spawn_timer = 0f; // 生成计时器
        private UniqueID unique_id; // 唯一标识组件

        void Awake()
        {
            unique_id = GetComponent<UniqueID>(); // 获取唯一标识组件
        }

        private void Start()
        {
            // 从存档中加载生成计时器的时间
            if (PlayerData.Get().HasCustomFloat(GetTimerUID()))
                spawn_timer = PlayerData.Get().GetCustomFloat(GetTimerUID());
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            float game_speed = TheGame.Get().GetGameTimeSpeedPerSec(); // 获取游戏时间速度
            spawn_timer += game_speed * Time.deltaTime;

            PlayerData.Get().SetCustomFloat(GetTimerUID(), spawn_timer); // 将生成计时器时间保存到存档中

            // 如果达到生成间隔
            if (spawn_timer > spawn_interval)
            {
                spawn_timer = 0f;
                SpawnIfNotMax(); // 如果未达到最大生成数，则生成对象
            }
        }

        // 如果未达到最大生成数，则生成对象
        public void SpawnIfNotMax()
        {
            if (!IsFull())
            {
                Spawn();
            }
        }

        // 生成对象
        public void Spawn()
        {
            CraftData data = spawn_data[Random.Range(0, spawn_data.Length)];
            if (data != null)
            {
                float radius = Random.Range(0f, spawn_radius);
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                Vector3 offset = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                Vector3 pos = transform.position + offset;
                Vector3 ground_pos;
                bool found = PhysicsTool.FindGroundPosition(pos, 100f, valid_floor_layer.value, out ground_pos);
                if (found)
                {
                    CraftData.Create(data, ground_pos); // 在地面上创建对象
                }
            }
        }

        // 判断是否已达到最大生成数
        public bool IsFull()
        {
            return CountObjectsInRange() >= spawn_max;
        }

        // 计算半径内的对象数量
        public int CountObjectsInRange()
        {
            int count = 0;
            foreach (CraftData data in spawn_data)
            {
                count += CraftData.CountObjectInRadius(data, transform.position, spawn_max_radius);
            }
            return count;
        }

        // 获取生成计时器的唯一标识
        public string GetTimerUID()
        {
            if (unique_id != null && !string.IsNullOrEmpty(unique_id.unique_id))
                return unique_id.unique_id + "_timer";
            return "";
        }
    }

}
