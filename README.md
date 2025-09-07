# Avances-del-Reto-MAV-con-Multiagentes

Proyecto Unity 2020+ (C#) para un reto de MAV multiagentes. La simulación integra drones autónomos que despegan, patrullan (ruta A↔B o búsqueda aleatoria), detectan personas por atributos mediante un prompt de misión y se aproximan/aterrizan cerca del objetivo. La detección es emulada (no CV real) con `AttributeDetector` usando coincidencia por texto (`missionQuery`). La nave se controla con la FSM de `Despegar` (Idle/Takeoff/Patrol/Approach/Land/Landed). La escena se puebla con ~50 NPCs únicos mediante `NPCSpawner`, cada uno con atributos (`PersonDescriptor`). Para depuración, `PersonDescriptionLister` lista/agrupa descriptores presentes.


## Estructura principal
- `Assets/Scenes`: `SampleScene`, `npc-spawn`, `edgardo`, `patob`.
- `Assets/Scripts`: `Despegar`, `AttributeDetector`, `NPCSpawner`, `PersonDescriptor`, `PersonDescriptionLister`, `MissionPromptController`, `SimpleMultiDroneCoordinator`.
- `Assets/Prefabs` y `Assets/Models`: dron, personaje genérico, sombreros y accesorios.

## Flujo
`NPCSpawner` puebla → `AttributeDetector` filtra por `missionQuery` → `Despegar` actualiza objetivo y aproxima/aterriza.

## Requisitos
- Unity 2020.1 o superior.

## Evidencias del reto
📄 [Revisión 1 - Evidencia del Reto](./Revison1-EvidenciaReto.pdf)

📄 [Revisión 2 - Evidencia del Reto](./docs/Revison2-EvidenciaReto%20(2).pdf)

📄 [Revisión 3 - Evidencia del Reto](./docs/Revison3-EvidenciaReto.pdf)
