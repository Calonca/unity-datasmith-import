using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Profiling;
using UnityEngine.UI;

/// <summary>
/// Handles elements properties
/// </summary>
public class MetadataManager : MonoBehaviour
{

    public string revitId;
    public string smithId;
    public string xmlNode;
    public GameObject gm;
    /*
    public void OnMouseDown()
    {
        Debug.Log(getDataFromXml());
    }
    */
    /// <summary>
    /// Return a formatted string containg the properties contained in the xml element
    /// </summary>
    /// <returns></returns>
    public string getDataFromXml()
    {

        if (xmlNode == null)
            return "no metadata";

        IEnumerable<XElement> properties = XElement.Parse(xmlNode)
            .Elements("KeyValueProperty");

        string output = "";//"id: " + revitId + "\n";
        bool toJoin = false;
        foreach (XElement keyTypeValueNode in properties)
        {
            string key = keyTypeValueNode.Attribute("name").Value;
            //                                                                         //
            string type = keyTypeValueNode.Attribute("type").Value;  //TODO casting
            //                                                                         //
            string value = keyTypeValueNode.Attribute("val").Value;
            output += key + ": " + value + (toJoin ? ", ||" : "\n");
            toJoin = !toJoin;
        }
        Debug.Log(output);
        return output;
    }

}