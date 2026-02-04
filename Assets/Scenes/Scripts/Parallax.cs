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

        // Reposition for endless scrolling
        if (Mathf.Abs(cam.position.x - transform.position.x) >= textureUnitSizeX)
        {
            float offsetPositionX = (cam.position.x - transform.position.x) % textureUnitSizeX;
            transform.position = new Vector3(cam.position.x + offsetPositionX, transform.position.y);
        }
    }
}