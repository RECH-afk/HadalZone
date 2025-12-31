using RKS.HadalZone.Core.Managers;
using UnityEngine;
using Zenject;

namespace RKS.HadalZone.Core.Installers
{
    sealed class ProjectInstaller : MonoInstaller
    {
        [Header("Managers")]
        [SerializeField] private LocalizationManager localizationManagerPrefab;
        [SerializeField] private AudioManager audioManagerPrefab;
        [SerializeField] private DiscordManager discordControllerPrefab;
        [SerializeField] private SaveManager saveManagerPrefab;
        [SerializeField] private TransitionManager transitionServicePrefab;

        public override void InstallBindings()
        {
            Debug.Log("[ProjectInstaller] Installing global managers...");

            Container.Bind<LocalizationManager>().FromComponentInNewPrefab(localizationManagerPrefab).AsSingle().NonLazy();
            Container.Bind<AudioManager>().FromComponentInNewPrefab(audioManagerPrefab).AsSingle().NonLazy();
            Container.Bind<DiscordManager>().FromComponentInNewPrefab(discordControllerPrefab).AsSingle().NonLazy();
            Container.Bind<SaveManager>().FromComponentInNewPrefab(saveManagerPrefab).AsSingle().NonLazy();
            Container.Bind<TransitionManager>().FromComponentInNewPrefab(transitionServicePrefab).AsSingle().NonLazy();
        }

    }
}
