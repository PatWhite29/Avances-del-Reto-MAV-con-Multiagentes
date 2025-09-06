using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class PersonDescriptionLister : MonoBehaviour
{
    [Header("Opciones")]
    public bool autoRefreshOnStart = true;
    public bool includeInactive = true;
    public bool groupByDescriptor = true;   // agrupa por texto y muestra conteos
    public bool logToConsole = true;

    [Header("Salida (solo lectura)")]
    [TextArea(5, 20)] public string report; // texto listo para copiar

    void Start()
    {
        if (autoRefreshOnStart)
            Refresh();
    }

    [ContextMenu("Refresh Now")] 
    public void Refresh()
    {
        var list = GetScenePersons(includeInactive);
        if (groupByDescriptor)
        {
            var grouped = list.GroupBy(pd => pd.ShortText())
                              .OrderByDescending(g => g.Count())
                              .ThenBy(g => g.Key);
            var lines = new List<string>();
            foreach (var g in grouped)
                lines.Add($"{g.Key} x{g.Count()}");
            report = string.Join("\n", lines);
        }
        else
        {
            var lines = list.Select(pd => $"{pd.gameObject.name}: {pd.ShortText()}");
            report = string.Join("\n", lines);
        }

        if (logToConsole)
            Debug.Log($"PersonDescriptionLister (count={list.Count}):\n{report}");
    }

    static List<PersonDescriptor> GetScenePersons(bool includeInactive)
    {
#if UNITY_2020_1_OR_NEWER
        if (!includeInactive)
            return FindObjectsOfType<PersonDescriptor>(false).ToList();
        else
        {
            // Incluir inactivos pero filtrar assets/prefabs fuera de escena
            return Resources.FindObjectsOfTypeAll<PersonDescriptor>()
                            .Where(pd => pd != null && pd.gameObject.scene.IsValid())
                            .ToList();
        }
#else
        // Compatibilidad básica
        return FindObjectsOfType<PersonDescriptor>().ToList();
#endif
    }
}

