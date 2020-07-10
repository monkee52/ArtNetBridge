using System;
using System.Collections.Generic;
using System.Text;

namespace AydenIO.ArtNet.Node {
    public class ArtNetValueChangedEventArgs : EventArgs {
        public byte Value { get; private set; }
        public byte OldValue { get; private set; }

        protected internal ArtNetValueChangedEventArgs(byte oldValue, byte value) {
            this.OldValue = oldValue;
            this.Value = value;
        }
    }
}
