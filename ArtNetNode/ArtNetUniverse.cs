using System;
using System.Collections.Generic;
using System.Text;

namespace AydenIO.ArtNet.Node {
    /// <summary>
    /// Represents a DMX universe
    /// </summary>
    public class ArtNetUniverse {
        /// <summary>
        /// The <c>ArtNetNode</c> that this universe belongs to
        /// </summary>
        public ArtNetNode Node { get; private set; }

        /// <summary>
        /// The index of the universe
        /// </summary>
        public int UniverseId { get; private set; }

        private ArtNetChannel[] channels;

        /// <summary>
        /// All the channels in the universe
        /// </summary>
        public IReadOnlyList<ArtNetChannel> Channels => this.channels;

        private int updateSequenceNumber = 0;

        /// <summary>
        /// Quick way to retrieve a DMX value for a channel
        /// </summary>
        /// <param name="channel">The channel index to retrieve</param>
        /// <returns>The DMX value</returns>
        public byte this[int channel] => this.Channels[channel].Value;

        /// <summary>
        /// Creates a DMX universe
        /// </summary>
        /// <param name="node">The <c>ArtNetNode</c> that the universe belongs to</param>
        /// <param name="universe">The index of the universe</param>
        internal ArtNetUniverse(ArtNetNode node, int universe) {
            this.Node = node;
            this.UniverseId = universe;

            // Create array of channels
            this.channels = new ArtNetChannel[512];

            // Create all the channels
            for (int i = 0; i < ArtNetNode.CHANNELS_PER_UNIVERSE; i++) {
                this.channels[i] = new ArtNetChannel(this, (ushort)i);
            }
        }

        /// <summary>
        /// Updates all the channels in the universe given a byte array
        /// </summary>
        /// <param name="buffer">The byte array source</param>
        /// <param name="startAt">Where in the array to start updating from</param>
        /// <param name="length">How many channels to grab</param>
        internal void UpdateFromBuffer(byte[] buffer, int startAt, int length) {
            // Call SetValue on all channels
            for (int i = 0; i < length; i++) {
                this.channels[i].SetValue(buffer[i + startAt], updateSequenceNumber);
            }

            // Track how many times this function has been called
            this.updateSequenceNumber++;
        }
    }
}
