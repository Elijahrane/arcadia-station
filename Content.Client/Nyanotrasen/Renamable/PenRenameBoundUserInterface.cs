using Content.Shared.PenRename;
using Robust.Client.GameObjects;

namespace Content.Client.PenRename
{
    /// <summary>
    /// Initializes a <see cref="PenRenameWindow"/> and updates it when new server messages are received.
    /// </summary>
    public sealed class PenRenameBoundUserInterface : BoundUserInterface
    {
        private PenRenameWindow? _window;

        public PenRenameBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();

            _window = new PenRenameWindow();
            if (State != null)
                UpdateState(State);

            _window.OpenCentered();

            _window.OnClose += Close;
            _window.OnNameEntered += OnNameChanged;
        }

        private void OnNameChanged(string newName)
        {
            SendMessage(new PenRenameNameChangedMessage(newName));
        }
        /// <summary>
        /// Update the UI state based on server-sent info
        /// </summary>
        /// <param name="state"></param>
        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);
            if (_window == null || state is not PenRenameBoundUserInterfaceState cast)
                return;

            _window.SetCurrentName(cast.CurrentName);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            _window?.Dispose();
        }
    }

}
