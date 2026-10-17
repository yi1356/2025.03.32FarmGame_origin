using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    // 允许精灵接收阴影

    [ExecuteInEditMode] // 在编辑模式下执行脚本
    public class SpriteShadow : MonoBehaviour
    {
        void Start()
        {
            if (GetComponent<Renderer>())
            {
                GetComponent<Renderer>().receiveShadows = true; // 如果对象有Renderer组件，则允许其接收阴影
            }
        }
    }

}