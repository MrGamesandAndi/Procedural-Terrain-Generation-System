using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.HeightModifiers
{
    [System.Serializable]
    public class FeatureConfig
    {
        public Texture2D heightMap;
        public float height;
        public int radius;
        public int numToSpawn = 1;
    }

    public class FeaturesHeightModifier : BaseHeightMapModifier
    {
        [SerializeField] List<FeatureConfig> _features;

        protected void SpawnFeature(FeatureConfig feature, int spawnX, int spawnY, int mapResolution, float[,] heightMap, Vector3 heightMapScale)
        {
            float _averageHeight = 0f;
            int numHeightSamples = 0;

            float[,] smoothHeights = new float[mapResolution, mapResolution];

            //Sum height values under feature
            for (int y = -feature.radius; y <= feature.radius; y++)
            {
                for (int x = -feature.radius; x <= feature.radius; x++)
                {
                    //Sum heightmap values
                    _averageHeight += heightMap[x + spawnX, y + spawnY];
                    numHeightSamples++;
                }
            }

            //Calculate average height
            _averageHeight /= numHeightSamples;
            float targetHeight = _averageHeight + (feature.height / heightMapScale.y);

            //Apply feature
            for (int y = -feature.radius; y <= feature.radius; y++)
            {
                int workingY = y + spawnY;
                float textureY = Mathf.Clamp01((float)(y + feature.radius) / (feature.radius * 2f));

                for (int x = -feature.radius; x <= feature.radius; x++)
                {
                    int workingX = x + spawnX;
                    float textureX = Mathf.Clamp01((float)(x + feature.radius) / (feature.radius * 2f));

                    //Sample heightmap
                    var pixelColor = feature.heightMap.GetPixelBilinear(textureX, textureY);
                    float strength = pixelColor.r;

                    //Blend based on strength
                    heightMap[workingX, workingY] = Mathf.Lerp(heightMap[workingX, workingY], targetHeight, strength);
                }
            }
        }

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            //Traverse features
            foreach (var feature in _features)
            {
                for (int featureIndex = 0; featureIndex < feature.numToSpawn; featureIndex++)
                {
                    int spawnX = generationData.Random(feature.radius, generationData.mapResolution - feature.radius);
                    int spawnY = generationData.Random(feature.radius, generationData.mapResolution - feature.radius);
                    //Debug.Log($"Spawned feature at {spawnX},{spawnY}");
                    SpawnFeature(feature, spawnX, spawnY, generationData.mapResolution, generationData.heightMap, generationData.heightmapScale);
                }
            }
        }
    }
}
