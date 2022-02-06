#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;


/// <summary>
/// This class is used to import meshes
/// </summary>
public class UmeshImporter
{
    /// <summary>
    /// Reads nSets floats representing a 3D point in space from br
    /// The coordinates are in centimeters
    /// </summary>
    private static Vector3[] ReadFloatCoords(BinaryReader br, uint nSets)
    {
        Vector3[] vects = new Vector3[nSets];
        for (int i = 0; i < nSets; i++)
        {
            vects[i] = ReadVector3(br);
        }
        return vects;
    }

    /// <summary>
    /// Reads nSets floats representing a 3D point in space from br
    /// The coordinates are in centimeters.
    /// Each sets of 3 coordinates is reversed to account for the fact that Unity uses a clockwise winding order for determining front-facing polygons.
    /// </summary>
    private static Vector3[] ReadFloatCoordsInverted(BinaryReader br, uint nSets)
    {
        Vector3[] vects = new Vector3[nSets];
        for (int i = 0; i < nSets / 3; i++)
        {
            Vector3 v1 = ReadVector3(br);
            Vector3 v2 = ReadVector3(br);
            Vector3 v3 = ReadVector3(br);

            vects[(i * 3)] = v3;
            vects[(i * 3) + 1] = v2;
            vects[(i * 3) + 2] = v1;
        }
        return vects;
    }

    /// <summary>
    /// Returns a vector3 representing a 3D point in space read from br
    /// </summary>
    private static Vector3 ReadVector3(BinaryReader br)
    {
        float x, y, z;
        x = br.ReadSingle();
        y = br.ReadSingle();
        z = br.ReadSingle();
        return new Vector3(x, y, z);
    }


    /// <summary>
    /// Reads n UInt32 representing a indexes of the triangles of the mesh
    /// </summary>
    private static uint[] ReadTris(BinaryReader br, uint n)
    {
        uint x, y, z;
        List<uint> lint = new List<uint>();
        for (int i = 0; i < n / 3; i++)
        {
            x = br.ReadUInt32();
            y = br.ReadUInt32();
            z = br.ReadUInt32();
            lint.Add(x);
            lint.Add(y);
            lint.Add(z);
        }
        return lint.ToArray();
    }

    /// <summary>
    /// Reads n UInt32 representing a indexes of the triangles of the mesh
    /// </summary>
    private static uint[] ReadTrisInverted(BinaryReader br, uint n)
    {
        uint x, y, z;
        List<uint> lint = new List<uint>();
        for (int i = 0; i < n / 3; i++)
        {
            x = br.ReadUInt32();
            y = br.ReadUInt32();
            z = br.ReadUInt32();
            lint.Add(z);
            lint.Add(y);
            lint.Add(x);
        }
        return lint.ToArray();
    }


    /// <summary>
    /// Reads nSets floats representing a 2D point in space from br
    /// Each sets of 3 coordinates is reversed to account for the fact that Unity uses a clockwise winding order for determining front-facing polygons.
    /// </summary>
    private static Vector2[] ReadFloatCoords2DInverted(BinaryReader br, uint nSets)
    {
        Vector2[] vects = new Vector2[nSets];
        for (int i = 0; i < nSets / 3; i++)
        {
            Vector2 v1 = ReadVector2(br);
            Vector2 v2 = ReadVector2(br);
            Vector2 v3 = ReadVector2(br);

            vects[(i * 3)] = v3;
            vects[(i * 3) + 1] = v2;
            vects[(i * 3) + 2] = v1;
        }
        return vects;
    }

    /// <summary>
    /// Reads nSets floats representing a 2D point in space from br
    /// </summary>
    private static Vector2[] ReadFloatCoords2D(BinaryReader br, uint nSets)
    {
        Vector2[] vects = new Vector2[nSets];
        for (int i = 0; i < nSets; i++)
        {
            float x, y;
            x = br.ReadSingle();
            y = br.ReadSingle();
            vects[i] = new Vector2(x, y);
        }
        return vects;
    }

    private static Vector2 ReadVector2(BinaryReader br)
    {
        float x, y;
        x = br.ReadSingle();
        y = br.ReadSingle();
        return new Vector2(x, y);
    }

    private static uint[] ReadUintArray(BinaryReader br, uint n)
    {
        uint[] arr = new uint[n];
        for (int i = 0; i < n; i++)
        {
            arr[i] = br.ReadUInt32();
        }
        return arr;
    }


