using UnityEditor;
using UnityEditor.SceneManagement;

namespace Statecraft.Game.EditorTools
{
    public static class Generated1948CharacterReviewMenu
    {
        public const string ScenePath = "Assets/Editor/Generated1948CharacterReview.unity";

        [MenuItem("Statecraft/Character review/Play seven generated characters")]
        public static void Play()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }
    }
}
