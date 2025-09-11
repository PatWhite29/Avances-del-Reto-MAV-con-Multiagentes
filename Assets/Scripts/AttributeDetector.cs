using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DisallowMultipleComponent]
public class AttributeDetector : MonoBehaviour
{
    [Header("Referencias")]
    public Camera perceptionCamera;        // La cámara del dron (FPV)
    public Transform drone;                // Transform del dron

    [Header("Búsqueda")]
    public float detectionRadius = 200f;   // metros
    public float fovDegrees = 120f;        // cono de visión
    public LayerMask occluders;            // capas que tapan visión (edificios, etc.)
    public float groundY = 0f;             // plano del suelo para proyectar

    [Header("Filtro")]
    public float confidenceThreshold = 0.1f;
    public string missionQuery = "person with orange jacket and yellow hard hat";

    // --- API pública compatible ---
    [System.Serializable]
    public class Box { public float x, y, width, height; } // en pixeles de pantalla
    [System.Serializable]
    public class TargetDetection
    {
        public float confidence;   // 0..1
        public Box box;            // bbox en px (screen-space)
    }

    public List<TargetDetection> Detections { get; private set; } = new List<TargetDetection>();
    public int LastInputWidth  => Screen.width;
    public int LastInputHeight => Screen.height;

    void Reset()
    {
        if (perceptionCamera == null) perceptionCamera = Camera.main;
        if (drone == null) drone = transform;
    }

    void Update()
    {
        SimulateDetections();
    }

    void SimulateDetections()
    {
        Detections.Clear();
        if (perceptionCamera == null || drone == null) return;

        // Encuentra personas cercanas (por tener PersonDescriptor)
        var hits = Physics.OverlapSphere(drone.position, detectionRadius)
                          .Select(c => c.GetComponentInParent<PersonDescriptor>())
                          .Where(p => p != null);

        foreach (var pd in hits)
        {
            var t = pd.transform;
            Vector3 toTarget = (t.position - perceptionCamera.transform.position);
            float angle = Vector3.Angle(perceptionCamera.transform.forward, toTarget);
            if (angle > fovDegrees * 0.5f) continue; // fuera de FOV

            // Raycast para comprobar línea de visión
            if (Physics.Raycast(perceptionCamera.transform.position, toTarget.normalized,
                out RaycastHit rh, toTarget.magnitude, occluders, QueryTriggerInteraction.Ignore))
                continue;

            // "Score" por coincidencia con la misión (muy simple: keywords)
            float score = ComputeMatchScore(pd, missionQuery);
            if (score < confidenceThreshold) continue;

            // BBox 2D (aproximación con Renderer bounds)
            var rend = t.GetComponentInChildren<Renderer>();
            if (rend == null) continue;
            Rect rect = BoundsToScreenRect(rend.bounds, perceptionCamera);
            if (rect.width <= 1f || rect.height <= 1f) continue;

            Detections.Add(new TargetDetection {
                confidence = Mathf.Clamp01(score),
                box = new Box { x = rect.x, y = rect.y, width = rect.width, height = rect.height }
            });
        }
    }

    // Busca en toda la escena la persona que mejor coincide con missionQuery (ignora FOV/oclusiones)
    public bool TryFindBestMatchingPerson(out Transform target, float minConfidence = -1f)
    {
        target = null;
        float thr = (minConfidence >= 0f) ? minConfidence : confidenceThreshold;

        // Obtener todos los PersonDescriptor activos (y en escena)
        var all = Resources.FindObjectsOfTypeAll<PersonDescriptor>()
                           .Where(pd => pd != null && pd.gameObject.scene.IsValid());

        float bestScore = thr;
        Transform best = null;
        foreach (var pd in all)
        {
            float s = ComputeMatchScore(pd, missionQuery);
            if (s >= bestScore)
            {
                bestScore = s;
                best = pd.transform;
            }
        }

        if (best != null)
        {
            target = best;
            return true;
        }
        return false;
    }

