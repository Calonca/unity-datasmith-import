using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;


[XmlRoot(ElementName = "MasterMaterial")]
public class MaterialXML
{
    [XmlAttribute(AttributeName = "name")]
    public string name;
    [XmlAttribute(AttributeName = "type")]
    public int type;//1 is default, 2 is glass
    [XmlIgnore]
    public Material mat;

    private Color DiffuseColor = new Color(0f,0f,0f,1f);
    private float DiffuseMapFading;
    private string DiffuseMap                              ;
    private float DiffuseMap_UVOffsetX                     ;
    private float DiffuseMap_UVOffsetY                     ;
    private float DiffuseMap_UVScaleX                      ;
    private float DiffuseMap_UVScaleY                      ;
    private float DiffuseMap_UVWAngle                      ;
    private bool TintEnabled                               ;
    private Color TintColor                                ;
    private float SelfIlluminationLuminance                ;
    private float SelfIlluminationColorTemperature         ;
    private Color SelfIlluminationFilter                   ;
    private bool SelfIlluminationMapEnable                 ;
    private float BumpAmount                               ;
    private string BumpMap                                 ;
    private float BumpMap_UVOffsetX                        ;
    private float BumpMap_UVOffsetY                        ;
    private float BumpMap_UVScaleX                         ;
    private float BumpMap_UVScaleY                         ;
    private float BumpMap_UVWAngle                         ;
    private bool IsMetal                                   ;
    private float Glossiness                               ;

    private float parsetoFloat(MatchCollection mc,int idx)
    {
        //Parses to float using the point as the decimal separator
        return float.Parse(mc[idx].Value, CultureInfo.InvariantCulture);
    }

    public void setProperties(XmlNode matNode, XmlNode docRoot)
    {
        mat = new Material(Shader.Find("Standard"));
        mat.name = name;

        XmlNodeList testList = matNode.SelectNodes("KeyValueProperty");
        foreach (XmlNode keyTypeValueNode in testList)
        {
            string name = keyTypeValueNode.Attributes.GetNamedItem("name").InnerText;
            string value = keyTypeValueNode.Attributes.GetNamedItem("val").InnerText;
            switch (name) {
                case "DiffuseColor":
                    {
                        Regex regex = new Regex(@"[+-]?([0-9]*[.])?[0-9]+");
                        MatchCollection rgba = regex.Matches(value);
                        mat.color = new Color(
                            parsetoFloat(rgba, 0),
                            parsetoFloat(rgba, 1),
                            parsetoFloat(rgba, 2),
                            parsetoFloat(rgba, 3));

                        break;
                    }
                case "DiffuseMap":
                    {
                        XmlNode textureNode = docRoot.SelectSingleNode("//Texture[@name='" + value+"']");
                        string filename = textureNode.Attributes.GetNamedItem("file").InnerText;
                        Debug.Log(filename);
                        int pos = filename.LastIndexOf('.');
                        Texture2D tex = Resources.Load(filename.Substring(0, pos)) as Texture2D;
                        //Texture2D tex = Resources.Load("a") as Texture2D;
                        mat.mainTexture  =tex;
                        //mat.name = filename;

                        break;
                    }

                default:
                    //Debug.Log("Property " + name + " not handled");
                    break;
            }
        }
    }

}
