using UnityEngine;

namespace Harukerryzi.Clash
{
    public sealed class ClashFlowController : MonoBehaviour
    {
        [SerializeField] private ClashController clashController;
        [SerializeField] private ClashIntroUI introUI;
        [SerializeField] private ClashResultUI resultUI;

        private void OnEnable()
        {
            if (clashController != null)
            {
                clashController.OnClashFinished.AddListener(ShowResult);
            }

            if (introUI != null)
            {
                introUI.OnIntroComplete.AddListener(StartClash);
            }

            if (resultUI != null)
            {
                resultUI.OnRetry.AddListener(Retry);
            }
        }

        private void OnDisable()
        {
            if (clashController != null)
            {
                clashController.OnClashFinished.RemoveListener(ShowResult);
            }

            if (introUI != null)
            {
                introUI.OnIntroComplete.RemoveListener(StartClash);
            }

            if (resultUI != null)
            {
                resultUI.OnRetry.RemoveListener(Retry);
            }
        }

        private void Start()
        {
            if (clashController != null)
            {
                clashController.SetStartOnAwake(false);
                clashController.ResetClash();
            }

            StartIntro();
        }

        public void StartIntro()
        {
            resultUI?.Hide();
            introUI?.Play();
        }

        public void StartClash()
        {
            resultUI?.Hide();
            clashController?.BeginClash();
        }

        public void Retry()
        {
            clashController?.ResetClash();
            StartIntro();
        }

        private void ShowResult(ClashResult result)
        {
            resultUI?.Show(result);
        }
    }
}
