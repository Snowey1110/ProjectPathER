using Unity.Netcode.Components;
using UnityEngine;

public class ClientNetworkAnimator : NetworkAnimator
{
    // This override tells Unity: "I am the owner, so I decide when to animate!"
    // This eliminates the input lag.
    protected override bool OnIsServerAuthoritative()
    {
        return false;
    }
}