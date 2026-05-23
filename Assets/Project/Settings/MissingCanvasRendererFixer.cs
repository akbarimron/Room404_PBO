using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MissingCanvasRendererFixer : MonoBehaviour
{
    private float nextScanTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateFixer()
    {
        GameObject fixerObject = new GameObject(nameof(MissingCanvasRendererFixer));
        DontDestroyOnLoad(fixerObject);
        fixerObject.hideFlags = HideFlags.HideAndDontSave;
        fixerObject.AddComponent<MissingCanvasRendererFixer>();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        FixMissingCanvasRenderers();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextScanTime)
            return;

        nextScanTime = Time.unscaledTime + 0.5f;
        FixMissingCanvasRenderers();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FixMissingCanvasRenderers();
    }

    private static void FixMissingCanvasRenderers()
    {
        Graphic[] graphics = Resources.FindObjectsOfTypeAll<Graphic>();
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic graphic = graphics[i];
            if (graphic == null)
                continue;

            GameObject graphicObject = graphic.gameObject;
            if (graphicObject == null || !graphicObject.scene.IsValid())
                continue;

            if (graphicObject.GetComponent<CanvasRenderer>() == null)
                graphicObject.AddComponent<CanvasRenderer>();
        }
    }
}
