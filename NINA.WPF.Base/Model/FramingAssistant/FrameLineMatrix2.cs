#region "copyright"

/*
    Copyright � 2016 - 2024 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using NINA.Astrometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace NINA.WPF.Base.Model.FramingAssistant {

    public class FrameLineMatrix2 : IDisposable {
        private const double MAXDEC = 89.999;
        private static SolidBrush gridAnnotationBrush = new SolidBrush(System.Drawing.Color.SteelBlue);
        private static Font gridAnnotationFont = new Font("Segoe UI", 7, System.Drawing.FontStyle.Italic);
        private static System.Drawing.Pen gridPen = new System.Drawing.Pen(Color.FromArgb(127, System.Drawing.Color.SteelBlue));
        private double currentDecStep;
        private double currentRAStep;
        private ViewportFoV currentViewport;
        private Dictionary<double, List<Coordinates>> decCoordinateMatrix = new Dictionary<double, List<Coordinates>>();
        private object lockObj = new object();
        private Dictionary<double, List<Coordinates>> raCoordinateMatrix = new Dictionary<double, List<Coordinates>>();
        private double resolution;
        private List<double> RA_STEPSIZES = new List<double>() { 1.25, 2.5, 3.75, 7.5, 15 };
        private List<double> DEC_STEPSIZES = new List<double>() { 0.5, 1, 2, 4, 12, 20 };

        public FrameLineMatrix2() {
            RAPoints = new List<FrameLine>();
            DecPoints = new List<FrameLine>();
        }
        public List<FrameLine> DecPoints { get; private set; }

        public List<FrameLine> RAPoints { get; private set; }

        public void CalculatePoints(ViewportFoV viewport) {
            lock (lockObj) {
                this.currentViewport = viewport;
                DetermineStepSizes();

                RAPoints.Clear();
                DecPoints.Clear();

                for(double ra = 0; ra < 360; ra += currentRAStep) {
                    if(currentViewport.ContainsCoordinates(ra, currentViewport.CenterCoordinates.Dec)) {
                        CalculateRAPoints(ra);
                    }                    
                }
                for (double dec = 0; dec < MAXDEC; dec += currentDecStep) {
                    if (currentViewport.ContainsCoordinates(currentViewport.CenterCoordinates.RADegrees, dec)) {
                        CalculateDecPoints(dec);
                    }
                    if (currentViewport.ContainsCoordinates(currentViewport.CenterCoordinates.RADegrees, -dec)) {
                        CalculateDecPoints(-dec);
                    }
                        
                }
            }
        }
        public void Draw(Graphics g) {
            lock (lockObj) {
                foreach (var frameLine in this.RAPoints) {
                    DrawRALineCollection(g, frameLine);
                }

                foreach (var frameLine in this.DecPoints) {
                    DrawDecLineCollection(g, frameLine);
                }
            }
        }

        private static void CalcCurve(PointF[] pts, float tension, out PointF p1, out PointF p2) {
            float deltaX, deltaY;
            deltaX = pts[2].X - pts[0].X;
            deltaY = pts[2].Y - pts[0].Y;
            p1 = new PointF((pts[1].X - tension * deltaX), (pts[1].Y - tension * deltaY));
            p2 = new PointF((pts[1].X + tension * deltaX), (pts[1].Y + tension * deltaY));
        }

        private void CalcCurveEnd(PointF end, PointF adj, float tension, out PointF p1) {
            p1 = new PointF(((tension * (adj.X - end.X) + end.X)), ((tension * (adj.Y - end.Y) + end.Y)));
        }

        /// <summary>
        /// Calculates the circles (or curved lines when not completely in view)
        /// </summary>
        /// <param name="viewport"></param>
        /// <param name="dec"></param>
        private void CalculateDecPoints(double dec) {
            var thickness = dec == 0 ? 3 : 1;
            var coordinates = decCoordinateMatrix[dec];

            var center = currentViewport.CenterCoordinates;
            var startRA = coordinates.Aggregate((x, y) => Math.Abs(x.RADegrees - center.RADegrees) < Math.Abs(y.RADegrees - center.RADegrees) ? x : y);
            var startIdx = coordinates.FindIndex(x => startRA.RADegrees == x.RADegrees);
            var iterator = 0;
            var list = new LinkedList<PointF>();

            if (currentViewport.ContainsCoordinates(startRA)) {
                var pointF = Project(startRA);
                list.AddLast(pointF);

                do {
                    iterator++;

                    var rightCoordinate = coordinates[(int)nfmod((startIdx + iterator), coordinates.Count)];
                    var leftCoordinate = coordinates[(int)nfmod((startIdx - iterator), coordinates.Count)];

                    var leftPointF = Project(leftCoordinate);
                    var rightPointF = Project(rightCoordinate);

                    list.AddLast(leftPointF);
                    list.AddFirst(rightPointF);

                    if (!currentViewport.ContainsCoordinates(leftCoordinate) && !currentViewport.ContainsCoordinates(rightCoordinate)) {
                        break;
                    }
                } while (iterator <= coordinates.Count / 2d);
            }

            DecPoints.Add(new FrameLine() { Collection = new List<PointF>(list), StrokeThickness = thickness, Closed = false, Angle = Angle.ByDegree(dec) });
        }

        /// <summary>
        /// Calculate the lines spanning from pole to pole
        /// </summary>
        /// <param name="viewport"></param>
        /// <param name="ra"></param>
        private void CalculateRAPoints(double ra) {
            var list = new List<PointF>();
            var thickness = 1;
            Coordinates prevCoordinate = null;
            bool atLeastOneInside = false;
            foreach (var coordinate in raCoordinateMatrix[ra]) {
                if (currentViewport.ContainsCoordinates(coordinate)) {
                    atLeastOneInside = true;
                    if (prevCoordinate != null) {
                        list.Add(Project(prevCoordinate));
                        prevCoordinate = null;
                    }

                    if (coordinate.RADegrees == 0 || coordinate.RADegrees == 180) {
                        thickness = 3;
                    }
                    list.Add(Project(coordinate));
                } else {
                    if (atLeastOneInside) {
                        list.Add(Project(coordinate));
                        break;
                    } else {
                        prevCoordinate = coordinate;
                    }
                }
            }
            RAPoints.Add(new FrameLine() { Collection = list, StrokeThickness = thickness, Closed = false, Angle = Angle.ByDegree(ra) });
        }

        private List<PointF> CardinalSpline(List<PointF> pts, float t, bool closed) {
            int i, nrRetPts;
            PointF p1, p2;
            float tension = t * (1f / 3f); //we are calculating contolpoints.

            if (closed)
                nrRetPts = (pts.Count + 1) * 3 - 2;
            else
                nrRetPts = pts.Count * 3 - 2;

            PointF[] retPnt = new PointF[nrRetPts];
            for (i = 0; i < nrRetPts; i++)
                retPnt[i] = new PointF();

            if (!closed) {
                CalcCurveEnd(pts[0], pts[1], tension, out p1);
                retPnt[0] = pts[0];
                retPnt[1] = p1;
            }
            for (i = 0; i < pts.Count - (closed ? 1 : 2); i++) {
                CalcCurve(new PointF[] { pts[i], pts[i + 1], pts[(i + 2) % pts.Count] }, tension, out p1, out p2);
                retPnt[3 * i + 2] = p1;
                retPnt[3 * i + 3] = pts[i + 1];
                retPnt[3 * i + 4] = p2;
            }
            if (closed) {
                CalcCurve(new PointF[] { pts[pts.Count - 1], pts[0], pts[1] }, tension, out p1, out p2);
                retPnt[nrRetPts - 2] = p1;
                retPnt[0] = pts[0];
                retPnt[1] = p2;
                retPnt[nrRetPts - 1] = retPnt[0];
            } else {
                CalcCurveEnd(pts[pts.Count - 1], pts[pts.Count - 2], tension, out p1);
                retPnt[nrRetPts - 2] = p1;
                retPnt[nrRetPts - 1] = pts[pts.Count - 1];
            }
            return new List<PointF>(retPnt);
        }

        private void DetermineStepSizes() {
            var decStep = currentViewport.VFoV / 4d;
            decStep = DEC_STEPSIZES.Aggregate((x, y) => Math.Abs(x - decStep) < Math.Abs(y - decStep) ? x : y);


            // The higher the absolute declination, the less RA steps are required
            double originalMin = 0d;
            double originalMax = 90d;
            double targetMin = 1d;
            double targetMax = 15d;
            double ratio = (Math.Abs(currentViewport.CenterCoordinates.Dec) - originalMin) / (originalMax - originalMin);
            double adjustedRatio = Math.Pow(ratio, 4);
            double decFactor = Math.Round(adjustedRatio * (targetMax - targetMin) + targetMin);

            var raStep = currentViewport.HFoV / 4d * Math.Round(decFactor);
            raStep = RA_STEPSIZES.Aggregate((x, y) => Math.Abs(x - raStep) < Math.Abs(y - raStep) ? x : y);

            // Limit the raStep when the pole is in view
            if (currentViewport.ContainsCoordinates(new Coordinates(Angle.ByDegree(0), Angle.ByDegree(MAXDEC), Epoch.J2000)) || currentViewport.ContainsCoordinates(new Coordinates(Angle.ByDegree(0), Angle.ByDegree(-MAXDEC), Epoch.J2000))) {
                raStep = 15d;
            }

            resolution = Math.Min(raStep, decStep) / 4d;

            if (currentRAStep != raStep) {
                currentRAStep = raStep;
                GenerateRACoordinateMatrix(raStep);
            }
            if (currentDecStep != decStep) {
                currentDecStep = decStep;
                GenerateDecCoordinateMatrix(decStep);
            }
        }

        private void DrawDecLineCollection(Graphics g, FrameLine frameLine) {
            if (frameLine.Collection.Count > 1) {
                var position = frameLine.Collection.FirstOrDefault(x => x.X > 0 && x.Y > 0);
                if (position != PointF.Empty) {
                    var text = $"{string.Format("{0:N2}", frameLine.Angle.Degree)}�";
                    var size = g.MeasureString(text, gridAnnotationFont);
                    g.DrawString(text, gridAnnotationFont, gridAnnotationBrush, (position.X), (position.Y));
                }
                DrawFrameLineCollection(g, frameLine);
            }
        }

        private void DrawFrameLineCollection(Graphics g, FrameLine frameLine) {
            var points = CardinalSpline(frameLine.Collection, 0.5f, frameLine.Closed);

            if (frameLine.StrokeThickness != 1) {
                using (var pen = new System.Drawing.Pen(gridPen.Color, frameLine.StrokeThickness)) {
                    g.DrawBeziers(pen, points.ToArray());
                }
            } else {
                g.DrawBeziers(gridPen, points.ToArray());
            }
        }

        private void DrawRALineCollection(Graphics g, FrameLine frameLine) {
            if (frameLine.Collection.Count > 1) {
                //Prevent annotations to overlap on southern pole
                var southPole = new Coordinates(0, -MAXDEC, Epoch.J2000, Coordinates.RAType.Degrees).XYProjection(currentViewport);
                PointF? position = frameLine.Collection.FirstOrDefault(x => x.X > 0 && x.Y > 0 && x.X < currentViewport.Width && x.Y < currentViewport.Height && Math.Abs(x.X - southPole.X) > 5 && Math.Abs(x.Y - southPole.Y) > 5);

                if (position != null) {
                    var hms = AstroUtil.HoursToHMS(frameLine.Angle.Hours);
                    var text = $"{hms.Substring(0, hms.Length - 3)}h";
                    var size = g.MeasureString(text, gridAnnotationFont);
                    g.DrawString(text, gridAnnotationFont, gridAnnotationBrush, (position.Value.X), Math.Max(0, (position.Value.Y - size.Height)));
                }

                DrawFrameLineCollection(g, frameLine);
            }
        }

        private void GenerateDecCoordinateMatrix(double decStep) {
            decCoordinateMatrix.Clear();

            for (double i = 0; i < 360; i += resolution) {
                for (double dec = 0; dec <= MAXDEC; dec += decStep) {
                    var coordinate = new Coordinates(Angle.ByDegree(i), Angle.ByDegree(dec), Epoch.J2000);
                    var coordinate2 = new Coordinates(Angle.ByDegree(i), Angle.ByDegree(-dec), Epoch.J2000);
                    if (!decCoordinateMatrix.ContainsKey(dec)) {
                        decCoordinateMatrix[dec] = new List<Coordinates>();
                    }
                    if (!decCoordinateMatrix.ContainsKey(-dec)) {
                        decCoordinateMatrix[-dec] = new List<Coordinates>();
                    }
                    decCoordinateMatrix[dec].Add(coordinate);
                    decCoordinateMatrix[-dec].Add(coordinate2);
                }
            }
        }

        private void GenerateRACoordinateMatrix(double raStep) {
            raCoordinateMatrix.Clear();
            double i = 0;
            do {
                i = Math.Min(MAXDEC, i + resolution);

                for (double ra = 0; ra < 360; ra += raStep) {
                    var coordinate = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(i), Epoch.J2000);
                    var coordinate2 = new Coordinates(Angle.ByDegree(ra), Angle.ByDegree(-i), Epoch.J2000);
                    if (!raCoordinateMatrix.ContainsKey(ra)) {
                        raCoordinateMatrix[ra] = new List<Coordinates>();
                    }

                    raCoordinateMatrix[ra].Add(coordinate);
                    raCoordinateMatrix[ra].Insert(0, coordinate2);
                }
            } while (i < MAXDEC);
        }
        private double nfmod(double a, double b) {
            return a - b * Math.Floor(a / b);
        }

        private PointF Project(Coordinates coordinates) {
            var p = coordinates.XYProjection(currentViewport);
            return new PointF((float)p.X, (float)p.Y);
        }

        public void Dispose() {
            gridAnnotationBrush.Dispose();
            gridAnnotationFont.Dispose();
            gridPen.Dispose();
        }
    }
}