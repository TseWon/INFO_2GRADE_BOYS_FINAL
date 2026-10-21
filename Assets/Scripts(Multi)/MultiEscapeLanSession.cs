using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace FacilityEscape
{
    // Direct LAN connection. Both peers load MultiScene1; only the host simulates the puzzles.
    [DefaultExecutionOrder(-50), DisallowMultipleComponent]
    public sealed class MultiEscapeLanSession : MonoBehaviour
    {
        [SerializeField] private MultiEscapeGame game;
        [SerializeField] private MultiEscapePlayer playerA, playerB;
        [SerializeField] private ushort port = 7777;
        private const string StateMessage = "Facility.State.v1", PoseMessage = "Facility.Pose.v1";
        private const string Protocol = "FacilityEscape-LAN-1";
        private NetworkManager manager;
        private ulong peerId = ulong.MaxValue;
        private bool stopping, receivedState, savedBackground;
        private double connectingSince, nextSend;
        private Vector3 remotePosition;
        private Quaternion remoteRotation;
        private bool hasRemotePose;
        public event Action Changed;
        public bool IsHost => manager != null && manager.IsHost && !stopping;
        public bool IsBusy => manager != null;
        public bool Connected => manager != null && !stopping && (manager.IsHost
            ? manager.ConnectedClientsIds.Count == 2 : manager.IsConnectedClient && receivedState);
        public bool CanStartRound => IsHost && Connected;
        public EscapeRole LocalRole { get; private set; } = EscapeRole.Blindfolded;
        public string Status { get; private set; } = "Create a room, or enter your friend's host IP.";
        public string LocalAddresses { get; private set; }
        public ushort Port => port;

        private void Awake()
        {
            game.Lan = this;
            game.onRoundStarted.AddListener(ResetRemotePose);
            savedBackground = Application.runInBackground;
            Application.runInBackground = true;
            try
            {
                LocalAddresses = string.Join(" / ", NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                    .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                    .Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork)
                    .Select(a => a.Address.ToString()).Distinct());
            }
            catch (NetworkInformationException) { LocalAddresses = "Check IPv4 in Windows Wi-Fi settings"; }
            if (string.IsNullOrEmpty(LocalAddresses)) LocalAddresses = "No Wi-Fi IPv4 address found";
        }

        public void Host() => Begin(true, "127.0.0.1");
        private void ResetRemotePose() => hasRemotePose = false;
        public void Join(string address)
        {
            if (!IPAddress.TryParse(address.Trim(), out var ip) || ip.AddressFamily != AddressFamily.InterNetwork)
            { Status = "Enter the host's IPv4 address, for example 192.168.0.10."; Changed?.Invoke(); return; }
            Begin(false, ip.ToString());
        }

        private void Begin(bool host, string address)
        {
            if (IsBusy || !Application.isPlaying) return;
            LocalRole = host ? EscapeRole.Blindfolded : EscapeRole.Observer;
            receivedState = hasRemotePose = false;
            peerId = ulong.MaxValue;
            game.ReturnToLobby();
            var networkRoot = new GameObject("Facility_LAN_Runtime");
            manager = networkRoot.AddComponent<NetworkManager>();
            var transport = networkRoot.AddComponent<UnityTransport>();
            transport.SetConnectionData(address, port, "0.0.0.0");
            manager.NetworkConfig = new NetworkConfig {
                NetworkTransport = transport, EnableSceneManagement = false,
                ConnectionApproval = true, ForceSamePrefabs = false, ProtocolVersion = 1,
                ConnectionData = Encoding.UTF8.GetBytes(Protocol), TickRate = 20
            };
            manager.ConnectionApprovalCallback = Approve;
            manager.OnClientConnectedCallback += OnConnected;
            manager.OnClientDisconnectCallback += OnDisconnected;
            manager.OnTransportFailure += TransportFailed;
            Status = host ? "Waiting for B. Host IP: " + LocalAddresses : "Connecting to " + address + "...";
            connectingSince = Time.realtimeSinceStartupAsDouble;
            bool started = host ? manager.StartHost() : manager.StartClient();
            if (!started) { Leave("Could not open the connection. Check port 7777 and try again."); return; }
            manager.CustomMessagingManager.RegisterNamedMessageHandler(StateMessage, ReceiveState);
            manager.CustomMessagingManager.RegisterNamedMessageHandler(PoseMessage, ReceivePose);
            Changed?.Invoke();
        }

        private void Approve(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
        {
            bool matching = Encoding.UTF8.GetString(request.Payload) == Protocol;
            bool hostSelf = request.ClientNetworkId == NetworkManager.ServerClientId;
            response.Approved = matching && (hostSelf || peerId == ulong.MaxValue) && game.Rules.Stage == EscapeStage.Ready;
            if (response.Approved && !hostSelf) peerId = request.ClientNetworkId;
            response.CreatePlayerObject = false;
            response.Pending = false;
            response.Reason = matching ? "This two-player room is full or already playing." : "Use the same game build as the host.";
        }

        private void OnConnected(ulong id)
        {
            if (manager.IsHost && manager.ConnectedClientsIds.Count == 2)
                Status = "A + B connected. Host can start the round.";
            else if (!manager.IsHost) Status = "Connected. Waiting for the host to start.";
            nextSend = 0;
            Changed?.Invoke();
        }

        private void OnDisconnected(ulong id)
        {
            if (stopping || manager == null) return;
            // A rejected third connection is not a departure of our B player.
            if (manager.IsHost && id != peerId) return;
            string reason = manager.DisconnectReason;
            Leave(reason == "This two-player room is full or already playing." || reason == "Use the same game build as the host."
                ? reason : "Connection lost. Both players return to the lobby. Create or join again.");
        }

        private void TransportFailed() => Leave("Network connection failed. Check Wi-Fi, host IP and firewall access.");
        public void Leave() => Leave("Left the room. Create a new room or join your friend.");
        private void Leave(string message)
        {
            if (stopping) return;
            Status = message;
            stopping = manager != null;
            receivedState = hasRemotePose = false;
            if (manager != null) manager.Shutdown();
            game.ReturnToLobby();
            Changed?.Invoke();
        }

        private void Update()
        {
            if (manager == null) return;
            if (stopping)
            {
                if (!manager.ShutdownInProgress)
                {
                    Destroy(manager.gameObject);
                    manager = null;
                    stopping = false;
                    Changed?.Invoke();
                }
                return;
            }
            if (!manager.IsHost && !Connected && Time.realtimeSinceStartupAsDouble - connectingSince > 12)
                Leave("Connection timed out. Check the host IP, same Wi-Fi, and Windows private-network firewall access.");
        }

        private void LateUpdate()
        {
            if (manager == null || stopping) return;
            if (hasRemotePose)
            {
                var remote = IsHost ? playerB : playerA;
                var motor = remote.GetComponent<CharacterController>();
                if (motor != null) motor.enabled = false;
                remote.transform.position = Vector3.Lerp(remote.transform.position, remotePosition,
                    1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
                remote.ViewCamera.transform.rotation = Quaternion.Slerp(remote.ViewCamera.transform.rotation,
                    remoteRotation, 1 - Mathf.Exp(-18 * Time.unscaledDeltaTime));
            }
            double now = Time.realtimeSinceStartupAsDouble;
            if (now < nextSend) return;
            nextSend = now + .05;
            if (IsHost && Connected)
            {
                ulong peer = manager.ConnectedClientsIds.First(id => id != NetworkManager.ServerClientId);
                Send(StateMessage, peer, JsonUtility.ToJson(game.CaptureSnapshot()));
                SendPose(peer, playerA);
            }
            else if (!IsHost && manager.IsConnectedClient) SendPose(NetworkManager.ServerClientId, playerB);
        }

        private void Send(string name, ulong recipient, string json)
        {
            using (var writer = new FastBufferWriter(8192, Allocator.Temp))
            {
                writer.WriteValueSafe(json);
                manager.CustomMessagingManager.SendNamedMessage(name, recipient, writer,
                    NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        [Serializable]
        private sealed class Pose
        {
            public int round;
            public Vector3 position;
            public Quaternion rotation;
        }
        private void SendPose(ulong recipient, MultiEscapePlayer player) => Send(PoseMessage, recipient,
            JsonUtility.ToJson(new Pose { round = game.RoundNumber, position = player.transform.position,
                rotation = player.ViewCamera.transform.rotation }));

        private void ReceiveState(ulong sender, FastBufferReader reader)
        {
            if (IsHost || sender != NetworkManager.ServerClientId || stopping) return;
            reader.ReadValueSafe(out string json);
            var state = JsonUtility.FromJson<MultiEscapeSnapshot>(json);
            if (state.round != game.RoundNumber) hasRemotePose = false;
            game.ApplyRemoteSnapshot(state);
            if (!receivedState) { receivedState = true; Changed?.Invoke(); }
        }

        private void ReceivePose(ulong sender, FastBufferReader reader)
        {
            if (stopping || (IsHost ? !manager.ConnectedClients.ContainsKey(sender) || sender == NetworkManager.ServerClientId
                : sender != NetworkManager.ServerClientId)) return;
            reader.ReadValueSafe(out string json);
            var pose = JsonUtility.FromJson<Pose>(json);
            if (pose.round != game.RoundNumber || !Finite(pose.position.x) || !Finite(pose.position.y) || !Finite(pose.position.z)
                || !Finite(pose.rotation.x) || !Finite(pose.rotation.y) || !Finite(pose.rotation.z) || !Finite(pose.rotation.w)) return;
            var remote = IsHost ? playerB : playerA;
            // Bound players can turn their heads, but cannot leave their chairs.
            remotePosition = game.Rules.Stage == EscapeStage.Escaped ? pose.position : remote.transform.position;
            remotePosition = new Vector3(Mathf.Clamp(remotePosition.x, -5, 5), Mathf.Clamp(remotePosition.y, -1, 3), Mathf.Clamp(remotePosition.z, -4, 10));
            remoteRotation = pose.rotation.normalized;
            hasRemotePose = true;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

        private void OnDestroy()
        {
            stopping = true;
            if (manager != null) { manager.Shutdown(); Destroy(manager.gameObject); }
            if (game != null && game.Lan == this) game.Lan = null;
            if (game != null) game.onRoundStarted.RemoveListener(ResetRemotePose);
            Application.runInBackground = savedBackground;
        }
    }
}
