using System.Collections;
using UnityEngine;

public class PlantGrowthAnimator : MonoBehaviour
{
    private static int activeGrowthAnimations = 0; // Track how many plant growth animations are currently active
    
    public static bool IsPlantGrowing => activeGrowthAnimations > 0;
    
    public static IEnumerator AnimatePlantGrowth(GameObject seed, float targetScale, Transform playerPos, MonoBehaviour monoBehaviour = null, float animationDuration = 0.5f)
    {
        activeGrowthAnimations++; // Increment counter when animation starts
        float elapsedTime = 0f;
        
        Vector3 startPos = seed.transform.position;
        Vector3 originalScale = seed.transform.localScale;
        
        // Get the plant's collider
        Collider2D plantCollider = seed.GetComponent<Collider2D>();
        
        // Calculate the plant's initial height for bottom-anchored scaling
        float plantHeight = plantCollider != null ? plantCollider.bounds.size.y : 1f;
        
        // Player positioning - keep them on top for first 70% of animation
        Vector3 lastPlayerPos = Vector3.zero;
        bool playerPosSet = false;
        
        while (elapsedTime < animationDuration) {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / animationDuration;
            
            // Apply bounce easing to progress
            float easedProgress = EaseOutBounce(progress);
            
            // Start at 0.5x scale and grow to 1.0x
            float baseScale = Mathf.Lerp(0.5f, 1f, easedProgress);
            
            // Add squash and stretch on top of the growth
            float stretchY = baseScale + (Mathf.Sin(progress * Mathf.PI * 2f) * 0.1f);
            float squashX = baseScale - (Mathf.Sin(progress * Mathf.PI * 2f) * 0.05f);
            
            seed.transform.localScale = new Vector3(
                originalScale.x * squashX,
                originalScale.y * stretchY,
                originalScale.z
            );
            
            // Adjust position so plant grows from the bottom (not center)
            // Move up by half the scale difference to keep bottom anchored
            float scaleOffset = (stretchY - 1f) * (plantHeight * 0.5f);
            seed.transform.position = new Vector3(startPos.x, startPos.y + scaleOffset, startPos.z);
            
            // If player is on seed, move them up with the plant's growth for first 70% of animation
            if (playerPos != null && progress <= 0.7f) {
                // Keep player at plant's X (which doesn't change) and on top of collider Y
                float colliderTop = plantCollider != null ? plantCollider.bounds.max.y : (seed.transform.position.y + (targetScale * 0.5f));
                playerPos.position = new Vector3(seed.transform.position.x, colliderTop + 0.5f, playerPos.position.z);
                lastPlayerPos = playerPos.position;
                playerPosSet = true;
            } else if (playerPos != null && playerPosSet && progress > 0.7f) {
                // Keep player at last position for remaining 30% of animation
                playerPos.position = lastPlayerPos;
            }
            
            yield return null;
        }
        
        // Ensure final position and scale are set exactly to original
        seed.transform.position = startPos;
        seed.transform.localScale = originalScale;
        
        activeGrowthAnimations--; // Decrement counter when animation finishes
        
        // Start swaying the plant after it finishes growing
        if (seed != null && monoBehaviour != null)
        {
            monoBehaviour.StartCoroutine(SwayPlant(seed));
        }
    }
    
    // Continuous swaying effect for planted plants
    public static IEnumerator SwayPlant(GameObject plant)
    {
        float elapsedTime = 0f;
        Vector3 originalPos = plant.transform.position;
        Vector3 originalScale = plant.transform.localScale;
        Collider2D plantCollider = plant.GetComponent<Collider2D>();
        float plantHeight = plantCollider != null ? plantCollider.bounds.size.y : 1f;
        
        while (plant != null)
        {
            elapsedTime += Time.deltaTime;
            
            // Only distort the top by scaling Y while keeping bottom anchored
            float stretchAmount = 1f + (Mathf.Sin(elapsedTime * 2f) * 0.08f); // Compress and stretch by 15%
            plant.transform.localScale = new Vector3(originalScale.x, originalScale.y * stretchAmount, originalScale.z);
            
            // Adjust position to keep the bottom anchored while only the top moves
            float positionOffset = (stretchAmount - 1f) * (plantHeight * 0.5f);
            plant.transform.position = new Vector3(originalPos.x, originalPos.y + positionOffset, originalPos.z);
            
            yield return null;
        }
    }
    
    // Bounce easing function for a fun bouncy effect
    static float EaseOutBounce(float t)
    {
        const float n1 = 7.5625f;
        const float d1 = 2.75f;

        if (t < 1f / d1) {
            return n1 * t * t;
        } else if (t < 2f / d1) {
            return n1 * (t -= 1.5f / d1) * t + 0.75f;
        } else if (t < 2.5f / d1) {
            return n1 * (t -= 2.25f / d1) * t + 0.9375f;
        } else {
            return n1 * (t -= 2.625f / d1) * t + 0.984375f;
        }
    }
}
