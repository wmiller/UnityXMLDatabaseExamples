using UnityEngine;
using System.Collections;
using System.Xml;
using System.Xml.Serialization;

[XmlRoot]
public sealed class TileInfo : DatabaseEntry
{
    [XmlElement]
    public int Height { get; private set; }

    [XmlElement]
    public int FeatureChance { get; private set; }

    [XmlElement]
    public DatabaseEntryRef<AssetInfo> PrefabInfoRef { get; private set; }

    [XmlArray("PossibleFeatures")]
    [XmlArrayItem("FeatureInfoRef")]
    public DatabaseEntryRef<FeatureInfo>[] PossibleFeatures { get; private set; }
}

[XmlRoot]
public sealed class FeatureInfo : DatabaseEntry
{
    [XmlElement]
    public DatabaseEntryRef<AssetInfo> PrefabInfoRef { get; private set; }
}