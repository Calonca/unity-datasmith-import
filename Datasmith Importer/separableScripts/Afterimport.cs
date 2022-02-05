using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Contains methods specific for your project
/// </summary>
public class Afterimport : MonoBehaviour
{



    /// <summary>
    /// This method is called after the model is imported
    /// </summary>
    /// <param name="obj">The imported model</param>
    public static void afterImport(GameObject obj)
    {
        List<Mesh> mr = obj.GetComponentsInChildren<MeshFilter>().Select(e => e.sharedMesh).ToList();

        int nVerts = mr.Select(m => m.vertexCount).Sum();
        int nTris = mr.Select(m => m.triangles.Length).Sum();
        Debug.Log("Import Complete, \ntotal v    =" + nVerts + "\ntotal tris =" + nTris);

        //obj.GetComponentsInChildren<MetadataManager>().ToList().ForEach(e => e.enabled = false);

        //obj.GetComponentsInChildren<MeshCollider>().ToList().ForEach(e => e.enabled = false);
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}