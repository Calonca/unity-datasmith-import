using UnityEngine;
using System.Xml;
using System.IO;
#if UNITY_EDITOR
using UnityEditor.AssetImporters;
#endif
using UnityEditor;
using System.Xml.Serialization;
using System.Collections.Generic;
using UnityEngine.Profiling;
using System;
using System.Xml.Linq;
using System.Linq;
using System.Globalization;

/// <summary>
/// Most important class for the importer, uses the other classes to import the datasmith assets
/// </summary>
#if UNITY_EDITOR
[ScriptedImporter(version: 1, ext: "udatasmith", AllowCaching = true)]
public class datasmithImporter : ScriptedImporter
{
    public CollideLevel ColliderComplexity = CollideLevel.Mesh;
    public ImportMode mode = ImportMode.uvAndSubmeshes;
    public bool debugMode = false;

    public Vector3 modelRotation = new Vector3(90f, 0f, 0f);
    private string filePath;
    private string filename;

    private Dictionary<string, Material> materials = new Dictionary<string, Material>();
    private Dictionary<string, Tuple<Mesh, Material[]>> meshAndMaterials = new Dictionary<string, Tuple<Mesh, Material[]>>();
    private Dictionary<string, XElement> metadata = new Dictionary<string, XElement>();

    GameObject mainObj;
    AssetImportContext ctx;

    XElement xmlDoc;

    bool debug = false;

    /// <summary>
    /// This method is called by unity when a new asset .udatasmith is found or when a user want to remiport the asset.
    /// Removes invalid charachter from the .udatasmith files and parses the result as an .xml file
    /// Creates materials, metadata, meshes, and model tree stucture and assings it to the ctx MainObject
    /// </summary>
    /// <param name="ctx">Contains the filePath and a prefab is created from the assigned MainObject</param>
    public override void OnImportAsset(AssetImportContext ctx)
    {
        this.ctx = ctx;
        //Debug.Log("Started reading");

        int pos = ctx.assetPath.LastIndexOf('.');
        filePath = ctx.assetPath.Substring(0, pos);

        pos = filePath.LastIndexOf('/') + 1;
        filename = filePath.Substring(pos, filePath.Length - pos);

        xmlDoc = LoadXMLAsset();
        var currentDirectory = Directory.GetCurrentDirectory();
        var purchaseOrderFilepath = Path.Combine(currentDirectory + "\\Assets\\Resources", filename + ".xml");

        //xmlDoc = XElement.Load(purchaseOrderFilepath);


        //Create prefab
        mainObj = new GameObject(filename);
        mainObj.transform.rotation = Quaternion.Euler(modelRotation.x, modelRotation.y, modelRotation.z);

        // 'cube' is a GameObject and will be automatically converted into a prefab
        // (Only the 'Main Asset' is eligible to become a Prefab.)
        ctx.AddObjectToAsset("main obj", mainObj);
        ctx.SetMainObject(mainObj);

        createMaterials(xmlDoc);
        metadata = getMetadata(xmlDoc);

        //Main obj can also be null since it DatasmithUnreal will become the new mainObj
        if (!debug)
        {
            createMeshes(xmlDoc);
            recursiveTreeBuilding(mainObj, xmlDoc);
        }

        Afterimport.afterImport(mainObj);
        AssetDatabase.Refresh();
    }

    /// <summary>
    /// Gets the metadata from the document
    /// </summary>
    /// <returns>A Dictionary of (datasmithId,xml element containign metadata)</returns>
    private Dictionary<string, XElement> getMetadata(XElement xmlDoc)
    {
        return xmlDoc.Elements("MetaData")
            .ToLookup(n => n.Attribute("reference").Value.Substring(6), n => n)
            .ToDictionary(n => n.Key, n => n.First());
        //Debug.Log(String.Join(", ",metadata.Values.Select(n=>n.Attribute("reference").Value).ToList()));
    }

