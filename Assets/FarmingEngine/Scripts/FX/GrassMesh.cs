using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 在正方形区域内生成草的网格
    /// </summary>
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(MeshFilter))]
    public class GrassMesh : MonoBehaviour
    {
        public float width = 1f; // 正方形区域的宽度
        public float height = 1f; // 正方形区域的高度
        public float spacing = 1f; // 草之间的间距

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

            if (width < 0.01f || height < 0.01f || spacing < 0.01f)
                return m;

            // 计算宽度和高度上的顶点数
            int nbw = Mathf.RoundToInt(width / spacing) + 1;
            int nbh = Mathf.RoundToInt(height / spacing) + 1;

            Vector3[] vertices = new Vector3[nbw * nbh]; // 顶点数组
            Vector3[] normals = new Vector3[nbw * nbh]; // 法线数组
            Vector4[] tangents = new Vector4[nbw * nbh]; // 切线数组
            Vector2[] uvs = new Vector2[nbw * nbh]; // UV数组
            int nb_tri = (nbw - 1) * (nbh - 1) * 6; // 三角形数量
            int[] triangles = new int[nb_tri]; // 三角形索引数组

            Vector3 normal = Vector3.up; // 法线方向
            Vector4 tangent = new Vector4(-1f, 0f, 0f, -1f); // 切线方向

            float offsetX = width / 2f; // X轴偏移
            float offsetY = height / 2f; // Y轴偏移
            float posX = 0f;
            float posY = 0f;
            int index = 0;
            
            // 生成顶点和相关属性
            for (int y = 0; y < nbh; y++)
            {
                posX = 0f;
                for (int x = 0; x < nbw; x++)
                {
                    vertices[index] = new Vector3(posX - offsetX, 0f, posY - offsetY); // 计算顶点位置
                    normals[index] = normal; // 设置法线
                    tangents[index] = tangent; // 设置切线
                    uvs[index] = new Vector2(posX / width, posY / height); // 设置UV坐标
                    posX += spacing;
                    index++;
                }
                posY += spacing;
            }

            index = 0;
            // 生成三角形索引
            for (int y = 0; y < nbh - 1; y++)
            {
                for (int x = 0; x < nbw - 1; x++)
                {
                    int vertexIndex = y * nbw + x;
                    triangles[index + 0] = vertexIndex + 0;
                    triangles[index + 1] = vertexIndex + 1 + nbw;
                    triangles[index + 2] = vertexIndex + 1;
                    triangles[index + 3] = vertexIndex + 0;
                    triangles[index + 4] = vertexIndex + nbw;
                    triangles[index + 5] = vertexIndex + 1 + nbw;
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
