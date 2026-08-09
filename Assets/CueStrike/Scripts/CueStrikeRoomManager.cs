using UnityEngine;
using UnityEngine.SceneManagement;

namespace CueStrike
{
    public enum CueStrikeRoomType
    {
        Tournament,
        WarpFantasy,
        Industrial,
        Luxury,
        SpaceNebula,
        ZenDojo,
        Cyberpunk,
        GrandArena
    }

    public class CueStrikeRoomManager : MonoBehaviour
    {
        public CueStrikeRoomType activeRoom = CueStrikeRoomType.Tournament;

        public void SelectRoom(CueStrikeRoomType roomType)
        {
            activeRoom = roomType;
            var sceneName = GetSceneName(roomType);
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
            }
        }

        public static string GetSceneName(CueStrikeRoomType roomType)
        {
            return roomType switch
            {
                CueStrikeRoomType.Tournament => "AAA_RoomDAY",
                CueStrikeRoomType.WarpFantasy => "WarpFantasy_Room",
                CueStrikeRoomType.Industrial => "Industrial_Room",
                CueStrikeRoomType.Luxury => "Luxury_Room",
                CueStrikeRoomType.SpaceNebula => "SpaceNebula_Room",
                CueStrikeRoomType.ZenDojo => "ZenDojo_Room",
                CueStrikeRoomType.Cyberpunk => "Cyberpunk_Room",
                CueStrikeRoomType.GrandArena => "GrandArena_Room",
                _ => string.Empty
            };
        }
    }
}
