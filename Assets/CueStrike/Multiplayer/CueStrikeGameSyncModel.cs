#if CUESTRIKE_NORMCORE
using Normal.Realtime;
using Normal.Realtime.Serialization;

namespace CueStrike.Multiplayer
{
    /// <summary>
    /// Normcore data model syncing high-level game state, turns, and scores.
    /// The partial class matches the Normcore code generator output.
    /// </summary>
    [RealtimeModel]
    public partial class CueStrikeGameSyncModel
    {
        [RealtimeProperty(1, true, true)]
        private int _currentPlayerIndex;

        [RealtimeProperty(2, true, true)]
        private int _currentGameState;

        [RealtimeProperty(3, true, true)]
        private int _player1Score;

        [RealtimeProperty(4, true, true)]
        private int _player2Score;

        [RealtimeProperty(5, true, true)]
        private bool _shotInProgress;
    }
}
#endif
