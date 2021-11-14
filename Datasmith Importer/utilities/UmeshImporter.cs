using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UmeshImporter
{
    //The coords are floats in centimeters
    private static Vector3[] ReadFloatCoords(BinaryReader br, uint nSets)
    {
        Vector3[] vects = new Vector3[nSets];
        for (int i = 0; i < nSets; i++)
        {
            float x, y, z;
            x = br.ReadSingle();
            y = br.ReadSingle();
            z = br.ReadSingle();
            vects[i] = new Vector3(x, y, z);
        }
        return vects;
    }

    //The coords are floats in centimeters
    private static Vector3[] ReadFloatCoordsInverted(BinaryReader br, uint nSets)
    {
        Vector3[] vects = new Vector3[nSets];
        for (int i = 0; i < nSets/3; i++)
        {
            Vector3 v1 = ReadVector3(br);
            Vector3 v2 = ReadVector3(br);
            Vector3 v3 = ReadVector3(br);

            vects[(i * 3)] = v3;
            vects[(i * 3)+1] = v2;
            vects[(i * 3) + 2] = v1;
        }
        return vects;
    }

    private static Vector3 ReadVector3(BinaryReader br)
    {
        float x, y, z;
        x = br.ReadSingle();
        y = br.ReadSingle();
        z = br.ReadSingle();
        return new Vector3(x, y, z);
    }


    //Reads tris inverted
    private static uint[] ReadTrisInverted(BinaryReader br, uint n)
    {
        uint x, y, z;
        List<uint> lint = new List<uint>();
        for (int i = 0; i < n/3; i++)
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
    

    //The coords are floats in centimeters
    private static Vector2[] ReadFloatCoords2DInverted(BinaryReader br, uint nSets)
    {
        Vector2[] vects = new Vector2[nSets];
        for (int i = 0; i < nSets/3; i++)
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

    //The coords are floats in centimeters
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

    //The triangles are uint
    private static uint[] ReadUintArray(BinaryReader br, uint n)
    {
        uint[] arr = new uint[n];
        for (int i = 0; i < n; i++)
        {
            arr[i] = br.ReadUInt32();
        }
        return arr;
    }
    public static string HexStr(byte[] p)
    {

        char[] c = new char[p.Length * 2 + 2];

        byte b;

        c[0] = '0'; c[1] = 'x';

        for (int y = 0, x = 2; y < p.Length; ++y, ++x)
        {

            b = ((byte)(p[y] >> 4));

            c[x] = (char)(b > 9 ? b + 0x37 : b + 0x30);

            b = ((byte)(p[y] & 0xF));

            c[++x] = (char)(b > 9 ? b + 0x37 : b + 0x30);

        }

        return new string(c);

    }

    private string readHexString(BinaryReader r, int l)
    {
        byte[] d = r.ReadBytes(l);
        return HexStr(d);
    }

    public static Mesh ImportFromString(string filename){
        Mesh mesh = new Mesh();
        mesh.Clear();

        uint filesize = 0;
        FileStream fileStream = new FileStream("Assets\\"+filename, FileMode.Open, FileAccess.Read);

        using (BinaryReader r = new BinaryReader(fileStream, System.Text.Encoding.UTF8)){

            r.ReadBytes(8);
            int nameLenght = r.ReadInt32();
            //Debug.Log("name len :" + nameLenght);

            r.ReadBytes(nameLenght-1);

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
            //Debug.Log("The materials are:\n" + string.Join(", ", materials));

            //tris smoothign group
            int[] smoothingGroups = ReadUintArray(r, trianglelen1).Select(u => Convert.ToInt32(u)).ToArray();

            //Debug.Log("The smoothing groups are:\n" + string.Join(", ", smoothingGroups));

            r.ReadBytes(4);

            //Vertices
            uint vertexLengt = vertexLengt = r.ReadUInt32();
            //Debug.Log("vertex len: " + vertexLengt  );
            Vector3[] vertices = ReadFloatCoords(r, vertexLengt).Select(f=>f/100).ToArray();
            
            //mesh.vertices = vertices;

            
            //Triangles
            uint triangleLenght = r.ReadUInt32();
            //Debug.Log("triangleLenght: " + triangleLenght);
            int[] triangles = ReadTrisInverted(r,triangleLenght).Select(u => Convert.ToInt32(u)).ToArray();
            /*triangles = Enumerable.Range(0,triangles.Length)
                        .Select(i=>new Tuple<int,int>(i,triangles[i]))
                        .GroupBy(i=>i.Item1/3)
                        .Select(kp => listConv(kp.ToList()).ToList())
                        .ToList();*/

            //mesh.triangles = triangles;

            List<Vector3> vxList = new List<Vector3>(vertices);
            //Debug.Log("Test 4 before pos 2 " + triangles..FindIndex(x=>x==4));

            //I am using triangleLength as an upper bound lenght but it can be smaller
            bool[] alreadyPresent = new bool[triangleLenght];

            for (int i =0; i < triangleLenght; i++)
            {
                int vertexNum = triangles[i];
                if (alreadyPresent[vertexNum])//Array contains triangles[i] after i
                {
                    //Debug.Log("Added vertex");
                    triangles[i] = vxList.Count();
                    vxList.Add(vxList[vertexNum]);
                }
                else
                {
                    alreadyPresent[vertexNum] = true;
                }
            }
            //printCoordsSets(vxList.ToArray(), "vertices");
            //Debug.Log("The triangles are:\n" + string.Join(", ", triangles));
            mesh.vertices = vxList.ToArray();
            mesh.triangles = triangles;

            //mesh.Optimize();
            mesh.RecalculateNormals();

            //Vertex normals 
            r.ReadBytes(8);
            uint normalsLenght = r.ReadUInt32();
            //Debug.Log("normalLenght: " + normalsLenght);
            Vector3[] normals = ReadFloatCoordsInverted(r, normalsLenght);
            //printCoordsSets(normals, "normals");
            //mesh.normals = normals;

            //Uvs
            uint uvLenght = r.ReadUInt32();
            //Debug.Log("uvLenght: " + uvLenght);
            Vector2[] uvs = ReadFloatCoords2DInverted(r, uvLenght);
            //printCoordsSets(uvs, "uvs");
            mesh.uv = uvs;

            /*string meshName = "Testmesh";
            AssetDatabase.CreateAsset(mesh, "Assets/Resources/" + meshName + "");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();*/

        }
        return mesh;

    }

    private static List<int> listConv(List<Tuple<int, int>> t)
    {
        return t.Select(i=>i.Item2).ToList();
    }
    public static void printCoordsSets(Vector3[] coords,string name)
    {
        string toPrint = coords.Select(cs=>cs.ToString()).Aggregate((acc,s)=>acc+"\n"+s);
        Debug.Log("The " + name + " are:\n" + toPrint);
    }

    public static void printCoordsSets(Vector2[] coords, string name)
    {
        string toPrint = coords.Select(cs => cs.ToString()).Aggregate((acc, s) => acc + "\n" + s);
        Debug.Log("The " + name + " are:\n" + toPrint);
    }



}

