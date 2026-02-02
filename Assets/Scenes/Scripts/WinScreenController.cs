using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinScreenController : MonoBehaviour {
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private GameObject winRoot;

    void Awake() {
        if (winRoot == null) {
            winRoot = gameObject;
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Return)) {
            if (winRoot.activeSelf) {
                Time.timeScale = 1f;
                winRoot.SetActive(false);
            } else {
                Show(Time.timeSinceLevelLoad);
            }
        }
    }

    public void Show(float timeSpent) {
        winRoot.SetActive(true);
        Time.timeScale = 0f;
        timeText.text = $"{Format(timeSpent)}";
    }

    public void Retry() {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    string Format(float t) {
        int m = Mathf.FloorToInt(t / 60f);
        int s = Mathf.FloorToInt(t % 60f);
        int ms = Mathf.FloorToInt((t * 100f) % 100f);
        return $"{m:0}:{s:00}:{ms:00}";
    }
}