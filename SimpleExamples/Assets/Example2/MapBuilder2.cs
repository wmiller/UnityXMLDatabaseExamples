using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MapBuilder2 : MapBuilder
{
    protected override void InitMapObjects()
    {
        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                int height = HeightField[y * Width + x];
                
                // Get all tiles in the database that match the height
                TileInfo[] tileInfos = Database.Instance.GetEntries<TileInfo>().Where((info) => info.Height == height).ToArray();
                
                // Instance the tile
                TileInfo tileInfo = tileInfos[Random.Range(0, tileInfos.Length)];
                InstantiateMapPrefab(x, y, MapObjectDepth.Tile, Resources.Load<GameObject>(tileInfo.PrefabPath));
                
                // See if we drop a feature
                if (tileInfo.PossibleFeatures != null && 
                    tileInfo.PossibleFeatures.Length > 0 && 
                    Random.Range(0, 100) < tileInfo.FeatureChance)
                {
                    // Select a random feature from the tile infos possible features
                    FeatureInfo featureInfo = tileInfo.PossibleFeatures[Random.Range(0, tileInfo.PossibleFeatures.Length)];
                    InstantiateMapPrefab(x, y, MapObjectDepth.Feature, Resources.Load<GameObject>(featureInfo.PrefabPath));
                }
            }
        }
    }
}
