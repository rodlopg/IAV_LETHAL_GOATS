using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalCompanyAI
{
    /// <summary>
    /// Put this on any object in the MainMenu scene and assign the references.
    /// Handles Play, Instructions (open/close) and Quit.
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Main Menu")]
        public GameObject mainPanel;
        public Button playButton;
        public Button instructionsButton;
        public Button quitButton;

        [Header("Instructions")]
        public GameObject instructionsPanel;
        public Button backButton;
        [Tooltip("Optional. If assigned, it is filled with the text below at startup.")]
        public TMP_Text instructionsText;
        public bool fillInstructionsText = true;

        [TextArea(12, 30)]
        public string instructions =
            "<b>OBJECTIVE</b>\n" +
            "Find and collect all 5 items hidden in the level, then you escape. " +
            "Don't die before you get them all.\n\n" +

            "<b>CONTROLS</b>\n" +
            "W A S D  -  Move\n" +
            "Mouse  -  Look around\n" +
            "Left Shift  -  Sprint (uses stamina)\n" +
            "Walk into an item to pick it up\n\n" +

            "<b>STAMINA</b>\n" +
            "Sprinting drains stamina. When it hits 0 you are exhausted and must " +
            "recover some stamina before you can sprint again.\n\n" +

            "<b>MONSTERS</b>\n" +
            "Bracken  -  Stalks you. Pay attention to where it is looking.\n" +
            "Coil-Head  -  Moves only when you are not looking at it.\n" +
            "Eyeless Dog  -  Blind, hunts by sound. Walk quietly; running is loud.\n" +
            "Thumper  -  Fast and aggressive. Keep your distance.\n" +
            "Nutcracker  -  Armed with a shotgun. Stay out of its line of fire.\n\n" +

            "<b>TIPS</b>\n" +
            "Walk to stay quiet and sprint only when you must. " +
            "Watch your health and stamina at the top of the screen.";

        private void Start()
        {
            // Returning from a paused level: make sure the menu is usable.
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (fillInstructionsText && instructionsText != null)
                instructionsText.text = instructions;

            if (playButton != null)
                playButton.onClick.AddListener(delegate { SceneLoader.Instance.LoadLevel(); });

            if (instructionsButton != null)
                instructionsButton.onClick.AddListener(ShowInstructions);

            if (backButton != null)
                backButton.onClick.AddListener(HideInstructions);

            if (quitButton != null)
                quitButton.onClick.AddListener(delegate { SceneLoader.Instance.QuitGame(); });

            HideInstructions();
        }

        private void Update()
        {
            // Escape closes the instructions panel.
            if (instructionsPanel != null && instructionsPanel.activeSelf && Input.GetKeyDown(KeyCode.Escape))
                HideInstructions();
        }

        public void ShowInstructions()
        {
            if (instructionsPanel != null)
                instructionsPanel.SetActive(true);

            if (mainPanel != null)
                mainPanel.SetActive(false);
        }

        public void HideInstructions()
        {
            if (instructionsPanel != null)
                instructionsPanel.SetActive(false);

            if (mainPanel != null)
                mainPanel.SetActive(true);
        }
    }
}