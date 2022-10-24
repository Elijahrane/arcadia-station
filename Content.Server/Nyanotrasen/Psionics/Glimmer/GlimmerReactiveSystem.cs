using Content.Server.Power.Components;
using Content.Server.Electrocution;
using Content.Server.Beam;
using Content.Shared.GameTicking;
using Content.Shared.Psionics.Glimmer;
using Content.Shared.Verbs;
using Content.Shared.Damage;
using Robust.Shared.Random;

namespace Content.Server.Psionics.Glimmer
{
    public sealed class GlimmerReactiveSystem : EntitySystem
    {
        [Dependency] private readonly SharedGlimmerSystem _sharedGlimmerSystem = default!;
        [Dependency] private readonly SharedAppearanceSystem _appearanceSystem = default!;
        [Dependency] private readonly ElectrocutionSystem _electrocutionSystem = default!;
        [Dependency] private readonly SharedAudioSystem _sharedAudioSystem = default!;
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly BeamSystem _beam = default!;


        public float Accumulator = 0;
        public const float UpdateFrequency = 15f;
        public GlimmerTier LastGlimmerTier = GlimmerTier.Minimal;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<RoundRestartCleanupEvent>(Reset);

            SubscribeLocalEvent<SharedGlimmerReactiveComponent, ComponentInit>(OnComponentInit);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, ComponentRemove>(OnComponentRemove);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, PowerChangedEvent>(OnPowerChanged);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, GlimmerTierChangedEvent>(OnTierChanged);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, GetVerbsEvent<AlternativeVerb>>(AddShockVerb);
            SubscribeLocalEvent<SharedGlimmerReactiveComponent, DamageChangedEvent>(OnDamageChanged);
        }

        /// <summary>
        /// Update relevant state on an Entity.
        /// </summary>
        /// <param name="glimmerTierDelta">The number of steps in tier
        /// difference since last update. This can be zero for the sake of
        /// toggling the enabled states.</param>
        private void UpdateEntityState(EntityUid uid, SharedGlimmerReactiveComponent component, GlimmerTier currentGlimmerTier, int glimmerTierDelta)
        {
            var isEnabled = true;

            if (component.RequiresApcPower)
                if (TryComp(uid, out ApcPowerReceiverComponent? apcPower))
                    isEnabled = apcPower.Powered;

            _appearanceSystem.SetData(uid, GlimmerReactiveVisuals.GlimmerTier, isEnabled ? currentGlimmerTier : GlimmerTier.Minimal);

            if (component.ModulatesPointLight)
                if (TryComp(uid, out SharedPointLightComponent? pointLight))
                {
                    pointLight.Enabled = isEnabled ? currentGlimmerTier != GlimmerTier.Minimal : false;

                    // The light energy and radius are kept updated even when off
                    // to prevent the need to store additional state.
                    //
                    // Note that this doesn't handle edge cases where the
                    // PointLightComponent is removed while the
                    // GlimmerReactiveComponent is still present.
                    pointLight.Energy += glimmerTierDelta * component.GlimmerToLightEnergyFactor;
                    pointLight.Radius += glimmerTierDelta * component.GlimmerToLightRadiusFactor;
                }

        }

        /// <summary>
        /// Track when the component comes online so it can be given the
        /// current status of the glimmer tier, if it wasn't around when an
        /// update went out.
        /// </summary>
        private void OnComponentInit(EntityUid uid, SharedGlimmerReactiveComponent component, ComponentInit args)
        {
            if (component.RequiresApcPower && !HasComp<ApcPowerReceiverComponent>(uid))
                Logger.Warning($"{ToPrettyString(uid)} had RequiresApcPower set to true but no ApcPowerReceiverComponent was found on init.");

            if (component.ModulatesPointLight && !HasComp<SharedPointLightComponent>(uid))
                Logger.Warning($"{ToPrettyString(uid)} had ModulatesPointLight set to true but no PointLightComponent was found on init.");

            UpdateEntityState(uid, component, LastGlimmerTier, (int) LastGlimmerTier);
        }

        /// <summary>
        /// Reset the glimmer tier appearance data if the component's removed,
        /// just in case some objects can temporarily become reactive to the
        /// glimmer.
        /// </summary>
        private void OnComponentRemove(EntityUid uid, SharedGlimmerReactiveComponent component, ComponentRemove args)
        {
            UpdateEntityState(uid, component, GlimmerTier.Minimal, -1 * (int) LastGlimmerTier);
        }

        /// <summary>
        /// If the Entity has RequiresApcPower set to true, this will force an
        /// update to the entity's state.
        /// </summary>
        private void OnPowerChanged(EntityUid uid, SharedGlimmerReactiveComponent component, ref PowerChangedEvent args)
        {
            if (component.RequiresApcPower)
                UpdateEntityState(uid, component, LastGlimmerTier, 0);
        }

