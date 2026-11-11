using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace SecondChance
{
    [RequireComponent(typeof(GameDriver))]
    public sealed class GameBootstrap : LifetimeScope
    {
        [SerializeField] private GameSettings settings;
        protected override void Configure(IContainerBuilder builder)
        {
            if (settings == null) throw new System.InvalidOperationException("Assign GameSettings to GameBootstrap.");
            builder.RegisterInstance(settings);
            builder.Register<GameSession>(Lifetime.Singleton);
            builder.RegisterComponent(GetComponent<GameDriver>());
        }
    }
}
