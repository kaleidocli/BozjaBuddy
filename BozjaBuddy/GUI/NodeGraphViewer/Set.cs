using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Common.Math;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BozjaBuddy.GUI.NodeGraphViewer
{
    internal class Set
    {
        private HashSet<SubSet> subSets = new();
        public float minBound { get; private set; } = 0;
        public float maxBound { get; private set; } = 0;
        public float minBoundLarger { get; private set; } = 0;
        public float maxBoundSmaller { get; private set; } = 0;

        public bool Contains(float val)
        {
            foreach (SubSet s in subSets)
            {
                if (!s.Contains(val)) return false;
            }
            return true;
        }
        /// <summary>
        /// <para>Add a range of value to the Set.</para>
        /// <para>Return false if the range is overlapped with the set already, otherwise true.</para>
        /// </summary>
        public bool AddRange(float negativeSide, float positiveSide)
        {
            if (!this.UnionIfOverlap(negativeSide, positiveSide))
            {
                SubSet A = new(negativeSide, positiveSide);
                this.subSets.Add(A);
                this.UpdateBounds(A);
                return true;
            }
            return false;
        }
        /// <summary>
        /// <para>Remove a range of value from the Set.</para>
        /// <para>Return false if nothing is removed.</para>
        /// </summary>
        public bool RemoveRange(float negativeSide, float positiveSide)
        {
            var res = this.DifferenceIfOverlap(negativeSide, positiveSide);
            return res;
        }
        public bool IsOverlap(float negativeSide, float positiveSide)
        {
            foreach (SubSet B in subSets)
            {
                if (B.IsOverlap(negativeSide, positiveSide) != OverlapType.None) return true;
            }
            return false;
        }
        public void ResetRange()
        {
            this.subSets.Clear();
            this.minBound = 0;
            this.maxBound = 0;
            this.minBoundLarger = 0;
            this.maxBoundSmaller = 0;
        }
        private bool UnionIfOverlap(float negativeSide, float positiveSide)
        {
            var A = new SubSet(negativeSide, positiveSide);
            SubSet? chosen = null;
            List<SubSet> garbo = new();
            foreach (SubSet B in this.subSets)
            {
                if (B.UnionIfOverlap(A))
                {
                    if (chosen == null)
                        chosen = B;
                    else
                        garbo.Add(chosen);
                }
            }
            if (chosen == null) return false;
            else
            {
                foreach (SubSet g in garbo)
                {
                    chosen.UnionIfOverlap(g);
                    this.subSets.Remove(g);
                }
                this.UpdateBounds();
                return true;
            }
        }
        private bool DifferenceIfOverlap(float negativeSide, float positiveSide)
        {
            var A = new SubSet(negativeSide, positiveSide);
            bool res = false;
            List<SubSet> garbo = new();
            List<SubSet> recruits = new();
            foreach (SubSet B in this.subSets)
            {
                if (B.DifferentIfOverlap(A, out var splitSets, out var isDeleted))
                {
                    if (isDeleted)
                    {
                        garbo.Add(B);
                    }
                    else if (splitSets != null)
                    {
                        garbo.Add(B);
                        recruits.Add(splitSets.Item1);
                        recruits.Add(splitSets.Item2);
                    }
                    res = true;
                }
            }
            foreach (var g in garbo)
            {
                this.subSets.Remove(g);
            }
            foreach (var r in recruits)
            {
                this.subSets.Add(r);
            }
            if (res) this.UpdateBounds();
            return res;
        }

        

        private void UpdateBounds()
        {
            foreach (var s in subSets)
            {
                this.UpdateBounds(s);
            }
        }
        private void UpdateBounds(SubSet A)
        {
            this.minBound = A.negativeSide < this.minBound
                                ? A.negativeSide
                                : this.minBound;
            this.maxBound = A.positiveSide > this.maxBound
                            ? A.positiveSide
                            : this.maxBound;
            this.minBoundLarger = A.negativeSide < this.minBound
                                         ? A.positiveSide
                                         : this.minBoundLarger;
            this.maxBoundSmaller = A.positiveSide > this.maxBound
                                        ? A.negativeSide
                                        : this.maxBoundSmaller;
        }
        /// <summary>
        /// <para>Find the two subsets with greatest distance between. Get the endpoints that made the distance, each endpoint from each subset.</para>
        /// <para>upperBound, lowerBound params define the range where the search will be.</para>
        /// </summary>
        public Tuple<float, float>? FindTwoFurthestEndpoints(float upperBound, float lowerBound)
        {
            List<float> endpoints = new();
            foreach (var s in subSets)
            {
                // check if the search range is within a subset
                if (s.negativeSide < lowerBound && s.positiveSide > upperBound) return null;
                // update upperBound/lowerBound if it's within a subset
                // OR the subset is out of search range
                if (s.negativeSide < lowerBound)
                {
                    if (s.positiveSide > lowerBound) lowerBound = s.positiveSide;
                    continue;
                }
                else if (s.positiveSide > upperBound)
                {
                    if (s.negativeSide < upperBound) upperBound = s.negativeSide;
                    continue;
                }
                endpoints.Add(s.negativeSide);
                endpoints.Add(s.positiveSide);
            }
            endpoints.Add(upperBound);
            endpoints.Add(lowerBound);
            endpoints.Sort();
            float? e1 = null;
            float? e2 = null;
            for (int i = 0; i < endpoints.Count / 2; i += 2)
            {
                if (e1 == null) e1 = endpoints[i];
                if (e2 == null) e2 = endpoints[i + 1];

                if (endpoints[i + 1] - endpoints[i] > (e2.Value - e1.Value))
                {
                    e1 = endpoints[i];
                    e2 = endpoints[i + 1];
                }
            }
            if (!(e1.HasValue && e2.HasValue)) return null;
            return new(e1.Value, e2.Value);
        }


        private class SubSet
        {
            public float positiveSide;
            public float negativeSide;

            private SubSet() { }
            public SubSet(float nergativeSide, float positiveSide)
            {
                this.negativeSide = nergativeSide;
                this.positiveSide = positiveSide;
            }
            public bool Contains(float val) => val > positiveSide && val < negativeSide;
            public OverlapType IsOverlap(SubSet A)
            {
                if (A.negativeSide < this.negativeSide)
                {
                    switch (A.positiveSide)
                    {
                        case var a when a < this.negativeSide: return OverlapType.None;
                        case var a when a > this.positiveSide: return OverlapType.Outside;
                        case var a when a > this.negativeSide: return OverlapType.Negative;
                    }
                }
                else if (A.negativeSide > this.positiveSide) return OverlapType.None;
                else
                {
                    switch (A.positiveSide)
                    {
                        case var a when a < this.positiveSide: return OverlapType.Inside;
                        case var a when a > this.positiveSide: return OverlapType.Positive;
                    }
                }
                return OverlapType.None;
            }
            public OverlapType IsOverlap(float negativeSide, float positiveSide) => this.IsOverlap(new SubSet(negativeSide, positiveSide));
            public bool UnionIfOverlap(SubSet A)
            {
                switch (this.IsOverlap(A))
                {
                    case var o when o == OverlapType.None: return false;
                    case var o when o == OverlapType.Negative:
                        this.negativeSide = A.negativeSide; return true;
                    case var o when o == OverlapType.Positive:
                        this.positiveSide = A.positiveSide; return true;
                    case var o when o == OverlapType.Outside:
                        this.negativeSide = A.negativeSide;
                        this.positiveSide = A.positiveSide;
                        return true;
                    case var o when o == OverlapType.Inside: return true;
                }
                return false;
            }
            public bool DifferentIfOverlap(SubSet A, out Tuple<SubSet, SubSet>? splitSets, out bool isDeleted)
            {
                splitSets = null;
                isDeleted = false;
                switch (this.IsOverlap(A))
                {
                    case var o when o == OverlapType.None: return false;
                    case var o when o == OverlapType.Negative:
                        this.negativeSide = A.positiveSide; return true;
                    case var o when o == OverlapType.Positive:
                        this.positiveSide = A.negativeSide; return true;
                    case var o when o == OverlapType.Outside:
                        isDeleted = true;
                        return true;
                    case var o when o == OverlapType.Inside:
                        splitSets = new(
                                new SubSet(this.negativeSide, A.negativeSide),
                                new SubSet(A.positiveSide, this.positiveSide)
                              );
                        return true;
                }
                return false;
            }
        }

        public enum OverlapType
        {
            None = 0,           // No overlap
            Negative = 1,       // A overlap B on the left (negative side)
            Positive = 2,       // A overlap B on the right (positive side)
            Inside = 3,         // A fully inside B 
            Outside = 4         // A fully wrap around B (B is fully inside A)
        }
    }
}
