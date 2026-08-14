using System;
using CyberTokyo.Core.Board;
using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// BuildingId -&gt; 建筑贴图。配了用图、没配退灰色占位块。
    /// 资产实例放 Assets/Resources/Data/，运行时 Resources.Load
    /// （原因见 [[unity-scene-ref-serialization-gotcha]]）。
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingVisuals", menuName = "Cyber Tokyo/Building Visuals")]
    public class BuildingVisualSO : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public BuildingId Building;
            public Sprite Sprite;
        }

        public Entry[] Entries;

        public Sprite Resolve(BuildingId building)
        {
            foreach (var entry in Entries)
            {
                if (entry.Building == building) return entry.Sprite;
            }
            return null;
        }
    }
}
