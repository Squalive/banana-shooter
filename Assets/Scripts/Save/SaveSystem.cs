using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Save
{
    public static class SaveSystem
    {
        private static readonly string keyword = "asdgasf";
    
        public static bool SaveData(string saveName, object data)
        {
            string path = GetPath(saveName);
            BinaryFormatter formatter = GetBinaryFormatter();

            FileStream file = File.Create(path);
        
            formatter.Serialize(file,data);
        
            file.Close();

            return true;
        }
        static BinaryFormatter GetBinaryFormatter()
        {
            BinaryFormatter formatter = new BinaryFormatter();

            return formatter;
        }
        [Obsolete]
        public static object LoadData(string saveName)
        {
            if (!File.Exists(GetPath(saveName)))
            {
                return null;
            }
            BinaryFormatter formatter = GetBinaryFormatter();

            FileStream file = File.Open(GetPath(saveName), FileMode.Open);

            try
            {
                object data = formatter.Deserialize(file);
                file.Close();
                return data;
            }
            catch (Exception e)
            {
                file.Close();
                Debug.Log(e.Message);
                return null;
            }
        }
    
        public static async Task<bool> SaveDataAsync(string saveName, object obj)
        {
            string path = GetPath(saveName);

            byte[] binaryData = SerializeToBinary(obj);
        
            bool flag=false;

            try
            {
                await using FileStream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite, 4096, true);
                await stream.WriteAsync(binaryData, 0, binaryData.Length);
                flag = true;
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            return flag;
        }
    
        private static byte[] SerializeToBinary(object obj)
        {
            BinaryFormatter formatter = GetBinaryFormatter();
            using MemoryStream stream = new MemoryStream();
            formatter.Serialize(stream, obj);
            return stream.ToArray();
        }
    
        public static IEnumerator LoadBinaryDataAsync(string saveName)
        {
            string path = GetPath(saveName);

            //Plain File I/O instead of UnityWebRequest: a bare filesystem path is not a valid URI, so on
            //Linux (Application.persistentDataPath starts with '/') Unity resolved it against its
            //"http://localhost/" base and turned it into an http request that could never succeed.
            Task<byte[]> readTask = Task.Run(() => File.ReadAllBytes(path));

            while (!readTask.IsCompleted)
                yield return null;

            if (readTask.IsFaulted)
            {
                Debug.LogError($"Failed to load {saveName} binary data: {readTask.Exception?.GetBaseException().Message}");
                yield return "Failed";
                yield break;
            }

            object data = null;
            string deserializeError = null;
            try
            {
                data = GetBinaryFormatter().Deserialize(new MemoryStream(readTask.Result));
            }
            catch (Exception e)
            {
                deserializeError = e.Message;
            }

            if (deserializeError != null)
            {
                Debug.LogError($"Failed to deserialize {saveName} binary data: {deserializeError}");
                yield return "Failed";
                yield break;
            }

            yield return data;
        }
    
        [Obsolete]
        public static void SaveToJSON<T> (List<T> toSave, string filename) {
            // Debug.Log (GetPath (filename));
            string content = JsonHelper.ToJson<T> (toSave.ToArray ());
            WriteFile (GetPath (filename), content);
        }
        [Obsolete]
        public static void SaveToJSON<T> (T toSave, string filename)
        {
            string content = JsonUtility.ToJson(toSave);
            WriteFile (GetPath (filename), content);
        }

        [Obsolete]
        public static void SaveToJSONConvert<T> (T toSave, string filename) {
            string content = JsonConvert.SerializeObject(toSave,Formatting.None,new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            
            });
            WriteFile (GetPath (filename), content);
        }

        [Obsolete]
        public static List<T> ReadListFromJSON<T> (string filename) {
            string content = ReadFile (GetPath (filename));

            if (string.IsNullOrEmpty (content) || content == "{}"|| (!content.Contains("{") || !content.Contains("}"))) {
                return new List<T> ();
            }

            List<T> res = JsonHelper.FromJson<T> (content).ToList ();

            return res;

        }
        public static T ReadFromJSON<T> (string filename) {
            string content = ReadFile (GetPath (filename));

            if (string.IsNullOrEmpty (content) || content == "{}" || (!content.Contains("{") || !content.Contains("}"))) {
                return default (T);
            }

            T res = JsonUtility.FromJson<T>(content);

            return res;

        }
        public static T ReadFromJSONConvert<T> (string filename) {
            string content = ReadFile (GetPath (filename));

            if (string.IsNullOrEmpty (content) || content == "{}" || (!content.Contains("{") || !content.Contains("}"))) {
                return default (T);
            }

            T res = JsonConvert.DeserializeObject<T> (content);

            return res;

        }
        public static string GetPath (string filename) {
            return Application.persistentDataPath + "/" + filename;
        }

        public static void WriteFile (string path, string content)
        {
            FileStream fileStream = File.Create(path);

            using StreamWriter writer = new StreamWriter (fileStream);
            writer.Write (EncryptDecrypt(content));
        }
        
        public static void WriteFileUnEncrypt (string path, string content)
        {
            FileStream fileStream = File.Create(path);

            using StreamWriter writer = new StreamWriter (fileStream);
            writer.Write (content);
        }

        public static async Task<float> WriteToFileAsync(string path, string content)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using (StreamWriter writer = new StreamWriter(path))
            {
                await writer.WriteAsync(EncryptDecrypt(content));
            }
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds / 1000f;
        }
        public static async Task<float> WriteToFileAsync(string path, byte[] bytes)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using (FileStream writer = new FileStream(path,FileMode.OpenOrCreate,FileAccess.Write))
            {
                await writer.WriteAsync(bytes, 0, bytes.Length);
            }
            stopwatch.Stop();
            return stopwatch.ElapsedMilliseconds / 1000f;
        }
        public static async Task<float> WriteToFileAsyncThread(string path,string content)
        {
            TaskCompletionSource<bool> complete = new TaskCompletionSource<bool>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            var thread = new Thread(() =>
            {
                using (StreamWriter writer = new StreamWriter(path))
                {
                    writer.Write(EncryptDecrypt(content));
                }
                stopwatch.Stop();
                complete.SetResult(true);
            });
            thread.Start();

            await complete.Task;
        
            thread.Abort();
        
            return stopwatch.ElapsedMilliseconds / 1000f;
        }
        public static async Task<float> WriteToFileAsyncThread(string path, byte[] bytes)
        {
            TaskCompletionSource<bool> complete = new TaskCompletionSource<bool>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            var thread = new Thread(() =>
            {
                using (FileStream writer = new FileStream(path,FileMode.OpenOrCreate,FileAccess.Write))
                {
                    writer.Write(bytes, 0, bytes.Length);
                }
                stopwatch.Stop();
                complete.SetResult(true);
            });
            thread.Start();

            await complete.Task;
        
            thread.Abort();
        
            return stopwatch.ElapsedMilliseconds / 1000f;
        }
        public static async void WriteToFileAsyncThread(string path, byte[] bytes,Action action)
        {
            TaskCompletionSource<bool> complete = new TaskCompletionSource<bool>();
            Stopwatch stopwatch = Stopwatch.StartNew();
            var thread = new Thread(() =>
            {
                using (FileStream writer = new FileStream(path,FileMode.OpenOrCreate,FileAccess.Write))
                {
                    writer.Write(bytes, 0, bytes.Length);
                }
                stopwatch.Stop();
                complete.SetResult(true);
            
                action?.Invoke();
            });
            thread.Start();

            await complete.Task;
        
            thread.Abort();
        }
        public static async Task<byte[]> ReadByteFromFileAsync(string path)
        {
            await using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.Asynchronous);
            byte[] buffer = new byte[fileStream.Length];
            var readAsync = await fileStream.ReadAsync(buffer, 0, (int)fileStream.Length);
            return buffer;
        }
    
        public static byte[] ReadByteFromFile(string path)
        {
            using (FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] buffer = new byte[fileStream.Length];
                var read = fileStream.Read(buffer, 0, (int)fileStream.Length);
                return buffer;
            }
        }
        public static string ReadFile (string path) {
            if (File.Exists (path)) {
                using (StreamReader reader = new StreamReader (path)) {
                    string content = reader.ReadToEnd ();
                
                    return EncryptDecrypt(content);
                }
            }
            return "";
        }
        public static string ReadFileNormally (string path) {
            if (File.Exists (path)) {
                using (StreamReader reader = new StreamReader (path)) {
                    string content = reader.ReadToEnd ();
                
                    return content;
                }
            }
            return "";
        }
    
        public static IEnumerator ReadFileAsyncThread(string path)
        {
            //Plain File I/O instead of UnityWebRequest. UnityWebRequest.Get() wants a URI, not a
            //filesystem path: a Windows "C:/..." path happens to be recognised as a local file, but a
            //"/home/..." path was merged with Unity's "http://localhost/" base and became an http
            //request, so on Linux none of the save files could ever be read back.
            Task<string> readTask = Task.Run(() => File.ReadAllText(path));

            while (!readTask.IsCompleted)
                yield return null;

            if (readTask.IsFaulted)
            {
                Debug.LogError($"Failed to read {path}: {readTask.Exception?.GetBaseException().Message}");
                yield return "";
                yield break;
            }

            yield return EncryptDecrypt(readTask.Result);
        }

        public static IEnumerator ReadFileAsyncTask(string path,TaskCompletionSource<bool> tcs)
        {
            Task<string> readTask = Task.Run(() => File.ReadAllText(path));

            while (!readTask.IsCompleted)
                yield return null;

            if (readTask.IsFaulted)
            {
                Debug.LogError($"Failed to read {path}: {readTask.Exception?.GetBaseException().Message}");
                tcs.TrySetResult(false);
                yield break;
            }

            tcs.TrySetResult(true);

            yield return EncryptDecrypt(readTask.Result);
        }
    
    
        static string EncryptDecrypt(string data)
        {
            StringBuilder result = new StringBuilder("");

            for (int i = 0; i < data.Length; i++)
            {
                result.Append((char) (data[i] ^ keyword[i % keyword.Length]));
            }

            return result.ToString();
        }
    }

    public static class JsonHelper {
        public static T[] FromJson<T> (string json) {
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>> (json);
            return wrapper.Items;
        }

        public static string ToJson<T> (T[] array) {
            Wrapper<T> wrapper = new Wrapper<T> ();
            wrapper.Items = array;
            return JsonUtility.ToJson (wrapper);
        }

        public static string ToJson<T> (T[] array, bool prettyPrint) {
            Wrapper<T> wrapper = new Wrapper<T> ();
            wrapper.Items = array;
            return JsonUtility.ToJson (wrapper, prettyPrint);
        }

        [Serializable]
        private class Wrapper<T> {
            public T[] Items;
        }
    }
}