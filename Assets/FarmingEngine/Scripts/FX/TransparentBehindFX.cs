using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 添加到物体上使其在背后时变为半透明
    /// </summary>
    
    public class TransparentBehindFX : MonoBehaviour
    {
        public float opacity = 0.5f; // 透明度
        public float distance = 5f; // 激活效果的距离阈值
        public float refresh_rate = 0.25f; // 刷新率

        private Selectable select; // 可选择组件
        private MeshRenderer[] renders; // 所有子MeshRenderer组件数组
        private float timer = 0f; // 计时器

        private List<Material> materials = new List<Material>(); // 存储原始材质列表
        private List<Material> materials_transparent = new List<Material>(); // 存储半透明材质列表

        private static List<TransparentBehindFX> see_list = new List<TransparentBehindFX>(); // 用于管理所有实例的静态列表

        void Awake()
        {
            see_list.Add(this); // 添加当前实例到静态列表中
            select = GetComponent<Selectable>(); // 获取Selectable组件
            renders = GetComponentsInChildren<MeshRenderer>(); // 获取所有子对象的MeshRenderer组件
            foreach (MeshRenderer render in renders)
            {
                foreach (Material material in render.sharedMaterials)
                {
                    bool valid_mat = material && MaterialTool.HasColor(material); // 检查材质是否有效
                    Material material_normal = valid_mat ? new Material(material) : null; // 复制原始材质
                    Material material_trans = valid_mat ? new Material(material) : null; // 复制半透明材质
                    if (material_trans != null && valid_mat)
                    {
                        material_trans.color = new Color(material_trans.color.r, material_trans.color.g, material_trans.color.b, material_trans.color.a * opacity); // 设置半透明材质的透明度
                        MaterialTool.ChangeRenderMode(material_trans, BlendMode.Fade); // 修改材质的混合模式为Fade
                    }
                    materials.Add(material_normal); // 添加原始材质到列表
                    materials_transparent.Add(material_trans); // 添加半透明材质到列表
                }
            }
        }

        private void OnDestroy()
        {
            see_list.Remove(this); // 当销毁对象时，从静态列表中移除
        }

        void Update()
        {
            if (select && !select.IsActive()) // 如果存在Selectable组件且未激活，则返回
                return;

            timer += Time.deltaTime; // 计时器累加

            if (timer > refresh_rate) // 若达到刷新率
            {
                timer = 0f; // 重置计时器
                UpdateSlow(); // 执行缓慢更新
            }
        }

        private void UpdateSlow()
        {
            Vector3 pos = TheCamera.Get().GetTargetPos(); // 获取相机的目标位置
            Vector3 cam_dir = TheCamera.Get().GetFacingFront(); // 获取相机的前方向
            Vector3 obj_dir = transform.position - pos; // 获取物体到相机的方向向量
            bool is_behind = Vector3.Dot(obj_dir.normalized, cam_dir) < 0f; // 检测物体是否在相机背后
            bool is_near = (transform.position - pos).magnitude < distance; // 检测物体是否在指定距离内
            SetMaterial(is_behind && is_near); // 设置材质（根据是否在背后和是否在近距离内）
        }

        private void SetMaterial(bool transparent)
        {
            int index = 0; // 材质索引
            foreach (MeshRenderer render in renders)
            {
                Material[] mesh_materials = render.sharedMaterials; // 获取当前MeshRenderer的所有共享材质
                for (int i = 0; i < mesh_materials.Length; i++)
                {
                    if (index < materials.Count && index < materials_transparent.Count)
                    {
                        Material mesh_mat = mesh_materials[i]; // 获取当前的材质
                        Material ref_mat = transparent ? materials_transparent[index] : materials[index]; // 根据是否需要透明效果选择对应的材质
                        if (ref_mat != mesh_mat && ref_mat != null)
                            mesh_materials[i] = ref_mat; // 替换当前材质
                    }
                    index++; // 索引递增
                }
                render.sharedMaterials = mesh_materials; // 更新MeshRenderer的共享材质数组
            }
        }

        public static List<TransparentBehindFX> GetAll()
        {
            return see_list; // 返回所有实例的静态列表
        }
    }

}
