using Robust.Shared.Prototypes;
using Content.Server.Abilities.Psionics;
using Content.Server.Radio.Components;
using Content.Server.Radio.EntitySystems;
using Content.Server.StationEvents.Events;
using Content.Server.NPC.Events;
using Content.Server.NPC.Systems;
using Content.Server.NPC.Prototypes;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Radio;

namespace Content.Server.Research.SophicScribe
{
    public sealed partial class SophicScribeSystem : EntitySystem
    {
        [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;
        [Dependency] private readonly RadioSystem _radioSystem = default!;
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
        [Dependency] private readonly NPCConversationSystem _conversationSystem = default!;

        private readonly ISawmill _sawmill = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<SophicScribeComponent, NPCConversationGetGlimmerEvent>(OnGetGlimmer);
            SubscribeLocalEvent<GlimmerEventEndedEvent>(OnGlimmerEventEnded);
        }

        private void OnGetGlimmer(EntityUid uid, SophicScribeComponent component, NPCConversationGetGlimmerEvent args)
        {
            if (args.Text == null)
            {
                _sawmill.Error($"{ToPrettyString(uid)} heard a glimmer reading prompt but has no text for it.");
                return;
            }

            var tier = _glimmerSystem.GetGlimmerTier();

            var glimmerReadingText = Loc.GetString(args.Text,
                ("glimmer", _glimmerSystem.Glimmer), ("tier", tier));

            var response = new NPCResponse(glimmerReadingText);
            _conversationSystem.QueueResponse(uid, response);
        }

        private void OnGlimmerEventEnded(GlimmerEventEndedEvent args)
        {
            var query = EntityQueryEnumerator<SophicScribeComponent>();
            while (query.MoveNext(out var scribe, out _))
            {
                if (!TryComp<IntrinsicRadioTransmitterComponent>(scribe, out var radio)) return;

                // mind entities when...
                var speaker = scribe;
                if (TryComp<MindSwappedComponent>(scribe, out var swapped))
                {
                    speaker = swapped.OriginalEntity;
                }

                var message = Loc.GetString(args.Message, ("decrease", args.GlimmerBurned), ("level", _glimmerSystem.Glimmer));
                var channel = _prototypeManager.Index<RadioChannelPrototype>("Common");
                _radioSystem.SendRadioMessage(speaker, message, channel, speaker);
            }
        }
    }

    public sealed class NPCConversationGetGlimmerEvent : NPCConversationEvent
    {
        [DataField("text")]
        public readonly string? Text;
    }
}
