using Robust.Shared.Serialization;

namespace Content.Shared.PenRename
{
    public class SharedPenRenameSystem : EntitySystem
    {
        /// Just for friending for now
    }
    /// <summary>
    /// Key representing which <see cref="BoundUserInterface"/> is currently open.
    /// Useful when there are multiple UI for an object. Here it's future-proofing only.
    /// </summary>
    [Serializable, NetSerializable]
    public enum PenRenameUiKey
    {
        Key,
    }

    /// <summary>
    /// Represents an <see cref="PenRenameComponent"/> state that can be sent to the client
    /// </summary>
    [Serializable, NetSerializable]
    public sealed class PenRenameBoundUserInterfaceState : BoundUserInterfaceState
    {
        public string CurrentName { get; }
        public PenRenameBoundUserInterfaceState(string currentName, string currentJob)
        {
            CurrentName = currentName;
        }
    }

    [Serializable, NetSerializable]
    public sealed class PenRenameNameChangedMessage : BoundUserInterfaceMessage
    {
        public string Name { get; }

        public PenRenameNameChangedMessage(string name)
        {
            Name = name;
        }
    }
}
