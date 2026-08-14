namespace CyberTokyo.Core.State
{
    /// <summary>
    /// 固定顺序 green/yellow/red/blue 轮转回合。OPEN-6（胜利条件）没定之前，
    /// 只管"下一个是谁"，不管"什么时候该结束"。
    /// </summary>
    public class TurnManager
    {
        private readonly GameState _state;
        private int _currentPlayerIndex;

        public TurnManager(GameState state)
        {
            _state = state;
            _currentPlayerIndex = 0;
        }

        public PlayerState CurrentPlayer => _state.Players[_currentPlayerIndex];

        public void NextTurn()
        {
            _currentPlayerIndex = (_currentPlayerIndex + 1) % _state.Players.Count;
        }
    }
}
