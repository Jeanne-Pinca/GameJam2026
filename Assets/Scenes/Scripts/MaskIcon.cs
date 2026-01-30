using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaskIcon : MonoBehaviour 
{
    private CanvasGroup canvasGroup;

    void Start() {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void SetOpacity(float opacity) {
        if (canvasGroup != null) {
            canvasGroup.alpha = opacity;
            Debug.Log("Mask opacity set to: " + opacity);
        }
    }
}
