using g3;
using netDxf;
using netDxf.Tables;
using System.Collections.Generic;
using System.Linq;

namespace DigitalStoneOptimizer
{
    public class MillingContext
    {
        public MillingContext(ApproximatedStone s, float toolDiameter)
        {
            /* Idea: sort all sections by their area and then try to fit them into each other. */
            _SortedSections = new ApproximatedStone(s.Sections.OrderByDescending(x => x.Poly.Bounds.Area));
            ToolDiameter = toolDiameter;
        }

        #region Properties

        public float ToolDiameter { get; }
        public List<PositionedStoneSection[]> PositionedSections { get; private set; }
        public List<StoneSection> UnableToFit { get; private set; }
        public int TotalSheets { get => PositionedSections.Count + UnableToFit.Count; }
        public float TotalStockVolume { get => _MaxSheetArea.Sum(x => x.Value * x.Key * _TotalSheets[x.Key]); }
        public float VolumeEfficiency { get => _UsefulVolume / TotalStockVolume; }
        public IEnumerable<IEnumerable<float>> PositionedElevations
        {
            get => PositionedSections.Select(x => x.Select(x => x.Model.Elevation));
        }
        public IEnumerable<float> NonFitElevations
        {
            get => UnableToFit.Select(x => x.Elevation);
        }
        public float CompactizationFactor { get; private set; }

        #endregion

        public void Calculate(int numberOfStonesToManufacture = 1)
        {
            _UsefulVolume = 0;
            _MaxSheetArea = new Dictionary<float, float>();
            _TotalSheets = new Dictionary<float, int>();
            PositionedSections = new List<PositionedStoneSection[]>();
            UnableToFit = new List<StoneSection>();
            Dictionary<float, List<StoneSection>> sections = new Dictionary<float, List<StoneSection>>();
            for (int i = 0; i < _SortedSections.Sections.Length; i++)
            {
                var item = _SortedSections.Sections[i];
                var bArea = (float)item.Poly.Bounds.Area;
                if (!_MaxSheetArea.ContainsKey(item.Thickness)) _MaxSheetArea.Add(item.Thickness, bArea);
                else if(bArea > _MaxSheetArea[item.Thickness]) _MaxSheetArea[item.Thickness] = bArea;
                bool contains = sections.TryGetValue(item.Thickness, out List<StoneSection> currentPool);
                if (!contains)
                {
                    var val = new List<StoneSection>();
                    sections.Add(item.Thickness, val);
                    currentPool = val;
                }
                currentPool.AddRange(Enumerable.Repeat(item, numberOfStonesToManufacture));
            }
            foreach (var item in sections)
            {
                CalculateThicknessBin(item.Value, item.Key);
            }
            CompactizationFactor = (float)_SortedSections.Sections.Length * numberOfStonesToManufacture / TotalSheets;
        }

        public void DrawDxf(DxfDocument doc)
        {
            Layer l = new Layer("Positioned Stone Sections");
            doc.Layers.Add(l);
            float xOffset = 0;
            foreach (var group in PositionedSections)
            {
                foreach (var item in group)
                {
                    item.DrawDxf(doc, l, l, 0, xOffset);
                }
                xOffset += (float)(group[0].Model.Poly.Bounds.Diagonal.x * 1.2);
            }
            l = new Layer("Unable to Fit") { Color = AciColor.Yellow };
            doc.Layers.Add(l);
            float extraElev = 0;
            foreach (var item in UnableToFit)
            {
                new PositionedStoneSection(item, 0).DrawDxf(doc, l, l, extraElev, xOffset);
                xOffset += (float)(item.Poly.Bounds.Diagonal.x * 1.1);
                extraElev += item.Thickness;
            }
        }

        #region Private

        private readonly ApproximatedStone _SortedSections;
        private Dictionary<float, float> _MaxSheetArea; //Per thickness
        private Dictionary<float, int> _TotalSheets;
        private float _UsefulVolume;

        private void CalculateThicknessBin(List<StoneSection> sections, float thickness)
        {
            List<PositionedStoneSection> currentBin = new List<PositionedStoneSection>(_SortedSections.Sections.Length);
            float currentElevation = 0;
            while (sections.Any())
            {
                currentBin.Add(new PositionedStoneSection(sections[0], currentElevation));
                sections.RemoveAt(0);
                for (int i = 0; i < sections.Count; i++)
                {
                    var last = currentBin.Last().Model.Poly;
                    if (last.Holes.Count == 0) break;
                    var item = sections[i];
                    if (last.Holes[0].FitsWithShift(item.Poly.Outer, out Vector2f shift, ToolDiameter))
                    {
                        currentBin.Add(new PositionedStoneSection(item, currentElevation, shift));
                        _UsefulVolume += (float)(item.Poly.Outer.Area - item.Poly.HoleArea) * item.Thickness;
                        sections.RemoveAt(i--);
                    }
                }
                if (currentBin.Count > 1)
                {
                    PositionedSections.Add(currentBin.ToArray());
                    currentElevation += thickness;
                }
                else
                {
                    UnableToFit.Add(currentBin[0].Model);
                }
                if (!_TotalSheets.ContainsKey(thickness)) _TotalSheets.Add(thickness, 1);
                else _TotalSheets[thickness] += 1;
                currentBin.Clear();
            }
        }

        #endregion
    }
}
