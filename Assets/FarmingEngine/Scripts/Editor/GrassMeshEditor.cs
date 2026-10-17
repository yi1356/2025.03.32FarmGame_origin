using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FarmingEngine.EditorTool
{
    /// <summary>
    /// GrassMesh的编辑器脚本
    /// </summary>

    [CustomEditor(typeof(GrassMesh)), CanEditMultipleObjects]
    public class GrassMeshEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            GrassMesh myScript = target as GrassMesh;

            DrawDefaultInspector(); // 绘制默认的检视面板

            if (GUILayout.Button("Refresh Now")) // 如果点击了“立即刷新”按钮
            {
                myScript.RefreshMesh(); // 调用GrassMesh脚本中的RefreshMesh方法
            }

            EditorGUILayout.Space(); // 添加一些空白空间
        }

    }

}