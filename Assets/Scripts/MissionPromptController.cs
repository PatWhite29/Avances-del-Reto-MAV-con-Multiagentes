using UnityEngine;

[DisallowMultipleComponent]
public class MissionPromptController : MonoBehaviour
{
    [Header("Global Mission Prompt")]
    [SerializeField] private string defaultMissionQuery = "person with orange jacket and yellow hard hat";
    [SerializeField] private bool applyOnStart = true;

    [Header("Optional Runtime UI (IMGUI)")]
    [SerializeField] private bool useRuntimeGUI = false;
    [SerializeField] private Rect guiRect = new Rect(12, 12, 420, 80);

    private string runtimeText;

    void Awake()
    {
        runtimeText = defaultMissionQuery;
    }

    void Start()
    {
        if (applyOnStart)
            ApplyDefaultMissionQuery();
    }

    // Llamable desde UI (InputField/TMP_InputField) vía UnityEvent<string>
    public void OnUIEndEdit(string text)
    {
        SetMissionQuery(text);
    }

    [ContextMenu("Apply Default Mission Query")]
    void ContextApply()
    {
        ApplyDefaultMissionQuery();
    }

    public void SetMissionQuery(string q)
    {
        if (q == null) q = string.Empty;
        var detectors = FindObjectsOfType<AttributeDetector>(true);
        for (int i = 0; i < detectors.Length; i++)
        {
            if (detectors[i] != null)
                detectors[i].missionQuery = q;
        }
        runtimeText = q;
        Debug.Log($"MissionPromptController: applied missionQuery to {detectors.Length} detectors => '{q}'");
    }

    void ApplyDefaultMissionQuery()
    {
        SetMissionQuery(defaultMissionQuery);
    }

    void OnGUI()
    {
        if (!useRuntimeGUI) return;

        var r = guiRect;
        GUI.Box(r, "Mission Prompt");
        var textRect = new Rect(r.x + 8, r.y + 24, r.width - 16, 22);
        var btnRect  = new Rect(r.x + r.width - 88, r.y + r.height - 30, 80, 22);

        runtimeText = GUI.TextField(textRect, runtimeText ?? string.Empty, 256);
        if (GUI.Button(btnRect, "Apply"))
            SetMissionQuery(runtimeText);
    }
}

