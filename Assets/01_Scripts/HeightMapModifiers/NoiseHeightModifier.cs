using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.HeightModifiers
{
    [System.Serializable]
    public class NoiseHeightPass
    {
        public float heightDelta = 1f;
        public float noiseScale = 1f;
    }

    public class NoiseHeightModifier : BaseHeightMapModifier
    {
        [SerializeField] List<NoiseHeightPass> _passes;

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            foreach (var pass in _passes)
            {
                for (int y = 0; y < generationData.mapResolution; y++)
                {
                    for (int x = 0; x < generationData.mapResolution; x++)
                    {
                        //Skip if incorrect biome
                        if (biomeIndex >= 0 && generationData.biomeMap[x, y] != biomeIndex)
                        {
                            continue;
                        }

                        float noiseValue = (Mathf.PerlinNoise(x * pass.noiseScale, y * pass.noiseScale) * 2f) - 1f;

                        //Calculate new height
                        float newHeight = generationData.heightMap[x, y] + (noiseValue * pass.heightDelta / generationData.heightmapScale.y);

                        //Blend based on strength
                        generationData.heightMap[x, y] = Mathf.Lerp(generationData.heightMap[x, y], newHeight, strength);
                    }
                }
            }
        }
    }
}
