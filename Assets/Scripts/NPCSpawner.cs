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

            // Asegurar etiqueta para detección emulada
            try { newNPC.tag = "Person"; } catch { /* si la tag no existe o es inválida */ }

            // Asegurar descriptor de persona para características aleatorias
            var descriptor = newNPC.GetComponent<PersonDescriptor>();
            if (descriptor == null) descriptor = newNPC.AddComponent<PersonDescriptor>();
            descriptor.randomizeOnStart = true;

            // Aplicar configuración única a este NPC
            ApplyConfigurationToNPC(newNPC, allConfigurations[i]);
        }
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
}