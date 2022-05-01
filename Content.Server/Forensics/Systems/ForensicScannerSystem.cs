using System.Linq;
using System.Threading;
using Content.Server.DoAfter;
using Content.Shared.Forensics;
using Content.Shared.Interaction;
using Robust.Server.GameObjects;

namespace Content.Server.Forensics
{
    public sealed class ForensicScannerSystem : EntitySystem
    {
        [Dependency] private readonly ForensicsSystem _forensics = default!;
        [Dependency] private readonly DoAfterSystem _doAfterSystem = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ForensicScannerComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<TargetScanSuccessfulEvent>(OnTargetScanSuccessful);
            SubscribeLocalEvent<ScanCancelledEvent>(OnScanCancelled);
        }

        private void OnScanCancelled(ScanCancelledEvent ev)
        {
            if (ev.Component == null)
                return;
            ev.Component.CancelToken = null;
        }

        private void OnTargetScanSuccessful(TargetScanSuccessfulEvent ev)
        {
            ev.Component.CancelToken = null;

            if (!TryComp<ForensicsComponent>(ev.Target, out var forensics))
              return;

            ev.Component.Fingerprints = forensics.Fingerprints.ToList();
            ev.Component.Fibers = forensics.Fibers.ToList();
            OpenUserInterface(ev.User, ev.Component);
        }

        private void OnAfterInteract(EntityUid uid, ForensicScannerComponent component, AfterInteractEvent args)
        {
            if (component.CancelToken != null)
            {
                component.CancelToken.Cancel();
                component.CancelToken = null;
            }

            if (args.Target == null || !args.CanReach)
                return;

            component.CancelToken = new CancellationTokenSource();
            _doAfterSystem.DoAfter(new DoAfterEventArgs(args.User, component.ScanDelay, component.CancelToken.Token, target: args.Target)
            {
                BroadcastFinishedEvent = new TargetScanSuccessfulEvent(args.User, args.Target, component),
                BroadcastCancelledEvent = new ScanCancelledEvent(uid, component),
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                BreakOnStun = true,
                NeedHand = true
            });
        }
        private void OpenUserInterface(EntityUid user, ForensicScannerComponent component)
        {
            if (!TryComp<ActorComponent>(user, out var actor))
                return;

            component.UserInterface?.Open(actor.PlayerSession);
            component.UserInterface?.SendMessage(new ForensicScannerUserMessage(component.Fingerprints, component.Fibers));
        }
        private sealed class ScanCancelledEvent : EntityEventArgs
        {
            public EntityUid Uid;
            public ForensicScannerComponent Component;

            public ScanCancelledEvent(EntityUid uid, ForensicScannerComponent component)
            {
                Uid = uid;
                Component = component;
            }
        }

        private sealed class TargetScanSuccessfulEvent : EntityEventArgs
        {
            public EntityUid User;
            public EntityUid? Target;
            public ForensicScannerComponent Component;

            public TargetScanSuccessfulEvent(EntityUid user, EntityUid? target, ForensicScannerComponent component)
            {
                User = user;
                Target = target;
                Component = component;
            }
        }
    }
}