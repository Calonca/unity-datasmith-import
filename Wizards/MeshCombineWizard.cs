//Modified vversion of this script: https://github.com/sirgru/MeshCombineWizard
//Original MIT licence: https://github.com/sirgru/MeshCombineWizard/blob/master/License.txt

#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System;

public class MeshCombineWizard : ScriptableWizard
{
	public bool is32bit = true;
	public GameObject combineParent;


	[MenuItem("Optimization/Mesh Combine Wizard")]
	static void CreateWizard()
	{
		var wizard = DisplayWizard<MeshCombineWizard>("Mesh Combine Wizard");

		// If there is selection, and the selection of one Scene object, auto-assign it
		var selectionObjects = Selection.objects;
		if (selectionObjects != null && selectionObjects.Length == 1) {
			var firstSelection = selectionObjects[0] as GameObject;
			if (firstSelection != null) {
				wizard.combineParent = firstSelection;
			}
		}
	}

	public void OnWizardCreate()
    {

        // Verify there is existing object root, ptherwise bail.
        if (combineParent == null)
        {
            Debug.LogError("Mesh Combine Wizard: Parent of objects to combne not assigned. Operation cancelled.");
            return;
        }

        AssetDatabase.CreateFolder("Assets", "CombinedMeshes_" + combineParent.name);
        // Remember the original position of the object. 
        // For the operation to work, the position must be temporarily set to (0,0,0).
        Vector3 originalPosition = combineParent.transform.position;
        combineParent.transform.position = Vector3.zero;

        // Locals
        Dictionary<Material, List<Tuple<MeshFilter, int>>> materialToMeshFilterList = new Dictionary<Material, List<Tuple<MeshFilter, int>>>();
        List<GameObject> combinedObjects = new List<GameObject>();

        MeshFilter[] meshFilters = combineParent.GetComponentsInChildren<MeshFilter>();


        Debug.Log("combined object len:" + meshFilters.Length);

        getMaterials(materialToMeshFilterList, meshFilters);

        int cbCount = 0;
        // For each material, create a new merged object, in the scene and in the assets folder.
        AssetDatabase.StartAssetEditing();
        foreach (var entry in materialToMeshFilterList)
        {
            List<Tuple<MeshFilter, int>> meshesWithSameMaterial = entry.Value;

            // Create a convenient material name
            string materialName = entry.Key.ToString().Split(' ')[0];
            CombineInstance[] combine = createCombine(meshesWithSameMaterial);

            // Create a new mesh using the combined properties
            var format = is32bit ? IndexFormat.UInt32 : IndexFormat.UInt16;
            Mesh combinedMesh = new Mesh { indexFormat = format };
            combinedMesh.CombineMeshes(combine);

            // Create asset
            materialName += "_" + combinedMesh.GetInstanceID();
            AssetDatabase.CreateAsset(combinedMesh, "Assets/" + "CombinedMeshes_" + combineParent.name + "/CombinedMeshes_" + materialName + ".asset");

            // Create game object
            string goName = (materialToMeshFilterList.Count > 1) ? "CombinedMeshes[vtx=" + combinedMesh.vertexCount+ "]_" + materialName : "CombinedMeshes_" + combineParent.name;
            GameObject combinedObject = new GameObject(goName);
            var filter = combinedObject.AddComponent<MeshFilter>();
            cbCount += combinedMesh.vertexCount;
            filter.sharedMesh = combinedMesh;

            var renderer = combinedObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = entry.Key;
            combinedObjects.Add(combinedObject);
        }
        AssetDatabase.StopAssetEditing();

        Debug.Log("combined object len:" + combinedObjects.Count);
        // If there were more than one material, and thus multiple GOs created, parent them and work with result
        GameObject resultGO = null;
        if (combinedObjects.Count > 1)
        {
            resultGO = new GameObject("CombinedMeshes_" + combineParent.name);
            foreach (var combinedObject in combinedObjects) combinedObject.transform.parent = resultGO.transform;
        }
        else
        {
            resultGO = combinedObjects[0];
        }

        // Create prefab
        PrefabUtility.SaveAsPrefabAssetAndConnect(resultGO, "Assets/" + resultGO.name + ".prefab", InteractionMode.UserAction);
        //PrefabUtility.ReplacePrefab(resultGO, prefab, ReplacePrefabOptions.ConnectToPrefab);

        // Disable the original and return both to original positions
        //combineParent.SetActive(false);
        combineParent.transform.position = originalPosition;
        resultGO.transform.parent = combineParent.transform.parent ?? combineParent.transform;
        resultGO.transform.position = originalPosition;
        Debug.Log("total mesh vertices" +cbCount);
        datasmithImporter.removeOriginalMeshes(combineParent.transform);
    }

