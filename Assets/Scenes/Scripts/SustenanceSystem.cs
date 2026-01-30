using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SustenanceSystem : MonoBehaviour 
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Image sustenanceBar;
    [SerializeField] private int chunkWidth = 16;
    [SerializeField] private float maxSustenance = 5f;
    [SerializeField] private float sustenanceDecreaseRate = 0.1f;
    
    private int currentInstance = 0;
    private float currentSustenance = 0f;
    private bool isGameOver = false;

    void Start() {
        if (cameraTransform == null && Camera.main != null) {
            cameraTransform = Camera.main.transform;
        }
        
        currentSustenance = maxSustenance;
        
        if (sustenanceBar != null) {
            sustenanceBar.fillAmount = 1f;
        }
        
        Debug.Log("Sustenance System started.");
    }

    void Update() {
        if (cameraTransform == null || isGameOver) return;
        
        DecreaseSustenance(sustenanceDecreaseRate * Time.deltaTime);
        
        int previousInstance = currentInstance;
        currentInstance = Mathf.FloorToInt(cameraTransform.position.x / chunkWidth);
        
        if (currentInstance != previousInstance) {
            Debug.Log("=== ENTERED INSTANCE: " + currentInstance + " ===");
            IncreaseSustenance(1f);
        }
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

    public int GetCurrentInstance() {
        return currentInstance;
    }
}
