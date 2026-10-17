using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 将此脚本添加到任何临时特效，以便在 'lifetime' 秒后销毁它
    /// </summary>

    public class SpawnFX : MonoBehaviour
    {

        public float lifetime = 5f; // 特效存在时间，单位为秒

        void Start()
        {
            Destroy(gameObject, lifetime); // 在特效创建后，经过指定的生命周期时间后销毁它
        }
    }

}