using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

[XmlRoot("AssetBundleInfo")]
public sealed class AssetBundleInfo : DatabaseEntry 
{
    [XmlElement]
    public string Name { get; private set; }

	[XmlElement]
	public string URL { get; private set; }
}