using System.Collections.Generic;
using System.IO;
using CryII.EasyHelper;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace CryII.I.UIFramework
{
    public class UIConfig
    {
        [JsonProperty("cache_file")] public string CacheFile;
        [JsonProperty("bind_flag")] public string BindFlag;
        [JsonProperty("func_get_path")] public string FuncGetPath;
        [JsonProperty("func_binding")] public string FuncBinding;
        [JsonProperty("func_get_component")] public string FuncGetComponent;
        [JsonProperty("func_get_sub_context")] public string FuncGetSubContext;
        [JsonProperty("class_ui_base_name")] public string ClassUIBaseName;
        
        private const string ConfigFile = "Packages/com.cryii.i.ui-framework/Assets/config.json";
        
        public static bool TryGet(out UIConfig uiConfig)
        {
            var initConfig = AssetDatabase.LoadAssetAtPath<TextAsset>(ConfigFile);
            if (initConfig == null)
            {
                Debug.LogError($"config file not found, it should have been located at: {ConfigFile}");
                uiConfig = default;
                return false;
            }
            
            uiConfig = JsonConvert.DeserializeObject<UIConfig>(initConfig.text);
            return uiConfig != null;
        }

        public bool ReadBindingCache(out Dictionary<string, string> bindingCache)
        {
            var cachePath = Path.Combine(Application.dataPath, CacheFile);
            using var fs = FileHelper.EnsureOpenFile(cachePath);
            using var sr = new StreamReader(fs);
            var cacheJson = sr.ReadToEnd();
            bindingCache = JsonConvert.DeserializeObject<Dictionary<string, string>>(cacheJson) ?? new Dictionary<string, string>();
            return true;
        }

        public void SaveBindingCache(in Dictionary<string, string> bindingCache)
        {
            var cacheJson = JsonConvert.SerializeObject(bindingCache);
            
            var cachePath = Path.Combine(Application.dataPath, CacheFile);
            using var fs = FileHelper.EnsureOpenFile(cachePath);
            using var sw = new StreamWriter(fs);
            sw.Write(cacheJson);
        }
    }
}