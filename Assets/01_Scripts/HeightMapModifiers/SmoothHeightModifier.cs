using UnityEngine;

namespace ProceduralGeneration.HeightModifiers
{
    public class SmoothHeightModifier : BaseHeightMapModifier
    {
        [SerializeField] int _smoothingKernelSize = 5;
        [Range(0f, 1f)] [SerializeField] float _maxHeightThreshold = 0.5f;
        [SerializeField] bool _useAdaptiveKernel = false;
        [SerializeField] int _minKernelSize = 2;
        [SerializeField] int _maxKernelSize = 7;

        public override void Execute(ProcGenManager.GenerationData generationData, int biomeIndex = -1, BiomeConfigSO biome = null)
        {
            if (biome != null)
            {
                Debug.LogError($"The Smooth modifier is not supported as a per biome modifier [{gameObject.name}].");
                return;
            }

            float[,] smoothHeights = new float[generationData.mapResolution, generationData.mapResolution];

            for (int y = 0; y < generationData.mapResolution; y++)
            {
                for (int x = 0; x < generationData.mapResolution; x++)
                {
                    float heightSum = 0f;
                    int numValues = 0;

                    //Set kernel size
                    int kernelSize = _smoothingKernelSize;

                    if (_useAdaptiveKernel)
                    {
                        kernelSize = Mathf.RoundToInt(Mathf.Lerp(_maxKernelSize, _minKernelSize, generationData.heightMap[x, y] / _maxHeightThreshold));
                    }

                    //Sum neighbouring values
                    for (int yDelta = -kernelSize; yDelta <= kernelSize; yDelta++)
                    {
                        int workingY = y + yDelta;

                        if (workingY < 0 || workingY >= generationData.mapResolution)
                        {
                            continue;
                        }

                        for (int xDelta = -kernelSize; xDelta <= kernelSize; xDelta++)
                        {
                            int workingX = x + xDelta;

                            if (workingX < 0 || workingX >= generationData.mapResolution)
                            {
                                continue;
                            }

                            heightSum += generationData.heightMap[workingX, workingY];
                            numValues++;
                        }
                    }

                    //Store the average smoothed height
                    smoothHeights[x, y] = heightSum / numValues;
                }
            }

            for (int y = 0; y < generationData.mapResolution; y++)
            {
                for (int x = 0; x < generationData.mapResolution; x++)
                {
                    //Blend based on strength
                    generationData.heightMap[x, y] = Mathf.Lerp(generationData.heightMap[x, y], smoothHeights[x, y], strength);
                }
            }
        }
    }
}
