using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace FarmingEngine
{
    /// <summary>
    /// 这是一个包装类，用于简化使用 NavMesh 系统功能
    /// 本来打算在单独的线程中使用，但由于 Unity 不支持在另一个线程上使用 NavMesh.CalculatePath，因此第二个线程目前已禁用。
    /// 您需要调用的唯一函数是 CalculatePath，在完成时应通过回调返回路径
    /// </summary>
    public class NavMeshToolPath
    {
        public Vector3 from; // 起始点
        public Vector3 to; // 目标点
        public int layerMask; // 图层掩码

        public bool completed = false; // 是否完成计算
        public bool success = false; // 是否成功找到路径
        public Vector3[] path; // 路径点数组
    }

    public class NavMeshTool
    {
        private static ConcurrentQueue<NavMeshToolPath> thread_list = new ConcurrentQueue<NavMeshToolPath>();

        /// <summary>
        /// 计算从起点到终点的路径
        /// </summary>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        /// <param name="layerMask">图层掩码</param>
        /// <param name="callback">计算完成后的回调函数</param>
        public static void CalculatePath(Vector3 from, Vector3 to, int layerMask, UnityAction<NavMeshToolPath> callback)
        {
            NavMeshToolPath tpath = new NavMeshToolPath();
            tpath.from = from;
            tpath.to = to;
            tpath.layerMask = layerMask;
            thread_list.Enqueue(tpath);

            // 异步（NavMesh.CalculatePath）不在主线程之外工作，当 Unity 修复此问题后，可以使用该函数
            // DoCalculatePath(tpath, callback);

            // 目前的临时解决方案，直到 Unity 支持在主线程之外的 NavMesh.CalculatePath
            CalculateThread();
            callback.Invoke(tpath);
        }

        // 使用异步方式计算路径，暂时不可用
        // private static async void DoCalculatePath(NavMeshToolPath tpath, UnityAction<NavMeshToolPath> callback)
        // {
        //     await Task.Run(CalculateThread);
        //     callback.Invoke(tpath);
        // }

        // 执行路径计算的方法
        private static void CalculateThread()
        {
            NavMeshToolPath tpath;
            bool succ = thread_list.TryDequeue(out tpath);
            if (succ)
            {
                NavMeshPath path = new NavMeshPath();
                bool success = NavMesh.CalculatePath(tpath.from, tpath.to, tpath.layerMask, path);
                tpath.success = success && path.status == NavMeshPathStatus.PathComplete;
                tpath.path = path.corners;
                tpath.completed = true;
            }
        }
    }
}
