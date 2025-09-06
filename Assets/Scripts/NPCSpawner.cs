using System;
using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class HatConfig //Para acomodar pocision sombreros en el inspector
{
    public GameObject prefab;
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localRotation = Vector3.zero; // En grados
    public Vector3 localScale = Vector3.one;
}

[System.Serializable]
public class AccessoryConfig //Para acomodar posicion accesorios en el inspector
{
    public GameObject prefab;
    public Vector3 localPosition = Vector3.zero;
    public Vector3 localRotation = Vector3.zero; // En grados
    public Vector3 localScale = Vector3.one;
}

// Clase para almacenar una combinación única de configuración
[System.Serializable]
public class NPCConfiguration
{
    public int materialIndex;
    public int hatIndex;
    public int accessoryIndex;
    
    public NPCConfiguration(int mat, int hat, int acc)
    {
        materialIndex = mat;
        hatIndex = hat;
        accessoryIndex = acc;
    }
    
    // Método para comparar configuraciones
    public bool Equals(NPCConfiguration other)
    {
        if (other == null) return false;
        return materialIndex == other.materialIndex && 
               hatIndex == other.hatIndex && 
               accessoryIndex == other.accessoryIndex;
    }
    
    // Override para debug más fácil
    public override string ToString()
    {
        return $"Material:{materialIndex}, Hat:{hatIndex}, Accessory:{accessoryIndex}";
    }
}

// Clase para almacenar información completa de cada NPC spawneado
[System.Serializable]
public class SpawnedNPCData
{
    public GameObject npcGameObject;
    public NPCConfiguration configuration;
    public Vector3 position;
    public int npcID; // ID único para identificar el NPC
    
    public SpawnedNPCData(GameObject npc, NPCConfiguration config, Vector3 pos, int id)
    {
        npcGameObject = npc;
        configuration = config;
        position = pos;
        npcID = id;
    }
    
    public override string ToString()
    {
        return $"NPC_{npcID}: {configuration} at {position}";
    }
}

public class NPCSpawner : MonoBehaviour
{
    //Variables para el inspector
    [Header("Assets para NPC")]
    public GameObject npcPrefab;
    public HatConfig[] hatConfigs;
    public AccessoryConfig[] accessoryConfigs;
    public Material[] materials;

    // Configuracion de spawn y spawn plane
    [Header("Configuración de Spawn")]
    public int numberOfNPC = 50;
    public GameObject spawnPlane;

    // Opción para modo de compatibilidad con FBX problemáticos
    [Header("Configuración FBX")]
    public bool useFBXCompatibilityMode = true;
    
    // Lista de todos los NPCs spawneados - ACCESIBLE DESDE OTROS SCRIPTS
    [Header("Debug Info")]
    [SerializeField] private List<SpawnedNPCData> spawnedNPCs = new List<SpawnedNPCData>();
    
    // Propiedad pública para acceder a la lista desde otros scripts
    public List<SpawnedNPCData> SpawnedNPCs { get { return spawnedNPCs; } }