    private static CombineInstance[] createCombine(List<Tuple<MeshFilter, int>> meshesWithSameMaterial)
    {
        CombineInstance[] combine = new CombineInstance[meshesWithSameMaterial.Count];
        for (int i = 0; i < meshesWithSameMaterial.Count; i++)
        {
            Mesh toCombine = meshesWithSameMaterial[i].Item1.sharedMesh;
            int subMeshIndex = meshesWithSameMaterial[i].Item2;

            //Debug.Log("Submeshes: " + toCombine.subMeshCount);
            //Debug.Log("Vertices: " + toCombine.vertices.Length);
            SubMeshDescriptor meshD = toCombine.GetSubMesh(subMeshIndex);
            //Debug.Log("Submesh "+i+" triangles from,to: " + meshD.indexStart + ", " + (meshD.indexStart + meshD.indexCount));

            Mesh meshWithMat = new Mesh();
            meshWithMat.indexFormat = toCombine.indexFormat;

            int[] tris = toCombine.GetTriangles(subMeshIndex);
            Vector3[] toCombineVerts = toCombine.vertices;
            Vector2[] toCombineUvs = toCombine.uv;
            //Debug.Log("tris: " +string.Join(", ",tris));


            Vector3[] verts = new Vector3[tris.Length];
            Vector2[] uvs = new Vector2[tris.Length];
            for (int j = 0; j < tris.Length; j++)
            {
                verts[j] = toCombineVerts[tris[j]];
                uvs[j] = toCombineUvs[tris[j]];
            }
            //Vector3[] verts = tris.Select(idx => toCombine.vertices[idx]).ToArray();

            for (int j = 0; j < tris.Length; j++)
            {
                tris[j] = j;
            }

            meshWithMat.vertices = verts;
            meshWithMat.triangles = tris;//Enumerable.Range(0, meshD.indexCount).ToArray();
            meshWithMat.uv = uvs;
            meshWithMat.RecalculateNormals();

            combine[i].mesh = meshWithMat;//   THIS IS TO CHANGE
            combine[i].transform = meshesWithSameMaterial[i].Item1.transform.localToWorldMatrix;
        }

        return combine;
    }

    private static void getMaterials(Dictionary<Material, List<Tuple<MeshFilter, int>>> materialToMeshFilterList, MeshFilter[] meshFilters)
    {
        // Go through all mesh filters and establish the mapping between the materials and all mesh filters using it.
        for (int i1 = 0; i1 < meshFilters.Length; i1++)
        {
            MeshFilter meshFilter = meshFilters[i1];
            var meshRenderer = meshFilter.GetComponent<MeshRenderer>();
            if (meshRenderer == null)
            {
                Debug.LogWarning("The Mesh Filter on object " + meshFilter.name + " has no Mesh Renderer component attached. Skipping.");
                continue;
            }

            var materials = meshRenderer.sharedMaterials;
            if (materials == null)
            {
                Debug.LogWarning("The Mesh Renderer on object " + meshFilter.name + " has no material assigned. Skipping.");
                continue;
            }
            /*
			// If there are multiple materials on a single mesh, cancel.
			if (materials.Length > 1) {
				// Rollback: return the object to original position
				combineParent.transform.position = originalPosition;
				Debug.LogError("Objects with multiple materials on the same mesh are not supported. Create multiple meshes from this object's sub-meshes in an external 3D tool and assign separate materials to each. Operation cancelled.");
				return;
			}*/
            //var material = materials[0];

            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                // Add material to mesh filter mapping to dictionary
                if (materialToMeshFilterList.ContainsKey(material))
                    materialToMeshFilterList[material].Add(new Tuple<MeshFilter, int>(meshFilter, i));
                else materialToMeshFilterList.Add(material, new List<Tuple<MeshFilter, int>>() { new Tuple<MeshFilter, int>(meshFilter, i) });
            }
        }
    }
}
#endif