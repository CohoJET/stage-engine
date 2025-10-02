using System.Collections.Specialized;
using StageEngine.Core.Game.Session;
using StageEngine.Core.Players;
using StageEngine.Core.UI;
using StageEngine.Fiasco.Game.Session;
using UnityEngine;
using UnityEngine.UIElements;

namespace StageEngine.Fiasco.UI
{
    [UxmlElement]
    public partial class FiascoPlayerCardElement : VisualElement, IPlayerCardElement
    {
        private static readonly string CARD_SEPARATOR = "|";
        
        private Label PlayerNameLabel => this.Q<Label>("player-name");
        private Label RelationshipCardALabel => this.Q<Label>("relationship-card-a");
        private Label RelationshipCardBLabel => this.Q<Label>("relationship-card-b");
        private Label RelationshipTargetLabel => this.Q<Label>("relationship-target");

        private Player player;
        private Card relationshipCardA;
        private Card relationshipCardB;

        public void Init(Player player)
        {
            this.player = player;
            PlayerNameLabel.text = player.Name;
            style.borderLeftColor = player.Color;

            SessionManager.Instance.GetSessionData<FiascoSessionData>().Cards.CollectionChanged += OnCardsCollectionChanged;
            RefreshAllCards();
        }

        private void OnCardsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (Card card in e.NewItems)
                    {
                        SetRelationshipCard(card);
                    }
                    break;

                case NotifyCollectionChangedAction.Reset:
                    RefreshAllCards();
                    break;

                case NotifyCollectionChangedAction.Replace:
                    RefreshAllCards();
                    break;
            }
        }
        private void RefreshAllCards()
        {
            if (relationshipCardA != null)
            {
                relationshipCardA.PropertyChanged -= OnCardPropertyChanged;
            }
            if (relationshipCardB != null)
            {
                relationshipCardB.PropertyChanged -= OnCardPropertyChanged;
            }
            
            relationshipCardA = null;
            relationshipCardB = null;
            
            var sessionData = SessionManager.Instance.GetSessionData<FiascoSessionData>();
            foreach (Card card in sessionData.Cards)
            {
                SetRelationshipCard(card);
            }
            
            UpdateCardsText();
        }
        private void SetRelationshipCard(Card card)
        {
            if (!card.PlayerA.Name.Equals(player.Name))
            {
                return;
            }

            if (relationshipCardA == null)
            {
                relationshipCardA = card;
                card.PropertyChanged += OnCardPropertyChanged;
                UpdateCardsText();
            }
            else if (relationshipCardB == null)
            {
                relationshipCardB = card;
                card.PropertyChanged += OnCardPropertyChanged;
                UpdateCardsText();
            }
        }

        private void OnCardPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateCardsText();
        }

        private void UpdateCardsText()
        {
            UpdateCardText(relationshipCardA, RelationshipCardALabel);
            UpdateCardText(relationshipCardB, RelationshipCardBLabel);
            UpdateRelationshipTarget();
        }

        private void UpdateRelationshipTarget()
        {
            if (relationshipCardA != null && relationshipCardA.PlayerB != null)
            {
                RelationshipTargetLabel.text = $"...with {relationshipCardA.PlayerB.Name}";
            }
            else
            {
                RelationshipTargetLabel.text = "...with someone";
            }
        }

        private void UpdateCardText(Card card, Label cardLabel)
        {
            if (card == null)
            {
                cardLabel.text = "...pending";
                return;
            }

            if (card.Category != null && card.Element != null)
            {
                cardLabel.text = $"{card.Category.Name} {CARD_SEPARATOR} {card.Element.Name}";
            }
            else if (card.Category != null)
            {
                cardLabel.text = $"{card.Category.Name} {CARD_SEPARATOR} ...pending";
            }
            else if (card.Table != null)
            {
                cardLabel.text = card.Table.Name;
            }
            else
            {
                cardLabel.text = "...pending";
            }
        }
    }
}