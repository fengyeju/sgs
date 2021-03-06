﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Sanguosha.Core.Triggers;
using Sanguosha.Core.Cards;
using Sanguosha.Core.UI;
using Sanguosha.Core.Skills;
using Sanguosha.Expansions.Basic.Cards;
using Sanguosha.Core.Players;
using Sanguosha.Core.Games;
using System.Diagnostics;
using Sanguosha.Expansions.Battle.Cards;

namespace Sanguosha.Expansions.OverKnightFame12.Skills
{
    /// <summary>
    /// 奇策–出牌阶段，你可以将所有的手牌（至少一张）当做任意一张非延时锦囊牌使用。每阶段限一次。
    /// </summary>
    public class QiCe : CardTransformSkill, IAdditionalTypedSkill
    {
        private static PlayerAttribute QiCeUsed = PlayerAttribute.Register("QiCeUsed", true);
        public override VerifierResult TryTransform(List<Card> cards, List<Player> arg, out CompositeCard card, bool isPlay)
        {
            card = new CompositeCard();
            card.Subcards = new List<Card>();
            card.Type = AdditionalType;
            card[TieSuoLianHuan.ProhibitReforging] = 1;
            if (Owner[QiCeUsed] == 1) return VerifierResult.Fail;
            if (Game.CurrentGame.CurrentPhase != TurnPhase.Play) return VerifierResult.Fail;
            if (Game.CurrentGame.CurrentPlayer != Owner) return VerifierResult.Fail;
            if (Owner.HandCards().Count == 0) return VerifierResult.Fail;
            if (AdditionalType == null)
            {
                return VerifierResult.Partial;
            }
            if (!CardCategoryManager.IsCardCategory(AdditionalType.Category, CardCategory.ImmediateTool) || AdditionalType is WuXieKeJi)
            {
                return VerifierResult.Fail;
            }
            if (cards != null && cards.Count > 0)
            {
                return VerifierResult.Fail;
            }

            card.Subcards.AddRange(Owner.HandCards());
            return VerifierResult.Success;
        }

        protected override bool DoTransformSideEffect(CompositeCard card, object arg, List<Player> targets, bool isPlay)
        {
            Owner[QiCeUsed] = 1;
            Game.CurrentGame.SyncImmutableCardsAll(Owner.HandCards());
            return true;
        }

        public override List<CardHandler> PossibleResults
        {
            get { return new List<CardHandler>(); }
        }

        public CardHandler AdditionalType { get; set; }
    }
}
