using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TimeSwitch : MonoBehaviour 
{
    public bool isPastMode = false;
    [SerializeField] private GameObject maskUI;
    [SerializeField] private MaskIcon maskIcon;
    [SerializeField] private Tilemap groundTilemap_Past;
    [SerializeField] private Tilemap groundTilemap_Present;
    [SerializeField] private Color pastModeColor = new Color(0.5f, 0.5f, 0.5f); // Past mode background color
    [SerializeField] private Color presentModeColor = new Color(0.3f, 0.5f, 0.8f); // Present mode background color
    [SerializeField] private Color presentFilterColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Gray filter for present mode
    // TODO: When importing assets, use ResourceLoad: Camera.main.backgroundColor = Resources.Load<Color>("Colors/PastColor");
    
    private PlantingSystem plantingSystem;
    private Camera mainCamera;
    private SpriteRenderer[] backgroundSprites;

    void Start() {
        plantingSystem = GetComponent<PlantingSystem>();
        mainCamera = Camera.main;
        
        // Get MaskIcon from maskUI if not assigned
        if (maskUI != null && maskIcon == null) {
            maskIcon = maskUI.GetComponent<MaskIcon>();
        }
        
        // Get all background sprite renderers
        GameObject backgroundMain = GameObject.Find("BackgroundMain");
        if (backgroundMain != null) {
            backgroundSprites = backgroundMain.GetComponentsInChildren<SpriteRenderer>();
        }
        
        // Initialize to present mode
        isPastMode = false;
        ToggleTimelineVisibility();
        ChangeCameraBackgroundColor();
        UpdateMaskOpacity();
        UpdateBackgroundFilter();
        ToggleTilemaps();
    }

    void Update() {
        // 1. Toggle Mode with 'E'
        if (Input.GetKeyDown(KeyCode.E)) {
            // Don't allow time switching if plants are currently growing
            if (PlantGrowthAnimator.IsPlantGrowing) {
                Debug.Log("Cannot switch time while plants are growing!");
                return;
            }
            
            bool wasPastMode = isPastMode;
            isPastMode = !isPastMode;
            
            ToggleTimelineVisibility();
            ChangeCameraBackgroundColor();
            UpdateMaskOpacity();
            UpdateBackgroundFilter();
            ToggleTilemaps();
            
            if (plantingSystem != null) {
                if (isPastMode && !wasPastMode) {
                    // Entering past mode: scale seeds down and clear them
                    plantingSystem.ScalePlantedSeedsToPastMode();
                    plantingSystem.ClearPlantedPositions();
                    Debug.Log("Entered PAST: Planting enabled.");
                } else if (!isPastMode && wasPastMode) {
                    // Entering present mode: scale seeds to grown heights
                    plantingSystem.ScalePlantedSeedsToPresentMode();
                    Debug.Log("Entered PRESENT: Seeds grown.");
                }
            }
        }
    }

    void ToggleTimelineVisibility() {
        // Toggle everything currently in the scene
        UpdateObjectsWithTag("PastOnly", isPastMode);
        UpdateObjectsWithTag("PresentOnly", !isPastMode);
    }

    void ChangeCameraBackgroundColor() {
        if (mainCamera != null) {
            mainCamera.backgroundColor = isPastMode ? pastModeColor : presentModeColor;
        }
    }

    void UpdateMaskOpacity() {
        if (maskIcon != null) {
            // 100% opacity (alpha = 1) in past mode, 30% opacity (alpha = 0.3) in present mode
            maskIcon.SetOpacity(isPastMode ? 1f : 0.3f);
        }
    }

    void UpdateBackgroundFilter() {
        if (backgroundSprites != null && backgroundSprites.Length > 0) {
            foreach (SpriteRenderer sprite in backgroundSprites) {
                if (sprite != null) {
                    // Apply gray filter in present mode, white (no filter) in past mode
                    sprite.color = isPastMode ? Color.white : presentFilterColor;
                }
            }
        }
    }

    // Helper to keep code clean
    void UpdateObjectsWithTag(string tagName, bool shouldBeVisible) {
        GameObject[] objects = GameObject.FindGameObjectsWithTag(tagName);
        foreach(GameObject obj in objects) {
            Renderer r = obj.GetComponent<Renderer>();
            if(r != null) r.enabled = shouldBeVisible;
        }
    }

    void ToggleTilemaps() {
        if (groundTilemap_Past != null) {
            groundTilemap_Past.gameObject.SetActive(isPastMode);
        }
        if (groundTilemap_Present != null) {
            groundTilemap_Present.gameObject.SetActive(!isPastMode);
        }
    }
}