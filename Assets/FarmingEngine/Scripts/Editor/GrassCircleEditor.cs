using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FarmingEngine.EditorTool
{
    /// <summary>
    /// GrassCircle的编辑器脚本
    /// </summary>

    [CustomEditor(typeof(GrassCircle)), CanEditMultipleObjects]
    public class GrassCircleEditor : Editor
    {

        public override void OnInspectorGUI()
        {
            GrassCircle myScript = target as GrassCircle;

            DrawDefaultInspector(); // 绘制默认的检视面板

            if (GUILayout.Button("Refresh Now")) // 如果点击了“立即刷新”按钮
            {
                myScript.RefreshMesh(); // 调用GrassCircle脚本中的RefreshMesh方法
            }

            EditorGUILayout.Space(); // 添加一些空白空间
        }

    }

}