    float ComputeMatchScore(PersonDescriptor pd, string query)
    {
        string q = query.ToLowerInvariant();
        float s = 0f;

        // Buscar formato: Material:X, Hat:Y, Accessory:Z
        int mat = -1, hat = -1, acc = -1;
        System.Text.RegularExpressions.Match mMat = System.Text.RegularExpressions.Regex.Match(q, @"material\s*:?\s*(\d+)");
        System.Text.RegularExpressions.Match mHat = System.Text.RegularExpressions.Regex.Match(q, @"hat\s*:?\s*(\d+)");
        System.Text.RegularExpressions.Match mAcc = System.Text.RegularExpressions.Regex.Match(q, @"accessory\s*:?\s*(\d+)");
        if (mMat.Success) int.TryParse(mMat.Groups[1].Value, out mat);
        if (mHat.Success) int.TryParse(mHat.Groups[1].Value, out hat);
        if (mAcc.Success) int.TryParse(mAcc.Groups[1].Value, out acc);

        // Obtener indices del NPC
        int npcMat = -1, npcHat = -1, npcAcc = -1;
        var npcObj = pd.gameObject;
        var spawnerData = npcObj.GetComponent<NPCReference>();
        if (spawnerData != null)
        {
            npcMat = spawnerData.materialIndex;
            npcHat = spawnerData.hatIndex;
            npcAcc = spawnerData.accessoryIndex;
        }

        // Score: 1.0 si todos coinciden, 0.7 si dos, 0.4 si uno
        int matches = 0;
        if (mat >= 0 && npcMat == mat) matches++;
        if (hat >= 0 && npcHat == hat) matches++;
        if (acc >= 0 && npcAcc == acc) matches++;

        if (matches == 3) s = 1.0f;
        else if (matches == 2) s = 0.7f;
        else if (matches == 1) s = 0.4f;
        else s = 0f;

        return Mathf.Clamp01(s);
    }

    Rect BoundsToScreenRect(Bounds b, Camera cam)
    {
        // Proyecta 8 vértices del bounds a pantalla y crea el rect que los contiene.
        var pts = new Vector3[8];
        var c = b.center; var e = b.extents;
        int i = 0;
        for (int xi = -1; xi <= 1; xi += 2)
        for (int yi = -1; yi <= 1; yi += 2)
        for (int zi = -1; zi <= 1; zi += 2)
            pts[i++] = cam.WorldToScreenPoint(c + Vector3.Scale(e, new Vector3(xi, yi, zi)));

        float minX = pts.Min(p => p.x), maxX = pts.Max(p => p.x);
        float minY = pts.Min(p => p.y), maxY = pts.Max(p => p.y);

        // Ajuste a coordenadas de GUI (y crece hacia abajo)
        float w = Mathf.Max(0f, maxX - minX);
        float h = Mathf.Max(0f, maxY - minY);
        // Nota: si tu overlay usa el origen en (0,0) arriba, no inviertas Y
        return new Rect(minX, Screen.height - maxY, w, h);
    }

    // Punto en el mundo hacia el objetivo más cercano con suficiente confianza
    public bool TryGetNearestTargetPoint(float planeY, out Vector3 worldPoint, float minConfidence = 0.3f)
    {
        worldPoint = default;
        if (Detections == null || Detections.Count == 0) return false;

        // Filtra por confianza
        var candidates = Detections.Where(r => r.confidence >= Mathf.Max(minConfidence, confidenceThreshold))
                                   .ToList();
        if (candidates.Count == 0) return false;

        // El más cercano (proyectando ray desde el centro de la bbox al plano Y)
        var cam = perceptionCamera;
        float bestDist = float.MaxValue;
        Vector3 bestPt = default;
        foreach (var d in candidates)
        {
            Vector2 centerPx = new Vector2(d.box.x + d.box.width * 0.5f, d.box.y + d.box.height * 0.5f);
            Ray r = cam.ScreenPointToRay(new Vector3(centerPx.x, centerPx.y, 0));
            if (Mathf.Abs(r.direction.y) < 1e-3f) continue; // casi paralelo al plano
            float t = (planeY - r.origin.y) / r.direction.y;
            if (t <= 0) continue;
            Vector3 hit = r.origin + r.direction * t;

            float dist = Vector3.Distance(drone.position, hit);
            if (dist < bestDist) { bestDist = dist; bestPt = hit; }
        }

        if (bestDist < float.MaxValue) { worldPoint = bestPt; return true; }
        return false;
    }
}
