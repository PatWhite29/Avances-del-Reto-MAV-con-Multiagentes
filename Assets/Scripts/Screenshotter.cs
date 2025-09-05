using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

// Las clases de configuración están ahora en SharedConfigs.cs

public class Screenshotter : MonoBehaviour
{
    [Header("Referencias de la Escena")]
    public GameObject npc;
    public Transform cameraRig;
    public Camera mainCamera;

    [Header("Configuración del NPC")]
    public NPCConfig npcConfig;

    [Header("Assets para Combinaciones")]
    public Material[] materials;
    public HatConfig[] hatConfigs;
    public AccessoryConfig[] accessoryConfigs;

    [Header("Configuración FBX")]
    public bool useFBXCompatibilityMode = true;

    [Header("Modo de Prueba")]
    [Range(0, 8)] public int testHatIndex = 0;
    [Range(0, 8)] public int testAccessoryIndex = 0;
    [Range(0, 8)] public int testMaterialIndex = 0;

    // Variables privadas
    private Renderer npcRenderer;
    private Transform hatAttachPoint;
    private Transform accessoryAttachPoint;
    private Vector3 originalNPCPosition;
    private Quaternion originalNPCRotation;
    private Vector3 originalNPCScale;

    void Start()
    {
        npcRenderer = npc.GetComponentInChildren<Renderer>();
        hatAttachPoint = npc.transform.Find("HatAttachmentPoint");
        accessoryAttachPoint = npc.transform.Find("AccessoryAttachmentPoint");

        // Guardar transformación original del NPC
        originalNPCPosition = npc.transform.localPosition;
        originalNPCRotation = npc.transform.localRotation;
        originalNPCScale = npc.transform.localScale;

        // Aplicar configuración inicial del NPC si está en modo compatibilidad
        if (useFBXCompatibilityMode)
        {
            ApplyNPCConfiguration();
        }
        
        // Aplicar la primera combinación para prueba visual
        ApplyTestCombination();

        // StartCoroutine(TakeAllScreenshots()); // Comentado temporalmente para revisar posicionamiento
    }

    void ApplyNPCConfiguration()
    {
        npc.transform.localPosition = npcConfig.localPosition;
        npc.transform.localRotation = Quaternion.Euler(npcConfig.localRotation);
        npc.transform.localScale = npcConfig.localScale;
    }

    void ResetNPCConfiguration()
    {
        npc.transform.localPosition = originalNPCPosition;
        npc.transform.localRotation = originalNPCRotation;
        npc.transform.localScale = originalNPCScale;
    }

    void ApplyTestCombination()
    {
        // Aplicar el material seleccionado para prueba
        if (materials.Length > 0 && testMaterialIndex < materials.Length)
        {
            npcRenderer.material = materials[testMaterialIndex];
        }

        // Limpiar sombreros/accesorios existentes
        ClearExistingItems();

        // Aplicar sombrero seleccionado
        if (hatConfigs.Length > 0 && testHatIndex < hatConfigs.Length && hatAttachPoint != null)
        {
            HatConfig hatConfig = hatConfigs[testHatIndex];
            GameObject testHat = Instantiate(hatConfig.prefab);
            testHat.name = "TestHat";
            
            // Hacer hijo del attachment point
            testHat.transform.SetParent(hatAttachPoint);
            
            if (useFBXCompatibilityMode)
            {
                testHat.transform.localPosition = hatConfig.localPosition;
                testHat.transform.localRotation = Quaternion.Euler(hatConfig.localRotation);
                testHat.transform.localScale = hatConfig.localScale;
            }
            else
            {
                testHat.transform.localPosition = Vector3.zero;
                testHat.transform.localRotation = Quaternion.identity;
                testHat.transform.localScale = Vector3.one;
            }
            
            Debug.Log($"Sombrero aplicado: {hatConfig.prefab.name} (índice {testHatIndex})");
        }
        else if (hatConfigs.Length > 0 && hatAttachPoint == null)
        {
            Debug.LogError("No se encontró 'HatAttachmentPoint' en el NPC. Verifica que existe como hijo del NPC.");
        }

        // Aplicar accesorio seleccionado
        if (accessoryConfigs.Length > 0 && testAccessoryIndex < accessoryConfigs.Length && accessoryAttachPoint != null)
        {
            AccessoryConfig accConfig = accessoryConfigs[testAccessoryIndex];
            GameObject testAccessory = Instantiate(accConfig.prefab);
            testAccessory.name = "TestAccessory";
            
            // Hacer hijo del attachment point
            testAccessory.transform.SetParent(accessoryAttachPoint);
            
            if (useFBXCompatibilityMode)
            {
                testAccessory.transform.localPosition = accConfig.localPosition;
                testAccessory.transform.localRotation = Quaternion.Euler(accConfig.localRotation);
                testAccessory.transform.localScale = accConfig.localScale;
            }
            else
            {
                testAccessory.transform.localPosition = Vector3.zero;
                testAccessory.transform.localRotation = Quaternion.identity;
                testAccessory.transform.localScale = Vector3.one;
            }
            
            Debug.Log($"Accesorio aplicado: {accConfig.prefab.name} (índice {testAccessoryIndex})");
        }
        else if (accessoryConfigs.Length > 0 && accessoryAttachPoint == null)
        {
            Debug.LogError("No se encontró 'AccessoryAttachmentPoint' en el NPC. Verifica que existe como hijo del NPC.");
        }

        Debug.Log("Combinación de prueba aplicada. Ahora puedes ajustar manualmente en Scene View y copiar los valores.");
    }

