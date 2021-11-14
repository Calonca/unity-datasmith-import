using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TMPro;
using UnityEngine;
using UnityEngine.Profiling;

public class MetadataManager : MonoBehaviour
{
    public TMP_Text dys;
    public string revitId;
    public string smithId;
    public string xmlName;

    /*public MetadataManager(string revitId, string smithId, string xmlName)
    {
        this.revitId = revitId;
        this.smithId = smithId;
        this.xmlName = xmlName;
    }*/

    bool entered = false;
    void OnMouseOver()
    {
        if (entered)
            return;
        entered = true;

        //If your mouse hovers over the GameObject with the script attached, output this message
        dys.text = getDataFromXml();
    }

    void OnMouseExit()
    {
        entered = false;
        //The mouse is no longer hovering over the GameObject so output this message each frame
        Debug.Log("Mouse is no longer on GameObject.");
        dys.text = "no obj";
    }

    // Following method reads the xml file and display its content
    public string getDataFromXml()
    {
        Profiler.BeginSample("Test get data from xml");
        Debug.Log(xmlName);
        TextAsset textXml = Resources.Load<TextAsset>(xmlName);
        XmlDocument xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(textXml.text);

        XmlNode dataNode = xmlDoc.SelectNodes("/DatasmithUnrealScene/MetaData[@reference='Actor." + smithId + "']")[0];

        string output = "id: " + revitId + "\n";
        XmlNodeList testList = dataNode.SelectNodes("KeyValueProperty");
        foreach (XmlNode keyTypeValueNode in testList)
        {
            string key = keyTypeValueNode.Attributes.GetNamedItem("name").InnerText;
            //                                                                         //
            string type = keyTypeValueNode.Attributes.GetNamedItem("type").InnerText;  //TODO casting
            //                                                                         //
            string value = keyTypeValueNode.Attributes.GetNamedItem("val").InnerText;
            output += key + ": " + value + "\n";

        }

        Profiler.EndSample();
        return output;
    }

    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log("tst");
        //Create a TMP_Text named MetadataDisplay if you receive any errors
        if (dys == null)
            dys = GameObject.Find("MetadataDisplay").GetComponent<TMP_Text>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
