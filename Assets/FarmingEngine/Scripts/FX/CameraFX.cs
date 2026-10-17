using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FarmingEngine
{

    /// <summary>
    /// 相机特效控制脚本，用于更新对象的位置和旋转以匹配主摄像机的位置和朝向。
    /// </summary>
    public class CameraFX : MonoBehaviour
    {
        void Start()
        {
            // 在对象启用时执行的初始化操作
        }

        void Update()
        {
            // 更新对象的位置为主摄像机的目标位置
            transform.position = TheCamera.Get().GetTargetPos();
            
            // 更新对象的旋转为主摄像机的朝向
            transform.rotation = Quaternion.LookRotation(TheCamera.Get().GetFacingFront(), Vector3.up);
        }
    }

}