    public static float parsetoFloat(string a)
    {
        //Parses to float using the point as the decimal separator
        return float.Parse(a, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Contains position, scale, rotation
    /// </summary>
    float[] psr = new float[10];

    /// <summary>
    /// For each child in the xml element creates a corresponding Unity Object recursively
    /// </summary>
    /// <param name="unityParent">The parent unity game object that will contain the new branches</param>
    /// <param name="node">A branch that will be added to the unity parent</param>
    private void recursiveTreeBuilding(GameObject unityParent, XElement node)
    {

        switch (node.Name.LocalName)
        {
            case "DatasmithUnrealScene":
                foreach (XElement child in node.Elements())
                    recursiveTreeBuilding(mainObj, child);
                break;
            case "Actor":
            case "ActorMesh":
                {
                    //Debug.Log("Code for "+node.Name.LocalName);
                    XAttribute nodelabel = node.Attribute("label");

                    string smithId = node.FirstAttribute.Value;//name

                    GameObject act = new GameObject((nodelabel != null ? nodelabel.Value : "no label") + ", [" + smithId + "]");//For now is smithId but it shoud become revitId
                    //act.name = nodelabel;



                    bool isActorMesh = (node.Name == "ActorMesh");
                    XElement transformNode = node.Element("Transform");//Temporary, should be taken by name

                    psr = transformNode.Attributes().Take(10).Select(a => parsetoFloat(a.Value)).ToArray();

                    act.transform.position = new Vector3(psr[0] / 100, psr[1] / 100, psr[2] / 100);
                    act.transform.localScale = new Vector3(psr[3], psr[4], psr[5]);
                    act.transform.rotation = new Quaternion(psr[6], psr[7], psr[8], psr[9]);
                    act.transform.parent = unityParent.transform;

                    if (isActorMesh)
                    {

                        //act.AddComponent<Renderer>();

                        MetadataManager metaManager = act.AddComponent<MetadataManager>();
                        metaManager.revitId = "test";
                        metaManager.smithId = smithId;
                        //metaManager.xmlNode = xmlDoc.Elements("MetaData")
                        //.Single(o => o.Attribute("reference").Value == "Actor." + smithId);
                        metaManager.gm = mainObj;

                        XElement metaNode = null;
                        metadata.TryGetValue(node.Attribute("name").Value, out metaNode);
                        if (metaNode != null)
                            metaManager.xmlNode = metaNode.ToString();


                        //Debug.Log("ref is :" + metaManager.xmlNode.Attribute("reference").Value);



                        string meshName = node.Element("mesh").Attribute("name").Value;


                        //Debug.Log("data: " + meshNode.Attributes[0].InnerText);
                        //ctx.AddObjectToAsset(materialName, mat);

                        //MeshFilter sc = act.AddComponent(typeof(MeshFilter)) as MeshFilter;

                        Mesh mesh = meshAndMaterials[meshName].Item1;



                        if (ColliderComplexity == CollideLevel.Mesh)
                        {
                            MeshCollider meshCollider = act.AddComponent<MeshCollider>();
                            meshCollider.sharedMesh = mesh;
                        }
                        else if (ColliderComplexity == CollideLevel.Box_Future_Release)
                            act.AddComponent<BoxCollider>();


                        act.AddComponent<MeshFilter>();
                        act.AddComponent<MeshRenderer>();

                        MeshRenderer mr = act.GetComponent<MeshRenderer>();
                        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                        mr.receiveShadows = false;

                        if (mode == ImportMode.uvAndSubmeshes)
                            mr.materials = meshAndMaterials[meshName].Item2;
                        else mr.material = meshAndMaterials[meshName].Item2[0];

                        act.GetComponent<MeshFilter>().mesh = mesh;
                    }

                    //Add metadata
                    ctx.AddObjectToAsset(smithId, act);

                    foreach (XElement child in node.Elements("children").Elements())
                        recursiveTreeBuilding(act, child);
                }
                break;


            //Nodes already handled elsewhere
            case "MetaData":
            case "Texture":
            case "MasterMaterial":
            case "UEPbrMaterial":
            case "StaticMesh":


            //Ignored nodes
            case "Camera":
            case "Version":
            case "SDKVersion":
            case "Host":
            case "Application":
            case "ResourcePath":
            case "User":
            case "Export":
                return;

            default:
                Debug.Log("Node not yet handled: " + node.Name);
                break;
        }

    }

    /// <summary>
    /// Removes original meshes from gm and children leaving only the colliders
    /// </summary>
    public static void removeOriginalMeshes(Transform gm)
    {
        MeshFilter[] meshFilters = gm.GetComponentsInChildren<MeshFilter>();
        MeshRenderer[] meshRenderer = gm.GetComponentsInChildren<MeshRenderer>();

        foreach (MeshFilter c in meshFilters)
        {
            DestroyImmediate(c);
        }
        foreach (MeshRenderer c in meshRenderer)
        {
            DestroyImmediate(c);
        }
    }

    /// <summary>
    /// Creates meshes and adds them to the meshAndMaterials dictionary
    /// </summary>
    private void createMeshes(XElement xmlDoc)
    {
        IEnumerable<XElement> nodes = xmlDoc.Elements("StaticMesh");
        string[] paths = nodes.Select(n => n.Element("file").Attribute("path").Value)
            .Distinct().ToArray();

        int i = 0;

        Dictionary<string, Mesh> meshes = importMeshes(paths);
        foreach (XElement meshNode in nodes)
        {
            string meshName = meshNode.Attribute("name").Value;
            Mesh m = meshes[meshNode.Element("file").Attribute("path").Value];
            ctx.AddObjectToAsset(meshName, m);
            meshAndMaterials.Add(meshName, new Tuple<Mesh, Material[]>(m, meshNode.Elements("Material").Select(e => materials[e.Attribute("name").Value]).ToArray()));
            i++;
        }

    }

    /// <summary>
    /// Imports and returns the meshes present in the input dictionary
    /// </summary>
    private Dictionary<string, Mesh> importMeshes(string[] paths)
    {
        return paths.Select(n =>
            new KeyValuePair<string, Mesh>(
                n,
                UmeshImporter.ImportFromFilepath("Resources\\" + n, 1, this))
                ).ToDictionary(x => x.Key, x => x.Value);


    }


    /// <summary>
    /// Creates materials with or without textures and saves their name
    /// </summary>
    /// <param name="xmlDoc"></param>
    private void createMaterials(XElement xmlDoc)
    {
        IEnumerable<XElement> nodes = xmlDoc.Elements("MasterMaterial");
        foreach (XElement node in nodes)
        {
            createMaterial(xmlDoc, node, false);
        }
        IEnumerable<XElement> pbrNodes = xmlDoc.Elements("UEPbrMaterial");
        foreach (XElement node in pbrNodes)
        {
            createMaterial(xmlDoc, node, true);
        }


    }

    /// <summary>
    /// Imports and adds a materials to the materials dictionary
    /// </summary>
    private void createMaterial(XElement xmlDoc, XElement materialNode, bool isPbr)
    {
        Material material = MaterialImporter.getMaterialFromNode(materialNode, xmlDoc, isPbr);
        ctx.AddObjectToAsset(material.name, material);
        materials.Add(material.name, material);
    }

    private XElement LoadXMLAsset()
    {

        string text = File.ReadAllText(filePath + ".udatasmith");

        text = text.Replace("&", "&amp;");//Not the best method https://stackoverflow.com/questions/1473826/parsing-xml-with-ampersand

        XElement xEl = XElement.Parse(text);
        //xEl.Save(filePath + ".xml");

        return xEl;
    }


}
#endif

/// <summary>
/// The the collider level of the model
/// </summary>
public enum CollideLevel
{
    None_Future_Release, Box_Future_Release, Mesh
}

/// <summary>
/// The import mode,
/// Use uvAndSubmeshes to import textures and CompressedNoUvNoSubmeshes for a more compact model
/// </summary>
public enum ImportMode
{
    uvAndSubmeshes, CompressedWithoutUvAndSubmeshes
}