    void Start()
    {
        //Checar que si se agrego el plano
        if (spawnPlane == null)
        {
            Debug.LogError("ERROR! No se encontro SpawnPlane en el inspector");
            return;
        }

        // Generar todas las combinaciones posibles
        List<NPCConfiguration> allConfigurations = GenerateAllConfigurations();
        
        // Verificar que tenemos suficientes configuraciones
        if (numberOfNPC > allConfigurations.Count)
        {
            Debug.LogError("Numero de NPC Mayor al numero de configuraciones posibles, favor de ingresar un numero menor a " + allConfigurations.Count);
            return;
        }

        //Leer dimensiones del SpawnPlane
        Renderer planeRenderer = spawnPlane.GetComponent<Renderer>();
        Bounds planeBounds = planeRenderer.bounds;

        //Loop para crear NPCs
        for (int i = 0; i < numberOfNPC; i++)
        {
            //Posicion random
            float randomX = UnityEngine.Random.Range(planeBounds.min.x, planeBounds.max.x);
            float randomZ = UnityEngine.Random.Range(planeBounds.min.z, planeBounds.max.z);

            Vector3 randomSpawnPosition = new Vector3(randomX, planeBounds.max.y, randomZ);

            //Crear NPC
            GameObject newNPC = Instantiate(npcPrefab, randomSpawnPosition, npcPrefab.transform.rotation);
            
            // Darle un nombre único al NPC
            newNPC.name = $"NPC_{i:000}";

            // Asegurar etiqueta para detección emulada
            try { newNPC.tag = "Person"; } catch { /* si la tag no existe o es inválida */ }

            // Asegurar descriptor de persona para características aleatorias
            var descriptor = newNPC.GetComponent<PersonDescriptor>();
            if (descriptor == null) descriptor = newNPC.AddComponent<PersonDescriptor>();
            descriptor.randomizeOnStart = true;

            // Aplicar configuración única a este NPC
            ApplyConfigurationToNPC(newNPC, allConfigurations[i]);
            
            // AGREGAR A LA LISTA: Crear registro del NPC spawneado
            SpawnedNPCData npcData = new SpawnedNPCData(newNPC, allConfigurations[i], randomSpawnPosition, i);
            spawnedNPCs.Add(npcData);
            
            Debug.Log($"Spawned: {npcData}");
        }
        
        Debug.Log($"Total NPCs spawneados: {spawnedNPCs.Count}");
    }

    // Genera todas las combinaciones posibles de material, sombrero y accesorio
    List<NPCConfiguration> GenerateAllConfigurations()
    {
        List<NPCConfiguration> configurations = new List<NPCConfiguration>();

        // Iterar por cada material
        for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
        {
            // Para cada sombrero
            for (int hatIndex = 0; hatIndex < hatConfigs.Length; hatIndex++)
            {
                // Para cada accesorio
                for (int accessoryIndex = 0; accessoryIndex < accessoryConfigs.Length; accessoryIndex++)
                {
                    configurations.Add(new NPCConfiguration(materialIndex, hatIndex, accessoryIndex));
                }
            }
        }

        Debug.Log("Se generaron " + configurations.Count + " configuraciones únicas posibles");
        return configurations;
    }

    // Aplica una configuración específica a un NPC
    void ApplyConfigurationToNPC(GameObject npc, NPCConfiguration config)
    {
        // Aplicar material
        Renderer npcRenderer = npc.GetComponentInChildren<Renderer>();
        if (npcRenderer != null)
        {
            npcRenderer.material = materials[config.materialIndex];
        }

        // Aplicar sombrero
        if (hatConfigs.Length > 0 && config.hatIndex < hatConfigs.Length)
        {
            HatConfig selectedHat = hatConfigs[config.hatIndex];
            GameObject hat = Instantiate(selectedHat.prefab);

            //Buscar el anclaje del sombrero
            Transform hatAttachPoint = npc.transform.Find("HatAttachmentPoint");

            if (hatAttachPoint != null)
            {
                hat.transform.SetParent(hatAttachPoint);

                if (useFBXCompatibilityMode)
                {
                    // Modo compatibilidad: usar configuraciones manuales
                    hat.transform.localPosition = selectedHat.localPosition;
                    hat.transform.localRotation = Quaternion.Euler(selectedHat.localRotation);
                    hat.transform.localScale = selectedHat.localScale;
                }
                else
                {
                    // Modo estándar: resetear transformaciones
                    hat.transform.localPosition = Vector3.zero;
                    hat.transform.localRotation = Quaternion.identity;
                    hat.transform.localScale = Vector3.one;
                }
            }
            else
            {
                Debug.LogError("No se encontró 'HatAttachmentPoint' en el NPC: " + npc.name);
                Destroy(hat); // Limpiar el objeto huérfano
            }
        }

        // Aplicar accesorio
        if (accessoryConfigs.Length > 0 && config.accessoryIndex < accessoryConfigs.Length)
        {
            AccessoryConfig selectedAccessory = accessoryConfigs[config.accessoryIndex];
            GameObject accessory = Instantiate(selectedAccessory.prefab);

            //Buscar anclaje de accesorio
            Transform accessoryAttachPoint = npc.transform.Find("AccessoryAttachmentPoint");

            if (accessoryAttachPoint != null)
            {
                accessory.transform.SetParent(accessoryAttachPoint);

                if (useFBXCompatibilityMode)
                {
                    // Modo compatibilidad: usar configuraciones manuales
                    accessory.transform.localPosition = selectedAccessory.localPosition;
                    accessory.transform.localRotation = Quaternion.Euler(selectedAccessory.localRotation);
                    accessory.transform.localScale = selectedAccessory.localScale;
                }
                else
                {
                    // Modo estándar: resetear transformaciones
                    accessory.transform.localPosition = Vector3.zero;
                    accessory.transform.localRotation = Quaternion.identity;
                    accessory.transform.localScale = Vector3.one;
                }
            }
            else
            {
                Debug.LogError("No se encontró 'AccessoryAttachmentPoint' en el NPC: " + npc.name);
                Destroy(accessory); // Limpiar el objeto huérfano
            }
        }
    }
    
