using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AydenIO.ArtNet.Node {
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
