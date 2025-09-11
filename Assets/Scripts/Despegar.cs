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
    public float velCrucero = 120f; // velocidad aumentada
    public float velDescenso = 3.5f;
    public float velGiro = 6f;

    [Header("Patrulla back-and-forth")]
    public Transform puntoA;                 // opcional si usas patrolArea
    public Transform puntoB;                 // opcional si usas patrolArea
    public BoxCollider patrolArea;           // opcional: área rectangular del campo
    public float margenBorde = 2f;           // margen desde los bordes del área
    public bool autoEjeMasLargo = true;      // usa el eje X/Z más largo del BoxCollider

    [Header("Búsqueda libre (aleatoria)")]
    public bool busquedaLibre = false;        // moverse en cualquier dirección escogiendo puntos aleatorios
    public float radioBusquedaLibre = 40f;   // usado si no hay patrolArea
    public float cambiarDestinoCada = 5f;    // segundos entre cambios de rumbo

    [Header("Detección")]
    public float radioDeteccion = 60f;
    [Range(1f,179f)] public float fovGrados = 70f;
    public float offsetAterrizaje = 1.8f;

    [Header("Tolerancias")]
    public float epsAltura = 0.15f;
    public float epsPos = 0.25f;

    [Header("Debug")]
    public bool dibujarGizmos = true;

    [Header("Detección por atributos")]
    public bool usarDeteccion = true; // habilita detección basada en descriptores
    public AttributeDetector detector;           // detector basado en PersonDescriptor
    public float actualizarObjetivoCada = 0.3f;
    private float tUltimaAct = -999f;
    private Transform marcadorObjetivo;

    // internos
    private Estado estado = Estado.Idle;
    private Vector3 origen;
    private float nivelSueloOrigen;
    private float yObjetivo;
    private Transform cacheHumano;

    private Vector3[] puntos = new Vector3[2];
    private int idxObjetivo = 0;
    private Vector3 puntoAterrizaje;
    private Vector3 destinoLibre;
    private float tUltimoCambioDestino = -999f;

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

        // Buscar objetivo del query automáticamente
        cacheHumano = null;
        if (usarDeteccion)
        {
            if (detector == null) detector = FindObjectOfType<AttributeDetector>();
            GameObject go = new GameObject("ObjetivoDeteccion");
            go.hideFlags = HideFlags.HideInHierarchy;
            marcadorObjetivo = go.transform;
            if (detector != null && detector.TryFindBestMatchingPerson(out Transform inicial, detector.confidenceThreshold))
            {
                cacheHumano = inicial;
            }
        }

        // Si encontró objetivo, ir directo a él
        if (cacheHumano != null)
        {
            estado = Estado.Approach;
        }
        else
        {
            // Si no hay objetivo, patrulla normal
            if (busquedaLibre)
                ElegirNuevoDestinoLibre(true);
            else
                ConfigurarRutaAB();
            estado = Estado.Takeoff;
        }
    }

    void Update()
    {
        switch (estado)
        {
            case Estado.Takeoff:   ActualizarTakeoff(); break;
            case Estado.Patrol:    if (busquedaLibre) ActualizarBusquedaLibre(); else ActualizarPatrullaAB(); BuscarHumano(); break;
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
            estado = (cacheHumano != null) ? Estado.Approach : Estado.Patrol;
    }

    // Patrulla ida/vuelta A<->B
    void ActualizarPatrullaAB()
    {
        Vector3 objetivo = new Vector3(puntos[idxObjetivo].x, yObjetivo, puntos[idxObjetivo].z);
        MoverHacia(objetivo, velCrucero);

        Vector2 a = new Vector2(transform.position.x, transform.position.z);
        Vector2 b = new Vector2(objetivo.x, objetivo.z);
        // Si llega al extremo, cambiar al otro extremo
        if (Vector2.Distance(a, b) <= epsPos)
        {
            idxObjetivo = 1 - idxObjetivo;
        }
    }

    // Patrulla aleatoria (moverse en cualquier dirección)
    void ActualizarBusquedaLibre()
    {
        // cambiar destino por tiempo o cercanía
        bool porTiempo = (Time.time - tUltimoCambioDestino) >= cambiarDestinoCada;
        bool porCercania = Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(destinoLibre.x, destinoLibre.z)) <= epsPos;
        if (porTiempo || porCercania)
            ElegirNuevoDestinoLibre(false);

        Vector3 objetivo = new Vector3(destinoLibre.x, yObjetivo, destinoLibre.z);
        MoverHacia(objetivo, velCrucero);
    }

    void ElegirNuevoDestinoLibre(bool forzar)
    {
        destinoLibre = PuntoAleatorioEnArea();
        tUltimoCambioDestino = Time.time;
    }

    Vector3 PuntoAleatorioEnArea()
    {
        if (patrolArea != null)
        {
            Bounds b = patrolArea.bounds;
            float x = Random.Range(b.min.x + margenBorde, b.max.x - margenBorde);
            float z = Random.Range(b.min.z + margenBorde, b.max.z - margenBorde);
            return new Vector3(x, yObjetivo, z);
        }
        else
        {
            // círculo alrededor del origen
            Vector2 r = Random.insideUnitCircle * Mathf.Max(1f, radioBusquedaLibre);
            return new Vector3(origen.x + r.x, yObjetivo, origen.z + r.y);
        }
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
        // Solo buscar por atributos, nunca usar fallback por etiqueta
        if (usarDeteccion && Time.time - tUltimaAct >= actualizarObjetivoCada)
        {
            tUltimaAct = Time.time;
            // Buscar el transform real del NPC que coincide con el query
            if (detector != null && detector.TryFindBestMatchingPerson(out Transform npc, detector.confidenceThreshold))
            {
                cacheHumano = npc;
                estado = Estado.Approach;
            }
        }
        // Si no hay detección, no hacer nada (no volver al origen)
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
        else if (busquedaLibre)
        {
            // radio de búsqueda libre
            Gizmos.color = new Color(0,0,1,0.2f);
            Gizmos.DrawWireSphere(Application.isPlaying ? origen : transform.position, radioBusquedaLibre);
        }
    }

    public Estado GetEstado() => estado;
}
