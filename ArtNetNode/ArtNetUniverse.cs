using System;
using System.Collections.Generic;
using System.Text;

namespace AydenIO.ArtNet.Node {
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
}
