using UnityEngine;
using System.Collections;

public class MapBuilder1 : MapBuilder
{
    public int FeatureChance = 30;
    public string[] TileLoadStrings;
    public string[] FeatureLoadStrings;

    protected override void InitMapObjects()
    {
        for (int y = 0; y < Height; ++y)
        {
            for (int x = 0; x < Width; ++x)
            {
                int height = HeightField[y * Width + x];

                // Create tile
                if (height >= 0 && height < TileLoadStrings.Length)
                {
					GameObject prefab = Resources.Load<GameObject>(TileLoadStrings[height]);
                    InstantiateMapPrefab(x, y, MapObjectDepth.Tile, prefab);
                }

                // Randomly create a feature for this tile (not on water)
                if (height > 0 && Random.Range(0, 100) < FeatureChance)
                {
                    string randomFeaturePath = FeatureLoadStrings[Random.Range(0, FeatureLoadStrings.Length)];
					GameObject prefab = Resources.Load<GameObject>(randomFeaturePath);
                    InstantiateMapPrefab(x, y, MapObjectDepth.Feature, prefab);
                }
            }
        }
    }
}
