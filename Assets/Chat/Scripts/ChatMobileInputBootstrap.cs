using UMI;
using UnityEngine;

namespace Avrin.Chat
{
    [DefaultExecutionOrder(-10000)]
    public sealed class ChatMobileInputBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            MobileInput.Init();
        }
    }
}
