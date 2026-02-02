using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlantingSystem : MonoBehaviour 
{
    [SerializeField] private GameObject sproutPrefab;
    [SerializeField] private GameObject plant_1Prefab;
    [SerializeField] private GameObject plant_2Prefab;
    [SerializeField] private GameObject plant_3Prefab;
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
    private Dictionary<GameObject, int> maxSafeHeights = new Dictionary<GameObject, int>(); // Store max safe height for each seed
    private HashSet<GameObject> passedSeeds = new HashSet<GameObject>(); // Seeds that player has already passed
    private HashSet<GameObject> grownSeeds = new HashSet<GameObject>(); // Track which seeds have already given sustenance
    private TimeSwitch timeSwitch;
    private SustenanceSystem sustenanceSystem;
    private float clearVicinity = 2f; // Distance threshold for marking seeds as passed

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
                // Skip if already marked as passed
                if (passedSeeds.Contains(seed)) {
                    continue;
                }
                
                float seedX = seedWorldPositions[seed].x;
                Vector3 seedPos = seedWorldPositions[seed];
                
                // Check for platforms/obstacles to the right of the seed (wider range)
                RaycastHit2D platformCheck = Physics2D.Raycast(seedPos, Vector2.right, 5f, groundLayer);
                
                if (platformCheck.collider != null) {
                    // There IS a platform/ground to the right - player must be PAST it
                    float platformRightEdge = platformCheck.point.x + 1f; // Add margin
                    if (playerX > platformRightEdge) {
                        passedSeeds.Add(seed);
                    }
                } else {
                    // No platform found to the right - player must be far enough away
                    if (playerX > seedX + 3f) {
                        passedSeeds.Add(seed);
                    }
                }
            }
        }
    }

    public void ClearPlantedPositions() {
        // Clear all seeds that haven't been passed yet OR seeds under floating platforms when player is near
        List<GameObject> seedsToRemove = new List<GameObject>();
        float playerX = playerPos.position.x;
        
        foreach (GameObject seed in plantedSeeds) {
            if (seed != null && seedWorldPositions.ContainsKey(seed)) {
                Vector3 seedPos = seedWorldPositions[seed];
                
                // Check if seed is under a platform
                RaycastHit2D platformAbove = Physics2D.Raycast(seedPos, Vector2.up, 0.5f, groundLayer);
                bool shouldClearDueToFloatingPlatform = false;
                
                if (platformAbove.collider != null) {
                    Vector3 platformPos = platformAbove.point;
                    
                    // Check if platform has ground support below it (is it floating?)
                    RaycastHit2D groundBelowPlatform = Physics2D.Raycast(platformPos, Vector2.down, 2f, groundLayer);
                    bool platformHasSupport = groundBelowPlatform.collider != null && groundBelowPlatform.collider != platformAbove.collider;
                    
                    if (!platformHasSupport) {
                        // It's a floating platform, now check player's position relative to platform edges
                        // Get platform bounds by raycasting left and right from the hit point
                        float platformLeftEdge = platformPos.x;
                        float platformRightEdge = platformPos.x;
                        
                        // Find left edge
                        RaycastHit2D leftEdgeCheck = Physics2D.Raycast(platformPos, Vector2.left, 10f, groundLayer);
                        if (leftEdgeCheck.collider == platformAbove.collider) {
                            platformLeftEdge = leftEdgeCheck.point.x - 0.5f;
                        }
                        
                        // Find right edge
                        RaycastHit2D rightEdgeCheck = Physics2D.Raycast(platformPos, Vector2.right, 10f, groundLayer);
                        if (rightEdgeCheck.collider == platformAbove.collider) {
                            platformRightEdge = rightEdgeCheck.point.x + 0.5f;
                        }
                        
                        // Check if player is within 1 unit of either edge
                        float distToLeftEdge = Mathf.Abs(playerX - platformLeftEdge);
                        float distToRightEdge = Mathf.Abs(playerX - platformRightEdge);
                        
                        if (distToLeftEdge <= 1f || distToRightEdge <= 1f) {
                            shouldClearDueToFloatingPlatform = true;
                        }
                    }
                }
                
                // Clear if: NOT passed OR should clear due to floating platform
                if (!passedSeeds.Contains(seed) || shouldClearDueToFloatingPlatform) {
                    Destroy(seed);
                    seedsToRemove.Add(seed);
                }
            }
        }
        
        // Remove cleared seeds from lists
        foreach (GameObject seed in seedsToRemove) {
            plantedSeeds.Remove(seed);
            plantedPositions.Remove(seedWorldPositions[seed]);
            seedHeights.Remove(seed);
            seedWorldPositions.Remove(seed);
            maxSafeHeights.Remove(seed);
            passedSeeds.Remove(seed);
        }
        
        if (seedsToRemove.Count > 0) {
            Debug.Log($"Cleared {seedsToRemove.Count} seeds that weren't passed yet!");
        }
    }

    public void ScalePlantedSeedsToPresentMode() {
        // Replace seeds with plant prefabs based on height when entering present mode
        List<GameObject> seedsToReplace = new List<GameObject>(plantedSeeds);
        
        foreach (GameObject seed in seedsToReplace) {
            if (seed != null) {
                // Assign random height if not already assigned
                if (!seedHeights.ContainsKey(seed)) {
                    int maxHeight = maxSafeHeights.ContainsKey(seed) ? maxSafeHeights[seed] : 3;
                    seedHeights[seed] = Random.Range(1, maxHeight + 1); // Random from 1 to maxHeight inclusive
                }
                
                int height = seedHeights[seed];
                Vector3 seedPosition = seed.transform.position;
                
                // Check if player is standing on this seed BEFORE replacement
                bool playerOnSeed = false;
                if (playerPos != null) {
                    float seedHalfWidth = 1f; // Increased width to account for larger plants
                    bool playerWithinHorizontalBounds = playerPos.position.x >= (seedPosition.x - seedHalfWidth) && 
                                                        playerPos.position.x <= (seedPosition.x + seedHalfWidth);
                    
                    float seedTop = seedPosition.y + 0.5f;
                    bool playerOnTopOfSeed = playerPos.position.y >= seedTop - 0.1f && 
                                             playerPos.position.y <= seedTop + 0.2f;
                    
                    if (playerWithinHorizontalBounds && playerOnTopOfSeed) {
                        playerOnSeed = true;
                    }
                }
                
                // Select appropriate prefab based on height
                GameObject plantPrefab = null;
                if (height == 1) {
                    plantPrefab = plant_1Prefab;
                } else if (height == 2) {
                    plantPrefab = plant_2Prefab;
                } else if (height == 3) {
                    plantPrefab = plant_3Prefab;
                }
                
                if (plantPrefab != null) {
                    // Instantiate the plant at the seed position
                    GameObject newPlant = Instantiate(plantPrefab, seedPosition, Quaternion.identity);
                    
                    // Get both colliders
                    Collider2D sproutCollider = seed.GetComponent<Collider2D>();
                    Collider2D plantCollider = newPlant.GetComponent<Collider2D>();
                    
                    // Get the sprout's collider bottom (where it touches the ground)
                    float sproutBottomY = sproutCollider != null ? sproutCollider.bounds.min.y : (seedPosition.y - 0.5f);
                    
                    // Position plant so its collider bottom is at the same Y as the sprout's bottom
                    if (plantCollider != null) {
                        // Calculate how far the plant's collider bottom is from its transform position
                        float plantBottomOffsetFromTransform = newPlant.transform.position.y - plantCollider.bounds.min.y;
                        // Now position it so the collider bottom lands at sproutBottomY
                        float newPlantY = sproutBottomY + plantBottomOffsetFromTransform;
                        newPlant.transform.position = new Vector3(seedPosition.x, newPlantY, 0f);
                    } else {
                        // No plant collider, just use seed position
                        newPlant.transform.position = new Vector3(seedPosition.x, seedPosition.y, 0f);
                    }
                    
                    // Update the plantedSeeds list to reference the new plant instead of the sprout
                    int seedIndex = plantedSeeds.IndexOf(seed);
                    plantedSeeds[seedIndex] = newPlant;
                    
                    // Transfer tracking data to new plant
                    seedHeights[newPlant] = seedHeights[seed];
                    seedHeights.Remove(seed);
                    
                    seedWorldPositions[newPlant] = seedWorldPositions[seed];
                    seedWorldPositions.Remove(seed);
                    
                    maxSafeHeights[newPlant] = maxSafeHeights[seed];
                    maxSafeHeights.Remove(seed);
                    
                    // Transfer passed seed status
                    if (passedSeeds.Contains(seed)) {
                        passedSeeds.Remove(seed);
                        passedSeeds.Add(newPlant);
                    }
                    
                    // Destroy the old sprout
                    Destroy(seed);
                    
                    // NOW check collision against the plant's actual final position
                    float colliderHalfWidth = 0.5f;
                    if (plantCollider != null) {
                        // Use actual bounds which account for collider offset
                        colliderHalfWidth = plantCollider.bounds.extents.x;
                    }
                    
                    // Update player detection with actual collider bounds
                    if (playerPos != null) {
                        // Use the actual collider bounds (left and right edges)
                        float colliderLeft = plantCollider != null ? plantCollider.bounds.min.x : (newPlant.transform.position.x - 0.5f);
                        float colliderRight = plantCollider != null ? plantCollider.bounds.max.x : (newPlant.transform.position.x + 0.5f);
                        bool playerWithinHorizontalBounds = playerPos.position.x >= colliderLeft && 
                                                            playerPos.position.x <= colliderRight;
                        
                        // Use the actual collider top position
                        float colliderTop = plantCollider != null ? plantCollider.bounds.max.y : newPlant.transform.position.y + 0.5f;
                        bool playerOnTopOfSeed = playerPos.position.y >= colliderTop - 0.1f && 
                                                 playerPos.position.y <= colliderTop + 0.2f;
                        
                        if (playerWithinHorizontalBounds && playerOnTopOfSeed) {
                            playerOnSeed = true;
                        }
                    }
                    
                    // Set sprite renderers
                    SpriteRenderer[] renderers = newPlant.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (SpriteRenderer r in renderers) {
                        r.sortingOrder = 10;
                        if (r.CompareTag("PastOnly")) r.enabled = false; // Hide past-only sprites in present mode
                        if (r.CompareTag("PresentOnly")) r.enabled = true; // Show present-only sprites
                    }
                    
                    // Animate the plant with growth animation (pass height for bounce movement, not scaling)
                    StartCoroutine(PlantGrowthAnimator.AnimatePlantGrowth(newPlant, height, playerOnSeed ? playerPos : null, 0.5f));
                    
                    // Track grown plants
                    if (!grownSeeds.Contains(newPlant)) {
                        grownSeeds.Add(newPlant);
                    }
                } else {
                    Debug.LogWarning($"Plant prefab for height {height} is not assigned!");
                }
            }
        }
    }


    public void ScalePlantedSeedsToPastMode() {
        // Replace all plants back to sprouts when entering past mode
        List<GameObject> plantsToReplace = new List<GameObject>(plantedSeeds);
        
        foreach (GameObject plant in plantsToReplace) {
            if (plant != null) {
                if (sproutPrefab != null) {
                    // Get the original position where the seed was planted
                    Vector3 originalPosition = seedWorldPositions.ContainsKey(plant) ? seedWorldPositions[plant] : plant.transform.position;
                    
                    // Instantiate the sprout at the original position
                    GameObject newSprout = Instantiate(sproutPrefab, originalPosition, Quaternion.identity);
                    
                    // Update the plantedSeeds list to reference the new sprout
                    int plantIndex = plantedSeeds.IndexOf(plant);
                    plantedSeeds[plantIndex] = newSprout;
                    
                    // Transfer tracking data to new sprout
                    if (seedHeights.ContainsKey(plant)) {
                        seedHeights[newSprout] = seedHeights[plant];
                        seedHeights.Remove(plant);
                    }
                    
                    if (seedWorldPositions.ContainsKey(plant)) {
                        seedWorldPositions[newSprout] = seedWorldPositions[plant];
                        seedWorldPositions.Remove(plant);
                    }
                    
                    if (maxSafeHeights.ContainsKey(plant)) {
                        maxSafeHeights[newSprout] = maxSafeHeights[plant];
                        maxSafeHeights.Remove(plant);
                    }
                    
                    // Transfer passed seed status
                    if (passedSeeds.Contains(plant)) {
                        passedSeeds.Remove(plant);
                        passedSeeds.Add(newSprout);
                    }
                    
                    // Destroy the old plant
                    Destroy(plant);
                    
                    // Set sprite renderers for past mode
                    SpriteRenderer[] renderers = newSprout.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (SpriteRenderer r in renderers) {
                        r.sortingOrder = 10;
                        if (r.CompareTag("PastOnly")) r.enabled = true; // Show past-only sprites
                        if (r.CompareTag("PresentOnly")) r.enabled = false; // Hide present-only sprites
                    }
                }
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
        
        // Check all sides of the seed for tile collisions (assuming 1x1 seed size)
        float seedHalfSize = 0.5f;
        
        // Check for collisions at all possible plant heights (1-3 units)
        // to determine the maximum safe height for this position
        int maxSafeHeight = 3;
        for (int height = 1; height <= 3; height++) {
            // Check from current position up to the plant top
            RaycastHit2D topHit = Physics2D.Raycast(spawnPos, Vector2.up, height, groundLayer);
            // Ignore the player when checking for blocking colliders
            if (topHit.collider != null && topHit.collider.gameObject != playerPos.gameObject) {
                maxSafeHeight = height - 1; // Max safe is one less than the blocking height
                if (maxSafeHeight < 1) maxSafeHeight = 1; // Ensure at least height 1 is possible
                break;
            }
        }
        
        // Only allow planting if at least height 1 is safe
        if (maxSafeHeight < 1) {
            Debug.Log("No safe height available for planting!");
            return false;
        }
        
        // Check left side
        RaycastHit2D leftHit = Physics2D.Raycast(spawnPos, Vector2.left, seedHalfSize, groundLayer);
        if (leftHit.collider != null) {
            Debug.Log("Tile blocking left of seed!");
            return false;
        }
        
        // Check right side
        RaycastHit2D rightHit = Physics2D.Raycast(spawnPos, Vector2.right, seedHalfSize, groundLayer);
        if (rightHit.collider != null) {
            Debug.Log("Tile blocking right of seed!");
            return false;
        }
        
        // Check for obstacles on left and right (original obstacle check)
        RaycastHit2D leftObstacleHit = Physics2D.Raycast(spawnPos, Vector2.left, sideCheckDistance, obstacleLayer);
        RaycastHit2D rightObstacleHit = Physics2D.Raycast(spawnPos, Vector2.right, sideCheckDistance, obstacleLayer);
        
        // Can plant if no obstacles on either side
        return leftObstacleHit.collider == null && rightObstacleHit.collider == null;
    }

    void PlantInPast(Vector3 pos) {
        if (sproutPrefab == null) {
            Debug.LogError("Sprout prefab is not assigned.");
            return;
        }
        
        // Determine max safe height for this position
        int maxSafeHeight = 3;
        for (int height = 1; height <= 3; height++) {
            RaycastHit2D topHit = Physics2D.Raycast(pos, Vector2.up, height, groundLayer);
            // Ignore the player when checking for blocking colliders
            if (topHit.collider != null && topHit.collider.gameObject != playerPos.gameObject) {
                maxSafeHeight = height - 1;
                if (maxSafeHeight < 1) maxSafeHeight = 1;
                break;
            }
        }
        
        // Check if planted at the edge of a floating platform - limit height to 1
        RaycastHit2D platformCheck = Physics2D.Raycast(pos, Vector2.up, 0.5f, groundLayer);
        if (platformCheck.collider != null) {
            Vector3 platformPos = platformCheck.point;
            
            // Check if platform is floating
            RaycastHit2D groundBelowPlatform = Physics2D.Raycast(platformPos, Vector2.down, 2f, groundLayer);
            bool platformHasSupport = groundBelowPlatform.collider != null && groundBelowPlatform.collider != platformCheck.collider;
            
            if (!platformHasSupport) {
                // It's a floating platform - check if we're near an edge
                float platformLeftEdge = platformPos.x;
                float platformRightEdge = platformPos.x;
                
                // Find left edge
                RaycastHit2D leftEdgeCheck = Physics2D.Raycast(platformPos, Vector2.left, 10f, groundLayer);
                if (leftEdgeCheck.collider == platformCheck.collider) {
                    platformLeftEdge = leftEdgeCheck.point.x - 0.5f;
                }
                
                // Find right edge
                RaycastHit2D rightEdgeCheck = Physics2D.Raycast(platformPos, Vector2.right, 10f, groundLayer);
                if (rightEdgeCheck.collider == platformCheck.collider) {
                    platformRightEdge = rightEdgeCheck.point.x + 0.5f;
                }
                
                // Check if planted position is within 1.5 units of either edge
                float distToLeftEdge = Mathf.Abs(pos.x - platformLeftEdge);
                float distToRightEdge = Mathf.Abs(pos.x - platformRightEdge);
                
                if (distToLeftEdge <= 1.5f || distToRightEdge <= 1.5f) {
                    // Limit to height 1 to avoid growing into platform
                    maxSafeHeight = Mathf.Min(maxSafeHeight, 1);
                    Debug.Log("Planted at floating platform edge - limiting height to 1");
                }
            }
        }
        
        // Track this position as planted
        plantedPositions.Add(pos);
        
        // Instantiate at the corrected position
        GameObject newSeed = Instantiate(sproutPrefab, pos, Quaternion.identity);
        
        // Track the spawned seed for later cleanup
        plantedSeeds.Add(newSeed);
        
        // Track the world position for clearing logic
        seedWorldPositions[newSeed] = pos;
        
        // Store the max safe height for this seed
        maxSafeHeights[newSeed] = maxSafeHeight;
        
        // Notify sustenance system that a plant was planted in this instance
        if (sustenanceSystem != null) {
            sustenanceSystem.NotifyPlantedInCurrentInstance();
        }
        
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
    
    // Public getters for sustenance system
    public HashSet<GameObject> GetPassedSeeds() {
        return passedSeeds;
    }
    
    public HashSet<GameObject> GetGrownSeeds() {
        return grownSeeds;
    }
}