    void ClearExistingItems()
    {
        // Limpiar sombrero de prueba anterior
        if (hatAttachPoint != null)
        {
            Transform existingHat = hatAttachPoint.Find("TestHat");
            if (existingHat != null) DestroyImmediate(existingHat.gameObject);
        }

        // Limpiar accesorio de prueba anterior
        if (accessoryAttachPoint != null)
        {
            Transform existingAccessory = accessoryAttachPoint.Find("TestAccessory");
            if (existingAccessory != null) DestroyImmediate(existingAccessory.gameObject);
        }
    }

    [ContextMenu("Aplicar Configuración de Prueba")]
    public void ApplyTestConfiguration()
    {
        if (useFBXCompatibilityMode)
        {
            ApplyNPCConfiguration();
        }
        ApplyTestCombination();
    }

    [ContextMenu("Copiar Valores del Scene View")]
    public void CopyValuesFromSceneView()
    {
        // Buscar y copiar valores del sombrero
        if (hatAttachPoint != null)
        {
            Transform testHat = hatAttachPoint.Find("TestHat");
            if (testHat != null && testHatIndex < hatConfigs.Length)
            {
                hatConfigs[testHatIndex].localPosition = testHat.localPosition;
                hatConfigs[testHatIndex].localRotation = testHat.localEulerAngles;
                hatConfigs[testHatIndex].localScale = testHat.localScale;
                Debug.Log($"✅ Valores copiados para sombrero {testHatIndex}: Pos{testHat.localPosition}, Rot{testHat.localEulerAngles}, Scale{testHat.localScale}");
            }
            else
            {
                Debug.LogWarning("No se encontró TestHat o índice inválido");
            }
        }

        // Buscar y copiar valores del accesorio
        if (accessoryAttachPoint != null)
        {
            Transform testAccessory = accessoryAttachPoint.Find("TestAccessory");
            if (testAccessory != null && testAccessoryIndex < accessoryConfigs.Length)
            {
                accessoryConfigs[testAccessoryIndex].localPosition = testAccessory.localPosition;
                accessoryConfigs[testAccessoryIndex].localRotation = testAccessory.localEulerAngles;
                accessoryConfigs[testAccessoryIndex].localScale = testAccessory.localScale;
                Debug.Log($"✅ Valores copiados para accesorio {testAccessoryIndex}: Pos{testAccessory.localPosition}, Rot{testAccessory.localEulerAngles}, Scale{testAccessory.localScale}");
            }
            else
            {
                Debug.LogWarning("No se encontró TestAccessory o índice inválido");
            }
        }
        
        Debug.Log("🎯 Valores copiados! Ahora están guardados en las configuraciones.");
    }

    [ContextMenu("Restaurar NPC Original")]
    public void RestoreOriginalNPC()
    {
        ResetNPCConfiguration();
        ClearExistingItems();
        Debug.Log("NPC restaurado a configuración original.");
    }

