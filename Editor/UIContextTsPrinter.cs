using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CryII.EasyHelper;
using UnityEditor;

namespace CryII.I.UIFramework
{
    public class UIContextTsPrinter
    {
        private readonly UIConfig _uiConfig;
        private readonly UIContext _uiContext;
        private readonly string _prefabSlug;

        public UIContextTsPrinter(UIConfig uiConfig, UIContext uiContext, string prefabSlug)
        {
            _uiConfig = uiConfig;
            _uiContext = uiContext;
            _prefabSlug = prefabSlug;
        }

        public string Generate()
        {
            var fields = GenFields();
            var prefabPath = GenBindPrefabPath();
            var components = GenBindComponents();

            var prefabFlag = string.Format(_uiConfig.BindFlag, _uiContext.bindHashCode);

            var tsCode = new StringBuilder();
            tsCode.Append(prefabFlag);
            tsCode.Append(TextHelper.TabScope(prefabPath));
            tsCode.Append(TextHelper.TabScope(fields, false));
            tsCode.Append(TextHelper.TabScope(components));
            tsCode.Append(TextHelper.TabLine(prefabFlag));

            return tsCode.ToString();
        }

        private bool CheckUIContextBindController(UIComponentNode node)
        {
            if (node.bindComponent is UIContext context && File.Exists(context.bindTsScript)) return true;
            
            DialogHelper.Tip($"组件：{node.fieldName} 未绑定UIControl！");
            EditorGUIUtility.PingObject(node.belongGo);
            return false;

        }

        private string GenFields()
        {
            var fields = new List<string>();
            foreach (var node in _uiContext.listComponentsInEditor)
            {
                var fieldName = node.fieldName;
                var compType = node.bindComponent.GetType();
                if (compType == typeof(UIContext))
                {
                    if (!CheckUIContextBindController(node))
                    {
                        continue;
                    }

                    var context = node.bindComponent as UIContext;
                    if (context is null) continue;
                    
                    var tsCode = File.ReadAllText(context.bindTsScript);
                    var match = Regex.Match(tsCode, @$"class\s+(\w+)\s+extends\s+({_uiConfig.ClassUIBaseName})");
                    if (!match.Success) continue;
                    
                    var className = match.Groups[1].Value;
                    var field = $"{fieldName}!: {className};";
                    fields.Add(field);
                }
                else
                {
                    var compTypeName = compType.FullName;
                    var fieldDef = $"{fieldName}!: CS.{compTypeName};";
                    fields.Add(fieldDef);
                }
            }

            return string.Join("\n", fields);
        }

        private string GenBindPrefabPath()
        {
            var @return = TextHelper.TabScope($"return \"{_prefabSlug}\";");
            return $"public {_uiConfig.FuncGetPath}(): string {{ {@return}}}";
        }

        private string GenBindComponents()
        {
            var nodes = _uiContext.listComponentsInEditor;

            var expressions = new List<string>();
            for (var i = 0; i < nodes.Count; ++i)
            {
                var node = nodes[i];
                var index = i.ToString();

                var refName = node.fieldName;
                var compType = node.bindComponent.GetType();
                if (compType == typeof(UIContext))
                {
                    if (!CheckUIContextBindController(node))
                    {
                        continue;
                    }

                    var context = node.bindComponent as UIContext;
                    if (context is null) continue;
                    
                    var tsCode = File.ReadAllText(context.bindTsScript);
                    var match = Regex.Match(tsCode, @$"class\s+(\w+)\s+extends\s+({_uiConfig.ClassUIBaseName})");
                    if (!match.Success) continue;
                    var className = match.Groups[1].Value;
                    var getControllerExp =
                        $"this.{refName} = this.{_uiConfig.FuncGetSubContext}({className}, {index})!;";
                    expressions.Add(getControllerExp);
                }
                else
                {
                    var compTypeName = compType.FullName;
                    var getNodeExp = $"this.{refName} = this.{_uiConfig.FuncGetComponent}<CS.{compTypeName}>({index});";
                    expressions.Add(getNodeExp);
                }
            }

            return $"protected {_uiConfig.FuncBinding}(): void {{{TextHelper.TabScope(string.Join("\n", expressions))}}}";
        }
    }
}