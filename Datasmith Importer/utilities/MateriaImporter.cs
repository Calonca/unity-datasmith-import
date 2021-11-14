using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using UnityEngine;

public class MaterialXML
{
    //These properties are not used, here to show what properties are presnt int he file
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

    private static float parsetoFloat(MatchCollection mc,int idx)
    {
        //Parses to float using the point as the decimal separator
        return float.Parse(mc[idx].Value, CultureInfo.InvariantCulture);
    }

    public static Material getMaterialFromNode(XmlNode matNode, XmlNode docRoot, bool isPbr)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = matNode.Attributes.GetNamedItem("name").InnerText;

        if (!isPbr)
        {
            int type = int.Parse(matNode.Attributes.GetNamedItem("Type").InnerText);//1 is default, 2 is glass
            XmlNodeList testList = matNode.SelectNodes("KeyValueProperty");
            foreach (XmlNode keyTypeValueNode in testList)
            {
                string name = keyTypeValueNode.Attributes.GetNamedItem("name").InnerText;
                string value = keyTypeValueNode.Attributes.GetNamedItem("val").InnerText;
                switch (name)
                {
                    case "DiffuseColor":
                        {
                            mat.color = getColorFromString(value);
                            break;
                        }
                    case "DiffuseMap":
                        {
                            mat.mainTexture = getTextureNamed(docRoot, value);
                            break;
                        }

                    default:
                        //Debug.Log("Property " + name + " not handled");
                        break;
                }
            }
        } else
        {
            XmlNode expressions = matNode.FirstChild;
            foreach (XmlNode NameConstantPairs in expressions.ChildNodes)
            {
                string name = NameConstantPairs.Attributes.GetNamedItem("Name").InnerText;
                switch (name)
                {
                    case "Base Color":
                        {
                            string constant = NameConstantPairs.Attributes.GetNamedItem("constant").InnerText;
                            mat.color = getColorFromString(constant);
                            break;
                        }
                    case "Base Texture":
                        {
                            string pathName = NameConstantPairs.Attributes.GetNamedItem("PathName").InnerText;
                            mat.mainTexture = getTextureNamed(docRoot, pathName);
                            break;
                        }

                    default:
                        //Debug.Log("Property " + name + " not handled");
                        break;
                }
            }
        }
        return mat;
    }

    private static Texture2D getTextureNamed(XmlNode docRoot, string pathName)
    {
        XmlNode textureNode = docRoot.SelectSingleNode("//Texture[@name='" + pathName + "']");
        string filename = textureNode.Attributes.GetNamedItem("file").InnerText;
        int pos = filename.LastIndexOf('.');
        Texture2D tex = Resources.Load(filename.Substring(0, pos)) as Texture2D;
        return tex;
    }

    private static Color getColorFromString(string value)
    {
        Regex rgbaRegex = new Regex(@"[+-]?([0-9]*[.])?[0-9]+");
        MatchCollection rgba = rgbaRegex.Matches(value);
        return new Color(
            parsetoFloat(rgba, 0),
            parsetoFloat(rgba, 1),
            parsetoFloat(rgba, 2),
            parsetoFloat(rgba, 3));
    }
}
