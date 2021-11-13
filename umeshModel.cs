using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class UmeshModel
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

        string hex="";
        int zeros =0;
        

        int readBytes=0;
        using (BinaryReader r = new BinaryReader(fileStream, System.Text.Encoding.UTF8)){
            while (readBytes < 200) {
                uint c = r.ReadUInt32();
                if (c==0)
                {
                    zeros++;
                }
                else
                {
                    if (zeros == 6)
                    {
                        break;
                        //Debug.Log("found non zero after " + zeros + " zeros at line: " + readBytes / 16);
                    }
                    zeros = 0;
                }
                readBytes+=4;
            }

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
            //Vertices
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

            for (int i =0; i < triangleLenght; i++)
            {
                int vertexNum = triangles[i];
                //Debug.Log("Testing vertex: "+vertexNum);
                int prevIndex = Array.FindIndex(triangles,v=>v==vertexNum);
                //Debug.Log("Pre index: " + prevIndex);
                if (prevIndex<i&&prevIndex>-1)//Array contains triangles[i] after i
                {
                    //Debug.Log("Added vertex");
                    triangles[i] = vxList.Count();
                    vxList.Add(vxList[vertexNum]);
                    //copy vertex
                    //escalate triangles[i] to new vertex
                }
            }
            //printCoordsSets(vxList.ToArray(), "vertices");
            //Debug.Log("The triangles are:\n" + string.Join(", ", triangles));
            mesh.vertices = vxList.ToArray();
            mesh.triangles = triangles;

            //mesh.Optimize();
            mesh.RecalculateNormals();

            //Remember to recalculate and invert them

            //Vertex normals 
            r.ReadBytes(8);
            uint normalsLenght = r.ReadUInt32();
            //Debug.Log("normalLenght: " + normalsLenght);
            Vector3[] normals = ReadFloatCoords(r, normalsLenght);
            //printCoordsSets(normals, "normals");
            //mesh.normals = normals;

            //Uvs
            uint uvLenght = r.ReadUInt32();
            //Debug.Log("uvLenght: " + uvLenght);
            Vector2[] uvs = ReadFloatCoords2D(r, uvLenght);
            //printCoordsSets(uvs, "uvs");
            //mesh.uv = uvs;

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