    // MÉTODOS PÚBLICOS PARA EL SCRIPT DEL DRON
    
    /// <summary>
    /// Encuentra todos los NPCs que coinciden con una configuración específica
    /// </summary>
    public List<SpawnedNPCData> FindNPCsByConfiguration(NPCConfiguration targetConfig)
    {
        List<SpawnedNPCData> matches = new List<SpawnedNPCData>();
        
        foreach(SpawnedNPCData npcData in spawnedNPCs)
        {
            if(npcData.configuration.Equals(targetConfig))
            {
                matches.Add(npcData);
            }
        }
        
        Debug.Log($"Encontrados {matches.Count} NPCs con configuración: {targetConfig}");
        return matches;
    }
    
    /// <summary>
    /// Encuentra el NPC más cercano con una configuración específica
    /// </summary>
    public SpawnedNPCData FindClosestNPCByConfiguration(NPCConfiguration targetConfig, Vector3 fromPosition)
    {
        List<SpawnedNPCData> matches = FindNPCsByConfiguration(targetConfig);
        
        if(matches.Count == 0) 
        {
            Debug.LogWarning($"No se encontró ningún NPC con configuración: {targetConfig}");
            return null;
        }
        
        SpawnedNPCData closest = matches[0];
        float closestDistance = Vector3.Distance(fromPosition, closest.position);
        
        foreach(SpawnedNPCData npcData in matches)
        {
            float distance = Vector3.Distance(fromPosition, npcData.position);
            if(distance < closestDistance)
            {
                closest = npcData;
                closestDistance = distance;
            }
        }
        
        Debug.Log($"NPC más cercano encontrado: {closest} a distancia {closestDistance}");
        return closest;
    }
    
    /// <summary>
    /// Obtiene información de un NPC por su ID
    /// </summary>
    public SpawnedNPCData GetNPCByID(int npcID)
    {
        foreach(SpawnedNPCData npcData in spawnedNPCs)
        {
            if(npcData.npcID == npcID)
            {
                return npcData;
            }
        }
        
        Debug.LogWarning($"No se encontró NPC con ID: {npcID}");
        return null;
    }
    
    /// <summary>
    /// Imprime todas las configuraciones spawneadas (útil para debug)
    /// </summary>
    public void PrintAllSpawnedNPCs()
    {
        Debug.Log("=== LISTA DE TODOS LOS NPCs SPAWNEADOS ===");
        foreach(SpawnedNPCData npcData in spawnedNPCs)
        {
            Debug.Log(npcData.ToString());
        }
    }
}