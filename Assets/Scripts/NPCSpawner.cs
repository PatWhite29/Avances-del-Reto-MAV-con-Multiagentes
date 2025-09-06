using System;
using UnityEngine;

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

        //Leer dimensiones del SpawnPlane
        Renderer planeRenderer = spawnPlane.GetComponent<Renderer>();
        Bounds planeBounds = planeRenderer.bounds;

        if (numberOfNPC < (hatConfigs.Length * accessoryConfigs.Length * materials.Length))
        {
            //Loop para crear NPCs
            for (int i = 0; i < numberOfNPC; i++)
            {
                //Posicion random
                float randomX = UnityEngine.Random.Range(planeBounds.min.x, planeBounds.max.x);
                float randomZ = UnityEngine.Random.Range(planeBounds.min.z, planeBounds.max.z);

                Vector3 randomSpawnPosition = new Vector3(randomX, planeBounds.max.y, randomZ);

                //Crear NPCs
                GameObject newNPC = Instantiate(npcPrefab, randomSpawnPosition, npcPrefab.transform.rotation);

                // Asegurar etiqueta para detección emulada
                try { newNPC.tag = "Person"; } catch { /* si la tag no existe o es inválida */ }

                // Asegurar descriptor de persona para características aleatorias
                var descriptor = newNPC.GetComponent<PersonDescriptor>();
                if (descriptor == null) descriptor = newNPC.AddComponent<PersonDescriptor>();
                descriptor.randomizeOnStart = true;

                //Personalizar NPC
                int materialIndex = UnityEngine.Random.Range(0, materials.Length);
                Renderer npcRenderer = newNPC.GetComponentInChildren<Renderer>();
                if (npcRenderer != null)
                {
                    npcRenderer.material = materials[materialIndex];
                }

                //Ponerles sombrerito
                if (hatConfigs.Length > 0)
                {
                    int hatIndex = UnityEngine.Random.Range(0, hatConfigs.Length);
                    HatConfig selectedHat = hatConfigs[hatIndex];

                    GameObject hat = Instantiate(selectedHat.prefab);

                    //Buscar el anclaje del sombrero
                    Transform hatAttachPoint = newNPC.transform.Find("HatAttachmentPoint");

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
                        Debug.LogError("No se encontró 'HatAttachmentPoint' en el NPC: " + newNPC.name);
                        Destroy(hat); // Limpiar el objeto huérfano
                    }
                }

                //Ponerle accesorios uwu
                if (accessoryConfigs.Length > 0)
                {
                    int accessoryIndex = UnityEngine.Random.Range(0, accessoryConfigs.Length);
                    AccessoryConfig selectedAccessory = accessoryConfigs[accessoryIndex];

                    GameObject accessory = Instantiate(selectedAccessory.prefab);

                    //Buscar anclaje de accesorio
                    Transform accessoryAttachPoint = newNPC.transform.Find("AccessoryAttachmentPoint");

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
                        Debug.LogError("No se encontró 'AccessoryAttachmentPoint' en el NPC: " + newNPC.name);
                        Destroy(accessory); // Limpiar el objeto huérfano
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Numero de NPC Mayor al numero de configuraciones posibles, favor de ingresar un numero menor a " + (hatConfigs.Length * accessoryConfigs.Length * materials.Length));
            return;
        }
    }
}