    /// <summary>
    /// Imports the mesh that has the given filepath
    /// </summary>
    /// <param name="filepath">The mesh filepath</param>
    /// <param name="submeshCount">The number of submeshes of the Mesh(Materials), currently ignored</param>
    /// <param name="importer">The importer, used to read user preferences</param>
    /// <returns>An intermediate mesh data structure. Intermediate between a binary file and an Unity Mesh</returns>
    public static Mesh ImportFromFilepath(string filepath, int submeshCount, datasmithImporter importer)
    {
        if (importer.mode == ImportMode.uvAndSubmeshes)
            return ImportWithUvAndSubmesh(filepath, submeshCount, importer);
        else return ImportCompressedWithoutUvAndSubmesh(filepath, submeshCount, importer);
    }

    public static Mesh ImportCompressedWithoutUvAndSubmesh(string filepath, int submeshCount, datasmithImporter importer)
    {
        if (importer.mode == ImportMode.uvAndSubmeshes) return ImportWithUvAndSubmesh(filepath, submeshCount, importer);

        bool debug = importer.debugMode;
        //Mesh mesh = new Mesh();
        uint filesize = 0;
        FileStream fileStream = new FileStream(Application.dataPath + filepath, FileMode.Open, FileAccess.Read);

        using (BinaryReader r = new BinaryReader(fileStream, System.Text.Encoding.UTF8))
        {

            r.ReadBytes(8);
            int nameLenght = r.ReadInt32();
            //Debug.Log("name len :" + nameLenght);

            r.ReadBytes(nameLenght - 1);

            r.ReadBytes(104);

            filesize = r.ReadUInt32();
            //Debug.Log("filesize is :" + filesize + " bytes");
            filesize = r.ReadUInt32();
            //Debug.Log("filesize is :" + filesize + " bytes");

            r.ReadBytes(16);

            //triangle len
            uint trianglelen1 = r.ReadUInt32();
            //Debug.Log("triangle len 1: " + trianglelen1);

            //tris material slot
            int[] materials = ReadUintArray(r, trianglelen1).Select(u => Convert.ToInt32(u)).ToArray();

            //tris smoothign group
            int[] smoothingGroups = ReadUintArray(r, trianglelen1).Select(u => Convert.ToInt32(u)).ToArray();
            //Debug.Log("The smoothing groups are:\n" + string.Join(", ", smoothingGroups));

            r.ReadBytes(4);

            //Vertices
            uint vertexLengt = vertexLengt = r.ReadUInt32();
            if (debug)
                Debug.Log("vertex len: " + vertexLengt);
            Vector3[] vertices = ReadFloatCoords(r, vertexLengt).Select(f => f / 100).ToArray();

            //mesh.vertices = vertices;

            //Triangles
            uint triangleLenght = r.ReadUInt32();
            if (debug)
                Debug.Log("triangleLenght: " + triangleLenght);
            int[] triangles = ReadTrisInverted(r, triangleLenght).Select(u => Convert.ToInt32(u)).ToArray();

            //mesh.SetIndices(triangles,MeshTopology.Triangles,0);
            if (debug)
                Debug.Log("The triangles before optimization are:\n" + string.Join(", ", triangles));

            if (debug)
                printCoordsSets(vertices, "vertices");
            if (debug)
                Debug.Log("The triangles are:\n" + string.Join(", ", triangles));

            //mesh.SetIndices(mesh.triangles, MeshTopology.Triangles, 0);

            //Vertex normals 
            r.ReadBytes(8);
            uint normalsLenght = r.ReadUInt32();
            //Debug.Log("normalLenght: " + normalsLenght);
            Vector3[] normals = ReadFloatCoords(r, normalsLenght);
            if (debug)
                printCoordsSets(normals, "normals");
            //if (debug) printCoordsSets(mesh.normals, "unity normals");
            //mesh.normals = normals;

            //Uvs
            uint uvLenght = r.ReadUInt32();
            //Debug.Log("uvLenght: " + uvLenght);
            Vector2[] uvs = ReadFloatCoords2DInverted(r, uvLenght);

            if (debug)
                printCoordsSets(uvs, "uvs");

            Mesh mesh = new Mesh();

            mesh.SetVertices(vertices);
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetIndices(triangles, MeshTopology.Triangles, 0);
            Vector3[] normals2 = new Vector3[vertexLengt];
            for (int i = 0; i < triangleLenght; i++)
            {
                Vector3 normal = normals[i];
                int associatedVertex = triangles[i];

                normals2[associatedVertex] = normal;
            }

            mesh.SetNormals(normals2);

            return mesh;
        }
    }


