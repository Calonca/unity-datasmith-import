using UnityEngine;
using System.Xml;
using System.IO;
using UnityEditor.AssetImporters;
using UnityEditor;
using System.Xml.Serialization;

[ScriptedImporter(version: 1, ext: "udatasmith", AllowCaching = true)]
public class datasmithImporter : ScriptedImporter
{
    private string filePath;
    private string filename;
    GameObject mainObj;
    AssetImportContext ctx;

    XmlDocument xmlDoc;

    bool debug = false;

    //Called when a new asset is found
    public override void OnImportAsset(AssetImportContext ctx)
    {
        this.ctx = ctx;
        Debug.Log("Started reading");
        UmeshModel model = new UmeshModel();
        Mesh mesh = new Mesh();
        if (debug) { 
            mesh = model.ImportFromString("elongedX.udsmesh");
        }

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

        if (debug)
        {
            var redMat = new Material(Shader.Find("Standard"));
            redMat.color = Color.red;
            // Assets must be assigned a unique identifier string consistent across imports
            ctx.AddObjectToAsset("my Material", redMat);


            GameObject redcube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            redcube.name = "redcub";
            redcube.transform.position = new Vector3(1, 1, 1);
            redcube.transform.parent = mainObj.transform;


            GameObject meshObject = new GameObject("meshObject");
            meshObject.name = "Imported";
            meshObject.transform.parent = mainObj.transform;
            ctx.AddObjectToAsset("elongedXmesh", mesh);
            meshObject.AddComponent<MeshFilter>();
            meshObject.AddComponent<MeshRenderer>();
            meshObject.AddComponent<Renderer>();
            meshObject.GetComponent<MeshFilter>().mesh = mesh;
            meshObject.GetComponent<Renderer>().material = redMat;
            ctx.AddObjectToAsset("meshObject", meshObject);

            GameObject recCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            recCube.name = "recursive";
            recCube.transform.position = new Vector3(1, 1, 2);
            recCube.transform.parent = redcube.transform;
            ctx.AddObjectToAsset("recursive", recCube);



            redcube.GetComponent<Renderer>().material = redMat;
            ctx.AddObjectToAsset("cubeREd", redcube);
        }
        createMaterials(xmlDoc);
        
        //Main obj can also be null since it DatasmithUnreal will become the new mainObj
        if (!debug)
            recursiveTreeBuilding(mainObj,xmlDoc.SelectSingleNode("DatasmithUnrealScene"));

        AssetDatabase.Refresh();
    }

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
                    Debug.Log("Code for actorMesh and actor");
                    string nodelabel = node.Attributes.GetNamedItem("label").InnerText;
                    GameObject act = new GameObject(nodelabel);
                    act.name = nodelabel;
                    XmlSerializer serializer = new XmlSerializer(typeof(TransformXML));


                    bool isActorMesh = (node.Name == "ActorMesh");
                    XmlNode transformNode = node.ChildNodes[isActorMesh ? 1 : 0];//Temporary, should be taken by name
                    TransformXML t = (TransformXML)serializer.Deserialize(new XmlNodeReader(transformNode));
                    act.transform.position = t.getPos();
                    act.transform.localScale = t.getScale();
                    act.transform.rotation = t.getRot();
                    act.transform.parent = unityParent.transform;


                    string smithId = node.Attributes.GetNamedItem("name").InnerText;

                    if (isActorMesh)
                    {
                        act.AddComponent<MeshFilter>();
                        act.AddComponent<MeshRenderer>();
                        act.AddComponent<Renderer>();

                        MetadataManager metaManager = act.AddComponent<MetadataManager>();
                        metaManager.revitId = "test";
                        metaManager.smithId = smithId;
                        metaManager.xmlName = filename;

                        MeshCollider meshCollider = act.AddComponent<MeshCollider>();

                        string meshName = node.FirstChild.Attributes[0].InnerText;

                        Debug.Log("meshName: " + meshName);
                        //meshName = "ff936cc591c8fdcb55a5dab1e4cb8e85";
                        XmlNode meshNode = xmlDoc.SelectSingleNode("/DatasmithUnrealScene/StaticMesh[@name='" + meshName + "']");

                        //Debug.Log("data: " + meshNode.Attributes[0].InnerText);
                        string materialName = meshNode.LastChild.Attributes.GetNamedItem("name").InnerText;
                        Debug.Log("materialName: " + materialName);
                        Material mat = Resources.Load<Material>(materialName);
                        act.GetComponent<MeshRenderer>().material = mat;
                        //ctx.AddObjectToAsset(materialName, mat);

                        //MeshFilter sc = act.AddComponent(typeof(MeshFilter)) as MeshFilter;
                        UmeshModel umeshImporter = new UmeshModel();
                        Mesh mesh = umeshImporter.ImportFromString("Resources\\" + meshNode.FirstChild.Attributes[0].InnerText);

                        meshCollider.sharedMesh = mesh;

                        ctx.AddObjectToAsset(meshName, mesh);
                        act.GetComponent<MeshFilter>().mesh = mesh;
                    }
                 
                    //Add metadata
                    ctx.AddObjectToAsset(smithId, act);

                    foreach (XmlNode child in node.LastChild.ChildNodes)
                        recursiveTreeBuilding(act, child);
                }
                 break;
            case "MetaData":
                return;
            case "Camera":
                return;
            case "Texture":
                return;
            case "MasterMaterial":
                return;
            case "StaticMesh":
                return;
            default:
                Debug.Log("Node not yet handled: "+node.Name);
                break;
        }

    }

    //Creates materials with or without textures and saves their name
    private void createMaterials(XmlDocument xmlDoc)
    {

        bool saveMatsOnDisk=false;
        XmlSerializer serializer = new XmlSerializer(typeof(MaterialXML));

        XmlNodeList nodes = xmlDoc.SelectNodes("/DatasmithUnrealScene/MasterMaterial");
        foreach (XmlNode node in nodes)
        {
            XmlNodeReader xmlReader = new XmlNodeReader(node);
            MaterialXML material = (MaterialXML)serializer.Deserialize(xmlReader);
            material.setProperties(node, xmlDoc.SelectSingleNode("/DatasmithUnrealScene"));
            if (saveMatsOnDisk)
            {
                AssetDatabase.CreateAsset(material.mat, "Assets/Resources/" + material.name + ".mat");
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                ctx.AddObjectToAsset(material.name, material.mat);
            }
        }


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