using UnityEngine;
using UnityEditor.VFX;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.VFX.UI;
using UnityEditor;
using NUnit.Framework;
using System.Runtime.CompilerServices;

using SysRegex = System.Text.RegularExpressions.Regex;

namespace UnityEditor.VFX
{
    internal class VFXSystemNames
    {

        public static readonly string DefaultSystemName = "System";

        private static readonly string IndexPattern = @" (\(([0-9])*\))$";
        private Dictionary<VFXModel, int> m_SystemToIndex = new Dictionary<VFXModel, int>();

        public static string GetSystemName(VFXModel model)
        {
            var data = model as VFXData;

            // general case
            if (data != null)
            {
                return data.title;
            }

            // special case for spawners
            var context = model as VFXContext;
            if (context != null)
            {
                if (context.contextType == VFXContextType.Spawner)
                    return context.label;
                else
                {
                    var contextData = context.GetData();
                    if (contextData != null)
                        return contextData.title;
                }

            }

            Debug.LogError("model not associated to a system");
            return null;
        }

        public static void SetSystemName(VFXModel model, string name)
        {
            var data = model as VFXData;
            if (data != null)
            {
                data.title = name;
                return;
            }

            var context = model as VFXContext;
            if (context != null)
            {
                if (context.contextType == VFXContextType.Spawner)
                {
                    context.label = name;
                    return;
                }
                else
                {
                    var contextData = context.GetData();
                    if (contextData != null)
                    {
                        contextData.title = name;
                        return;
                    }
                }
            }

            Debug.LogError("model not associated to a system");
        }

        public string GetUniqueSystemName(VFXModel model)
        {
            int index;
            if (m_SystemToIndex.TryGetValue(model, out index))
            {
                var wishedName = GetSystemName(model);
                if (wishedName == string.Empty)
                    wishedName = DefaultSystemName;
                if (wishedName != null)
                {
                    var format = "{0} ({1})";
                    var newName = index == 0 ? wishedName : string.Format(format, wishedName, index);
                    return newName;
                }
            }
            Debug.LogError("GetUniqueSystemName::Error: model not registered");
            return GetSystemName(model);
        }

        private static int ExtractIndex(string name)
        {
            if (SysRegex.IsMatch(name, IndexPattern))
            {
                var afterOpeningBracket = name.LastIndexOf('(') + 1;
                var closingBracket = name.LastIndexOf(')');
                var index = name.Substring(afterOpeningBracket, closingBracket - afterOpeningBracket);
                return int.Parse(index);
            }
            return 0;
        }

        public void Sync(VFXGraph graph)
        {
            var models = new HashSet<ScriptableObject>();
            graph.CollectDependencies(models, false);

            var systems = models.OfType<VFXContext>()
                .Where(c => c.contextType == VFXContextType.Spawner || c.GetData() != null)
                .Select(c => c.contextType == VFXContextType.Spawner ? c as VFXModel : c.GetData())
                .Distinct().ToList();

            Init(systems);
        }

        private void Init(IEnumerable<VFXModel> models)
        {
            m_SystemToIndex.Clear();
            foreach (var system in models)
            {
                var systemName = GetSystemName(system);
                var index = GetIndex(systemName);
                m_SystemToIndex[system] = index;
            }
            Debug.Log("Init");
        }

        private int GetIndex(string unindexedName)
        {
            int index = -1;

            List<int> unavailableIndices;
            if (string.IsNullOrEmpty(unindexedName) || unindexedName == DefaultSystemName)
                unavailableIndices = m_SystemToIndex.Where(pair => (GetSystemName(pair.Key) == string.Empty || GetSystemName(pair.Key) == DefaultSystemName)).Select(pair => pair.Value).ToList();
            else
                unavailableIndices = m_SystemToIndex.Where(pair => GetSystemName(pair.Key) == unindexedName).Select(pair => pair.Value).ToList();
            if (unavailableIndices != null && unavailableIndices.Count() > 0)
            {
                unavailableIndices.Sort();
                for (int i = 0; i < unavailableIndices.Count(); ++i)
                    if (i != unavailableIndices[i])
                    {
                        index = i;
                        break;
                    }

                if (index == -1)
                    index = unavailableIndices[unavailableIndices.Count() - 1] + 1;
            }
            else
            {
                index = 0;
            }

            return index;
        }

    }
}
