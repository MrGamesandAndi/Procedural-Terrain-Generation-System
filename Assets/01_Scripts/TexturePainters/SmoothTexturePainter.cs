using UnityEngine;

namespace ProceduralGeneration.TexturePainters
{
    public class SmoothTexturePainter : BaseTexturePainter
    {
        [SerializeField] int _smoothingKernelSize = 5;

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            if (biome != null)
            {
                Debug.LogError($"The Smooth modifier is not supported as a per biome modifier [{gameObject.name}].");
                return;
            }

            for (int layer = 0; layer < generationData.alphaMaps.GetLength(2); layer++)
            {
                float[,] smoothAlphaMaps = new float[generationData.alphaMapResolution, generationData.alphaMapResolution];

                for (int y = 0; y < generationData.alphaMapResolution; y++)
                {
                    for (int x = 0; x < generationData.alphaMapResolution; x++)
                    {
                        float alphaSum = 0f;
                        int numValues = 0;

                        //Sum neighbouring values
                        for (int yDelta = -_smoothingKernelSize; yDelta <= _smoothingKernelSize; yDelta++)
                        {
                            int workingY = y + yDelta;

                            if (workingY < 0 || workingY >= generationData.alphaMapResolution)
                            {
                                continue;
                            }

                            for (int xDelta = -_smoothingKernelSize; xDelta <= _smoothingKernelSize; xDelta++)
                            {
                                int workingX = x + xDelta;

                                if (workingX < 0 || workingX >= generationData.alphaMapResolution)
                                {
                                    continue;
                                }

                                alphaSum += generationData.alphaMaps[workingX, workingY, layer];
                                numValues++;
                            }
                        }

                        //Store the average smoothed alpha
                        smoothAlphaMaps[x, y] = alphaSum / numValues;
                    }
                }

                for (int y = 0; y < generationData.alphaMapResolution; y++)
                {
                    for (int x = 0; x < generationData.alphaMapResolution; x++)
                    {
                        //Blend based on strength
                        generationData.alphaMaps[x, y, layer] = Mathf.Lerp(generationData.alphaMaps[x, y, layer], smoothAlphaMaps[x, y], strength);
                    }
                }
            }
        }
    }
}
