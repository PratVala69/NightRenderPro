using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class MeshDuplicator : MonoBehaviour
{
    [SerializeField] private Transform _parentForCopy;
    [SerializeField] private Material _materialForCopy;
    // Частина вашого AR-менеджер-скрипта
    [SerializeField] private ARMeshManager arMeshManager;
    void Start()
    {
        // Отримайте компонент ARMeshManager
        arMeshManager = GetComponent<ARMeshManager>();
        if (arMeshManager != null)
        {
            arMeshManager.meshesChanged += OnMeshesChanged;
        }
    }

    void OnMeshesChanged(ARMeshesChangedEventArgs eventArgs)
    {
        // Обробка доданих сіток
        Debug.Log(" eventArgs.updated.Count : " + eventArgs.updated.Count);
        foreach (var arMesh in eventArgs.added)
        {
            // arMesh.gameObject - це об'єкт сітки, створений AR Foundation.
            // Це те місце, де ви робите свою копію
            Debug.Log(" added: " + eventArgs.added.Count);
            

            // Якщо ви хочете, щоб ваша копія залишалася статичною, а оригінал продовжував оновлюватися:
            // Ви можете зберігати посилання на 'arMesh' і 'meshCopy' для подальшої логіки.
        }
    
        // Обробка оновлених сіток (зазвичай не потрібно для створення КОПІЇ)
        // Якщо вам потрібна "остання" версія сітки, ви можете оновлювати копію тут.
        foreach (var arMesh in eventArgs.updated)
        {
            GameObject originalMesh = arMesh.gameObject;
        
            Debug.Log(" updated : " + eventArgs.updated.Count);
            // Створюємо копію
            Debug.Log(originalMesh.name + "  ====== Orinanal");
            GameObject meshCopy = CreateIndependentMeshCopy(originalMesh, transform);
            
            MeshRenderer mR = meshCopy.GetComponent<MeshRenderer>();
            mR.material = _materialForCopy;
            mR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
            Debug.LogError(mR.shadowCastingMode + "  ======   shadowCastingMode ");
            Debug.LogError(mR.material.name + "  ====== Material Name");
            
            
        }
    }
    
    public static GameObject CreateIndependentMeshCopy(GameObject originalMeshObject, Transform parent)
    {
        // 1. Перевірте, чи є оригінал дійсним
        MeshFilter originalMeshFilter = originalMeshObject.GetComponent<MeshFilter>();
        if (originalMeshFilter == null || originalMeshFilter.sharedMesh == null)
        {
            Debug.LogError("Original object does not have a valid MeshFilter.");
            return null;
        }

        // 2. Створіть новий GameObject для копії
        GameObject duplicateObject = new GameObject(originalMeshObject.name + "_Copy");
        duplicateObject.transform.SetParent(parent);
        duplicateObject.transform.position = originalMeshObject.transform.position;
        duplicateObject.transform.rotation = originalMeshObject.transform.rotation;
        duplicateObject.transform.localScale = originalMeshObject.transform.localScale;

        // 3. Створіть новий компонент MeshFilter
        MeshFilter duplicateMeshFilter = duplicateObject.AddComponent<MeshFilter>();

        // 4. Створіть абсолютно новий об'єкт Mesh
        Mesh newMesh = new Mesh();
        
        // ВАЖЛИВО: Скопіюйте всі дані з оригінального Mesh в новий
        newMesh.vertices = originalMeshFilter.sharedMesh.vertices;
        newMesh.triangles = originalMeshFilter.sharedMesh.triangles;
        newMesh.normals = originalMeshFilter.sharedMesh.normals;
        newMesh.uv = originalMeshFilter.sharedMesh.uv;
        newMesh.colors = originalMeshFilter.sharedMesh.colors;

        // Опціонально: перерахуйте параметри для кращої геометрії
        newMesh.RecalculateBounds();
        // newMesh.RecalculateNormals(); 

        // 5. Призначте новий Mesh для MeshFilter дубліката
        duplicateMeshFilter.mesh = newMesh;

        // 6. Скопіюйте MeshRenderer для візуалізації
        MeshRenderer originalRenderer = originalMeshObject.GetComponent<MeshRenderer>();
        
        if (originalRenderer != null)
        {
            MeshRenderer duplicateRenderer = duplicateObject.AddComponent<MeshRenderer>();
            
        }

        // 7. Опціонально: Скопіюйте MeshCollider, якщо він потрібен для взаємодії
        MeshCollider originalCollider = originalMeshObject.GetComponent<MeshCollider>();
        if (originalCollider != null)
        {
            MeshCollider duplicateCollider = duplicateObject.AddComponent<MeshCollider>();
            duplicateCollider.sharedMesh = newMesh; // Використовуйте новий Mesh
        }

        return duplicateObject;
    }
}
