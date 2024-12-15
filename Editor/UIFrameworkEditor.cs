using System.Collections.Generic;
using System.IO;
using CryII.I.EasyHub;
using CryII.EasyHelper;
using UnityEditor;
using UnityEngine;

namespace CryII.I.UIFramework
{
    public static class UIFrameworkEditor
    {
        public const string PackagesAssetsPath = "Packages/com.cryii.i.ui-framework/Assets";

        public static void InitializeUIFramework()
        {
            if (!InitConfig.TryGet(out var easyInitConfig)) return;
            
            CopyUIAssets(easyInitConfig);
            CopyLuaLogic(easyInitConfig);
        }

        private static void CopyUIAssets(InitConfig initConfig)
        {
            var uiRootName = "ui_root.prefab";
            var uiPrefabDirectory = Path.Combine(initConfig.WorkDirectoryConfig.UIPrefabDirectory, uiRootName);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(uiPrefabDirectory) != null)
            {
                Debug.Log($"the ui root prefab already exists.");
                return;
            }
            
            var uiRootCopyFrom = Path.Combine(PackagesAssetsPath, uiRootName);
            AssetDatabase.CopyAsset(uiRootCopyFrom, uiPrefabDirectory);
        }

        private static void CopyLuaLogic(InitConfig initConfig)
        {
            var copyFrom = Path.GetFullPath(Path.Combine(PackagesAssetsPath, "lua_logic"));
            var copyTo = Path.GetFullPath(Path.Combine(Application.dataPath, initConfig.LuaConfig.DevDirectory));
            var skipFiles = new List<string>();
            FileHelper.CopyDirectory(copyFrom, copyTo, false, skipFiles);
            if (skipFiles.Count > 0)
            {
                Debug.Log($"{skipFiles.Count} files have been skipped for copying: \n {string.Join("\n", skipFiles)}");
            }
        }
    }
}