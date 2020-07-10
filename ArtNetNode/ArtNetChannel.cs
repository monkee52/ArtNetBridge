using System;
using System.Collections.Generic;
using System.Text;

namespace AydenIO.ArtNet.Node {
    public class ArtNetChannel {
        /// <value>A reference to the associated <c>ArtNetUniverse</c> for this channel</value>
        public ArtNetUniverse Universe { get; private set; }

        /// <value>The channel index</value>
        public ushort ChannelId { get; private set; }

        private byte? underlyingValue = null;

        /// <value>Returns the current DMX value of the channel</value>
        public byte Value => this.underlyingValue ?? 0;

        /// <summary>
        /// Updates the DMX value of the channel, calling event handlers as needed
        /// </summary>
        /// <param name="newValue">The new DMX value</param>
        /// <param name="sequenceCounter">A sequence counter to be used to prevent a handler being called multiple times if it's watching the same channels.</param>
        internal void SetValue(byte newValue, int sequenceCounter) {
            // Ensure value has changed
            if (this.underlyingValue != newValue) {
                // Get old value
                byte oldValue = this.Value;

                // Update internal state
                this.underlyingValue = newValue;

                // Trigger event handlers
                this.ValueChanged?.Invoke(this, new ArtNetValueChangedEventArgs(oldValue, newValue, sequenceCounter));
            }
        }

        /// <summary>
        /// A handler that is called whenever the DMX value has changed
        /// </summary>
        public event EventHandler<ArtNetValueChangedEventArgs> ValueChanged;

        /// <summary>
        /// Creates an <c>ArtNetChannel</c>
        /// </summary>
        /// <param name="universe">The <c>ArtNetUniverse</c> the channel belongs to</param>
        /// <param name="channel">The channel index</param>
        protected internal ArtNetChannel(ArtNetUniverse universe, ushort channel) {
            this.Universe = universe;
            this.ChannelId = channel;
        }
    }
}
