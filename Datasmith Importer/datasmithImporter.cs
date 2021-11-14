using UnityEngine;
using System.Xml;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System;

[ScriptedImporter(version: 1, ext: "udatasmith", AllowCaching = true)]
public class datasmithImporter : ScriptedImporter
{
    

    private string filePath;
    private string filename;
    private Dictionary<string,Material> materials = new Dictionary<string, Material>();
    private Dictionary<string, Tuple<Mesh,Material>> meshMatPairs = new Dictionary<string, Tuple<Mesh, Material>>();
    GameObject mainObj;
    AssetImportContext ctx;

    XmlDocument xmlDoc;

    bool debug = false;

    //Called when a new asset is found
    public override void OnImportAsset(AssetImportContext ctx)
    {
        Profiler.BeginSample("Test importer large");
        this.ctx = ctx;
        Debug.Log("Started reading");

        int pos = ctx.assetPath.LastIndexOf('.');
        filePath = ctx.assetPath.Substring(0, pos);

        pos = filePath.LastIndexOf('/') + 1;
        filename = filePath.Substring(pos, filePath.Length - pos);

        if (!File.Exists(filePath + ".xml"))
        {
            Debug.Log("not exist");
            xmlDoc = createXMLAsset();
        }
        else
        {
            TextAsset textXml = Resources.Load<TextAsset>(filename);
            xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(textXml.text);
        }

        //Create prefab
        mainObj = new GameObject(filename);

        // 'cube' is a GameObject and will be automatically converted into a prefab
        // (Only the 'Main Asset' is eligible to become a Prefab.)
        ctx.AddObjectToAsset("main obj", mainObj);
        ctx.SetMainObject(mainObj);

        createMaterials(xmlDoc);

        //Main obj can also be null since it DatasmithUnreal will become the new mainObj
        if (!debug)
        {
            createMeshes(xmlDoc);
            recursiveTreeBuilding(mainObj, xmlDoc.SelectSingleNode("DatasmithUnrealScene"));
        }
        AssetDatabase.Refresh();

        Profiler.EndSample();
    }


    XmlSerializer serializer = new XmlSerializer(typeof(TransformXML));
    private void recursiveTreeBuilding(GameObject unityParent,XmlNode node)
    {
        switch (node.Name)
        {
            case "DatasmithUnrealScene":
                foreach (XmlNode child in node.ChildNodes)
                    recursiveTreeBuilding(mainObj,child);
                break;
            case "Actor":
            case "ActorMesh":            
                {
                    //Debug.Log("Code for actorMesh and actor");
                    string nodelabel = node.Attributes.GetNamedItem("label").InnerText;
                    string smithId = node.Attributes.GetNamedItem("name").InnerText;

                    GameObject act = new GameObject(nodelabel+", ["+smithId+"]");//For now is smithId but it shoud become revitId
                    //act.name = nodelabel;


                    bool isActorMesh = (node.Name == "ActorMesh");
                    XmlNode transformNode = node.ChildNodes[isActorMesh ? 1 : 0];//Temporary, should be taken by name
                    TransformXML t = (TransformXML)serializer.Deserialize(new XmlNodeReader(transformNode));
                    act.transform.position = t.getPos();
                    act.transform.localScale = t.getScale();
                    act.transform.rotation = t.getRot();
                    act.transform.parent = unityParent.transform;



                    if (isActorMesh)
                    {
                        act.AddComponent<MeshFilter>();
                        act.AddComponent<MeshRenderer>();
                        //act.AddComponent<Renderer>();

                        MetadataManager metaManager = act.AddComponent<MetadataManager>();
                        metaManager.revitId = "test";
                        metaManager.smithId = smithId;
                        metaManager.xmlName = filename;

                        MeshCollider meshCollider = act.AddComponent<MeshCollider>();
                        string meshName = node.FirstChild.Attributes[0].InnerText;


                        //Debug.Log("data: " + meshNode.Attributes[0].InnerText);

                        //Debug.Log("materialName: " + materialName);
                        //Material mat = Resources.Load<Material>(filename+"/"+materialName);
                        
                        act.GetComponent<MeshRenderer>().material = meshMatPairs[meshName].Item2;
                        //ctx.AddObjectToAsset(materialName, mat);

                        //MeshFilter sc = act.AddComponent(typeof(MeshFilter)) as MeshFilter;

                        Mesh mesh = meshMatPairs[meshName].Item1;

                        meshCollider.sharedMesh = mesh;


                        act.GetComponent<MeshFilter>().mesh = mesh;
                    }
                 
                    //Add metadata
                    ctx.AddObjectToAsset(smithId, act);

                    foreach (XmlNode child in node.LastChild.ChildNodes)
                        recursiveTreeBuilding(act, child);
                }
                 break;
            
            
            //Nodes already handled elsewhere
            case "MetaData":
            case "Texture":
            case "MasterMaterial":
            case "StaticMesh":

            case "Camera":
            //Ignored nodes
            case "Version":
            case "SDKVersion":
            case "Host":
            case "Application":
            case "ResourcePath":
            case "User":
            case "Export":

                return;
            default:
                Debug.Log("Node not yet handled: "+node.Name);
                break;
        }

    }

