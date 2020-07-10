using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AydenIO.ArtNet.Node {
    /// <summary>
    /// A collection of <c>ArtNetUniverse</c>s
    /// </summary>
    public class ArtNetUniverseCollection : IReadOnlyCollection<ArtNetUniverse> {
        /// <summary>
        /// The <c>ArtNetNode</c> that the universe collection belongs to
        /// </summary>
        public ArtNetNode Node { get; private set; }

        private IDictionary<int, ArtNetUniverse> universes;
        private IEnumerable<int> allowedUniverses;

        private object syncRoot;

        /// <summary>
        /// Create a collection of <c>ArtNetUniverse</c>s
        /// </summary>
        /// <param name="node">The <c>ArtNetNode</c> the collection belongs to</param>
        /// <param name="allowedUniverses">A list of universes that are allowed to be received</param>
        internal ArtNetUniverseCollection(ArtNetNode node, IEnumerable<int> allowedUniverses) {
            this.Node = node;

            this.universes = new Dictionary<int, ArtNetUniverse>();
            this.allowedUniverses = allowedUniverses;

            this.syncRoot = new object();
        }

        /// <summary>
        /// Check if a given universe index is allowed
        /// </summary>
        /// <param name="index">The universe index</param>
        /// <returns>Whether the index is allowed</returns>
        private bool UniverseAllowed(int index) {
            // Null indicates all universes are allowed
            if (this.allowedUniverses == null) {
                return true;
            }

            return this.allowedUniverses.Contains(index);
        }

        /// <summary>
        /// Get a universe given its index
        /// </summary>
        /// <param name="index">The universe index</param>
        /// <returns>The <c>ArtNetUniverse</c> for the given index</returns>
        public ArtNetUniverse this[int index] {
            get {
                ArtNetUniverse universe = null;

                // Concurrent dictionary
                lock (this.syncRoot) {
                    // Check if universe already exists
                    if (this.universes.ContainsKey(index)) {
                        universe = this.universes[index];
                    } else if (!this.universes.ContainsKey(index) && this.UniverseAllowed(index)) { // Create universe
                        universe = new ArtNetUniverse(this.Node, index);

                        this.universes[index] = universe;
                    }
                }

                // Return universe, or null if universe is not allowed
                return universe;
            }
        }

        public int Count => this.universes.Count;

        public IEnumerator<ArtNetUniverse> GetEnumerator() {
            return this.universes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return ((IEnumerable)this.universes.Values).GetEnumerator();
        }
    }
}
