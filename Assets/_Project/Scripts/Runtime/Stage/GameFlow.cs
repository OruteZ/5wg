using UnityEngine.SceneManagement;
using UnityEngine;

namespace FiveWG.Stage
{
    /// <summary>
    /// 씬 사이의 이동을 한 곳에 모은다. UI가 씬 이름 문자열을 직접 들고 있지 않게 하기 위함.
    /// 여기 상수는 Build Settings에 등록된 씬 이름과 일치해야 한다.
    /// </summary>
    public static class GameFlow
    {
        public const string MainMenuScene = "MainMenu";
        public const string StageScene = "Stage01";

        public static void LoadMainMenu() => SceneManager.LoadScene(MainMenuScene);

        public static void LoadStage() => SceneManager.LoadScene(StageScene);

        /// <summary>현재 씬을 다시 로드한다. 스테이지 상태는 되돌리지 않고 씬째로 갈아끼운다.</summary>
        public static void RestartCurrentScene() =>
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        public static void Quit()
        {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
    #else
            Application.Quit();
    #endif
        }
    }
}
