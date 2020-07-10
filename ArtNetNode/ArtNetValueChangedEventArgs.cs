using System;
using System.Collections.Generic;
using System.Text;

namespace AydenIO.ArtNet.Node {
    /// <summary>
    /// Used in event handlers to notify listeners of a change in DMX value
    /// </summary>
    public class ArtNetValueChangedEventArgs : EventArgs {
        /// <value>Gets the new/current value of the DMX channel</value>
        public byte Value { get; private set; }

        /// <value>Gets the previous value of the DMX channel</value>
        public byte OldValue { get; private set; }

        /// <value>Used to track which update the change belongs to</value>
        public int UpdateSequenceNumber { get; private set; }

        /// <summary>
        /// Creates an event arguments object for DMX value changed event handlers
        /// </summary>
        /// <param name="oldValue">The previous value</param>
        /// <param name="value">The new/current value</param>
        /// <param name="updateSequenceNumber">Which update the change belongs to</param>
        internal ArtNetValueChangedEventArgs(byte oldValue, byte value, int updateSequenceNumber) {
            this.OldValue = oldValue;
            this.Value = value;
            this.UpdateSequenceNumber = updateSequenceNumber;
        }
    }
}
