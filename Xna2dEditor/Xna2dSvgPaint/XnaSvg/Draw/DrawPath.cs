﻿#region Header

/*  --------------------------------------------------------------------------------------------------------------
 *  I Software Innovations
 *  --------------------------------------------------------------------------------------------------------------
 *  SVG Artieste 2.0
 *  --------------------------------------------------------------------------------------------------------------
 *  File     :       DrawPath.cs
 *  Author   :       ajaysbritto@yahoo.com
 *  Date     :       20/03/2010
 *  --------------------------------------------------------------------------------------------------------------
 *  Change Log
 *  --------------------------------------------------------------------------------------------------------------
 *  Author	Comments
 */

#endregion Header

namespace Draw
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Drawing.Drawing2D;
    using System.Globalization;
    using System.Windows.Forms;

    using SVGLib;
using Microsoft.Xna.Framework;
    using Fyri2dEditor.Xna2dDrawingLibrary;

    public sealed class XnaDrawPath : XnaDrawLine
    {
        #region Fields

        public int InsertAt;

        private const string Tag = "path";

        readonly char[] _commands = { 'M', 'L', 'Z' }; //--M indicates move to, L LineTo and Z Close Figure
        private readonly bool _gotX, _gotY;
        private readonly List<PathCommands> _pointArray;// list of points
        private readonly float _x, _y;
        private readonly String _xStr, _yStr, _xStrNxt;

        private GraphicsPath _lastGp;
        Point _pointToInsert;

        #endregion Fields

        #region Constructors

        public XnaDrawPath()
        {
            _pointArray = new List<PathCommands>();
            init();
        }

        public XnaDrawPath(float x1, float y1)
        {
            _x = 0.0F;
            _pointArray = new List<PathCommands> {new PathCommands(new Point((int)x1, (int)y1), 'M')};
            init();
        }

        public XnaDrawPath(String[] arr)
        {
            _x = 0.0F;
            char currentCommand = new char();
            char nextCommand = new char();

            _pointArray = new List<PathCommands>();
            init();

            try
            {

                for (int i = 0; i < arr.Length; i++)
                {
                    int idx;
                    if ((idx = arr[i].IndexOfAny(_commands)) >= 0)
                    {
                        if (idx == 0)
                        {
                            currentCommand = arr[i][idx];
                            arr[i] = arr[i].Substring(1, arr[i].Length - 1);

                            if (!_gotX)
                            {
                                _xStr = arr[i];
                                _gotX = true;
                            }
                            else
                            {
                                _yStr = arr[i];
                                _gotY = true;
                            }

                        }
                        else if (idx == (arr[i].Length - 1))
                        {
                            currentCommand = arr[i][idx];
                            arr[i] = arr[i].Substring(0, arr[i].Length - 1);

                            if (!_gotX)
                            {
                                _xStr = arr[i];
                                
                                _gotX = true;
                            }
                            else
                            {
                                _yStr = arr[i];
                                _gotY = true;
                            }

                        }
                        else
                        {
                            _yStr = arr[i].Substring(0, idx);
                            _xStrNxt = arr[i].Substring(idx + 1, arr[i].Length - idx - 1);
                            nextCommand = arr[i][idx];
                            _gotX = _gotY = true;
                        }
                    }
                    else
                    {
                        if (arr[i].Length > 0)
                        {
                            float t;
                            if (float.TryParse(arr[i], out t))
                            {
                                if (!_gotX)
                                {
                                    _xStr = arr[i];
                                    _gotX = true;
                                }
                                else
                                {
                                    _yStr = arr[i];
                                    _gotY = true;
                                }
                            }
                        }
                    }

                    if (_gotX && _gotY)
                    {
                        if (float.TryParse(_xStr, out _x))
                        {
                            if (float.TryParse(_yStr, out _y))
                            {
                                _pointArray.Add(new PathCommands(new Point((int)_x, (int)_y), currentCommand));
                            }
                        }

                        if (_xStrNxt.Length > 0)
                        {
                            _xStr = _xStrNxt;
                            _xStrNxt = "";
                            currentCommand = nextCommand;
                            nextCommand = '\0';

                            _gotX = true;
                            _gotY = false;
                        }
                        else
                        {
                            _gotY = _gotX = false;
                        }
                    }
                }

                //Now Change the last command to Z.
                //We wont care if it presnt in the actual.
                //We just need to close the figure
                _pointArray[_pointArray.Count - 1].Pc = 'Z';
            }
            catch (Exception ex)
            {
                ErrH.Log("DrawPath", "DrawPath", ex.ToString(), ErrH._LogPriority.Info);
                MessageBox.Show("Error in SVGPath");
            }
        }

        #endregion Constructors

        #region Properties

        public override int HandleCount
        {
            get
            {
                return _pointArray.Count;
            }
        }

        #endregion Properties

        #region Methods

        private void init()
        {
            LoadCursor();
            Initialize();
        }

        public static XnaDrawPath Create(SvgPath svp)
        {
            XnaDrawPath dp;

            try
            {
                string s = svp.PathData.Trim();
                s = s.Replace("\r", "");
                s = s.Replace("\n", " ");
                s = s.Trim();
                string[] arr = s.Split(' ');

                dp = new XnaDrawPath(arr) {Name = svp.ShapeName};
                dp.SetStyleFromSvg(svp);
            }
            catch (Exception ex)
            {
                ErrH.Log("DrawPath", "Create", ex.ToString(), ErrH._LogPriority.Info);
                dp = null;
            }

            return dp;
        }

        public void AddPoint(Point point)
        {
            _pointArray.Add(new PathCommands(point, 'L'));
        }

        //Drawing is complete. Now you may please close the figure
        public void CloseFigure()
        {
            _pointArray[_pointArray.Count - 1].Pc = 'Z';
        }

        public override void Draw(XnaDrawingContext g)
        {
            Point p0 = new Point();
            Point p1 = new Point();
            Point p2 = new Point();

            //GraphicsPath gp = new GraphicsPath();

            try
            {
                //g.SmoothingMode = SmoothingMode.AntiAlias;

                //Pen pen = new Pen(Stroke, StrokeWidth);
                //pen.Color = Color.Black;

                //Brush br = new SolidBrush(Fill);
                IEnumerator enumerator = _pointArray.GetEnumerator();

                while (enumerator.MoveNext())
                {

                    switch (((PathCommands)enumerator.Current).Pc)
                    {
                        case 'M':
                            p1 = ((PathCommands)enumerator.Current).P;
                            //gp.CloseFigure();
                            //gp.StartFigure();
                            //p0 = p1;
                            break;

                        case 'L':
                            p2 = ((PathCommands)enumerator.Current).P;
                            //gp.AddLine(p1, p2);
                            p1 = p2;
                            break;

                        case 'Z':
                            p2 = ((PathCommands)enumerator.Current).P;
                            //gp.AddLine(p1, p2); gp.CloseFigure();
                            break;

                        default:
                            break;
                    }

                }

                //g.FillPath(br, gp);
                //g.DrawPath(pen, gp);
                //_lastGp = gp;

                //pen.Dispose();
            }
            catch (Exception ex)
            {
                ErrH.Log("DrawPolygon", "Draw", ex.ToString(), ErrH._LogPriority.Info);
            }

            //base.Draw(g);
        }

        public override Point GetHandle(int handleNumber)
        {
            if (handleNumber < 1)
                handleNumber = 1;

            if (handleNumber > _pointArray.Count)
                handleNumber = _pointArray.Count;

            return _pointArray[handleNumber - 1].P;
        }

        /// <summary>
        /// Get curesor for the handle
        /// </summary>
        /// <param name="handleNumber"></param>
        /// <returns></returns>
        public override Cursor GetHandleCursor(int handleNumber)
        {
            return Cursors.SizeAll;
        }

        public override string GetXmlStr(Point scale)
        {
            char prevCommand = new char();
            char curCommand;
            bool zPresent = false;

            string s = "<";
            s += Tag;
            s += GetStrStyle(scale);
            s += " d = "+"\"";
            IEnumerator enumerator = _pointArray.GetEnumerator();
            while (enumerator.MoveNext())
            {
                float x = ((PathCommands)enumerator.Current).P.X / scale.X;
                float y = ((PathCommands)enumerator.Current).P.Y / scale.Y;

                curCommand = ((PathCommands)enumerator.Current).Pc;

                if (curCommand != prevCommand)
                {
                    if (curCommand == 'Z')
                    {
                        s += x.ToString(CultureInfo.InvariantCulture) + " " + y.ToString(CultureInfo.InvariantCulture);
                        s += curCommand.ToString();
                        zPresent = true;

                    }
                    else
                    {
                        s = s.Trim();

                        if (prevCommand == 'Z')
                            s += "\r\n";

                        s += curCommand.ToString();
                        s += x.ToString(CultureInfo.InvariantCulture) + " " + y.ToString(CultureInfo.InvariantCulture);
                    }

                    prevCommand = curCommand;
                }
                else
                {
                    s += x.ToString(CultureInfo.InvariantCulture) + " " + y.ToString(CultureInfo.InvariantCulture);
                }

                s += " ";
            }

            //Append an Z to the end to close figure
            if(!zPresent)
                s += "Z";

            s += "\"";

            //If we have a shape name
            if(Name.Length > 0)
                s += " ShapeName = \"" + Name + "\"";

            s += " />" + "\r\n";
            return s;
        }

        /// <summary>
        /// Hit test.
        /// Return value: -1 - no hit
        ///                0 - hit anywhere
        ///                > 1 - handle number
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public override int HitTest(Point point)
        {
            if (_lastGp != null)
                if(_lastGp.PathPoints.Length > 0)
                    HitOnCircumferance = HitTestForOutLine(_lastGp.PathPoints, point);
            return base.HitTest(point);
        }

        public override void MouseClickOnBorder()
        {
            if((Control.ModifierKeys & Keys.Control) != 0)
            _pointArray.Insert(InsertAt, new PathCommands(_pointToInsert, 'L'));
        }

        public override void MouseClickOnHandle(int handle)
        {
            if ((Control.ModifierKeys & Keys.Alt) != 0)
            {
                if (_pointArray.Count > 2) //Don't allow to remove the handles if it is a simple line
                {
                    if ((_pointArray[handle - 1].Pc == 'z') || (_pointArray[handle - 1].Pc == 'Z'))
                    {
                        _pointArray[handle - 2].Pc = 'Z';
                    }

                    if ((_pointArray[handle - 1].Pc == 'm') || (_pointArray[handle - 1].Pc == 'M'))
                    {
                        _pointArray[1].Pc = 'M';
                    }

                    _pointArray.RemoveAt(handle-1);
                }
            }
        }

        public override void Move(float deltaX, float deltaY)
        {
            int n = _pointArray.Count;

            for (int i = 0; i < n; i++)
            {
                Point point = new Point((int)(_pointArray[i].P.X + deltaX), (int)(_pointArray[i].P.Y + deltaY));
                _pointArray[i].P = point;
            }

            Invalidate();
        }

        public override void MoveHandleTo(Point point, int handleNumber)
        {
            if (handleNumber < 1)
                handleNumber = 1;

            if (handleNumber > _pointArray.Count)
                handleNumber = _pointArray.Count;

            _pointArray[handleNumber - 1].P = point;
            Invalidate();
        }

        public override void Resize(Point newscale, Point oldscale)
        {
            for (int i = 0; i < _pointArray.Count; i++)
                _pointArray[i].P = RecalcPoint(_pointArray[i].P, newscale, oldscale);
            RecalcStrokeWidth(newscale, oldscale);
            Invalidate();
        }

        protected override void CreateObjects()
        {
            if (AreaPath != null)
                return;
            try
            {
                // Create closed path which contains all polygon vertexes
                AreaPath = new GraphicsPath();
                var regionRectF = new Rectangle(0, 0, 0, 0);

                IEnumerator enumerator = _pointArray.GetEnumerator();

                if (enumerator.MoveNext())
                {
                    float x1 = ((PathCommands)enumerator.Current).P.X;     // previous point
                    float y1 = ((PathCommands)enumerator.Current).P.Y;     // previous point

                    regionRectF.X = (int)x1;
                    regionRectF.Y = (int)y1;

                    regionRectF.Width = (int)x1;
                    regionRectF.Height = (int)y1;

                }

                while (enumerator.MoveNext())
                {

                    float x2 = ((PathCommands)enumerator.Current).P.X;             // current point
                    float y2 = ((PathCommands)enumerator.Current).P.Y;             // current point

                    if (regionRectF.X > x2)
                        regionRectF.X = (int)x2;

                    if (regionRectF.Y > y2)
                        regionRectF.Y = (int)y2;

                    if (regionRectF.Width < x2)
                        regionRectF.Width = (int)x2;

                    if (regionRectF.Height < y2)
                        regionRectF.Height = (int)y2;
                }

                regionRectF.Width = regionRectF.Width - regionRectF.X;
                regionRectF.Height = regionRectF.Height - regionRectF.Y;
                if (regionRectF.Width < 1.0F)
                    regionRectF.Width = 1;

                if (regionRectF.Height < 1.0F)
                    regionRectF.Height = 1;

                // Create region from the path
                System.Drawing.Rectangle rect = new System.Drawing.Rectangle(regionRectF.X, regionRectF.Y, regionRectF.Width, regionRectF.Height);
                AreaRegion = new System.Drawing.Region(rect);
            }
            catch (Exception ex)
            {
                ErrH.Log("DrawPath", "Create", ex.ToString(), ErrH._LogPriority.Info);
            }
        }

        private static void LoadCursor()
        {
        }

        private bool HitTestForOutLine(System.Drawing.PointF[] points, Point hitPoint)
        {
            bool retVal = false;
            for (int i = 0; i < HandleCount - 1; i++)
            {

                // Create path which contains wide line
                // for easy mouse selection
                GraphicsPath areaPath1 = new GraphicsPath();
                System.Drawing.Pen AreaPen1 = new System.Drawing.Pen(System.Drawing.Color.Black, 2);
                areaPath1.AddLine(points[i].X, points[i].Y, points[i + 1].X, points[i+1].Y);
                areaPath1.Widen(AreaPen1);

                // Create region from the path
                System.Drawing.Region areaRegion = new System.Drawing.Region(areaPath1);
                System.Drawing.Point hPoint = new System.Drawing.Point(hitPoint.X, hitPoint.Y);
                retVal = areaRegion.IsVisible(hPoint);

                InsertAt = i+1;
                _pointToInsert = new Point(hitPoint.X, hitPoint.Y);

                if (retVal)
                    break;
            }

            return retVal;
        }

        public override object Clone()
        {
            XnaDrawPath newDrawPath = new XnaDrawPath();
            foreach (PathCommands pc in _pointArray)
            {
                newDrawPath._pointArray.Add(new PathCommands(pc.P, pc.Pc));
            }
            newDrawPath.Stroke = Stroke;
            newDrawPath.StrokeWidth = StrokeWidth;
            newDrawPath.Fill = Fill;
            return newDrawPath;
        }

        #endregion Methods
    }
}