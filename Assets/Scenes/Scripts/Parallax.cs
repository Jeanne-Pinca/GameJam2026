using UnityEngine;

public class Parallax : MonoBehaviour
{
    [Header("Manual Adjustment")]
    [Range(0, 1)] 
    public float parallaxFactor; // 1 = stays with camera, 0 = stays static

    private Transform cam;
    private Vector3 lastCamPos;
    private float textureUnitSizeX;

    void Start()
    {
        cam = Camera.main.transform;
        lastCamPos = cam.position;

        // Get the width of your sprite to calculate the repeat point
        Sprite sprite = GetComponent<SpriteRenderer>().sprite;
        textureUnitSizeX = sprite.texture.width / sprite.pixelsPerUnit;
    }

    void LateUpdate()
    {
        Vector3 deltaMovement = cam.position - lastCamPos;
        // Move the layer based on the camera's delta movement and your manual factor
        transform.position += new Vector3(deltaMovement.x * parallaxFactor, 0, 0);
        lastCamPos = cam.position;

        // Seamless endless scrolling with multiple sprites
        float distanceFromCamera = transform.position.x - cam.position.x;
        
        // When sprite moves more than one full width away, reposition it on the other side
        if (distanceFromCamera > textureUnitSizeX)
        {
            transform.position = new Vector3(transform.position.x - (textureUnitSizeX * 2f), transform.position.y, transform.position.z);
        }
        else if (distanceFromCamera < -textureUnitSizeX)
        {
            transform.position = new Vector3(transform.position.x + (textureUnitSizeX * 2f), transform.position.y, transform.position.z);
        }
    }
}