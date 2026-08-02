using EasyPeasyFirstPersonController;

namespace RKS.HadalZone.Player
{
    public interface IInteractable
    {
        string Prompt { get; }
        bool CanInteract { get; }
        void OnInteract(PlayerController player);
    }
}
