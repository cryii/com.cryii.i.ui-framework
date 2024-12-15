using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CryII.I.EasyHub;
using CryII.EasyHelper;
using UnityEditor;
using UnityEngine;

namespace CryII.I.UIFramework
{
    [CustomEditor(typeof(UIContext))]
    public class UIContextEditor : Editor
    {
        private SerializedProperty _bindHashCode;
        private SerializedProperty _bindTsScript;
        private SerializedProperty _listComponentsInEditor;
        private SerializedProperty _listComponents;
        
        private UIContext _target;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            LayoutHelper.Vertical(() =>
            {
                DrawTopShortcutMenu();

                EditorGUILayout.Space();

                DrawComponentsList();

                EditorGUILayout.Space();

                DrawAddComponentAreaByDrag();
            });
        }

        private void OnEnable()
        {
            _target = target as UIContext;
            
            _bindHashCode = serializedObject.FindProperty("bindHashCode");
            _bindTsScript = serializedObject.FindProperty("bindTsScript");
            _listComponentsInEditor = serializedObject.FindProperty("listComponentsInEditor");
            _listComponents = serializedObject.FindProperty("listComponents");

            if (string.IsNullOrEmpty(_bindHashCode.stringValue))
            {
                UpdateHashCode();
            }
        }

        private void UpdateHashCode()
        {
            if (!UIConfig.TryGet(out var config))
            {
                return;
            }
            
            if (!config.ReadBindingCache(out var cache))
            {
                return;
            }

            if (cache.TryGetValue(_bindHashCode.stringValue, out var tsFile))
            {
                cache.Remove(_bindHashCode.stringValue);
            }
            
            var hashCodeNew = HashHelper.Code(32);
            _bindHashCode.stringValue = hashCodeNew;
            
            UpdateSerializedObject();
        }

        private void UpdateScriptCode()
        {
            var activeGo = Selection.activeGameObject;
            if (!SystemHelper.TryGetPrefabPath(activeGo, out var prefabPath))
            {
                DialogHelper.Tip("需要打开预制体场景进行此操作！");
                return;
            }

            if (string.IsNullOrEmpty(prefabPath))
            {
                DialogHelper.Tip("资源引用丢失！");
                return;
            }
            
            if (!InitConfig.TryGet(out var easyInitConfig)) return;
            
            if (!prefabPath.StartsWith(easyInitConfig.WorkDirectoryConfig.UIPrefabDirectory))
            {
                Debug.LogError($"this prefab is not in: {easyInitConfig.WorkDirectoryConfig.UIPrefabDirectory}");
                return;
            }

            var prefabSlug = prefabPath.Replace(easyInitConfig.WorkDirectoryConfig.UIPrefabDirectory, "");
            if (!TryGenerateTsCode(prefabSlug))
            {
                DialogHelper.Tip($"已复制到粘贴板。");
                return;
            }

            Debug.Log("update script success!");
        }

        private void UpdateBindingTsScript(string tsFile)
        {
            _bindTsScript.stringValue = tsFile;
            UpdateSerializedObject();

            if (!UIConfig.TryGet(out var config))
            {
                return;
            }

            if (!config.ReadBindingCache(out var cache))
            {
                return;
            }

            cache[_bindHashCode.stringValue] = tsFile;
            config.SaveBindingCache(cache);
        }

        private bool TryFindTsScriptByHashCode(out string tsScript)
        {
            tsScript = default;
            if (!InitConfig.TryGet(out var easyInitConfig)) return false;

            var tsUIPath = PathHelper.Combine(Application.dataPath, easyInitConfig.LuaConfig.TsUIDirectory);
            var files = Directory.GetFiles(tsUIPath, "*.ts", SearchOption.AllDirectories);
            foreach (var f in files)
            {
                var fullPath = Path.GetFullPath(f);

                var txt = File.ReadAllText(fullPath);
                var isMathHashCode =
                    Regex.IsMatch(txt, _bindHashCode.stringValue, RegexOptions.IgnorePatternWhitespace);
                if (isMathHashCode)
                {
                    tsScript = fullPath;

                    UpdateBindingTsScript(fullPath);
                    return true;
                }
            }

            tsScript = string.Empty;
            return false;
        }

