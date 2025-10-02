using UnityEngine.UIElements;
using System.Collections.Specialized;
using StageEngine.Core.Players;
using StageEngine.Core.Game.Session;

namespace StageEngine.Core.UI
{
    [UxmlElement]
    public partial class PlayersListElement : VisualElement, IInitializableElement
    {
        public VisualTreeAsset PlayerCardTemplate { get; set; }

        public PlayersListElement() { }

        public void Init()
        {
            SessionManager.Instance.Data.Players.CollectionChanged += OnPlayersCollectionChanged;
            RefreshAllPlayers();
        }

        private void OnPlayersCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Player player in e.NewItems)
                    {
                        AddPlayerCard(player);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    RefreshAllPlayers();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    RefreshAllPlayers();
                    break;
            }
        }

        private void RefreshAllPlayers()
        {
            contentContainer.Clear();

            foreach (var player in SessionManager.Instance.Data.Players)
            {
                AddPlayerCard(player);
            }
        }

        private void AddPlayerCard(Player player)
        {
            if (PlayerCardTemplate == null) return;

            var container = PlayerCardTemplate.Instantiate();
            contentContainer.Add(container);

            var element = container.Query<VisualElement>()
                .Where(element => element is IPlayerCardElement)
                .First() as IPlayerCardElement;
            element.Init(player);
        }
    }
}
