using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Despegar : MonoBehaviour
{
    public enum Estado { Idle, Takeoff, Patrol, Approach, Land, Landed }

    [Header("Referencias")]
    public Transform humano;                 // si es null se busca por tag "Humano"
    public LayerMask obstaculosMask;
    public LayerMask sueloMask = ~0;

    [Header("Alturas")]
    public float alturaCrucero = 12f;
    public float margenObstaculos = 3f;

    [Header("Velocidades")]
    public float velAscenso = 4f;
    public float velCrucero = 7f;
    public float velDescenso = 3.5f;
    public float velGiro = 6f;

    [Header("Patrulla back-and-forth")]
    public Transform puntoA;                 // opcional si usas patrolArea
    public Transform puntoB;                 // opcional si usas patrolArea
    public BoxCollider patrolArea;           // opcional: área rectangular del campo
    public float margenBorde = 2f;           // margen desde los bordes del área
    public bool autoEjeMasLargo = true;      // usa el eje X/Z más largo del BoxCollider

    [Header("Detección")]
    public float radioDeteccion = 60f;
    [Range(1f,179f)] public float fovGrados = 70f;
    public float offsetAterrizaje = 1.8f;

    [Header("Tolerancias")]
    public float epsAltura = 0.15f;
    public float epsPos = 0.25f;

    [Header("Debug")]
    public bool dibujarGizmos = true;

    // internos
    private Estado estado = Estado.Idle;
    private Vector3 origen;
    private float nivelSueloOrigen;
    private float yObjetivo;
    private Transform cacheHumano;

    private Vector3[] puntos = new Vector3[2];
    private int idxObjetivo = 0;
    private Vector3 puntoAterrizaje;

    void Start()
    {
        origen = transform.position;

        // Suelo bajo el dron
        if (Physics.Raycast(new Vector3(origen.x, origen.y + 1000f, origen.z), Vector3.down,
            out RaycastHit hit, 2000f, sueloMask))
            nivelSueloOrigen = hit.point.y;
        else
            nivelSueloOrigen = origen.y;

        yObjetivo = nivelSueloOrigen + alturaCrucero;

        // Humano
        cacheHumano = humano != null ? humano : GameObject.FindGameObjectWithTag("Humano")?.transform;

        // Configurar ruta A-B
        ConfigurarRutaAB();

        estado = Estado.Takeoff;
    }

    void Update()
    {
        switch (estado)
        {
            case Estado.Takeoff:   ActualizarTakeoff(); break;
            case Estado.Patrol:    ActualizarPatrullaAB(); BuscarHumano(); break;
            case Estado.Approach:  ActualizarApproach(); break;
            case Estado.Land:      ActualizarLand(); break;
            case Estado.Landed:    break;
        }
    }

    // ---------- Configurar ruta ----------
    void ConfigurarRutaAB()
    {
        if (puntoA != null && puntoB != null)
        {
            puntos[0] = new Vector3(puntoA.position.x, yObjetivo, puntoA.position.z);
            puntos[1] = new Vector3(puntoB.position.x, yObjetivo, puntoB.position.z);
            return;
        }

        if (patrolArea != null)
        {
            // bounds en mundo
            Bounds b = patrolArea.bounds;
            Vector3 c = b.center;
            Vector3 half = b.extents;

            // elegir eje más largo
            bool usarX = autoEjeMasLargo ? (b.size.x >= b.size.z) : true;

            if (usarX)
            {
                float x0 = c.x - (half.x - margenBorde);
                float x1 = c.x + (half.x - margenBorde);
                puntos[0] = new Vector3(x0, yObjetivo, c.z);
                puntos[1] = new Vector3(x1, yObjetivo, c.z);
            }
            else
            {
                float z0 = c.z - (half.z - margenBorde);
                float z1 = c.z + (half.z - margenBorde);
                puntos[0] = new Vector3(c.x, yObjetivo, z0);
                puntos[1] = new Vector3(c.x, yObjetivo, z1);
            }
            return;
        }

        // fallback: 20 m a izquierda/derecha del origen
        puntos[0] = new Vector3(origen.x - 20f, yObjetivo, origen.z);
        puntos[1] = new Vector3(origen.x + 20f, yObjetivo, origen.z);
    }

    // ---------- Estados ----------
    void ActualizarTakeoff()
    {
        Vector3 destino = new Vector3(transform.position.x, yObjetivo, transform.position.z);
        MoverHacia(destino, velAscenso);
        if (Mathf.Abs(transform.position.y - yObjetivo) <= epsAltura)
            estado = Estado.Patrol;
    }

    // Patrulla ida/vuelta A<->B
    void ActualizarPatrullaAB()
    {
        Vector3 objetivo = new Vector3(puntos[idxObjetivo].x, yObjetivo, puntos[idxObjetivo].z);
        MoverHacia(objetivo, velCrucero);

        Vector2 a = new Vector2(transform.position.x, transform.position.z);
        Vector2 b = new Vector2(objetivo.x, objetivo.z);
        if (Vector2.Distance(a, b) <= epsPos)
            idxObjetivo = 1 - idxObjetivo; // cambiar extremo
    }

    void ActualizarApproach()
    {
        if (cacheHumano == null) { estado = Estado.Patrol; return; }

        Vector3 sobre = new Vector3(cacheHumano.position.x, yObjetivo, cacheHumano.position.z);
        MoverHacia(sobre, velCrucero);

        if (Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                             new Vector2(sobre.x, sobre.z)) <= 0.8f)
        {
            Vector3 dir = (transform.position - sobre); dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = Random.insideUnitSphere;
            dir.y = 0f; dir.Normalize();

            Vector3 plano = cacheHumano.position + dir * offsetAterrizaje;

            if (Physics.Raycast(plano + Vector3.up * 200f, Vector3.down, out RaycastHit hit, 500f, sueloMask))
                puntoAterrizaje = hit.point;
            else
                puntoAterrizaje = new Vector3(plano.x, nivelSueloOrigen, plano.z);

            estado = Estado.Land;
        }
    }

    void ActualizarLand()
    {
        Vector3 destino = puntoAterrizaje;
        Vector3 encima = new Vector3(destino.x, yObjetivo, destino.z);

        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                             new Vector3(destino.x, 0, destino.z)) > 0.5f)
            MoverHacia(encima, velCrucero);
        else
            MoverHacia(destino, velDescenso);

        if (Vector3.Distance(transform.position, destino) <= epsPos + 0.05f)
            estado = Estado.Landed;
    }

    // ---------- Detección ----------
    void BuscarHumano()
    {
        if (cacheHumano == null)
        {
            cacheHumano = GameObject.FindGameObjectWithTag("Humano")?.transform;
            if (cacheHumano == null) return;
        }

        Vector3 toHumano = cacheHumano.position - transform.position;
        float dist = toHumano.magnitude;
        if (dist > radioDeteccion) return;

        Vector3 forward = transform.forward; forward.y = 0f;
        Vector3 plano = new Vector3(toHumano.x, 0f, toHumano.z);
        float ang = Vector3.Angle(forward, plano);
        if (ang > fovGrados * 0.5f) return;

        if (Physics.Raycast(transform.position, toHumano.normalized, out RaycastHit hit, radioDeteccion, ~0))
        {
            if (hit.transform == cacheHumano || hit.transform.IsChildOf(cacheHumano))
                estado = Estado.Approach;
        }
    }

    // ---------- Util ----------
    void MoverHacia(Vector3 destino, float velocidad)
    {
        Vector3 dir = destino - transform.position;
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);

        Vector3 dirXZ = new Vector3(dir.x, 0f, dir.z);
        if (dirXZ.sqrMagnitude > 0.0001f)
        {
            Quaternion rotDeseada = Quaternion.LookRotation(dirXZ.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotDeseada, velGiro * Time.deltaTime);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!dibujarGizmos) return;

        Gizmos.color = Color.yellow;
        if (puntos != null && puntos.Length == 2 && (puntos[0] != Vector3.zero || puntos[1] != Vector3.zero))
        {
            Vector3 a = new Vector3(puntos[0].x, transform.position.y, puntos[0].z);
            Vector3 b = new Vector3(puntos[1].x, transform.position.y, puntos[1].z);
            Gizmos.DrawLine(a, b);
            Gizmos.DrawSphere(a, 0.3f);
            Gizmos.DrawSphere(b, 0.3f);
        }
        if (patrolArea != null)
        {
            Gizmos.color = new Color(0,1,0,0.2f);
            Gizmos.DrawWireCube(patrolArea.bounds.center, patrolArea.bounds.size);
        }
    }

    public Estado GetEstado() => estado;
}