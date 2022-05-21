using Content.Shared.Interaction;
using Content.Shared.Tag;
using Robust.Server.GameObjects;
using Content.Shared.PenRename;
using Content.Server.UserInterface;

namespace Content.Server.PenRename
{
    public sealed class PenRenameSystem : EntitySystem
    {
        [Dependency] private readonly TagSystem _tagSystem = default!;
        [Dependency] private readonly UserInterfaceSystem _userInterfaceSystem = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<PenRenamableComponent, AfterInteractUsingEvent>(OnAfterInteractUsing);
            SubscribeLocalEvent<PenRenamableComponent, PenRenameNameChangedMessage>(OnNameChanged);
        }

        private void OnAfterInteractUsing(EntityUid uid, PenRenamableComponent component, AfterInteractUsingEvent args)
        {
            if (!_tagSystem.HasTag(args.Used, "Write"))
                return;

            if (!TryComp<ActorComponent>(args.User, out var actor))
                return;

            if (!_userInterfaceSystem.TryGetUi(uid, PenRenameUiKey.Key, out var ui) || ui == null)
                return;

            ui.Open(actor.PlayerSession);
        }

        private void OnNameChanged(EntityUid uid, PenRenamableComponent component, PenRenameNameChangedMessage args)
        {
            MetaData(uid).EntityName = args.Name;
        }
    }
}