        private bool TryGenerateTsCode(string prefabSlug)
        {
            if (!UIConfig.TryGet(out var config))
            {
                return false;
            }
            
            if (!config.ReadBindingCache(out var cache))
            {
                return false;
            }
            
            if (!cache.TryGetValue(_bindHashCode.stringValue, out var tsScript)
                || !File.Exists(tsScript))
            {
                if (!TryFindTsScriptByHashCode(out tsScript))
                {
                    return false;
                }
            }
            
            var tsCode = File.ReadAllText(tsScript);
            
            var tsBuilder = new UIContextTsPrinter(config, _target, prefabSlug);
            var overwriteCode = tsBuilder.Generate();
            
            var prefabFlag = string.Format(config.BindFlag, _bindHashCode.stringValue);
            var regexBindingCode = $@"{prefabFlag}[\s\S]*{prefabFlag}";
            var matchBindingCode = Regex.Match(tsCode, regexBindingCode);
            if (matchBindingCode.Success)
            {
                var oldCode = matchBindingCode.Groups[0].Value;
                tsCode = tsCode.Replace(oldCode, overwriteCode);
                File.WriteAllText(tsScript, tsCode);
                return true;
            }
            
            var matchPrefabFlag = Regex.Match(tsCode, prefabFlag);
            if (matchPrefabFlag.Success)
            {
                var flagCode = matchPrefabFlag.Groups[0].Value;
                tsCode = tsCode.Replace(flagCode, overwriteCode);
                File.WriteAllText(tsScript, tsCode);
                return true;
            }

            return false;
        }

        private void DrawTopShortcutMenu()
        {
            LayoutHelper.Vertical(() =>
            {
                var needRefreshHashCode = GUILayout.Button(_bindHashCode.stringValue);
                if (needRefreshHashCode && DialogHelper.Warning("confirm to refresh hash code?"))
                {
                    UpdateHashCode();
                }

                var toOpenTsScript = GUILayout.Button(_bindTsScript.stringValue);
                if (toOpenTsScript)
                {
                    if (string.IsNullOrEmpty(_bindTsScript.stringValue) || !File.Exists(_bindTsScript.stringValue))
                    {
                        DialogHelper.Tip($"已复制到粘贴板。");
                    }
                    else
                    {
                        SystemHelper.OpenFileByVsCode(_bindTsScript.stringValue);
                    }
                }

                var needUpdateCode = GUILayout.Button("Override");
                if (needUpdateCode)
                {
                    UpdateScriptCode();
                }
            });
        }

        private void DrawComponentsList()
        {
            for (var nodeIndex = 0; nodeIndex < _listComponentsInEditor.arraySize; ++nodeIndex)
            {
                var node = _listComponentsInEditor.GetArrayElementAtIndex(nodeIndex);

                var propBelongGo = node.FindPropertyRelative("belongGo");
                var belongGo = propBelongGo.objectReferenceValue as GameObject;

                if (belongGo == null)
                {
                    _listComponentsInEditor.DeleteArrayElementAtIndex(nodeIndex);
                    _listComponents.DeleteArrayElementAtIndex(nodeIndex);

                    UpdateSerializedObject();
                    return;
                }

                LayoutHelper.Horizontal(() =>
                {
                    DrawNodeIndex(nodeIndex, node);
                    DrawRefName(nodeIndex, node);
                    DrawBindingComponent(nodeIndex, node);
                    DrawDeleteButton(nodeIndex, node);
                });
            }
        }

        private void DrawNodeIndex(int nodeIndex, SerializedProperty node)
        {
            var propBelongGo = node.FindPropertyRelative("belongGo");
            var belongGo = (propBelongGo.objectReferenceValue as GameObject)!;
            var indexDisplay = $"{nodeIndex}.{belongGo.name}";
            var pingBelongGo = GUILayout.Button(indexDisplay, GUILayout.ExpandWidth(true));
            if (pingBelongGo)
            {
                EditorGUIUtility.PingObject(belongGo);
            }
        }