    IEnumerator TakeAllScreenshots()
    {
        Debug.Log("Iniciando sesión de fotos y anotación automática...");
        string folderPath = Application.dataPath + "/../Generated_Dataset";
        if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

        float[] angles = { 0f, 45f, 90f, 135f, 180f, 225f, 270f, 315f };

        for (int matIndex = 0; matIndex < materials.Length; matIndex++)
        {
            npcRenderer.material = materials[matIndex];

            for (int hatIndex = 0; hatIndex < hatConfigs.Length; hatIndex++)
            {
                HatConfig hatConfig = hatConfigs[hatIndex];
                GameObject currentHat = Instantiate(hatConfig.prefab);
                
                if (hatAttachPoint != null)
                {
                    currentHat.transform.SetParent(hatAttachPoint);
                    
                    if (useFBXCompatibilityMode)
                    {
                        currentHat.transform.localPosition = hatConfig.localPosition;
                        currentHat.transform.localRotation = Quaternion.Euler(hatConfig.localRotation);
                        currentHat.transform.localScale = hatConfig.localScale;
                    }
                    else
                    {
                        currentHat.transform.localPosition = Vector3.zero;
                        currentHat.transform.localRotation = Quaternion.identity;
                        currentHat.transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    Debug.LogError("No se encontró 'HatAttachmentPoint' en el NPC");
                    Destroy(currentHat);
                    continue;
                }

                Renderer hatRenderer = currentHat.GetComponentInChildren<Renderer>();

                for (int accIndex = 0; accIndex < accessoryConfigs.Length; accIndex++)
                {
                    AccessoryConfig accConfig = accessoryConfigs[accIndex];
                    GameObject currentAccessory = Instantiate(accConfig.prefab);
                    
                    if (accessoryAttachPoint != null)
                    {
                        currentAccessory.transform.SetParent(accessoryAttachPoint);
                        
                        if (useFBXCompatibilityMode)
                        {
                            currentAccessory.transform.localPosition = accConfig.localPosition;
                            currentAccessory.transform.localRotation = Quaternion.Euler(accConfig.localRotation);
                            currentAccessory.transform.localScale = accConfig.localScale;
                        }
                        else
                        {
                            currentAccessory.transform.localPosition = Vector3.zero;
                            currentAccessory.transform.localRotation = Quaternion.identity;
                            currentAccessory.transform.localScale = Vector3.one;
                        }
                    }
                    else
                    {
                        Debug.LogError("No se encontró 'AccessoryAttachmentPoint' en el NPC");
                        Destroy(currentAccessory);
                        continue;
                    }

                    Renderer accRenderer = currentAccessory.GetComponentInChildren<Renderer>();

                    foreach (float angle in angles)
                    {
                        cameraRig.rotation = Quaternion.Euler(0, angle, 0);
                        
                        // Esperar al frame para que las posiciones se actualicen
                        yield return new WaitForEndOfFrame();

                        string baseFilename = $"{materials[matIndex].name}_{hatConfig.prefab.name}_{accConfig.prefab.name}_angle-{angle}";
                        string imagePath = Path.Combine(folderPath, baseFilename + ".png");
                        string labelPath = Path.Combine(folderPath, baseFilename + ".txt");

                        // Tomar la foto
                        ScreenCapture.CaptureScreenshot(imagePath);

                        // Escribir el archivo de anotación
                        WriteAnnotationFile(labelPath, matIndex, hatIndex, accIndex, hatRenderer, accRenderer);
                        
                        yield return new WaitForSeconds(0.1f);
                    }
                    Destroy(currentAccessory);
                }
                Destroy(currentHat);
            }
        }
        
        // Restaurar configuración original del NPC al finalizar
        if (useFBXCompatibilityMode)
        {
            ResetNPCConfiguration();
        }
        
        Debug.Log("¡Proceso completado! Dataset generado en 'Generated_Dataset'.");
    }

    void WriteAnnotationFile(string path, int matId, int hatId, int accId, Renderer hatRend, Renderer accRend)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            // Anotación para el cuerpo del NPC
            string npcLine = GetYoloLine(npcRenderer, matId);
            if (npcLine != null) writer.WriteLine(npcLine);

            // Anotación para el sombrero
            // El ID de clase del sombrero empieza después de los materiales
            string hatLine = GetYoloLine(hatRend, hatId + materials.Length);
            if (hatLine != null) writer.WriteLine(hatLine);

            // Anotación para el accesorio
            // El ID del accesorio empieza después de materiales y sombreros
            string accLine = GetYoloLine(accRend, accId + materials.Length + hatConfigs.Length);
            if (accLine != null) writer.WriteLine(accLine);
        }
    }

    string GetYoloLine(Renderer rend, int classId)
    {
        if (rend == null || !rend.isVisible) return null;

        Bounds bounds = rend.bounds;
        Vector3[] corners = new Vector3[8];
        corners[0] = new Vector3(bounds.min.x, bounds.min.y, bounds.min.z);
        corners[1] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
        corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
        corners[3] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
        corners[4] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
        corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
        corners[6] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
        corners[7] = new Vector3(bounds.max.x, bounds.max.y, bounds.max.z);

        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        for (int i = 0; i < 8; i++)
        {
            Vector3 screenPoint = mainCamera.WorldToScreenPoint(corners[i]);
            if (screenPoint.x < minX) minX = screenPoint.x;
            if (screenPoint.x > maxX) maxX = screenPoint.x;
            if (screenPoint.y < minY) minY = screenPoint.y;
            if (screenPoint.y > maxY) maxY = screenPoint.y;
        }

        // Evitar cajas fuera de la pantalla
        if (maxX < 0 || minX > Screen.width || maxY < 0 || minY > Screen.height) return null;

        float width = maxX - minX;
        float height = maxY - minY;
        float centerX = minX + width / 2;
        float centerY = minY + height / 2;

        // Normalizar para YOLO
        float normCenterX = centerX / Screen.width;
        float normCenterY = 1 - (centerY / Screen.height); // La 'Y' en YOLO se cuenta desde arriba
        float normWidth = width / Screen.width;
        float normHeight = height / Screen.height;

        return $"{classId} {normCenterX} {normCenterY} {normWidth} {normHeight}";
    }
}