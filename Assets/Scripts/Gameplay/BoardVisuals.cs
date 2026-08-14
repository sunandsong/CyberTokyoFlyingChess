using UnityEngine;

namespace CyberTokyo.Gameplay
{
    /// <summary>
    /// 所有美术查找表打包成一个对象传递，免得 Initialize 的参数列表随素材类别增长。
    /// palette 必须有（起码要有占位色），其余都允许 null（= 该类素材还没做，用占位）。
    /// </summary>
    public class BoardVisuals
    {
        public TileColorPaletteSO Palette;
        public BuildingVisualSO Buildings;
        public CenterStateVisualSO CenterStates;
        public RewardIconSO RewardIcons;
        public PieceVisualSO Pieces;

        /// <summary>从 Resources/Data/ 把有的都装上，没有的保持 null</summary>
        public static BoardVisuals LoadFromResources()
        {
            return new BoardVisuals
            {
                Palette = Resources.Load<TileColorPaletteSO>("Data/TileColorPalette"),
                Buildings = Resources.Load<BuildingVisualSO>("Data/BuildingVisuals"),
                CenterStates = Resources.Load<CenterStateVisualSO>("Data/CenterStateVisuals"),
                RewardIcons = Resources.Load<RewardIconSO>("Data/RewardIcons"),
                Pieces = Resources.Load<PieceVisualSO>("Data/PieceVisuals"),
            };
        }
    }
}
