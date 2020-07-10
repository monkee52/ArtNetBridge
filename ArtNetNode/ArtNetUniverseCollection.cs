using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AydenIO.ArtNet.Node {
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
}