    public static Mesh ImportWithUvAndSubmesh(string filepath, int submeshCount, datasmithImporter importer)
    {
        bool debug = importer.debugMode;
        //Mesh mesh = new Mesh();
        uint filesize = 0;
        FileStream fileStream = new FileStream(Application.dataPath + filepath, FileMode.Open, FileAccess.Read);

        using (BinaryReader r = new BinaryReader(fileStream, System.Text.Encoding.UTF8))
        {

            r.ReadBytes(8);
            int nameLenght = r.ReadInt32();
            //Debug.Log("name len :" + nameLenght);

            r.ReadBytes(nameLenght - 1);

            r.ReadBytes(104);

            filesize = r.ReadUInt32();
            //Debug.Log("filesize is :" + filesize + " bytes");
            filesize = r.ReadUInt32();
            //Debug.Log("filesize is :" + filesize + " bytes");

            r.ReadBytes(16);

            //triangle len
            uint trianglelen1 = r.ReadUInt32();
            //Debug.Log("triangle len 1: " + trianglelen1);

            //tris material slot
            int[] materials = ReadUintArray(r, trianglelen1).Select(u => Convert.ToInt32(u)).ToArray();

            submeshCount = materials.Max() + 1;//Right now I am not using the input submesh count
            //Debug.Log("Materials max:\n" + materials.Max()+1);
            //Debug.Log("The materials are:\n" + string.Join(", ", materials));

            //tris smoothign group
            int[] smoothingGroups = ReadUintArray(r, trianglelen1).Select(u => Convert.ToInt32(u)).ToArray();
            //Debug.Log("The smoothing groups are:\n" + string.Join(", ", smoothingGroups));

            r.ReadBytes(4);

            //Vertices
            uint vertexLengt = vertexLengt = r.ReadUInt32();
            if (debug)
                Debug.Log("vertex len: " + vertexLengt);
            Vector3[] vertices = ReadFloatCoords(r, vertexLengt).Select(f => f / 100).ToArray();

            //mesh.vertices = vertices;

            //Triangles
            uint triangleLenght = r.ReadUInt32();
            if (debug)
                Debug.Log("triangleLenght: " + triangleLenght);
            int[] triangles = ReadTris(r, triangleLenght).Select(u => Convert.ToInt32(u)).ToArray();

            //mesh.SetIndices(triangles,MeshTopology.Triangles,0);
            //mesh.SetTriangles(triangles, 0);
            if (debug)
                Debug.Log("The triangles before optimization are:\n" + string.Join(", ", triangles));

            List<Vector3> vxList = new List<Vector3>();
            //Debug.Log("Test 4 before pos 2 " + triangles..FindIndex(x=>x==4));


            for (int i = 0; i < triangleLenght / 3; i++)
            {
                vxList.Add(vertices[triangles[i * 3 + 2]]);
                vxList.Add(vertices[triangles[i * 3 + 1]]);
                vxList.Add(vertices[triangles[i * 3]]);
            }

            if (debug)
                printCoordsSets(vxList.ToArray(), "vertices");
            if (debug)
                Debug.Log("The triangles are:\n" + string.Join(", ", triangles));

            //mesh.SetIndices(mesh.triangles, MeshTopology.Triangles, 0);
            int[] tris = Enumerable.Range(0, triangles.Length).ToArray();

            List<int[]> trisInSubMeshes = new List<int[]>();

            trisInSubMeshes = tris
            .GroupBy(i => materials[i / 3])//groups of key:material, indexes
            .Select(kp => kp.ToArray())
            .ToList();


            //Vertex normals 
            r.ReadBytes(8);
            uint normalsLenght = r.ReadUInt32();
            //Debug.Log("normalLenght: " + normalsLenght);
            Vector3[] normals = ReadFloatCoords(r, normalsLenght);
            if (debug)
                printCoordsSets(normals, "normals");
            //if (debug) printCoordsSets(mesh.normals, "unity normals");
            //mesh.normals = normals;

            //Uvs
            uint uvLenght = r.ReadUInt32();
            //Debug.Log("uvLenght: " + uvLenght);
            Vector2[] uvs = ReadFloatCoords2DInverted(r, uvLenght);

            if (debug)
                printCoordsSets(uvs, "uvs");
            Mesh mesh = new Mesh();


            bool hasALotOfVertex = vxList.Count > 65535;//16 bits=65535 

            /*if (vxList.Count<200|| hasALotOfVertex)
                return mesh;*/
            mesh.vertices = vxList.ToArray();
            for (int i = 0; i < submeshCount; i++)
            {
                if (hasALotOfVertex)
                    mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.subMeshCount = submeshCount;
                mesh.SetTriangles(trisInSubMeshes[i], i);
            }

            mesh.uv = uvs;
            mesh.normals = normals;

            return mesh;
        }
    }

    public static void printCoordsSets(Vector3[] coords, string name)
    {
        string toPrint = coords.Select(cs => cs.ToString()).Aggregate((acc, s) => acc + "\n" + s);
        Debug.Log("The " + name + " are:\n" + toPrint);
    }

    public static void printCoordsSets(Vector2[] coords, string name)
    {
        string toPrint = coords.Select(cs => cs.ToString()).Aggregate((acc, s) => acc + "\n" + s);
        Debug.Log("The " + name + " are:\n" + toPrint);
    }

}
#endif