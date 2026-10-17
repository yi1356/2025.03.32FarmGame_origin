using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{
    [RequireComponent(typeof(Selectable))]
    public class ReadObject : MonoBehaviour
    {
        public string title; // 标题

        [TextArea(3, 4)]
        public string text; // 文本内容

        void Start()
        {

        }

    }

}