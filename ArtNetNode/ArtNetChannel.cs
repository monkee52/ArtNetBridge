using System;
using System.Collections.Generic;
using System.Text;

namespace AydenIO.ArtNet.Node {
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

        public event EventHandler<ArtNetValueChangedEventArgs> ValueChanged;

        protected internal ArtNetChannel(ArtNetUniverse universe, ushort channel) {
            this.Universe = universe;
            this.Channel = channel;
        }
    }
}
