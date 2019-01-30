using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ArtNetDynaliteBridge {
    public class ArtNetValueChangedEventArgs : EventArgs {
        public byte Value { get; private set; }
        public byte OldValue { get; private set; }

        protected internal ArtNetValueChangedEventArgs(byte oldValue, byte value) {
            this.OldValue = oldValue;
            this.Value = value;
        }
    }

    public delegate void ArtNetValueChangedEventHandler(object sender, ArtNetValueChangedEventArgs e);

    public class ArtNetChannel {
        protected ArtNetUniverse Universe { get; private set; }
        public ushort Channel { get; private set; }

        private byte _Value;

        public byte Value {
            get {
                return this._Value;
            }

            internal set {
                if (value != this._Value) {
                    byte oldValue = this._Value;

                    this._Value = value;

                    this.ValueChanged?.Invoke(this, new ArtNetValueChangedEventArgs(oldValue, this._Value));
                }
            }
        }

        public event ArtNetValueChangedEventHandler ValueChanged;

        protected internal ArtNetChannel(ArtNetUniverse universe, ushort channel) {
            this.Universe = universe;
            this.Channel = channel;
        }
    }

    public class ArtNetUniverse {
        protected ArtNetNode Node { get; private set; }
        protected ushort Universe { get; private set; }
        public ArtNetChannel[] Channels { get; private set; }

        public byte this[ushort channel] {
            get {
                return this.Channels[channel].Value;
            }
            internal set {
                this.Channels[channel].Value = value;
            }
        }

        protected internal ArtNetUniverse(ArtNetNode node, ushort universe) {
            this.Node = node;
            this.Universe = universe;
            this.Channels = new ArtNetChannel[512];

            for (int i = 0; i < 512; i++) {
                this.Channels[i] = new ArtNetChannel(this, (ushort)i);
            }
        }

        protected internal void UpdateFromBuffer(byte[] buffer, int startAt, int length) {
            for (ushort i = 0; i < length; i++) {
                this[i] = buffer[i + startAt];
            }
        }
    }

    public class ArtNetUniverseCollection : IReadOnlyList<ArtNetUniverse> {
        protected ArtNetNode Node { get; private set; }
        private IDictionary<int, ArtNetUniverse> _Universes;

        public ArtNetUniverseCollection(ArtNetNode node) {
            this.Node = node;
            this._Universes = new Dictionary<int, ArtNetUniverse>();
        }

        public ArtNetUniverse this[int index] {
            get {
                if (!this._Universes.ContainsKey(index)) {
                    this._Universes[index] = new ArtNetUniverse(this.Node, (ushort)index);
                }

                return this._Universes[index];
            }
        }

        public int Count => this._Universes.Count;

        public IEnumerator<ArtNetUniverse> GetEnumerator() {
            return this._Universes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return this._Universes.Values.GetEnumerator();
        }
    }

    public class ArtNetNode {
        public const ushort ARTNET_PORT = 0x1936;

        private readonly byte[] ARTNET_HEADER = (new[] { 'A', 'r', 't', '-', 'N', 'e', 't', '\0' }).OfType<byte>().ToArray();

        public const ushort OPCODE_OpPoll = 0x2000;
        public const ushort OPCODE_OpPollReply = 0x2100;
        public const ushort OPCODE_OpOutput = 0x5000;

        private UdpClient Socket;
        private Thread SocketThread;
        private bool SocketLoopRunning;

        public ArtNetUniverseCollection Universes { get; private set; }

        public string Host { get; private set; }
        public ushort Port { get; private set; }

        public ArtNetNode(string host = null, ushort port = ARTNET_PORT) {
            this.Universes = new ArtNetUniverseCollection(this);

            this.Host = host;
            this.Port = port;
        }

        public void Start() {
            if (this.SocketThread != null) {
                return;
            }

            this.SocketLoopRunning = true;

            this.SocketThread = new Thread(new ThreadStart(this.SocketLoop));

            this.SocketThread.IsBackground = true;

            this.SocketThread.Start();
        }

        public void Stop() {
            if (this.SocketThread != null) {
                this.SocketLoopRunning = false;

                this.SocketThread.Join();

                this.SocketThread = null;
            }
        }

        private async void SocketLoop() {
            IPEndPoint address = new IPEndPoint(this.Host == null ? IPAddress.Any : IPAddress.Parse(this.Host), this.Port);

            this.Socket = new UdpClient(address);

            this.Socket.EnableBroadcast = true;

            //this.Socket.ExclusiveAddressUse = false;
            //this.Socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            //this.Socket.Client.Bind(address);

            while (this.SocketLoopRunning) {
                Console.Write(".");
                UdpReceiveResult result = await this.Socket.ReceiveAsync();

                // Determine if packet is art-net packet
                // Art-Net packets have a 12-byte header
                if (result.Buffer.Length < 12) {
                    continue;
                }

                // packets start with Art-Net\0
                if (!result.Buffer.Take(8).SequenceEqual(ARTNET_HEADER)) {
                    continue;
                }

                ushort opcode = BitConverter.ToUInt16(result.Buffer, 8);

                if (!BitConverter.IsLittleEndian) {
                    // Opcode is little-endian
                    opcode = (ushort)((opcode >> 8) | ((opcode & 0xff) << 8));
                }

                ushort version = BitConverter.ToUInt16(result.Buffer, 10);

                if (BitConverter.IsLittleEndian) {
                    // Version is big-endian
                    version = (ushort)((version >> 8) | ((version & 0xff) << 8));
                }

                switch (opcode) {
                    case OPCODE_OpPoll:
                        byte talkToMe = result.Buffer[12];

                        // TODO: Handle talktome

                        byte priority = result.Buffer[13];

                        break;
                    case OPCODE_OpOutput: // Handle DMX packets
                        ushort universe = BitConverter.ToUInt16(result.Buffer, 14);

                        if (!BitConverter.IsLittleEndian) {
                            // Universe is little-endian
                            universe = (ushort)((universe >> 8) | ((universe & 0xff) << 8));
                        }

                        ushort length = BitConverter.ToUInt16(result.Buffer, 16);

                        if (BitConverter.IsLittleEndian) {
                            // Length is big-endian
                            length = (ushort)((length >> 8) | ((length & 0xff) << 8));
                        }

                        this.Universes[universe].UpdateFromBuffer(result.Buffer, 18, length);

                        break;
                    default:
                        Console.WriteLine("Unknown opcode: {0}", opcode);
                        break;
                }
            }

            this.Socket.Close();

            this.Socket = null;
        }
    }
}
