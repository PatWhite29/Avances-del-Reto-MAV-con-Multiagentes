# Avances-del-Reto-MAV-con-Multiagentes

Proyecto Unity 2020+ (C#) para un reto de MAV multiagentes. La simulación integra drones autónomos que despegan, patrullan (ruta A↔B o búsqueda aleatoria), detectan personas por atributos mediante un prompt de misión y se aproximan/aterrizan cerca del objetivo. La detección es emulada (no CV real) con `AttributeDetector` usando coincidencia por texto (`missionQuery`). La nave se controla con la FSM de `Despegar` (Idle/Takeoff/Patrol/Approach/Land/Landed). La escena se puebla con ~50 NPCs únicos mediante `NPCSpawner`, cada uno con atributos (`PersonDescriptor`). Para depuración, `PersonDescriptionLister` lista/agrupa descriptores presentes.


## Estructura principal
- `Assets/Scenes`: `SampleScene`, `npc-spawn`, `edgardo`, `patob`.
- `Assets/Scripts`: `Despegar`, `AttributeDetector`, `NPCSpawner`, `PersonDescriptor`, `PersonDescriptionLister`, `MissionPromptController`, `SimpleMultiDroneCoordinator`.
- `Assets/Prefabs` y `Assets/Models`: dron, personaje genérico, sombreros y accesorios.

## Flujo
`NPCSpawner` puebla → `AttributeDetector` filtra por `missionQuery` → `Despegar` actualiza objetivo y aproxima/aterriza.

## Requisitos
- Unity Hub 3 o superior.
- Unity 6 (Editor 6000.0.57f1). El proyecto fue guardado con esta versión.
- macOS o Windows con hardware compatible con URP.

## Instalación y ejecución (desde cero)
- Clonar o descargar: `git clone https://github.com/<tu-usuario>/Avances-del-Reto-MAV-con-Multiagentes.git`
  - Alternativa: Descargar ZIP desde GitHub y descomprimir.
- Abrir en Unity Hub: Add → seleccionar la carpeta del repo.
- Instalar la versión recomendada: si Hub te pide 6000.0.57f1, instálala y ábrelo con esa.
- Importación inicial: al abrir por primera vez, Unity resolverá paquetes (URP, Input System, etc.). Espera a que termine.
- Abrir una escena de ejemplo:
  - `Assets/Scenes/npc-spawn.unity` (población masiva de NPCs; ideal para probar detección y aproximación).
  - `Assets/Scenes/SampleScene.unity` (escena base).
- Ejecutar: presiona Play en el Editor.
  - Los drones patrullan (A↔B o libre) y, si hay coincidencia con el prompt de misión, se aproximan y aterrizan.
  - Para cambiar el prompt global: selecciona en la escena el objeto con `MissionPromptController` y edita `defaultMissionQuery`. Marca `useRuntimeGUI` si quieres cambiarlo en tiempo de ejecución (aparecerá un cuadro sencillo en pantalla).
- Multi‑drone (opcional): duplica el prefab/objeto del dron con `Despegar` y ajusta `patrullaOffset` para separarlos; asigna un `AttributeDetector` por dron.

### Build (opcional)
- File → Build Settings… → PC, Mac & Linux Standalone → Add Open Scenes → Build & Run.
- Si ves materiales rosas: asegúrate de que el proyecto use URP (ya incluido) y reimporta los materiales si es necesario.

## Evidencias del reto
📄 [Revisión 1 - Evidencia del Reto](./Revison1-EvidenciaReto.pdf)

📄 [Revisión 2 - Evidencia del Reto](./docs/Revison2-EvidenciaReto%20(2).pdf)

📄 [Revisión 3 - Evidencia del Reto](./docs/Revison3-EvidenciaReto.pdf)

📄 [Revisión Final - Evidencia del Reto](./docs/RevisonFinal-EvidenciaReto.pdf)

📄 [Presentación del Reto](./docs/presentacion.pdf)
