using UnityEngine;

public class LoseScreenManager : MonoBehaviour
{
    [SerializeField] private SustenanceSystem sustenanceSystem;
    [SerializeField] private GameObject loseScreen;

    private bool hasShown = false;

    void Start()
    {
        if (sustenanceSystem == null)
        {
            sustenanceSystem = FindObjectOfType<SustenanceSystem>();
        }

        if (loseScreen != null)
        {
            loseScreen.SetActive(false);
        }
    }

    void Update()
    {
        if (hasShown || sustenanceSystem == null || loseScreen == null)
        {
            return;
        }

        if (sustenanceSystem.IsGameOver())
        {
            hasShown = true;
            if (loseScreen != null)
            {
                loseScreen.SetActive(true);
            }
        }
    }
}
