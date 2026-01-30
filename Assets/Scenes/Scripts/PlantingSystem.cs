using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantingSystem : MonoBehaviour 
{
    [SerializeField] private GameObject sproutPrefab;
    [SerializeField] private Transform playerPos;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private float checkDistance = 1.5f;
    [SerializeField] private float sideCheckDistance = 1f;
    [SerializeField] private float playerMinDistance = 1.5f;
    
    private HashSet<Vector3> plantedPositions = new HashSet<Vector3>();
    private List<GameObject> plantedSeeds = new List<GameObject>();
    private Dictionary<GameObject, int> seedHeights = new Dictionary<GameObject, int>(); // Store random height for each seed
    private Dictionary<GameObject, Vector3> seedWorldPositions = new Dictionary<GameObject, Vector3>(); // Track seed world positions
    private HashSet<GameObject> passedSeeds = new HashSet<GameObject>(); // Seeds that player has already passed
    private HashSet<GameObject> grownSeeds = new HashSet<GameObject>(); // Track which seeds have already given sustenance
    private TimeSwitch timeSwitch;
    private SustenanceSystem sustenanceSystem;
    private float clearVicinity = 3f; // Clear seeds when player is within 3 units

    void Start() {
        timeSwitch = GetComponent<TimeSwitch>();
        sustenanceSystem = FindObjectOfType<SustenanceSystem>();
        
        if (sustenanceSystem == null) {
            Debug.LogError("SustenanceSystem not found in scene!");
        }
    }

    void Update() {
        // Plant Seed with 'Q' (Only in Past)
        if (Input.GetKeyDown(KeyCode.Q) && timeSwitch.isPastMode) {
            AttemptPlanting();
        }
        
        // Track which seeds have been passed by the player
        UpdatePassedSeeds();
    }
    
    void UpdatePassedSeeds() {
        float playerX = playerPos.position.x;
        
        foreach (GameObject seed in plantedSeeds) {
            if (seed != null && seedWorldPositions.ContainsKey(seed)) {
                float seedX = seedWorldPositions[seed].x;
                float distanceToSeed = Mathf.Abs(seedX - playerX);
                
                // If player is more than clearVicinity away, mark seed as passed
                if (distanceToSeed > clearVicinity) {
                    passedSeeds.Add(seed);
                }
            }
        }
    }

    public void ClearPlantedPositions() {
        // Only clear seeds that haven't been passed yet and are within vicinity
        List<GameObject> seedsToRemove = new List<GameObject>();
        float playerX = playerPos.position.x;
        
        foreach (GameObject seed in plantedSeeds) {
            if (seed != null && seedWorldPositions.ContainsKey(seed)) {
                // Only clear if NOT already passed and within vicinity
                if (!passedSeeds.Contains(seed)) {
                    float seedX = seedWorldPositions[seed].x;
                    float distanceToSeed = Mathf.Abs(seedX - playerX);
                    
                    if (distanceToSeed <= clearVicinity) {
                        Destroy(seed);
                        seedsToRemove.Add(seed);
                    }
                }
            }
        }
        
        // Remove cleared seeds from lists
        foreach (GameObject seed in seedsToRemove) {
            plantedSeeds.Remove(seed);
            plantedPositions.Remove(seedWorldPositions[seed]);
            seedHeights.Remove(seed);
            seedWorldPositions.Remove(seed);
            passedSeeds.Remove(seed);
        }
        
        Debug.Log($"Cleared fresh seeds within {clearVicinity} units. Passed plants remain!");
    }

    public void ScalePlantedSeedsToPresentMode() {
        // Scale all seeds to random heights when entering present mode
        foreach (GameObject seed in plantedSeeds) {
            if (seed != null) {
                // Assign random height if not already assigned
                if (!seedHeights.ContainsKey(seed)) {
                    seedHeights[seed] = Random.Range(1, 4); // 1, 2, or 3 units
                }
                
                // Scale the seed - only grow upwards from bottom
                float growthScale = seedHeights[seed];
                seed.transform.localScale = new Vector3(1f, growthScale, 1f);
                
                // Move seed up so bottom stays anchored
                Vector3 pos = seed.transform.position;
                pos.y += (growthScale - 1f) * 0.5f;
                seed.transform.position = pos;
                
                // Track grown plants
                if (!grownSeeds.Contains(seed)) {
                    grownSeeds.Add(seed);
                }
                
            }
        }
    }

    public void ScalePlantedSeedsToPastMode() {
        // Scale all seeds back to small when entering past mode
        foreach (GameObject seed in plantedSeeds) {
            if (seed != null) {
                // Get original position and move back down
                if (seedHeights.ContainsKey(seed)) {
                    float growthScale = seedHeights[seed];
                    Vector3 pos = seed.transform.position;
                    pos.y -= (growthScale - 1f) * 0.5f;
                    seed.transform.position = pos;
                }
                
                seed.transform.localScale = new Vector3(1f, 1f, 1f);
            }
        }
    }

    void AttemptPlanting() {
        // Determine which direction the player is facing based on their scale
        float facingDirection = playerPos.localScale.x > 0 ? 1f : -1f;
        
        // Start the ray from player position
        Vector2 rayStart = playerPos.position;
        
        // Visual helper: You will see a red line in the SCENE view showing where the ray hits
        Debug.DrawRay(rayStart, Vector2.down * checkDistance, Color.red, 1f);

        RaycastHit2D hit = Physics2D.Raycast(rayStart, Vector2.down, checkDistance, groundLayer);
        
        if (hit.collider != null) {
            // hit.point is the exact top of the floor tile
            // We add 0.5f to the Y to place the CENTER of the square on top of the tile
            // Plant in the direction the player is facing
            Vector3 spawnPos = new Vector3(hit.point.x + (1f * facingDirection), hit.point.y + 0.5f, 0f);
            
            // Check for obstacles blocking the spot
            if (CanPlantAtPosition(spawnPos)) {
                PlantInPast(spawnPos);
            } else {
                Debug.Log("Cannot plant here! An obstacle is blocking the way.");
            }
        } else {
            Debug.Log("Cannot plant here! No ground detected below player.");
        }
    }

    bool CanPlantAtPosition(Vector3 spawnPos) {
        // Check if already planted at this position
        if (plantedPositions.Contains(spawnPos)) {
            Debug.Log("Already planted here!");
            return false;
        }
        
        // Check if player is too close to the spawn position
        float distToPlayer = Vector3.Distance(playerPos.position, spawnPos);
        if (distToPlayer < playerMinDistance) {
            Debug.Log("Too close to player!");
            return false;
        }
        
        // Check if there's a collider at the spawn position (from planted seeds)
        Collider2D[] collidersAtPos = Physics2D.OverlapCircleAll(spawnPos, 0.2f);
        foreach (Collider2D col in collidersAtPos) {
            if (col.gameObject != gameObject) {
                Debug.Log("Something is blocking this spot!");
                return false;
            }
        }
        
        // Check if the bottom of the seed collider is supported by ground
        // Assuming seed is 1 unit wide, check both left and right edges to ensure full support
        Vector2 seedLeftBottom = new Vector2(spawnPos.x - 0.4f, spawnPos.y - 0.5f);
        Vector2 seedRightBottom = new Vector2(spawnPos.x + 0.4f, spawnPos.y - 0.5f);
        
        RaycastHit2D groundSupportLeft = Physics2D.Raycast(seedLeftBottom, Vector2.down, 0.1f, groundLayer);
        RaycastHit2D groundSupportRight = Physics2D.Raycast(seedRightBottom, Vector2.down, 0.1f, groundLayer);
        
        // Both sides must have ground support
        if (groundSupportLeft.collider == null || groundSupportRight.collider == null) {
            Debug.Log("Seed not fully supported by ground!");
            return false;
        }
        
        // Check for obstacles to the left
        RaycastHit2D leftHit = Physics2D.Raycast(spawnPos, Vector2.left, sideCheckDistance, obstacleLayer);
        // Check for obstacles to the right
        RaycastHit2D rightHit = Physics2D.Raycast(spawnPos, Vector2.right, sideCheckDistance, obstacleLayer);
        
        // Can plant if no obstacles on either side
        return leftHit.collider == null && rightHit.collider == null;
    }

    void PlantInPast(Vector3 pos) {
        if (sproutPrefab == null) {
            Debug.LogError("Sprout prefab is not assigned.");
            return;
        }
        
        // Track this position as planted
        plantedPositions.Add(pos);
        
        // Instantiate at the corrected position
        GameObject newSeed = Instantiate(sproutPrefab, pos, Quaternion.identity);
        
        // Track the spawned seed for later cleanup
        plantedSeeds.Add(newSeed);
        
        // Track the world position for clearing logic
        seedWorldPositions[newSeed] = pos;
        
        // Force Z-axis to 0 to keep it in the correct depth
        newSeed.transform.position = new Vector3(pos.x, pos.y, 0f);

        // Manually refresh visibility and Sorting Order for the new seed's renderers
        SpriteRenderer[] renderers = newSeed.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer r in renderers) {
            // Ensure it is drawn on top of the tilemap (usually Order 0)
            r.sortingOrder = 10; 

            if (r.CompareTag("PastOnly")) r.enabled = timeSwitch.isPastMode;
            if (r.CompareTag("PresentOnly")) r.enabled = !timeSwitch.isPastMode;
        }
    }
}
