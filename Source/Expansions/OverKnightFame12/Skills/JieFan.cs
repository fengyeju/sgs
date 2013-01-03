﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sanguosha.Core.UI;
using Sanguosha.Core.Skills;
using Sanguosha.Core.Players;
using Sanguosha.Core.Games;
using Sanguosha.Core.Triggers;
using Sanguosha.Core.Exceptions;
using Sanguosha.Core.Cards;
using System.Diagnostics;
using Sanguosha.Expansions.Basic.Cards;

namespace Sanguosha.Expansions.OverKnightFame12.Skills
{
    /// <summary>
    /// 解烦-当有角色进入濒死状态时，你可对当前回合角色使用一张【杀】，此【杀】造成伤害时，你防止此伤害，视为对该濒死角色使用了一张【桃】。
    /// </summary>
    public class JieFan : TriggerSkill
    {
        protected void Run(Player Owner, GameEvent gameEvent, GameEventArgs eventArgs)
        {
            var dying = eventArgs.Targets[0];
            var shaTarget = Game.CurrentGame.CurrentPlayer;
            ISkill skill;
            List<Card> cards;
            List<Player> players;
            while (true)
            {
                if (Game.CurrentGame.UiProxies[Owner].AskForCardUsage(new CardUsagePrompt("JieFan"), new JieDaoShaRen.JieDaoShaRenVerifier(shaTarget),
                    out skill, out cards, out players))
                {
                    NotifySkillUse();
                    try
                    {
                        GameEventArgs args = new GameEventArgs();
                        Owner[Sha.NumberOfShaUsed]--;
                        args.Source = Owner;
                        args.Targets = new List<Player>(players);
                        args.Targets.Add(shaTarget);
                        args.Skill = skill;
                        args.Cards = cards;
                        args.ReadonlyCard = new ReadOnlyCard(new Card() { Place = new DeckPlace(null, null) });
                        args.ReadonlyCard[JieFanSha] = shaTarget.Id + 1;
                        Game.CurrentGame.Emit(GameEvent.CommitActionToTargets, args);
                    }
                    catch (TriggerResultException e)
                    {
                        Trace.Assert(e.Status == TriggerResult.Retry);
                        continue;
                    }
                }
                break;
            }
        }
        private class JieFanTaoCardTransformSkill : CardTransformSkill
        {
            public override VerifierResult TryTransform(List<Card> cards, object arg, out CompositeCard card)
            {
                Trace.Assert(cards == null || cards.Count == 0);
                card = new CompositeCard();
                card.Type = new Tao();
                return VerifierResult.Success;
            }
            protected override void NotifyAction(Player source, List<Player> targets, CompositeCard cards)
            {
            }
        }

        static CardAttribute JieFanSha = CardAttribute.Register("JieFanSha");
        public JieFan()
        {
            var trigger = new AutoNotifyPassiveSkillTrigger(
                this,
                Run,
                TriggerCondition.Global
            ) { AskForConfirmation = false, IsAutoNotify = false };
            Triggers.Add(GameEvent.PlayerDying, trigger);
            var trigger2 = new AutoNotifyPassiveSkillTrigger(
                this,
                (p, e, a) => { return a.ReadonlyCard != null && a.ReadonlyCard[JieFanSha] != 0; },
                (p, e, a) =>
                    {
                        Player target = Game.CurrentGame.Players[a.ReadonlyCard[JieFanSha] - 1];
                        GameEventArgs args = new GameEventArgs();
                        args.Source = Owner;
                        args.Targets = new List<Player>() {target};
                        args.Skill = new JieFanTaoCardTransformSkill();
                        args.Cards = new List<Card>();
                        Game.CurrentGame.Emit(GameEvent.CommitActionToTargets, args);
                        throw new TriggerResultException(TriggerResult.End);
                    },
                TriggerCondition.Global
            ) { AskForConfirmation = false };
            Triggers.Add(GameEvent.DamageCaused, trigger2);
            IsAutoInvoked = null;
        }
    }
}