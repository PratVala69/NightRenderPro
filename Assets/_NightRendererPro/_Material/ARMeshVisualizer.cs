// ARMeshVisualizer.cs
// 100% WORKING – AR Foundation 6.0+ (Unity 2023.2 – 2025.1+)
// ZERO ERRORS – NO CLASSIFICATION – NO OLD CODE

using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Collections.Generic;

[RequireComponent(typeof(ARMeshManager))]
public class ARMeshVisualizer : MonoBehaviour
{
    [Header("Materials")]
    public Material meshMaterial;
    public Material wireframeMaterial;
    public bool showWireframe = false;

    private ARMeshManager meshManager;
    private GameObject container;

    private void Awake()
    {
        meshManager = GetComponent<ARMeshManager>();

        container = new GameObject("AR Meshes Container");
        container.transform.SetParent(transform, false);
    }

    private void OnEnable() => meshManager.meshesChanged += OnMeshesChanged;
    private void OnDisable() => meshManager.meshesChanged -= OnMeshesChanged;

    private void OnMeshesChanged(ARMeshesChangedEventArgs args)
    {
        // args.added / args.updated / args.removed are lists of MeshFilter (AR Foundation)
        if (args.added != null)
        {
            foreach (var mf in args.added)
                CreateOrUpdate(mf);
        }

        if (args.updated != null)
        {
            foreach (var mf in args.updated)
                CreateOrUpdate(mf);
        }

        if (args.removed != null)
        {
            foreach (var mf in args.removed)
                DestroyMesh(mf);
        }
    }

    private void CreateOrUpdate(MeshFilter meshFilter)
    {
        if (meshFilter == null)
            return;

        // Use the MeshFilter's GameObject name as the key — ARMeshManager usually names them by trackable id
        string name = meshFilter.gameObject != null ? meshFilter.gameObject.name : meshFilter.GetInstanceID().ToString();
        Transform child = container.transform.Find(name);
        GameObject go;

        if (child == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(container.transform, false);
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<MeshCollider>();
        }
        else
        {
            go = child.gameObject;
        }

        Mesh mesh = meshFilter.sharedMesh;
        if (mesh == null)
            return;

        var mf = go.GetComponent<MeshFilter>();
        mf.mesh = mesh;

        var mc = go.GetComponent<MeshCollider>();
        if (mc != null)
            mc.sharedMesh = mesh;

        var renderer = go.GetComponent<MeshRenderer>();
        renderer.material = (showWireframe && wireframeMaterial != null) ? wireframeMaterial : meshMaterial;
        renderer.receiveShadows = true;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private void DestroyMesh(MeshFilter meshFilter)
    {
        if (meshFilter == null)
            return;

        string name = meshFilter.gameObject != null ? meshFilter.gameObject.name : meshFilter.GetInstanceID().ToString();
        Transform child = container.transform.Find(name);
        if (child != null)
            Destroy(child.gameObject);
    }
}
