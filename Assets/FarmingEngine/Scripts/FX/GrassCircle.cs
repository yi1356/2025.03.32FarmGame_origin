using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 在圆形区域内生成草的网格
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class GrassCircle : MonoBehaviour
    {
        public float radius = 1f; // 圆形区域的半径
        public float spacing = 1f; // 草之间的间距
        public int precision = 10; // 圆形的精度，即分成多少份

        private MeshRenderer render;
        private MeshFilter mesh;

        void Awake()
        {
            mesh = GetComponent<MeshFilter>();
            render = GetComponent<MeshRenderer>();
            RefreshMesh(); // 在 Awake 时刷新网格
        }

        /// <summary>
        /// 创建草的网格
        /// </summary>
        /// <returns>生成的草的网格</returns>
        Mesh CreateMesh()
        {
            Mesh m = new Mesh();
            m.name = "GrassMesh";

            if (precision < 1 || radius < 0.01f || spacing < 0.01f)
                return m;

            int nbstep = Mathf.Max(Mathf.RoundToInt(radius / spacing), 1); // 计算步数
            int nbang = precision + 1; // 角度精度
            Vector3[] vertices = new Vector3[nbstep * nbang + 1]; // 顶点数组
            Vector3[] normals = new Vector3[nbstep * nbang + 1]; // 法线数组
            Vector4[] tangents = new Vector4[nbstep * nbang + 1]; // 切线数组
            Vector2[] uvs = new Vector2[nbstep * nbang + 1]; // UV数组
            int nb_tri = (nbstep - 1) * precision * 6 + precision * 3; // 三角形数量
            int[] triangles = new int[nb_tri]; // 三角形索引数组

            Vector3 normal = Vector3.up; // 法线方向
            Vector4 tangent = new Vector4(-1f, 0f, 0f, -1f); // 切线方向

            // 中心点
            vertices[0] = Vector3.zero;
            normals[0] = normal;
            tangents[0] = tangent;
            uvs[0] = new Vector2(0.5f, 0.5f); // 中心点的UV坐标

            int index = 1;
            for (int a = 0; a < nbang; a++)
            {
                float angle = (a * 360f / (float)precision) * Mathf.Deg2Rad; // 当前角度
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)); // 方向向量
                for (int x = 0; x < nbstep; x++)
                {
                    float dist = ((x + 1) / (float)nbstep) * radius; // 当前距离
                    vertices[index] = dir * dist; // 计算顶点位置
                    normals[index] = normal; // 设置法线
                    tangents[index] = tangent; // 设置切线
                    // 根据位置设置UV坐标
                    uvs[index] = new Vector2(Mathf.Clamp01(dir.x * dist * 0.5f / radius + 0.5f), 
                                             Mathf.Clamp01(dir.z * dist * 0.5f / radius + 0.5f));
                    index++;
                }
            }

            index = 0;
            for (int a = 0; a < nbang - 1; a++)
            {
                int vertexIndex = a * nbstep + 1;
                // 中心点和当前圆环的第一个点，以及下一个圆环的第一个点构成三角形
                triangles[index + 0] = 0;
                triangles[index + 1] = vertexIndex + nbstep;
                triangles[index + 2] = vertexIndex;
                index += 3;

                for (int x = 0; x < nbstep - 1; x++)
                {
                    vertexIndex = a * nbstep + x + 1;
                    // 每两个圆环之间的四个顶点构成两个三角形
                    triangles[index + 0] = vertexIndex;
                    triangles[index + 1] = vertexIndex + nbstep;
                    triangles[index + 2] = vertexIndex + 1;
                    triangles[index + 3] = vertexIndex + nbstep;
                    triangles[index + 4] = vertexIndex + nbstep + 1;
                    triangles[index + 5] = vertexIndex + 1;
                    index += 6;
                }
            }

            m.vertices = vertices;
            m.normals = normals;
            m.tangents = tangents;
            m.uv = uvs;
            m.triangles = triangles;

            return m;
        }

        /// <summary>
        /// 刷新草的网格
        /// </summary>
        public void RefreshMesh()
        {
            mesh.mesh = CreateMesh(); // 重新生成并赋值网格
        }
    }
}
