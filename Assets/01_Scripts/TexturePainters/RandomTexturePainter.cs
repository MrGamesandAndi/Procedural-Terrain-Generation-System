using System.Collections.Generic;
using UnityEngine;

namespace ProceduralGeneration.TexturePainters
{
    [System.Serializable]
    public class RandomPainterConfig
    {
        public TextureConfig textureToPaint;
        [Range(0f, 1f)] public float intensityModifier = 1f;
        public float noiseScale;
        [Range(0f, 1f)] public float noiseThreshold;
    }

    public class RandomTexturePainter : BaseTexturePainter
    {
        [SerializeField] TextureConfig _baseTexture;
        [SerializeField] List<RandomPainterConfig> _paintingConfigs;

        [System.NonSerialized] List<TextureConfig> _cachedTextures = null;

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            int baseTextureLayer = generationData.manager.GetLayerForTexture(_baseTexture);

            for (int y = 0; y < generationData.alphaMapResolution; y++)
            {
                int heightMapY = Mathf.FloorToInt((float)y * (float)generationData.mapResolution / (float)generationData.alphaMapResolution);

                for (int x = 0; x < generationData.alphaMapResolution; x++)
                {
                    int heightMapX = Mathf.FloorToInt((float)x * (float)generationData.mapResolution / (float)generationData.alphaMapResolution);

                    //Skip if incorrect biome
                    if (biomeIndex >= 0 && generationData.biomeMap[heightMapX, heightMapY] != biomeIndex)
                    {
                        continue;
                    }

                    //Perform painting
                    foreach (var config in _paintingConfigs)
                    {
                        //Check if noise test passed
                        float noiseValue = Mathf.PerlinNoise(x * config.noiseScale, y * config.noiseScale);

                        if (generationData.Random(0f, 1f) >= noiseValue)
                        {
                            int layer = generationData.manager.GetLayerForTexture(config.textureToPaint);
                            generationData.alphaMaps[x, y, layer] = strength * config.intensityModifier;
                        }
                    }

                    generationData.alphaMaps[x, y, baseTextureLayer] = strength;
                }
            }
        }

        public override List<TextureConfig> RetrieveTextures()
        {
            if (_cachedTextures == null)
            {
                _cachedTextures = new List<TextureConfig>();
                _cachedTextures.Add(_baseTexture);

                foreach (var config in _paintingConfigs)
                {
                    _cachedTextures.Add(config.textureToPaint);
                }
            }

            return _cachedTextures;
        }
    }
}