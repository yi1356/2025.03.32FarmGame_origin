using UnityEngine;
using System;
using System.Collections;

namespace FarmingEngine
{
    /// <summary>
    /// 可序列化的Vector3版本
    /// 在文件系统中很有用
    /// </summary>
    [System.Serializable]
    public struct Vector3Data
    {
        public float x;
        public float y;
        public float z;

        public Vector3Data(float iX, float iY, float iZ)
        {
            x = iX;
            y = iY;
            z = iZ;
        }

        public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}]", x, y, z);
        }

        // 转换为真实的Vector3
        public static implicit operator Vector3(Vector3Data rValue)
        {
            return new Vector3(rValue.x, rValue.y, rValue.z);
        }

        public static implicit operator Vector3Data(Vector3 rValue)
        {
            return new Vector3Data(rValue.x, rValue.y, rValue.z);
        }
    }

    /// <summary>
    /// 可序列化的Quaternion版本
    /// </summary>
    [System.Serializable]
    public struct QuaternionData
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public QuaternionData(float iX, float iY, float iZ, float iW)
        {
            x = iX;
            y = iY;
            z = iZ;
            w = iW;
        }

        public override string ToString()
        {
            return String.Format("[{0}, {1}, {2}, {3}]", x, y, z, w);
        }

        // 转换为真实的Quaternion
        public static implicit operator Quaternion(QuaternionData rValue)
        {
            return new Quaternion(rValue.x, rValue.y, rValue.z, rValue.w);
        }

        public static implicit operator QuaternionData(Quaternion rValue)
        {
            return new QuaternionData(rValue.x, rValue.y, rValue.z, rValue.w);
        }
    }

}