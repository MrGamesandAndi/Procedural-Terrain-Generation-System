using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProceduralGeneration.UI.Debug
{
    public class ProcGenDebugUI : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI _statusDisplay;
        [SerializeField] Button _regenerateButton;
        [SerializeField] ProcGenManager _targetManager;

        public void OnRegenerate()
        {
            _regenerateButton.interactable = false;
            StartCoroutine(PerformRegeneration());
        }

        private IEnumerator PerformRegeneration()
        {
            yield return _targetManager.AsyncRegenerateWorld(OnStatusReported);
            _regenerateButton.interactable = true;
            yield return null;
        }

        private void OnStatusReported(GenerationStage currentStage, string status)
        {
            _statusDisplay.text = $"Step {(int)currentStage} of {(int)GenerationStage.NumStages}: {status}";
        }
    }
}
