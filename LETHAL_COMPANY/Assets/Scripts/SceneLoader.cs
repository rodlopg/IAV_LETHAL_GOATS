using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LethalCompanyAI
{
    /// <summary>
    /// Persistent scene loader with a fade-to-black transition. The next scene is loaded
    /// in the background while the screen fades out, then activated and faded back in.
    ///
    /// No setup is required: SceneLoader.Instance creates itself the first time it is used
    /// (it also builds its own fade overlay). To change the scene names, edit the defaults
    /// below or place a SceneLoader on an object in your first scene and set them there.
    /// Both scenes must be added in File > Build Settings.
    /// </summary>
    [DisallowMultipleComponent]
    public class SceneLoader : MonoBehaviour
    {
        public string mainMenuScene = "MainMenu";
        public string levelScene = "Level";
        [Min(0.05f)] public float fadeDuration = 0.6f;

        private static SceneLoader instance;

        public static SceneLoader Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = new GameObject("SceneLoader");
                    instance = go.AddComponent<SceneLoader>(); // Awake sets everything up
                }
                return instance;
            }
        }

        public bool IsLoading { get; private set; }

        private CanvasGroup fadeGroup;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            BuildFadeOverlay();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                SceneManager.sceneLoaded -= OnSceneLoaded;
                instance = null;
            }
        }

        public void LoadMainMenu()
        {
            LoadScene(mainMenuScene);
        }

        public void LoadLevel()
        {
            LoadScene(levelScene);
        }

        public void ReloadCurrentScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        public void LoadScene(string sceneName)
        {
            if (IsLoading)
                return;

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError("SceneLoader: scene '" + sceneName + "' is not in Build Settings.", this);
                return;
            }

            StartCoroutine(LoadRoutine(sceneName));
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private IEnumerator LoadRoutine(string sceneName)
        {
            IsLoading = true;

            // Start loading immediately; keep the scene inactive until the fade is complete.
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            yield return Fade(1f);

            while (operation.progress < 0.9f)
                yield return null;

            // Undo anything the pause menu did before the new scene starts.
            Time.timeScale = 1f;
            AudioListener.pause = false;

            operation.allowSceneActivation = true;
            while (!operation.isDone)
                yield return null;

            yield return null; // let the new scene run its Start() before revealing it
            yield return Fade(0f);

            IsLoading = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            Time.timeScale = 1f;
            AudioListener.pause = false;
        }

        private IEnumerator Fade(float targetAlpha)
        {
            fadeGroup.blocksRaycasts = true; // block clicks while transitioning

            float startAlpha = fadeGroup.alpha;
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime; // works while timeScale is 0
                fadeGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }

            fadeGroup.alpha = targetAlpha;
            fadeGroup.blocksRaycasts = targetAlpha > 0.99f;
        }

        private void BuildFadeOverlay()
        {
            GameObject canvasObject = new GameObject("FadeCanvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = short.MaxValue;

            fadeGroup = canvasObject.AddComponent<CanvasGroup>();
            fadeGroup.alpha = 0f;
            fadeGroup.interactable = false;
            fadeGroup.blocksRaycasts = false;

            GameObject imageObject = new GameObject("Black");
            imageObject.transform.SetParent(canvasObject.transform, false);

            Image image = imageObject.AddComponent<Image>();
            image.color = Color.black;

            RectTransform rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}