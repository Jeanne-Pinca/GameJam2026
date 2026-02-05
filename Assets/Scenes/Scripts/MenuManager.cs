using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Button playButton;
    [SerializeField] private Button mainMenuQuitButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button pauseMainMenuButton;
    [SerializeField] private Button pauseQuitButton;
    
    private bool isPauseMenuOpen = false;
    private bool isGameRunning = false;
    private float previousTimeScale = 1f;
    
    void Start()
    {
        // Find panels if not assigned
        if (mainMenuPanel == null)
            mainMenuPanel = GameObject.Find("MenuPanel");
        
        if (pauseMenuPanel == null)
            pauseMenuPanel = GameObject.Find("PauseMenuPanel");
        
        // Find main menu buttons
        if (playButton == null && mainMenuPanel != null)
            playButton = mainMenuPanel.transform.Find("PlayButton")?.GetComponent<Button>();
        
        if (mainMenuQuitButton == null && mainMenuPanel != null)
            mainMenuQuitButton = mainMenuPanel.transform.Find("QuitButton")?.GetComponent<Button>();
        
        // Find pause menu buttons
        if (resumeButton == null && pauseMenuPanel != null)
            resumeButton = pauseMenuPanel.transform.Find("ResumeButton")?.GetComponent<Button>();
        
        if (pauseMainMenuButton == null && pauseMenuPanel != null)
            pauseMainMenuButton = pauseMenuPanel.transform.Find("MainMenuButton")?.GetComponent<Button>();
        
        if (pauseQuitButton == null && pauseMenuPanel != null)
            pauseQuitButton = pauseMenuPanel.transform.Find("QuitButton")?.GetComponent<Button>();
        
        // Setup main menu button listeners
        if (playButton != null)
            playButton.onClick.AddListener(StartGame);
        
        if (mainMenuQuitButton != null)
            mainMenuQuitButton.onClick.AddListener(QuitGame);
        
        // Setup pause menu button listeners
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
        
        if (pauseMainMenuButton != null)
            pauseMainMenuButton.onClick.AddListener(GoToMainMenu);
        
        if (pauseQuitButton != null)
            pauseQuitButton.onClick.AddListener(QuitGame);
        
        // Start with main menu open, pause menu closed
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        isGameRunning = false;
        Time.timeScale = 0f; // Pause the game while main menu is open
    }
    
    void Update()
    {
        // Only allow ESC to pause if game is running
        if (isGameRunning && Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPauseMenuOpen)
                ResumeGame();
            else
                PauseGame();
        }
    }
    
    void StartGame()
    {
        isGameRunning = true;
        Time.timeScale = 1f;
        
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        // Reset player position FIRST so camera is at starting location before terrain generation
        PlayerMovement playerMovement = FindObjectOfType<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.ResetPlayer();
        }
        
        // Reset the game by calling the procedural generation reset
        ProceduralGeneration procGen = FindObjectOfType<ProceduralGeneration>();
        if (procGen != null)
        {
            procGen.ResetGame();
        }
        
        // Reset all plants
        PlantingSystem plantingSystem = FindObjectOfType<PlantingSystem>();
        if (plantingSystem != null)
        {
            plantingSystem.ResetPlants();
        }
        
        // Reset sustenance bar
        SustenanceSystem sustenanceSystem = FindObjectOfType<SustenanceSystem>();
        if (sustenanceSystem != null)
        {
            sustenanceSystem.ResetSustenance();
        }
        
        // Reset to present mode
        TimeSwitch timeSwitch = FindObjectOfType<TimeSwitch>();
        if (timeSwitch != null)
        {
            timeSwitch.ResetToPresent();
        }
        
        Debug.Log("Game started");
    }
    
    void PauseGame()
    {
        isPauseMenuOpen = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f; // Pause the game
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
            Debug.Log("Pause menu opened");
        }
    }
    
    void ResumeGame()
    {
        isPauseMenuOpen = false;
        Time.timeScale = previousTimeScale; // Resume the game
        
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
            Debug.Log("Pause menu closed");
        }
    }
    
    void GoToMainMenu()
    {
        isPauseMenuOpen = false;
        isGameRunning = false;
        Time.timeScale = 1f; // Reset time scale
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
        
        Debug.Log("Returned to main menu");
    }
    
    void QuitGame()
    {
        Time.timeScale = 1f; // Reset time scale before quitting
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