        /// <summary>
        ///     Enable / disable special effects from higher tiers.
        /// </summary>
        private void OnTierChanged(EntityUid uid, SharedGlimmerReactiveComponent component, GlimmerTierChangedEvent args)
        {
            if (!TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
                return;

            if (args.CurrentTier >= GlimmerTier.Dangerous)
            {
                receiver.PowerDisabled = false;
                receiver.NeedsPower = false;
            } else
            {
                receiver.NeedsPower = true;
            }
        }

        private void AddShockVerb(EntityUid uid, SharedGlimmerReactiveComponent component, GetVerbsEvent<AlternativeVerb> args)
        {
            if(!args.CanAccess || !args.CanInteract)
                return;

            if (!TryComp<ApcPowerReceiverComponent>(uid, out var receiver))
                return;

            if (receiver.NeedsPower)
                return;

            AlternativeVerb verb = new()
            {
                Act = () =>
                {
                    _sharedAudioSystem.PlayPvs(component.ShockNoises, args.User);
                    _electrocutionSystem.TryDoElectrocution(args.User, null, _sharedGlimmerSystem.Glimmer / 200, TimeSpan.FromSeconds((float) _sharedGlimmerSystem.Glimmer / 100), false);
                },
                IconTexture = "/Textures/Interface/VerbIcons/Spare/poweronoff.svg.192dpi.png",
                Text = Loc.GetString("power-switch-component-toggle-verb"),
                Priority = -3
            };
            args.Verbs.Add(verb);
        }

        private void OnDamageChanged(EntityUid uid, SharedGlimmerReactiveComponent component, DamageChangedEvent args)
        {
            Logger.Error("Received event");
            Logger.Error("origin: " + args.Origin);
            if (args.Origin == null)
                return;

            // if (!_random.Prob((float) _sharedGlimmerSystem.Glimmer / 1000))
            //     return;

            var tier = _sharedGlimmerSystem.GetGlimmerTier();
            Logger.Error("Tier: " + tier);
            if (tier < GlimmerTier.High)
                return;

            Logger.Error("Tier is good.");
            string beamproto;

            switch (tier)
            {
                case GlimmerTier.Dangerous:
                    beamproto = "SuperchargedLightning";
                    break;
                case GlimmerTier.Critical:
                    beamproto = "HyperchargedLightning";
                    break;
                default:
                    beamproto = "ChargedLightning";
                    break;
            }
            Logger.Error("Beamproto: " + beamproto);

            var lxform = Transform(uid);
            var txform = Transform(args.Origin.Value);

            if (!lxform.Coordinates.TryDistance(EntityManager, txform.Coordinates, out var distance))
                return;
            if (distance > (float) (_sharedGlimmerSystem.Glimmer / 100))
                return;

            Logger.Error("Creating beam...");
            _beam.TryCreateBeam(uid, args.Origin.Value, beamproto);
        }

        private void Reset(RoundRestartCleanupEvent args)
        {
            Accumulator = 0;

            // It is necessary that the GlimmerTier is reset to the default
            // tier on round restart. This system will persist through
            // restarts, and an undesired event will fire as a result after the
            // start of the new round, causing modulatable PointLights to have
            // negative Energy if the tier was higher than Minimal on restart.
            LastGlimmerTier = GlimmerTier.Minimal;
        }

        public override void Update(float frameTime)
        {
            base.Update(frameTime);
            Accumulator += frameTime;

            if (Accumulator > UpdateFrequency)
            {
                var currentGlimmerTier = _sharedGlimmerSystem.GetGlimmerTier();
                if (currentGlimmerTier != LastGlimmerTier) {
                    var glimmerTierDelta = (int) currentGlimmerTier - (int) LastGlimmerTier;
                    var ev = new GlimmerTierChangedEvent(LastGlimmerTier, currentGlimmerTier, glimmerTierDelta);

                    foreach (var reactive in EntityQuery<SharedGlimmerReactiveComponent>())
                    {
                        UpdateEntityState(reactive.Owner, reactive, currentGlimmerTier, glimmerTierDelta);
                        RaiseLocalEvent(reactive.Owner, ev);
                    }

                    LastGlimmerTier = currentGlimmerTier;
                }
                Accumulator = 0;
            }
        }
    }

    /// <summary>
    /// This event is fired when the broader glimmer tier has changed,
    /// not on every single adjustment to the glimmer count.
    ///
    /// <see cref="SharedGlimmerSystem.GetGlimmerTier"/> has the exact
    /// values corresponding to tiers.
    /// </summary>
    public class GlimmerTierChangedEvent : EntityEventArgs
    {
        /// <summary>
        /// What was the last glimmer tier before this event fired?
        /// </summary>
        public readonly GlimmerTier LastTier;

        /// <summary>
        /// What is the current glimmer tier?
        /// </summary>
        public readonly GlimmerTier CurrentTier;

        /// <summary>
        /// What is the change in tiers between the last and current tier?
        /// </summary>
        public readonly int TierDelta;

        public GlimmerTierChangedEvent(GlimmerTier lastTier, GlimmerTier currentTier, int tierDelta)
        {
            LastTier = lastTier;
            CurrentTier = currentTier;
            TierDelta = tierDelta;
        }
    }
}

