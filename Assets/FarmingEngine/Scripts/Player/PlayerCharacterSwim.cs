using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 脚本用于允许玩家游泳
    /// 确保玩家角色有一个独特的层级设置（如Player层）
    /// </summary>

    [RequireComponent(typeof(PlayerCharacter))]
    public class PlayerCharacterSwim : MonoBehaviour
    {
        public float swim_speed = 1f; // 游泳速度
        public float swim_energy = 1f; // 每秒消耗的能量
        public LayerMask water_layer = (1 << 4); // 触发游泳状态的水层
        public LayerMask water_obstacle_layer = (1 << 14); // 角色忽略的障碍物层，允许游泳
        public Transform swim_mesh_offset; // 游泳网格偏移位置
        public float swim_offset_y = -1f; // 游泳时的垂直偏移
        public bool swim_offset_camera = false; // 是否调整摄像机偏移
        public GameObject swim_start_fx; // 游泳开始特效
        public GameObject swim_ongoing_fx; // 游泳持续特效
        public AudioClip swim_start_audio; // 游泳开始音效

        private PlayerCharacter character;
        private bool is_swimming = false; // 是否正在游泳
        private Vector3 swim_mesh_tpos; // 游泳网格的目标位置
        private int[] cground_layers = new int[0]; // 角色周围的地面层级
        private GameObject swimming_fx; // 游泳特效实例

        void Awake()
        {
            character = GetComponent<PlayerCharacter>();

            // 设置游泳网格的初始位置
            if (swim_mesh_offset != null)
                swim_mesh_tpos = swim_mesh_offset.transform.localPosition;

            // 实例化游泳持续特效
            if (swim_ongoing_fx != null)
            {
                swimming_fx = Instantiate(swim_ongoing_fx, transform);
                swimming_fx.SetActive(false);
            }

            // 忽略角色与水障碍物层的碰撞
            foreach (int layer in PhysicsTool.LayerMaskToLayers(water_obstacle_layer))
                Physics.IgnoreLayerCollision(gameObject.layer, layer);
        }

        private void FixedUpdate()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (character.IsDead())
                return;

            // 检测角色周围的地面层级
            Vector3 center = character.GetColliderCenter();
            float hradius = character.GetColliderHeightRadius();
            cground_layers = PhysicsTool.DetectGroundLayers(center, hradius);
        }

        void Update()
        {
            if (TheGame.Get().IsPaused())
                return;

            if (character.IsDead())
                return;

            // 开始游泳
            if (!is_swimming && PhysicsTool.IsAnyLayerInLayerMask(cground_layers, water_layer))
                StartSwim();
            // 停止游泳
            else if (is_swimming && !PhysicsTool.IsAnyLayerInLayerMask(cground_layers, water_layer))
                StopSwimming();

            // 调整游泳网格的偏移位置
            if (swim_mesh_offset != null)
                swim_mesh_offset.transform.localPosition = Vector3.Lerp(swim_mesh_offset.transform.localPosition, swim_mesh_tpos, 20f * Time.deltaTime);

            // 消耗游泳能量
            if (is_swimming)
                character.Attributes.AddAttribute(AttributeType.Energy, -swim_energy * Time.deltaTime);
        }

        private void StartSwim()
        {
            if (!is_swimming)
            {
                is_swimming = true;
                swim_mesh_tpos += Vector3.up * swim_offset_y; // 增加游泳网格的垂直偏移
                // 播放游泳开始特效
                if (swim_start_fx != null)
                    Instantiate(swim_start_fx, transform.position, swim_start_fx.transform.rotation);
                // 激活游泳持续特效
                if (swimming_fx != null)
                    swimming_fx.SetActive(true);
                character.TriggerBusy(0.25f); // 角色忙碌状态
                // 调整摄像机偏移
                if (swim_offset_camera)
                    TheCamera.Get().SetOffset(Vector3.up * swim_offset_y);
                // 播放游泳开始音效
                TheAudio.Get().PlaySFX("character", swim_start_audio);
            }
        }

        private void StopSwimming()
        {
            if (is_swimming)
            {
                is_swimming = false;
                swim_mesh_tpos -= Vector3.up * swim_offset_y; // 减少游泳网格的垂直偏移
                // 停止游泳持续特效
                if (swimming_fx != null)
                    swimming_fx.SetActive(false);
                character.TriggerBusy(0.25f); // 角色忙碌状态
                // 恢复摄像机偏移
                if (swim_offset_camera)
                    TheCamera.Get().SetOffset(Vector3.zero);
            }
        }

        public bool IsSwimming()
        {
            return is_swimming; // 返回当前是否正在游泳
        }
    }
}
