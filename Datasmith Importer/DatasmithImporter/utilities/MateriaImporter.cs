#if UNITY_EDITOR
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using UnityEngine;

/// <summary>
/// Utiliy class to import materials
/// </summary>
public class MaterialImporter
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
        return datasmithImporter.parsetoFloat(mc[idx].Value);
    }



    public static Material getMaterialFromNode(XElement matNode, XElement docRoot, bool isPbr)
    {
        Material mat = new Material(Shader.Find("Standard"));
        mat.name = matNode.Attribute("name").Value;

        if (!isPbr)
        {
            int type = int.Parse(matNode.Attribute("Type").Value);//1 is default, 2 is glass
            var testList = matNode.Elements("KeyValueProperty");
            foreach (XElement keyTypeValueNode in testList)
            {
                string name = keyTypeValueNode.Attribute("name").Value;
                string value = keyTypeValueNode.Attribute("val").Value;
                switch (name)
                {
                    case "DiffuseColor":
                        {
                            mat.color = getColorFromRGBString(value);
                            break;
                        }
                    case "DiffuseMap":
                        {
                            mat.mainTexture = importTextureNamed(docRoot, value);
                            break;
                        }

                    default:
                        //Debug.Log("Property " + name + " not handled");
                        break;
                }
            }
        } else
        {
            XElement expressions = matNode.Element("Expressions");
            foreach (XElement NameConstantPairs in expressions.Elements())
            {
                XAttribute n = NameConstantPairs.Attribute("Name");
                string name = (n!=null?n.Value:NameConstantPairs.Name.LocalName);
                if (name == null)
                    name = "Color";
                switch (name)
                {
                    case "Color":
                    case "Base Color":
                        {
                            string constant = NameConstantPairs.Attribute("constant").Value;
                            mat.color = getColorFromRGBString(constant);
                            break;
                        }
                    case "Base Texture":
                    case "Texture":
                        {
                            string pathName = NameConstantPairs.Attribute("PathName").Value;
                            mat.mainTexture = importTextureNamed(docRoot, pathName);
                            break;
                        }

                    default:
                        //Debug.Log("Property " + name + " not handled");
                        break;
                }
            }
        }
        mat.enableInstancing = true;

        return mat;
    }

    private static Texture2D importTextureNamed(XElement docRoot, string texName)
    {
        var textureNode = docRoot.Elements("Texture").Where(a=>a.Attribute("name").Value== texName).First();
        string filename = textureNode.Attribute("file").Value;
        int pos = filename.LastIndexOf('.');
        Texture2D tex = Resources.Load(filename.Substring(0, pos)) as Texture2D;
        return tex;
    }

    private static Color getColorFromRGBString(string value)
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

#endif