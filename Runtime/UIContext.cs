using System;
using System.Collections.Generic;
using UnityEngine;

namespace CryII.I.UIFramework
{
    [Serializable]
    public class UIComponentNode
    {
        public string fieldName;
        public GameObject belongGo;
        public Component bindComponent;
    }

    public class UIContext : MonoBehaviour
    {
#if UNITY_EDITOR
        [HideInInspector] 
        public string bindHashCode; 
        [HideInInspector] 
        public string bindTsScript;

        [HideInInspector] 
        public List<UIComponentNode> listComponentsInEditor = new();
#endif
        
        [HideInInspector] 
        public List<Component> listComponents = new();
        public UIAnimator uiAnimator;
    }
}