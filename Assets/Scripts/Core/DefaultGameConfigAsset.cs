using CyberTokyo.Core.Board;
using CyberTokyo.Core.Reward;
using UnityEngine;

namespace CyberTokyo.Core
{
    /// <summary>
    /// 离线兜底配置 —— 唯一一个用 ScriptableObject 装的配置数据，因为它就是要被
    /// Inspector 拖来拖去、当设计时资产用的东西。真正来自网络的配置（Phase 4）
    /// 走的是普通 C# 类反序列化，不走这个类型 —— 两者结构一致，但语义不同，
    /// 不要为了省事把两者合并。
    ///
    /// 内容由 Assets/Scripts/Editor/DefaultConfigGenerator.cs 生成，
    /// 不要手改这个 asset 文件本身的字段值。
    /// </summary>
    [CreateAssetMenu(fileName = "DefaultGameConfig", menuName = "Cyber Tokyo/Default Game Config")]
    public class DefaultGameConfigAsset : ScriptableObject
    {
        public BoardConfigDto Board;
        public RewardConfigDto Reward;
    }
}
