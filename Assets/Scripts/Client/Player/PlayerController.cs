using Utility;
using UnityEngine;
using Unity.Netcode;

namespace PTB.Client.Player
{
    public class PlayerNetworkedController : NetworkBehaviour
    {
        [SerializeField] private int _correctAnswers;

    }
}