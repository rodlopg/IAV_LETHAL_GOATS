using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LethalCompanyAI
{
    /// <summary>
    /// Tracks the item counter UI and shows the end-of-game menu (win or death).
    /// Death is detected through GameManager.onGameOver, victory through ItemCollector.
    /// Add to any GameObject in the level (e.g. the Canvas). References are auto-found if empty.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    [DisallowMultipleComponent]
    public class GameUIManager : MonoBehaviour
    {
        [Header("Scene References (auto-found if empty)")]
        public GameManager gameManager;
        public ItemCollector itemCollector;
        public PlayerController playerController;
        public PlayerMonsterIntegration playerHealth;

        [Header("HUD")]
        public TMP_Text itemCounterText;
        public string counterFormat = "Items: {0}/{1}";
        public TMP_Text healthText;
        public string healthFormat = "Health: {0}/{1}";
        public TMP_Text staminaText;
        public string staminaFormat = "Stamina: {0}%";
        [Tooltip("Optional. An Image with Image Type = Filled for the stamina bar.")]
        public Image staminaFill;

        [Header("End Menu")]
        public GameObject endPanel;
        public TMP_Text endTitleText;
        public TMP_Text endMessageText;
        public Button mainMenuButton;
        [Tooltip("Optional.")]
        public Button restartButton;

        [Header("Messages")]
        public string winTitle = "YOU ESCAPED";
        [TextArea] public string winMessage = "You collected every item and made it out alive.";
        public string loseTitle = "YOU DIED";
        [TextArea] public string loseMessage = "The monsters got you.";

        public bool GameEnded { get; private set; }

        private void Start()
        {
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();
            if (itemCollector == null)
                itemCollector = FindFirstObjectByType<ItemCollector>();
            if (playerController == null)
                playerController = FindFirstObjectByType<PlayerController>();
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<PlayerMonsterIntegration>();

            if (endPanel != null)
                endPanel.SetActive(false);

            if (mainMenuButton != null)
                mainMenuButton.onClick.AddListener(GoToMainMenu);
            if (restartButton != null)
                restartButton.onClick.AddListener(Restart);

            if (itemCollector != null)
            {
                itemCollector.ItemCollected += HandleItemCollected;
                itemCollector.AllItemsCollected += HandleAllItemsCollected;
                UpdateCounter(itemCollector.Collected, itemCollector.Required);
            }
            else
            {
                Debug.LogWarning("GameUIManager: no ItemCollector found in the scene.", this);
            }

            if (gameManager != null)
                gameManager.onGameOver.AddListener(HandlePlayerDied);
            else
                Debug.LogWarning("GameUIManager: no GameManager found, death menu will not show.", this);
        }

        private int lastHealth = -1;
        private int lastStaminaPercent = -1;

        private void Update()
        {
            if (playerController != null)
            {
                if (staminaFill != null)
                    staminaFill.fillAmount = playerController.Stamina01;

                if (staminaText != null)
                {
                    int percent = Mathf.CeilToInt(playerController.Stamina01 * 100f);
                    if (percent != lastStaminaPercent)
                    {
                        lastStaminaPercent = percent;
                        staminaText.text = string.Format(staminaFormat, percent);
                    }
                }
            }

            if (healthText != null && playerHealth != null && playerHealth.CurrentHealth != lastHealth)
            {
                lastHealth = playerHealth.CurrentHealth;
                healthText.text = string.Format(healthFormat, lastHealth, playerHealth.maxHealth);
            }
        }

        private void HandleItemCollected(int collected, int required)
        {
            UpdateCounter(collected, required);
        }

        private void UpdateCounter(int collected, int required)
        {
            if (itemCounterText != null)
                itemCounterText.text = string.Format(counterFormat, collected, required);
        }

        private void HandleAllItemsCollected()
        {
            ShowEndMenu(winTitle, winMessage);
        }

        private void HandlePlayerDied()
        {
            ShowEndMenu(loseTitle, loseMessage);
        }

        private void ShowEndMenu(string title, string message)
        {
            if (GameEnded)
                return; // whichever ending happens first wins

            GameEnded = true;

            if (playerController != null)
                playerController.SetInputEnabled(false);
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (endTitleText != null)
                endTitleText.text = title;
            if (endMessageText != null)
                endMessageText.text = message;
            if (endPanel != null)
                endPanel.SetActive(true);

            Time.timeScale = 0f;
            AudioListener.pause = true;
        }

        public void GoToMainMenu()
        {
            SceneLoader.Instance.LoadMainMenu();
        }

        public void Restart()
        {
            SceneLoader.Instance.ReloadCurrentScene();
        }

        private void OnDestroy()
        {
            if (itemCollector != null)
            {
                itemCollector.ItemCollected -= HandleItemCollected;
                itemCollector.AllItemsCollected -= HandleAllItemsCollected;
            }

            if (gameManager != null)
                gameManager.onGameOver.RemoveListener(HandlePlayerDied);
        }
    }
}