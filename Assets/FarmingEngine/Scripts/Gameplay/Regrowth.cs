using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    public enum RegrowthType
    {
        OnDeath = 0, // 在死亡时重生
        OnCreate = 10, // 在创建时重生
    }

    /// <summary>
    /// 将此脚本附加到对象上，使其在死亡后自动重生
    /// </summary>

    [RequireComponent(typeof(UniqueID))]
    public class Regrowth : MonoBehaviour
    {
        public RegrowthType type; // 何时尝试重生新对象？在死亡时还是创建时？
        public IdData spawn_data; // 将要重生的对象数据，如果为空，将使用已设置的 Spawnable/Item/Construction/Plant 数据
        public float range = 10f; // 原始对象周围可以重生的范围
        public int max = 5; // 如果范围内已经有这么多相同对象，则不会重生
        public float probability = 0.5f; // 在死亡时重生的概率
        public float duration = 48f; // 死亡到重生之间的游戏内时间（小时）
        public LayerMask valid_floor = 1 << 9; // 可以生长的地板层
        public bool random_rotation = false; // 如果为真，Y 轴将随机旋转
        public bool random_scale = false; // 如果为真，缩放将在 -0.25 到 +0.25 之间变化

        private UniqueID unique_id;
        private SObject sobject;
        private Destructible destruct; // 可能为 null
        private Item item; // 可能为 null

        private void Awake()
        {
            unique_id = GetComponent<UniqueID>();
            sobject = GetComponent<SObject>();
            destruct = GetComponent<Destructible>();
            item = GetComponent<Item>();

            if (type == RegrowthType.OnDeath)
            {
                if (destruct != null)
                    destruct.onDeath += CreateRegrowth;
                if (item != null)
                    item.onDestroy += CreateRegrowth;
            }
        }

        private void Start()
        {
            if (type == RegrowthType.OnCreate)
            {
                CreateRegrowth();
            }
        }

        private void CreateRegrowth()
        {
            IdData data = spawn_data != null ? spawn_data : sobject?.GetData();
            if (data != null && !string.IsNullOrEmpty(unique_id.unique_id) && !PlayerData.Get().HasWorldRegrowth(unique_id.unique_id))
            {
                int nb = SObject.CountSceneObjects(data, transform.position, range);
                if (nb < max)
                {
                    // 查找位置
                    Vector3 position = FindPosition();
                    if (IsPositionValid(position))
                    {
                        Quaternion rotation = transform.rotation;
                        float scale = 1f;
                        if (random_rotation)
                            rotation = Quaternion.Euler(rotation.eulerAngles.x, Random.Range(0f, 360f), 0f);
                        if (random_scale)
                            scale = Random.Range(0.75f, 1.25f);

                        CreateRegrowthData(unique_id.unique_id, data.id, SceneNav.GetCurrentScene(), position, rotation, valid_floor, scale, duration, probability);
                    }
                }
            }
        }

        private Vector3 FindPosition()
        {
            int nbtry = 0;
            bool valid = false;
            Vector3 position = transform.position;
            while (!valid && nbtry < 10)
            { // 尝试找到有效位置 10 次
                position = transform.position;
                float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float radius = Random.Range(0f, range);
                position += new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius;
                position.y = FindYPosition(position);
                valid = IsPositionValid(position);
                nbtry++;
            }
            return position;
        }

        private float FindYPosition(Vector3 pos)
        {
            Vector3 center = pos + Vector3.up * 10f;
            Vector3 ground_pos;
            bool found = PhysicsTool.FindGroundPosition(center, 20f, valid_floor, out ground_pos);
            return found ? ground_pos.y : pos.y;
        }

        private bool IsPositionValid(Vector3 pos)
        {
            Vector3 center = pos + Vector3.up * 0.5f;
            Vector3 ground_pos;
            return PhysicsTool.FindGroundPosition(center, 1f, valid_floor, out ground_pos);
        }

        // 从现有重生数据中生成预制件，在其计时器达到持续时间后
        public static GameObject SpawnRegrowth(RegrowthData data)
        {
            bool valid = PhysicsTool.FindGroundPosition(data.pos, 10f, data.layer, out Vector3 ground_pos);
            if (valid && Random.value < data.probability)
            {
                CraftData cdata = CraftData.Get(data.data_id);
                SpawnData sdata = SpawnData.Get(data.data_id);

                if (cdata != null && data.scene == SceneNav.GetCurrentScene())
                {
                    GameObject nobj = Craftable.Create(cdata, data.pos);
                    nobj.transform.rotation = data.rot;
                    nobj.transform.localScale = nobj.transform.localScale * data.scale;
                    return nobj;
                }
                else if (sdata != null && data.scene == SceneNav.GetCurrentScene())
                {
                    return Spawnable.Create(sdata, data.pos, data.rot, data.scale);
                }
            }

            return null;
        }

        // 创建重生数据，对象死亡后
        public static RegrowthData CreateRegrowthData(string uid, string id, string scene, Vector3 pos, Quaternion rot, LayerMask layer, float scale, float duration, float probability)
        {
            RegrowthData data = new RegrowthData();
            data.data_id = id;
            data.uid = uid;
            data.scene = scene;
            data.pos = pos;
            data.rot = rot;
            data.layer = layer.value;
            data.scale = scale;
            data.time = duration;
            data.probability = probability;
            PlayerData.Get().AddWorldRegrowth(uid, data);
            return data;
        }
    }
}
