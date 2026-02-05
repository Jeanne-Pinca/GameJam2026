using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SustenanceSystem : MonoBehaviour 
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Image sustenanceBar;
    [SerializeField] private int chunkWidth = 16;
    [SerializeField] private int maxInstance = 10;
    [SerializeField] private WinScreenController winScreen;
    [SerializeField] private float maxSustenance = 5f;
    [SerializeField] private float sustenanceDecreaseRate = 0.1f;
    [SerializeField] private float plantingBonusSustenance = 1f; 
    
    private int currentInstance = 0;
    private int previousInstance = -1;
    private float currentSustenance = 0f;
    private bool isGameOver = false;
    private bool hasWon = false;
    private bool plantedInPreviousInstance = false;
    private bool hasReceivedInstanceBonus = false;
    private bool isPaused = true; // Start paused, unpause at instance 3
    private PlantingSystem plantingSystem;
    private TimeSwitch timeSwitch;

    void Start() {
        if (cameraTransform == null && Camera.main != null) {
            cameraTransform = Camera.main.transform;
        }
        
        plantingSystem = FindObjectOfType<PlantingSystem>();
        timeSwitch = FindObjectOfType<TimeSwitch>();
        
        if (plantingSystem == null) {
            Debug.LogError("PlantingSystem not found!");
        }
        
        if (timeSwitch == null) {
            Debug.LogError("TimeSwitch not found!");
        }
        
        currentSustenance = maxSustenance;
        
        if (sustenanceBar != null) {
            sustenanceBar.fillAmount = 1f;
        }
        
        Debug.Log("Sustenance System started.");
    }

    void Update() {
        if (cameraTransform == null || isGameOver) return;
        
        // Only decrease sustenance if not paused
        if (!isPaused) {
            DecreaseSustenance(sustenanceDecreaseRate * Time.deltaTime);
        }
        
        previousInstance = currentInstance;
        currentInstance = Mathf.FloorToInt(cameraTransform.position.x / chunkWidth);
        
        // Check if we entered a new instance
        if (currentInstance != previousInstance) {
            Debug.Log("=== ENTERED INSTANCE: " + currentInstance + " FROM: " + previousInstance + " ===");
            OnEnterNewInstance();
        }

        if (!hasWon && currentInstance >= maxInstance) {
            hasWon = true;
            if (winScreen != null) {
                winScreen.Show(Time.timeSinceLevelLoad);
            } else {
                Debug.LogWarning("WinScreenController not assigned.");
            }
        }
    }
    
    void OnEnterNewInstance() {
        // Start decreasing sustenance bar when entering instance 3
        if (currentInstance >= 3 && isPaused) {
            isPaused = false;
            Debug.Log("▶ Sustenance bar STARTED decreasing at instance 3");
        }
        
        // Award +1 sustenance ONLY if ALL conditions are met:
        // 1. Player planted in previous instance
        // 2. Player enters next instance (this method is called)
        // 3. Currently in present mode (not past mode)
        // 4. All passed seeds are grown (prevents time-switching abuse)
        if (plantingSystem != null && timeSwitch != null && 
            plantedInPreviousInstance && 
            !timeSwitch.isPastMode && 
            AreAllPassedSeedsGrown() && 
            !hasReceivedInstanceBonus) {
            IncreaseSustenance(plantingBonusSustenance);
            hasReceivedInstanceBonus = true;
            Debug.Log("✓ Bonus sustenance awarded! Planted in previous instance + all seeds grown in present mode!");
        }
        
        // Reset tracking for new instance
        plantedInPreviousInstance = false;
        hasReceivedInstanceBonus = false;
    }
    
    bool AreAllPassedSeedsGrown() {
        if (plantingSystem == null) return false;
        
        // Get passed seeds from planting system
        var passedSeeds = plantingSystem.GetPassedSeeds();
        var grownSeeds = plantingSystem.GetGrownSeeds();
        
        if (passedSeeds.Count == 0) return false;
        
        // Check if all passed seeds are also grown
        foreach (GameObject seed in passedSeeds) {
            if (seed != null && !grownSeeds.Contains(seed)) {
                return false; // Found a passed seed that hasn't grown yet
            }
        }
        
        return true; // All passed seeds are grown
    }
    
    public void NotifyPlantedInCurrentInstance() {
        plantedInPreviousInstance = true;
    }
    
    void DecreaseSustenance(float amount) {
        currentSustenance = Mathf.Max(currentSustenance - amount, 0f);
        UpdateSustenanceBar();
        
        if (currentSustenance <= 0f && !isGameOver) {
            GameOver();
        }
    }

    void IncreaseSustenance(float amount) {
        currentSustenance = Mathf.Min(currentSustenance + amount, maxSustenance);
        UpdateSustenanceBar();
        Debug.Log("+" + amount + " Sustenance! Total: " + currentSustenance.ToString("F1") + "/" + maxSustenance);
    }
    
    void GameOver() {
        isGameOver = true;
        Debug.Log("GAME OVER! Sustenance depleted!");
        Time.timeScale = 0f;
    }

    void UpdateSustenanceBar() {
        if (sustenanceBar != null) {
            sustenanceBar.fillAmount = currentSustenance / maxSustenance;
        }
    }

    public float GetSustenance() {
        return currentSustenance;
    }

    public bool IsGameOver() {
        return isGameOver;
    }

    public int GetCurrentInstance() {
        return currentInstance;
    }

    public int GetMaxInstance() {
        return maxInstance;
    }
}
