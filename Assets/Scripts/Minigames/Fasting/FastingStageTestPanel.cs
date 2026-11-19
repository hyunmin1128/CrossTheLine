using UnityEngine;
namespace CrossTheLine.Minigames
{
    public sealed class FastingStageTestPanel : MonoBehaviour
    {
        public FastingTopDownGame game;
        public void StartStage(int stage){if(game)game.BeginStageForTesting(stage);}
    }
}
