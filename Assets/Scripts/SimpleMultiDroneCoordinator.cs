using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class SimpleMultiDroneCoordinator : MonoBehaviour
{
    [Header("Coordination Settings")]
    [SerializeField] private bool coordinatorEnabled = true;
    [SerializeField] private float reassessInterval = 0.5f;
    [SerializeField] private bool includeInactiveDetectors = true;

    [Header("Debug")]
    [SerializeField] private bool drawDebug = false;
    [SerializeField] private Color debugLineColor = new Color(0f, 1f, 1f, 0.9f);

    private float lastReassessTime = -999f;

    // Estado actual de coordinación (solo memoria local)
    private readonly List<AttributeDetector> detectors = new List<AttributeDetector>();
    private readonly Dictionary<AttributeDetector, Transform> currentAssignments = new Dictionary<AttributeDetector, Transform>();
    private readonly HashSet<Transform> claimedTargets = new HashSet<Transform>();

    public IReadOnlyDictionary<AttributeDetector, Transform> Assignments => currentAssignments;

    void Update()
    {
        if (!coordinatorEnabled) return;
        if (Time.time - lastReassessTime < Mathf.Max(0.02f, reassessInterval)) return;
        lastReassessTime = Time.time;
        ReassessAssignments();
    }

    void ReassessAssignments()
    {
        detectors.Clear();
        var found = FindObjectsOfType<AttributeDetector>(includeInactiveDetectors);
        detectors.AddRange(found.Where(d => d != null));

        // Propuestas: mejor candidato por detector (solo lectura)
        var desired = new Dictionary<AttributeDetector, Transform>();
        foreach (var d in detectors)
        {
            Transform t;
            bool ok = d.TryFindBestMatchingPerson(out t, d.confidenceThreshold);
            desired[d] = (ok && t != null && t.gameObject.scene.IsValid()) ? t : null;
        }

        // Agrupar por el mismo objetivo
        var byTarget = new Dictionary<Transform, List<AttributeDetector>>();
        foreach (var kv in desired)
        {
            var det = kv.Key; var target = kv.Value;
            if (target == null) continue;
            if (!byTarget.TryGetValue(target, out var list)) { list = new List<AttributeDetector>(); byTarget[target] = list; }
            list.Add(det);
        }

        // Resolver conflictos: se queda el dron más cercano
        claimedTargets.Clear();
        foreach (var pair in byTarget)
        {
            Transform target = pair.Key;
            List<AttributeDetector> contendants = pair.Value;

            AttributeDetector winner = null;
            float bestDist = float.MaxValue;

            foreach (var d in contendants)
            {
                var droneTf = GetDroneTransform(d);
                if (droneTf == null) continue;
                float dist = Vector3.Distance(droneTf.position, target.position);
                if (dist < bestDist) { bestDist = dist; winner = d; }
            }

            if (winner != null)
            {
                currentAssignments[winner] = target;
                claimedTargets.Add(target);

                // Los demás quedan sin objetivo asignado (libres para patrullar)
                foreach (var d in contendants)
                {
                    if (d == winner) continue;
                    currentAssignments[d] = null;
                }
            }
        }

        // Detectores sin propuesta o que ya no existen
        foreach (var d in detectors)
        {
            if (!desired.TryGetValue(d, out var t) || t == null)
                currentAssignments[d] = null;
        }

        // Limpieza: quitar asignaciones huérfanas
        var keys = currentAssignments.Keys.ToList();
        foreach (var k in keys)
        {
            if (k == null || !detectors.Contains(k)) { currentAssignments.Remove(k); continue; }
            var t = currentAssignments[k];
            if (t == null) continue;
            if (!t || !t.gameObject.scene.IsValid()) currentAssignments[k] = null;
        }
    }

    Transform GetDroneTransform(AttributeDetector d)
    {
        if (d == null) return null;
        if (d.drone != null) return d.drone;
        return d.transform; // fallback
    }

    void OnDrawGizmos()
    {
        if (!drawDebug || currentAssignments == null) return;
        Gizmos.color = debugLineColor;
        foreach (var kv in currentAssignments)
        {
            var d = kv.Key; var t = kv.Value;
            if (d == null || t == null) continue;
            var droneTf = GetDroneTransform(d);
            if (droneTf == null) continue;
            Gizmos.DrawLine(droneTf.position, t.position);
            Gizmos.DrawSphere(t.position, 0.3f);
        }
    }
}

