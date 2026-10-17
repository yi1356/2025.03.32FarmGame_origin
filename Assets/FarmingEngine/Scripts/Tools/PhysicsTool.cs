using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 通用的物理功能类
    /// </summary>
    public class PhysicsTool
    {
        // 检测物体是否接触地面，返回地面距离和法线
        public static bool DetectGround(Transform root, Vector3 center, float hdist, float radius, LayerMask ground_layer, out float ground_distance, out Vector3 ground_normal)
        {
            // 定义射线发射点
            Vector3 p1 = center;
            Vector3 p2 = center + Vector3.left * radius;
            Vector3 p3 = center + Vector3.right * radius;
            Vector3 p4 = center + Vector3.forward * radius;
            Vector3 p5 = center + Vector3.back * radius;

            // 定义射线检测结果
            RaycastHit h1, h2, h3, h4, h5, hd;
            // 发射射线并获取结果
            bool f1 = Physics.Raycast(p1, Vector3.down, out h1, hdist, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f2 = Physics.Raycast(p2, Vector3.down, out h2, hdist, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f3 = Physics.Raycast(p3, Vector3.down, out h3, hdist, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f4 = Physics.Raycast(p4, Vector3.down, out h4, hdist, ground_layer.value, QueryTriggerInteraction.Ignore);
            bool f5 = Physics.Raycast(p5, Vector3.down, out h5, hdist, ground_layer.value, QueryTriggerInteraction.Ignore);
            // 排除根物体和其子物体作为接触点
            f1 = f1 && h1.collider.transform != root && !h1.collider.transform.IsChildOf(root);
            f2 = f2 && h2.collider.transform != root && !h2.collider.transform.IsChildOf(root);
            f3 = f3 && h3.collider.transform != root && !h3.collider.transform.IsChildOf(root);
            f4 = f4 && h4.collider.transform != root && !h4.collider.transform.IsChildOf(root);
            f5 = f5 && h5.collider.transform != root && !h5.collider.transform.IsChildOf(root);

            // 是否接触到地面
            bool is_grounded = f1 || f2 || f3 || f4 || f5;
            ground_normal = Vector3.up;
            ground_distance = 0f;

            // 如果接触到地面，计算地面距离
            if (is_grounded)
            {
                Vector3 hit_center = Vector3.zero;
                int nb = 0;
                if (f1) { hit_center += h1.point; nb++; }
                if (f2) { hit_center += h2.point; nb++; }
                if (f3) { hit_center += h3.point; nb++; }
                if (f4) { hit_center += h4.point; nb++; }
                if (f5) { hit_center += h5.point; nb++; }
                // 在边缘处增加更长的射线以防不足
                if (Physics.Raycast(p1, Vector3.down, out hd, 1f + hdist, ground_layer.value, QueryTriggerInteraction.Ignore)) { hit_center += hd.point; nb++; }
                hit_center = hit_center / nb;
                ground_distance = (hit_center - root.position).y;
            }

            // 如果接触到地面，计算地面法线
            if (is_grounded)
            {
                Vector3 normal = Vector3.zero;
                int nb = 0;
                if (f1) { normal += FlipNormalUp(h1.normal); nb++; }
                if (f2) { normal += FlipNormalUp(h2.normal); nb++; }
                if (f3) { normal += FlipNormalUp(h3.normal); nb++; }
                if (f4) { normal += FlipNormalUp(h4.normal); nb++; }
                if (f5) { normal += FlipNormalUp(h5.normal); nb++; }
                if (Physics.Raycast(p1, Vector3.down, out hd, 1f + hdist, ground_layer.value, QueryTriggerInteraction.Ignore)) { normal += FlipNormalUp(hd.normal); nb++; }
                normal = normal / nb;
                ground_normal = normal.normalized;
            }

            // 绘制射线（用于调试）
            // Debug.DrawRay(p1, Vector3.down * hdist);
            // Debug.DrawRay(p2, Vector3.down * hdist);
            // Debug.DrawRay(p3, Vector3.down * hdist);
            // Debug.DrawRay(p4, Vector3.down * hdist);
            // Debug.DrawRay(p5, Vector3.down * hdist);

            return is_grounded;
        }

        // 检测某一点下方的所有图层
        public static int[] DetectGroundLayers(Vector3 center, float hdist)
        {
            Vector3 p1 = center;
            RaycastHit[] hits = Physics.RaycastAll(p1, Vector3.down, hdist, ~0, QueryTriggerInteraction.Ignore);
            int[] layers = new int[hits.Length];

            for (int i = 0; i < hits.Length; i++)
                layers[i] = hits[i].collider.gameObject.layer;

            return layers;
        }

        // 查找给定位置下方地面的位置
        public static bool FindGroundPosition(Vector3 pos, float max_y, out Vector3 ground_pos)
        {
            return FindGroundPosition(pos, max_y, ~0, out ground_pos); // 所有图层
        }

        // 查找给定位置下方地面的位置
        public static bool FindGroundPosition(Vector3 pos, float max_y, LayerMask ground_layer, out Vector3 ground_pos)
        {
            Vector3 start_pos = pos + Vector3.up * max_y;
            RaycastHit rhit;
            bool is_hit = Physics.Raycast(start_pos, Vector3.down, out rhit, max_y * 2f, ~0, QueryTriggerInteraction.Ignore);
            bool is_in_right_layer = is_hit && rhit.collider != null && IsLayerInLayerMask(rhit.collider.gameObject.layer, ground_layer.value);
            ground_pos = rhit.point;
            return is_hit && is_in_right_layer;
        }

        // 翻转法线向上
        public static Vector3 FlipNormalUp(Vector3 normal)
        {
            if (normal.y < 0f)
                return -normal; // 面朝上
            return normal;
        }

        // 射线检测碰撞的指定图层
        public static bool RaycastCollisionLayer(Vector3 pos, Vector3 dir, LayerMask layer, out RaycastHit hit)
        {
            // Debug.DrawRay(pos, dir);
            return Physics.Raycast(pos, dir.normalized, out hit, dir.magnitude, layer.value, QueryTriggerInteraction.Ignore);
        }

        // 射线检测碰撞（检测所有图层）
        public static bool RaycastCollision(Vector3 pos, Vector3 dir, out RaycastHit hit)
        {
            // Debug.DrawRay(pos, dir);
            return Physics.Raycast(pos, dir.normalized, out hit, dir.magnitude, ~0, QueryTriggerInteraction.Ignore);
        }

        // 检查图层是否在指定的图层掩码中
        public static bool IsLayerInLayerMask(int layer, LayerMask mask)
        {
            return (LayerToLayerMask(layer).value & mask.value) > 0;
        }

        // 检查任何一个图层是否在指定的图层掩码中
        public static bool IsAnyLayerInLayerMask(int[] layers, LayerMask mask)
        {
            bool is_in_layer = false;
            for (int i = 0; i < layers.Length; i++)
                is_in_layer = is_in_layer || IsLayerInLayerMask(layers[i], mask);
            return is_in_layer;
        }

        // 将图层转换为图层掩码
        public static LayerMask LayerToLayerMask(int layer)
        {
            return (LayerMask)1 << layer;
        }

        // 将图层掩码转换为图层列表
        public static List<int> LayerMaskToLayers(LayerMask mask)
        {
            uint bits = (uint)mask.value;
            List<int> layers = new List<int>();
            for (int i = 31; bits > 0; i--)
            {
                if ((bits >> i) > 0)
                {
                    bits = (bits << (32 - i)) >> (32 - i);
                    layers.Add(i);
                }
            }
            return layers;
        }
    }
}
