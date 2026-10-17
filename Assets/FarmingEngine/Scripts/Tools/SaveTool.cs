using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

namespace FarmingEngine
{
    /// <summary>
    /// 将类写入磁盘或从磁盘读取包含类的文件的脚本
    /// TODO:这里现在还看不透？？？？？？
    /// </summary>

    [System.Serializable]
    public class SaveTool
    {
        // 从文件加载任何类的数据，确保该类标记为 [System.Serializable]
        public static T LoadFile<T>(string filename) where T : class
        {
            T data = null;
            string fullpath = Application.persistentDataPath + "/" + filename;
            if (IsValidFilename(filename) && File.Exists(fullpath))
            {
                FileStream file = null;
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    file = File.Open(fullpath, FileMode.Open);
                    data = (T)bf.Deserialize(file);
                    file.Close();
                }
                catch (System.Exception e)
                {
                    Debug.Log("加载数据出错 " + e);
                    if (file != null) file.Close();
                }
            }
            return data;
        }

        // 将任何类的数据保存到文件，确保该类标记为 [System.Serializable]
        public static void SaveFile<T>(string filename, T data) where T : class
        {
            if (IsValidFilename(filename))
            {
                FileStream file = null;
                try
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    string fullpath = Application.persistentDataPath + "/" + filename;
                    file = File.Create(fullpath);
                    bf.Serialize(file, data);
                    file.Close();
                }
                catch (System.Exception e)
                {
                    Debug.Log("保存数据出错 " + e);
                    if (file != null) file.Close();
                }
            }
        }

        // 删除指定文件
        public static void DeleteFile(string filename)
        {
            string fullpath = Application.persistentDataPath + "/" + filename;
            if (File.Exists(fullpath))
                File.Delete(fullpath);
        }

        // 返回所有保存文件的列表
        public static List<string> GetAllSave(string extension = "")
        {
            List<string> saves = new List<string>();
            string[] files = Directory.GetFiles(Application.persistentDataPath);
            foreach (string file in files)
            {
                if (file.EndsWith(extension))
                {
                    string filename = Path.GetFileName(file);
                    if (!saves.Contains(filename))
                        saves.Add(filename);
                }
            }
            return saves;
        }

        // 检查文件是否存在
        public static bool DoesFileExist(string filename)
        {
            string fullpath = Application.persistentDataPath + "/" + filename;
            return IsValidFilename(filename) && File.Exists(fullpath);
        }

        // 检查文件名是否有效
        public static bool IsValidFilename(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
                return false; // 文件名不能为空白

            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (filename.Contains(c.ToString()))
                    return false; // 不允许包含任何特殊字符
            }
            return true;
        }
    }

}