    //Creates materials with or without textures and saves their name
    private void createMeshes(XmlDocument xmlDoc)
    {
      
        XmlSerializer serializer = new XmlSerializer(typeof(MaterialImporter));

        XmlNodeList nodes = xmlDoc.SelectNodes("/DatasmithUnrealScene/StaticMesh");
        foreach (XmlNode meshNode in nodes)
        {
            string meshName = meshNode.Attributes[0].InnerText;
            Debug.Log("Reading mesh: "+meshName);
            Mesh mesh = UmeshImporter.ImportFromString("Resources\\" + meshNode.FirstChild.Attributes[0].InnerText);
            //XmlNode meshNode = xmlDoc.SelectSingleNode("/DatasmithUnrealScene/StaticMesh[@name='" + meshName + "']");
            string materialName = meshNode.LastChild.Attributes.GetNamedItem("name").InnerText;

            ctx.AddObjectToAsset(meshName, mesh);
            meshMatPairs.Add(meshName, new Tuple<Mesh,Material>(mesh,materials[materialName]));
        }


    }

    //Creates materials with or without textures and saves their name
    private void createMaterials(XmlDocument xmlDoc)
    {

        XmlNodeList nodes = xmlDoc.SelectNodes("/DatasmithUnrealScene/MasterMaterial");
        foreach (XmlNode node in nodes)
        {
            createMaterial(xmlDoc, node,false);
        }
        XmlNodeList pbrNodes = xmlDoc.SelectNodes("/DatasmithUnrealScene/UEPbrMaterial");
        foreach (XmlNode node in pbrNodes)
        {
            createMaterial(xmlDoc, node,true);
        }
        

    }

    bool saveMatsOnDisk = false;
    private void createMaterial(XmlDocument xmlDoc, XmlNode node,bool isPbr)
    {
        Material material = MaterialImporter.getMaterialFromNode(node, xmlDoc.SelectSingleNode("/DatasmithUnrealScene"),isPbr);
        if (saveMatsOnDisk)
        {
            AssetDatabase.CreateAsset(material, "Assets/Resources/" + material.name + ".mat");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        else
        {
            ctx.AddObjectToAsset(material.name, material);
            materials.Add(material.name, material);
        }
        //Debug.Log("Added material named: " + material.name);
    }

    private XmlDocument createXMLAsset()
    {

        string text = File.ReadAllText(filePath + ".udatasmith");
        
        text = text.Replace("&", "&amp;");//Not the best method https://stackoverflow.com/questions/1473826/parsing-xml-with-ampersand
        TextAsset textAsset = new TextAsset(text);
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(text);


        xmlDoc.Save(filePath + ".xml");

        //AssetDatabase.CreateAsset(textAsset, filePath+".xml");
        //AssetDatabase.SaveAssets();
        //AssetDatabase.Refresh();
        return xmlDoc;
    }


}
[XmlRoot(ElementName = "Transform")]
public class TransformXML
{
    [XmlAttribute(AttributeName = "tx")]
    public float tx;
    [XmlAttribute(AttributeName = "ty")]
    public float ty;
    [XmlAttribute(AttributeName = "tz")]
    public float tz;

    [XmlAttribute(AttributeName = "sx")]
    public float sx;
    [XmlAttribute(AttributeName = "sy")]
    public float sy;
    [XmlAttribute(AttributeName = "sz")]
    public float sz;

    [XmlAttribute(AttributeName = "qx")]
    public float qx;
    [XmlAttribute(AttributeName = "qy")]
    public float qy;
    [XmlAttribute(AttributeName = "qz")]
    public float qz;
    [XmlAttribute(AttributeName = "qw")]
    public float qw;
    public Vector3 getPos()
    {
        return new Vector3(tx/100,ty/100,tz/100);
    }
    public Vector3 getScale()
    {
        return new Vector3(sx, sy, sz);
    }
    public Quaternion getRot()
    {
        return new Quaternion(qx,qy,qz,qw);
    }

}