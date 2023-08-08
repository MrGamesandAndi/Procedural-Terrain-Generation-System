using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;

namespace ProceduralGeneration
{
    [CustomEditor(typeof(ProcGenManager))]
    public class ProcGenManagerEditor : Editor
    {
        int _progressID;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Regenerate Textures"))
            {
                ProcGenManager targetManager = serializedObject.targetObject as ProcGenManager;
                targetManager.RegenerateTextures();
            }

            if (GUILayout.Button("Regenerate Detail Prototypes"))
            {
                ProcGenManager targetManager = serializedObject.targetObject as ProcGenManager;
                targetManager.RegenerateDetailPrototypes();
            }

            if (GUILayout.Button("Regenerate World"))
            {
                ProcGenManager targetManager = serializedObject.targetObject as ProcGenManager;
                EditorCoroutineUtility.StartCoroutine(PerformRegeneration(targetManager), this);
            }
        }

        private IEnumerator PerformRegeneration(ProcGenManager targetManager)
        {
            _progressID = Progress.Start("Regenerating terrain...");
            yield return targetManager.AsyncRegenerateWorld(OnStatusReported);
            Progress.Remove(_progressID);
            yield return null;
        }

        private void OnStatusReported(GenerationStage currentStage, string status)
        {
            Progress.Report(_progressID, (int)currentStage, (int)GenerationStage.NumStages, status);
        }
    }
}