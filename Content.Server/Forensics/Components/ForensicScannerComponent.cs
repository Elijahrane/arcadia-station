using System.Threading;
using Content.Server.UserInterface;
using Content.Shared.Forensics;
using Robust.Server.GameObjects;

namespace Content.Server.Forensics
{
    [RegisterComponent]
    public sealed class ForensicScannerComponent : Component
    {
        public CancellationTokenSource? CancelToken;

        public List<string> Fingerprints = new();
        public List<string> Fibers = new();

        [DataField("scanDelay")]
        public float ScanDelay = 3.0f;
    }
}