        private void DrawRefName(int nodeIndex, SerializedProperty node)
        {
            var propRefName = node.FindPropertyRelative("fieldName");
            var refName = propRefName.stringValue;
            var refNameNew = EditorGUILayout.TextField(refName, GUILayout.MinWidth(120));
            if (refNameNew != refName)
            {
                propRefName.stringValue = refNameNew;
                UpdateSerializedObject();
            }
        }

        private void DrawBindingComponent(int index, SerializedProperty node)
        {
            var propRefName = node.FindPropertyRelative("fieldName");
            var propBelongGo = node.FindPropertyRelative("belongGo");
            var propBindingComp = node.FindPropertyRelative("bindComponent");

            var belongGo = (propBelongGo.objectReferenceValue as GameObject)!;
            var refComp = propBindingComp.objectReferenceValue as Component;

            List<string> componentOptions = new();

            var compsTargetHas = belongGo.GetComponents<Component>();
            var compIndex = 0;
            for (var i = 0; i < compsTargetHas.Length; ++i)
            {
                var comp = compsTargetHas[i];
                if (comp == refComp) compIndex = i;

                var compType = comp.GetType();
                var compTypeName = compType.FullName;
                if (compTypeName != null)
                {
                    var compName = compTypeName.Split(".").Last();
                    var compNameDisplay = $"{i}.{compName}";
                    componentOptions.Add(compNameDisplay);
                }
            }

            var optionIndex = EditorGUILayout.Popup(compIndex, componentOptions.ToArray(), GUILayout.Width(150));
            if (optionIndex != compIndex)
            {
                var compRef = _listComponents.GetArrayElementAtIndex(index);
                compRef.objectReferenceValue = compsTargetHas[optionIndex];
                propBindingComp.objectReferenceValue = compsTargetHas[optionIndex];

                UpdateSerializedObject();
            }
        }

        private void DrawDeleteButton(int nodeIndex, SerializedProperty node)
        {
            var needDelete = GUILayout.Button("-", GUILayout.MaxWidth(20));
            if (needDelete)
            {
                _listComponentsInEditor.DeleteArrayElementAtIndex(nodeIndex);
                _listComponents.DeleteArrayElementAtIndex(nodeIndex);

                UpdateSerializedObject();
            }
        }

        private void DrawAddComponentAreaByDrag()
        {
            var rect = EditorGUILayout.GetControlRect(true, 60);
            if (rect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                if (Event.current.type == EventType.DragExited)
                {
                    var objs = DragAndDrop.objectReferences;
                    for (var i = 0; i < objs.Length; ++i)
                    {
                        var instance = objs[i] as GameObject;
                        if (!instance)
                        {
                            return;
                        }

                        var goName = instance.name;

                        var nodeCount = _listComponentsInEditor.arraySize;
                        _listComponentsInEditor.InsertArrayElementAtIndex(nodeCount);

                        var node = _listComponentsInEditor.GetArrayElementAtIndex(nodeCount);
                        var propRefName = node.FindPropertyRelative("fieldName");
                        var propBelongGo = node.FindPropertyRelative("belongGo");
                        var propBindingComp = node.FindPropertyRelative("bindComponent");
                        propRefName.stringValue = goName;
                        propBelongGo.objectReferenceValue = instance;
                        propBindingComp.objectReferenceValue = instance.transform;

                        _listComponents.InsertArrayElementAtIndex(nodeCount);
                        var compRef = _listComponents.GetArrayElementAtIndex(nodeCount);
                        compRef.objectReferenceValue = instance.transform;

                        UpdateSerializedObject();
                    }
                }
            }
        }

        private void UpdateSerializedObject()
        {
            EditorUtility.SetDirty(_target);

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            serializedObject.UpdateIfRequiredOrScript();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}