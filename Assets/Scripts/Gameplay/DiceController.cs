using UnityEngine;

namespace CyberTokyo.Gameplay
{
    public class DiceController : MonoBehaviour
    {
        public int LastRoll { get; private set; }

        public int Roll()
        {
            LastRoll = Random.Range(1, 7);
            return LastRoll;
        }
    }
}
