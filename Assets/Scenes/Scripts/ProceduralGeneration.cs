using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class ProceduralGeneration : MonoBehaviour
{
    [SerializeField] int width, height;
    [SerializeField] float smoothness;
    [SerializeField] float seed;
    [SerializeField] TileBase groundTile_Past;
    [SerializeField] TileBase groundTile_Present;
    [SerializeField] Tilemap groundTilemap_Past;
    [SerializeField] Tilemap groundTilemap_Present;
    [SerializeField] Transform targetCamera;
    [SerializeField] int chunksAhead = 1;
    [SerializeField] int chunksBehind = 1;
    private int initialLeftBoundary = 0; // Prevents chunks from being generated behind the start
    private bool allowBackwardGeneration = false; // Allows behind chunks only after player moves forward
    private GameObject leftBoundaryWall; // Invisible wall to prevent going too far left
    private int playerStartChunk = 0; // Track which chunk the player starts in
    HashSet<int> generatedChunks = new HashSet<int>();
    int lastCenterChunk = int.MinValue;

    void Start()
    {
        // Randomize seed every play for different terrain generation
        seed = Random.Range(0, 10000);
        
        if (targetCamera == null && Camera.main != null)
        {
            targetCamera = Camera.main.transform;
        }
        GenerateInitialChunks();
    }

    void Update()
    {
        if (targetCamera == null)
        {
            return;
        }

        int centerChunk = Mathf.FloorToInt(targetCamera.position.x / width);
        if (centerChunk != lastCenterChunk)
        {
            UpdateChunks(centerChunk);
            lastCenterChunk = centerChunk;
        }
    }

    /* void Update() // for testing purposes - comment it out or remove later
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            GenerateMap();
        }
    }
    */
    void GenerateInitialChunks()
    {
        groundTilemap_Past.ClearAllTiles();
        groundTilemap_Present.ClearAllTiles();
        generatedChunks.Clear();
        lastCenterChunk = int.MinValue;
        allowBackwardGeneration = false; // Disable backward generation on start
        if (targetCamera != null)
        {
            // Calculate the leftmost chunk visible in the camera
            Camera cam = targetCamera.GetComponent<Camera>();
            float cameraWidth = cam.orthographicSize * 2f * cam.aspect;
            float cameraLeftEdge = targetCamera.position.x - (cameraWidth / 2f);
            int leftChunk = Mathf.FloorToInt(cameraLeftEdge / width);
            
            initialLeftBoundary = leftChunk; // Set boundary at the left edge of camera view
            int centerChunk = Mathf.FloorToInt(targetCamera.position.x / width);
            playerStartChunk = centerChunk; // Track the actual player's chunk (camera center)
            UpdateChunks(centerChunk);
            lastCenterChunk = centerChunk;
            
            // Create invisible wall at the left boundary to prevent going back
            CreateLeftBoundaryWall();
        }
    }

    void CreateLeftBoundaryWall()
    {
        // Create an invisible wall at the left boundary to the left of where player spawns
        leftBoundaryWall = new GameObject("LeftBoundaryWall");
        leftBoundaryWall.transform.position = new Vector3(initialLeftBoundary * width + 8f, height / 2f, 0f);
        
        // Add collider for the wall
        BoxCollider2D wallCollider = leftBoundaryWall.AddComponent<BoxCollider2D>();
        wallCollider.size = new Vector2(1f, height * 2f); // Tall wall to block all heights
        wallCollider.isTrigger = false; // Solid wall
        
        // Make it invisible (no renderer)
        Debug.Log("Left boundary wall created at X position: " + leftBoundaryWall.transform.position.x);
    }

    public int[,] GenerateArray(int width, int height, bool empty)
    {
        int[,] map = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                map[x, y] = (empty) ? 0 : 1;
            }
        }
        return map;
    }

    public int[,] TerrainGeneration(int[,] map, int startX)
    {
        // Default ground height (3 units from bottom)
        int groundHeight = 3;
        
        // Fill ground layer
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < groundHeight; y++)
            {
                map[x, y] = 1;
            }
        }
        
        // Generate random platforms
        GeneratePlatforms(map, startX, groundHeight);
        
        return map;
    }

    void GeneratePlatforms(int[,] map, int startX, int groundHeight)
    {
        // Instances 0, 1, 2, 3, 4, 5 and starting chunk have no platforms
        int chunkIndex = startX / width;
        if (chunkIndex == 0 || chunkIndex == 1 || chunkIndex == 2 || chunkIndex == 3 || chunkIndex == 4 || chunkIndex == 5 || chunkIndex == playerStartChunk)
        {
            Debug.Log($"Chunk {chunkIndex} - No platforms (no platform zone)");
            return;
        }
        
        // Use chunk position + seed to create unique but deterministic seed for this chunk
        Random.InitState(startX + (int)seed);
        
        // Generate exactly 2 platforms per chunk
        int numPlatforms = 2;
        int[] platformWidths = { 2, 3, 3 };
        int[] platformHeights = { 2, 3, 5 };
        
        // Heights for platforms (ensure varying heights)
        int[] platformHeightsPerPlatform = new int[numPlatforms];
        platformHeightsPerPlatform[0] = Random.Range(2, 4);  // Platform 1: 2-3 units tall
        
        if (numPlatforms >= 2)
            platformHeightsPerPlatform[1] = Random.Range(3, 6);  // Platform 2: 3-5 units tall
        
        if (numPlatforms >= 3)
            platformHeightsPerPlatform[2] = Random.Range(2, 4);  // Platform 3: 2-3 units tall
        
        // Shuffle to randomize which platform is the tower
        int towerIndex = Random.Range(0, numPlatforms);
        platformHeightsPerPlatform[towerIndex] = Random.Range(5, 8); // Make this one a tower (5-7 units)
        
        // Positions for the platforms (distributed across chunk with at least 2 unit gaps)
        int[] platformXPositions = new int[numPlatforms];
        int[] platformWidthsForPlatforms = new int[numPlatforms];
        
        // Platform 1: left section
        platformWidthsForPlatforms[0] = platformWidths[Random.Range(0, platformWidths.Length)];
        platformXPositions[0] = Random.Range(0, width / 5);
        
        if (numPlatforms >= 2)
        {
            // Platform 2: middle section with at least 2 unit gap from platform 1
            platformWidthsForPlatforms[1] = platformWidths[Random.Range(0, platformWidths.Length)];
            int minStartPlatform2 = platformXPositions[0] + platformWidthsForPlatforms[0] + 2;
            platformXPositions[1] = Random.Range(minStartPlatform2, Mathf.Min(2 * width / 3, width - platformWidthsForPlatforms[1]));
        }
        
        if (numPlatforms >= 3)
        {
            // Platform 3: right section with at least 2 unit gap from platform 2
            platformWidthsForPlatforms[2] = platformWidths[Random.Range(0, platformWidths.Length)];
            int minStartPlatform3 = platformXPositions[1] + platformWidthsForPlatforms[1] + 2;
            platformXPositions[2] = Random.Range(minStartPlatform3, Mathf.Min(width - platformWidthsForPlatforms[2], width - 1));
        }
        
        // Heights for platforms (y-axis positions)
        int[] platformYPositions = new int[numPlatforms];
        platformYPositions[0] = Random.Range(groundHeight + 3, groundHeight + 6);
        if (numPlatforms >= 2)
            platformYPositions[1] = Random.Range(groundHeight + 2, groundHeight + 5);
        if (numPlatforms >= 3)
            platformYPositions[2] = Random.Range(groundHeight + 3, groundHeight + 6);
        
        // Determine which platform will be floating (not touching ground or other platforms)
        int floatingPlatformIndex = Random.Range(0, numPlatforms);
        
        int platformsPlaced = 0;
        
        // Place the platforms
        for (int i = 0; i < numPlatforms; i++)
        {
            int x = platformXPositions[i];
            int platformWidth = platformWidthsForPlatforms[i];
            int platformHeight = platformHeightsPerPlatform[i];
            int y = platformYPositions[i];
            
            // Adjust y if not floating
            if (i != floatingPlatformIndex)
            {
                y = groundHeight; // Place on ground
            }
            
            // Ensure platform doesn't go out of bounds
            if (x + platformWidth > width)
            {
                platformWidth = width - x;
            }
            
            // For platforms that would go out of bounds, adjust height instead of skipping
            if (y + platformHeight >= height)
            {
                platformHeight = height - y - 1;
                if (platformHeight < 1)
                    continue; // Only skip if we can't fit even 1 unit
            }
            
            // Place the platform
            for (int px = x; px < x + platformWidth && px < width; px++)
            {
                for (int py = y; py < y + platformHeight && py < height; py++)
                {
                    if (py >= 0)
                    {
                        map[px, py] = 1;
                    }
                }
            }
            
            platformsPlaced++;
            Debug.Log($"Platform {i + 1} - X: {x}, Y: {y}, Width: {platformWidth}, Height: {platformHeight}, Floating: {i == floatingPlatformIndex}, Tower: {i == towerIndex}");
        }
        
        Debug.Log($"Total platforms placed in chunk {startX}: {platformsPlaced}");
    }

    public void RenderMap(int[,] map, Tilemap groundTilemap, TileBase groundTilebase, int startX)
    {
        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                if (map[x, y] == 1)
                {
                    groundTilemap.SetTile(new Vector3Int(startX + x, y, 0), groundTilebase);
                }
            }
        }
    }

    void UpdateChunks(int centerChunk)
    {
        // Enable backward generation only once player moves forward from the initial boundary
        if (centerChunk > initialLeftBoundary) {
            allowBackwardGeneration = true;
        }
        
        // Never generate behind initialLeftBoundary - only ahead
        int minChunk = allowBackwardGeneration ? Mathf.Max(centerChunk - chunksBehind, initialLeftBoundary) : initialLeftBoundary;
        
        for (int chunk = minChunk; chunk <= centerChunk + chunksAhead; chunk++)
        {
            if (!generatedChunks.Contains(chunk))
            {
                GenerateChunk(chunk);
                generatedChunks.Add(chunk);
            }
        }

        List<int> toRemove = new List<int>();
        foreach (int chunk in generatedChunks)
        {
            if (chunk < minChunk || chunk > centerChunk + chunksAhead)
            {
                toRemove.Add(chunk);
            }
        }

        foreach (int chunk in toRemove)
        {
            DeleteChunk(chunk);
            generatedChunks.Remove(chunk);
        }
    }

    void GenerateChunk(int chunkIndex)
    {
        int startX = chunkIndex * width;
        int[,] map = GenerateArray(width, height, true);
        map = TerrainGeneration(map, startX);
        RenderMap(map, groundTilemap_Past, groundTile_Past, startX);
        RenderMap(map, groundTilemap_Present, groundTile_Present, startX);
    }

    void DeleteChunk(int chunkIndex)
    {
        int startX = chunkIndex * width;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                groundTilemap_Past.SetTile(new Vector3Int(startX + x, y, 0), null);
                groundTilemap_Present.SetTile(new Vector3Int(startX + x, y, 0), null);
            }
        }
    }

}
