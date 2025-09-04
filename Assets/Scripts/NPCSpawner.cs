using System;
using UnityEngine;

public class NPCSpawner : MonoBehaviour
{
    // Assets
    [Header("Assets para NPC")]
    public GameObject npcPrefab;
    public GameObject[] hatPrefabs;
    public GameObject[] accesoryPrefabs;
    public Material[] materials;

    // Configuracion de spawn y spawn plane
    [Header("Configuración de Spawn")]
    public int numberOfNPC = 50;
    public GameObject spawnPlane;

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

        //Loop para crear NPCs
        for (int i = 0; i < numberOfNPC; i++)
        {
            //Posicion random
            float randomX = UnityEngine.Random.Range(planeBounds.min.x, planeBounds.max.x);
            float randomZ = UnityEngine.Random.Range(planeBounds.min.z, planeBounds.max.z);

            Vector3 randomSpawnPosition = new Vector3(randomX, planeBounds.max.y, randomZ);

            //Crear NPCs
            GameObject newNPC = Instantiate(npcPrefab, randomSpawnPosition, npcPrefab.transform.rotation);

            //Personalizar NPC
            int materialIndex = UnityEngine.Random.Range(0, materials.Length);
            Renderer npcRenderer = newNPC.GetComponentInChildren<Renderer>();
            if (npcRenderer != null)
            {
                npcRenderer.material = materials[materialIndex];
            }

            //Ponerles sombrerito
            if (hatPrefabs.Length > 0)
            {
                int hatIndex = UnityEngine.Random.Range(0, hatPrefabs.Length);
                GameObject hat = Instantiate(hatPrefabs[hatIndex]);

                //Buscar el anclaje del sombrero
                Transform hatAttachPoint = newNPC.transform.Find("HatAttachmentPoint");

                if (hatAttachPoint != null)
                {
                    hat.transform.SetParent(hatAttachPoint);
                    hat.transform.localPosition = Vector3.zero;
                    hat.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    Debug.LogError("No se encontró 'HatAttachmentPoint' en el NPC: " + newNPC.name);
                }
            }

            //Pomerle accesorios uwu
            if (accesoryPrefabs.Length > 0)
            {
                int accesoryIndex = UnityEngine.Random.Range(0, accesoryPrefabs.Length);
                GameObject accesory = Instantiate(accesoryPrefabs[accesoryIndex]);

                //Buscar anclaje de accesorio
                Transform accesoryAttachPoint = newNPC.transform.Find("AccessoryAttachmentPoint");

                if (accesoryAttachPoint != null)
                {
                    accesory.transform.SetParent(accesoryAttachPoint);
                    accesory.transform.localPosition = Vector3.zero;
                    accesory.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    Debug.LogError("No se encontró 'AccessoryAttachmentPoint' en el NPC: " + newNPC.name);
                }
            }
        }
    }
}
