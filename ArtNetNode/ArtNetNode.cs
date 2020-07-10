using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace AydenIO.ArtNet.Node {
    public class ArtNetNode : IDisposable {
        public const ushort ARTNET_PORT = 0x1936;
        public const ushort VERSION = 0x0001;

        public const int CHANNELS_PER_UNIVERSE = 512;

        private readonly byte[] ARTNET_HEADER = (new[] { 'A', 'r', 't', '-', 'N', 'e', 't', '\0' }).OfType<byte>().ToArray();

        private UdpClient socket;
        private Thread socketThread;

        /// <summary>
        /// Returns the collection of universes this node handles
        /// </summary>
        public ArtNetUniverseCollection Universes { get; private set; }

        /// <summary>
        /// Returns an <c>ArtNetUniverse</c> given a DMX universe index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ArtNetUniverse this[int index] => this.Universes[index];

        /// <summary>
        /// The <c>IPEndPoint</c> the node is listening on
        /// </summary>
        public IPEndPoint LocalEndPoint { get; private set; }

        private object syncRoot;

        /// <summary>
        /// Create an <c>ArtNetNode</c>
        /// </summary>
        /// <param name="endPoint">The <c>IPEndPoint</c> for the node to listen on</param>
        /// <param name="allowedUniverses">An <c>IEnumerable&lt;int&gt;</c> of universes to listen for</param>
        public ArtNetNode(IPEndPoint endPoint, IEnumerable<int> allowedUniverses = null) {
            this.Initialize(endPoint, allowedUniverses);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host">The <c>IPAddress</c> for the node to listen on</param>
        /// <param name="port">The port for the node to listen on</param>
        /// <param name="allowedUniverses">An <c>IEnumerable&lt;int&gt;</c> of universes to listen for</param>
        public ArtNetNode(IPAddress host, ushort port = ARTNET_PORT, IEnumerable<int> allowedUniverses = null) {
            this.Initialize(new IPEndPoint(host, port), allowedUniverses);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="host">The IP address string for the node to listen on</param>
        /// <param name="port">The port for the node to listen on</param>
        /// <param name="allowedUniverses">An <c>IEnumerable&lt;int&gt;</c> of universes to listen for</param>
        public ArtNetNode(string host = null, ushort port = ARTNET_PORT, IEnumerable<int> allowedUniverses = null) {
            // Handle null host
            IPAddress address = host == null ? IPAddress.Any : IPAddress.Parse(host);

            this.Initialize(new IPEndPoint(address, port), allowedUniverses);
        }

        // Prevent double initialization
        private bool initialized = false;

        /// <summary>
        /// Initializes the object
        /// </summary>
        /// <param name="endPoint">The <c>IPEndPoint</c> for the node to listen on</param>
        /// <param name="allowedUniverses">An <c>IEnumerable&lt;int&gt;</c> of universes to listen for</param>
        private void Initialize(IPEndPoint endPoint, IEnumerable<int> allowedUniverses = null) {
            if (!this.initialized) {
                // Prevent double initialization
                this.initialized = true;

                // Create universes
                this.Universes = new ArtNetUniverseCollection(this, allowedUniverses);

                // Set up properties and fields
                this.LocalEndPoint = endPoint;

                this.syncRoot = new object();
            }
        }

        /// <summary>
        /// Begin listening for ArtNet packets
        /// </summary>
        /// <returns>Whether the call started the listener thread</returns>
        public bool Start() {
            // Prevent double start
            lock (this.syncRoot) {
                // Check to make sure no thread is already running
                if (this.socketThread != null) {
                    return false;
                }

                // Create thread
                this.socketThread = new Thread(new ThreadStart(this.SocketLoop)) {
                    IsBackground = true,
                    Name = nameof(this.socketThread)
                };

                // Start thread
                this.socketThread.Start();

                return true;
            }
        }

        /// <summary>
        /// Stop listening for ArtNet packets
        /// </summary>
        /// <returns>Whether the call stopped the listener thread</returns>
        public bool Stop() {
            // Prevent double stop
            lock (this.syncRoot) {
                // Check to make sure there is a thread running
                if (this.socketThread == null) {
                    return false;
                }

                // Close socket
                this.socket.Close();
                this.socket.Dispose();
                this.socket = null;

                // Wait for thread to finish
                this.socketThread.Join();

                this.socketThread = null;

                return true;
            }
        }

        /// <summary>
        /// The listener thread function
        /// </summary>
        private void SocketLoop() {
            // Set up socket
            this.socket = new UdpClient {
                EnableBroadcast = true,
                ExclusiveAddressUse = false
            };

            // Allow multiple nodes to listen on the same port
            this.socket.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            // Bind to the port
            this.socket.Client.Bind(this.LocalEndPoint);

            // Forever
            while (true) {
                // Receive UDP datagram
                IPEndPoint endPoint = null;
                byte[] buffer;

                try {
                    buffer = this.socket.Receive(ref endPoint);
                } catch (ObjectDisposedException) {
                    break;
                } catch (SocketException e) when (e.SocketErrorCode == SocketError.TimedOut) {
                    continue;
                } catch (SocketException e) when (e.SocketErrorCode == SocketError.Interrupted) {
                    break;
                }

                // Determine if packet is art-net packet
                // Art-Net packets have a 12-byte header
                if (buffer.Length < 12) {
                    continue;
                }

                // packets start with Art-Net\0
                if (!buffer.Take(ARTNET_HEADER.Length).SequenceEqual(ARTNET_HEADER)) {
                    continue;
                }

                // Get opcode
                ushort opcodeRaw = BitConverter.ToUInt16(buffer, 8);

                if (!BitConverter.IsLittleEndian) {
                    // Opcode is little-endian
                    opcodeRaw = (ushort)((opcodeRaw >> 8) | ((opcodeRaw & 0xff) << 8));
                }

                ArtNetOpCode opcode = (ArtNetOpCode)opcodeRaw;

                // Get version
                ushort version = BitConverter.ToUInt16(buffer, 10);

                if (BitConverter.IsLittleEndian) {
                    // Version is big-endian
                    version = (ushort)((version >> 8) | (version << 8));
                }

                // Determine what to do with remaining data
                switch (opcode) {
                    case ArtNetOpCode.OpOutput: // Handle DMX packets
                        ushort universe = BitConverter.ToUInt16(buffer, 14);

                        if (!BitConverter.IsLittleEndian) {
                            // Universe is little-endian
                            universe = (ushort)((universe >> 8) | ((universe & 0xff) << 8));
                        }

                        ushort length = BitConverter.ToUInt16(buffer, 16);

                        if (BitConverter.IsLittleEndian) {
                            // Length is big-endian
                            length = (ushort)((length >> 8) | ((length & 0xff) << 8));
                        }

                        this.Universes[universe].UpdateFromBuffer(buffer, 18, length);

                        break;
                    default:
                        // TODO: ???
                        break;
                }
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    // TODO: dispose managed state (managed objects).
                    ((IDisposable)this.socket)?.Dispose();
                    this.socket = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~ArtNetNode()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose() {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
