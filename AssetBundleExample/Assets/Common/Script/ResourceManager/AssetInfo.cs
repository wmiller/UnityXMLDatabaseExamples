using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

[XmlRoot("AssetInfo")]
public sealed class AssetInfo : DatabaseEntry 
{
    [XmlElement]
    public string Path { get; private set; }

    [XmlElement]
    public DatabaseEntryRef<AssetBundleInfo> AssetBundleInfoRef { get; private set; }
}
