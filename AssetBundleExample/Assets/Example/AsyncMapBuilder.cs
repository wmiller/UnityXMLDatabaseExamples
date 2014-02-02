using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class AsyncMapBuilder : MapBuilder
{
    protected override void InitMapObjects()
    {
        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                int height = HeightField[y * Width + x];
                int tempX = x;
                int tempY = y;
                
                // Get all tiles in the database that match the height
                TileInfo[] tileInfos = Database.Instance.GetEntries<TileInfo>().Where((info) => info.Height == height).ToArray();
                
                // Instance the tile
                TileInfo tileInfo = tileInfos[Random.Range(0, tileInfos.Length)];
                resourceManager.LoadAssetAsync<GameObject>(tileInfo.PrefabInfoRef.Entry, (prefab) => 
                {
                    InstantiateMapPrefab(tempX, tempY, MapObjectDepth.Tile, prefab);
                });

                // See if we drop a feature
                if (tileInfo.PossibleFeatures != null && 
                    tileInfo.PossibleFeatures.Length > 0 && 
                    Random.Range(0, 100) < tileInfo.FeatureChance)
                {
                    // Select a random feature from the tile infos possible features
                    FeatureInfo featureInfo = tileInfo.PossibleFeatures[Random.Range(0, tileInfo.PossibleFeatures.Length)];
                    resourceManager.LoadAssetAsync<GameObject>(featureInfo.PrefabInfoRef.Entry, (prefab) => 
                    {
                        InstantiateMapPrefab(tempX, tempY, MapObjectDepth.Feature, prefab);
                    });
                }
            }
        }
    }
}
