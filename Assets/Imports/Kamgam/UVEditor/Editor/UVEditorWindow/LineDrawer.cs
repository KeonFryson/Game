using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UVEditor
{
    // Uses GL API instead of UITK to draw lines which is faster bot not anti aliased
    // (n2h: Use Painter2D in future Unity versions as that is aliased).
    public class LineDrawer : ImmediateModeElement
    {
        public struct SimpleVertex
        {
            public Vector2 Pos;
            public Color Tint;

            public SimpleVertex(Vector2 pos, Color tint) : this(pos)
            {
                Tint = tint;
            }

            public SimpleVertex(Vector2 pos) : this()
            {
                Pos = pos;
                Tint = Color.white;
            }

            public static implicit operator Vector2(SimpleVertex sv) => sv.Pos;
            public static implicit operator Vector3(SimpleVertex sv) => (Vector3) sv.Pos;
            public static implicit operator SimpleVertex(Vector2 v) => new SimpleVertex(v);
            public static implicit operator SimpleVertex(Vector3 v) => new SimpleVertex((Vector2)v);
        }

        /// <summary>
        /// NOTICE: If enabled then you also have to REVERSE the winding order of the vertices.
        /// </summary>
        public bool InvertVertical;
        public List<SimpleVertex> Vertices = new List<SimpleVertex>();
        /// <summary>
        /// The line width in pixels.
        /// </summary>
        public float LineWidth = 2f;
        public Color LineColor = new Color(1f, 1f, 1f, 1f);

        // If null then a new line is started after two new vertices have been added.
        public Vector2? _startVertex = null;

        // Cache of the last vertex because we need two points to draw a line ;)
        public Vector2? _previousVertex = null;

        private int _numOfVertices;
        public int NumOfVertices => _numOfVertices;
        public bool NextLinesCanBeDrawn(int numOfLinesToDraw = 1)
        {
            return _numOfVertices + numOfLinesToDraw * 6 <= MAX_NUM_OF_VERTICES;
        }

        public const int MAX_NUM_OF_VERTICES = 999_999;

        public void ClearVertices()
        {
            Vertices.Clear();
            _numOfVertices = 0;
            _previousVertex = null;
            _startVertex = null;
        }

        public void StartNewLine()
        {
            _startVertex = null;
            _previousVertex = null;
        }

        /// <summary>
        /// Extends an existing line or starts a new one of none existed.<br />
        /// Add vertices in CCW order - or - if InvertVertical is TRUE then CW order.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void AddVertex(float x, float y)
        {
            AddVertex(new Vector2(x, y));
        }

        public void AddVertex(float x, float y, Color color)
        {
            AddVertex(new Vector2(x, y), color);
        }

        public void AddVertex(Vector2 vertex, bool closeLoop = false)
        {
            AddVertex(vertex, LineColor, closeLoop);
        }

        /// <summary>
        /// Extends an existing line or starts a new one of none existed.<br />
        /// Add vertices in CCW order - or - if InvertVertical is TRUE then CW order.
        /// </summary>
        /// <param name="vertex"></param>
        /// <param name="color"></param>
        /// <param name="closeLoop"></param>
        /// <returns>Whether or not the vertex has been added successfully.</returns>
        public void AddVertex(Vector2 vertex, Color color, bool closeLoop = false)
        {
            // Invert y-axis?
            float height = style.height.value.value;
            if (InvertVertical && !Mathf.Approximately(height, 0f))
            {
                vertex = new Vector2(vertex.x, height - vertex.y);
            }

            // Check if we have a previous point, if yes then draw a line.
            // If not then remember it and wait for the next point.
            if (_previousVertex.HasValue)
            {
                // Draw a line consiting of two triangles.
                if (InvertVertical)
                {
                    drawLineCW(_previousVertex.Value, vertex, color);
                }
                else
                {
                    drawLineCCW(_previousVertex.Value, vertex, color);
                }
            }

            _previousVertex = vertex;

            if (closeLoop && _startVertex.HasValue)
            {
                if (InvertVertical)
                {
                    drawLineCW(_previousVertex.Value, _startVertex.Value, color);
                }
                else
                {
                    drawLineCCW(_previousVertex.Value, _startVertex.Value, color);
                }
            }

            if (!_startVertex.HasValue)
                _startVertex = vertex;
        }

        private void drawLineCCW(Vector2 from, Vector2 to)
        {
            drawLineCCW(from, to, LineColor);
        }

        private void drawLineCCW(Vector2 from, Vector2 to, Color color)
        {
            // Visual elements can not have more than 65k vertices.
            if (_numOfVertices + 6 >= MAX_NUM_OF_VERTICES)
                throw new System.Exception("LineDrawer can not draw " + _numOfVertices + " because a VisualElement must not allocate more than 65535 vertices.");

            var vector = to - from;
            var normal = new Vector2(vector.y, -vector.x).normalized;

            // tri 1
            Vertices.Add(new SimpleVertex(from - normal * LineWidth * 0.5f, color));
            Vertices.Add(new SimpleVertex(to + normal * LineWidth * 0.5f, color));
            Vertices.Add(new SimpleVertex(from + normal * LineWidth * 0.5f, color));

            // tri 2
            Vertices.Add(new SimpleVertex(from - normal * LineWidth * 0.5f, color));
            Vertices.Add(new SimpleVertex(to - normal * LineWidth * 0.5f, color));
            Vertices.Add(new SimpleVertex(to + normal * LineWidth * 0.5f, color));

            _numOfVertices += 6;
        }

        private void drawLineCW(Vector2 from, Vector2 to)
        {
            drawLineCW(from, to, LineColor);
        }

        private void drawLineCW(Vector2 from, Vector2 to, Color color)
        {
            // Visual elements can not have more than 65k vertices.
            if (_numOfVertices + 6 >= MAX_NUM_OF_VERTICES)
                throw new System.Exception("LineDrawer can not draw " + (_numOfVertices + 6) + " because a VisualElement must not allocate more than 65535 vertices.");

            var vector = to - from;
            var normal = new Vector2(vector.y, -vector.x).normalized;

            // tri 1
            Vertices.Add(new SimpleVertex(from + normal * LineWidth * 0.5f, color));
            Vertices.Add(new SimpleVertex(to + normal * LineWidth * 0.5f, color));
            Vertices.Add(new SimpleVertex(from - normal * LineWidth * 0.5f, color));

            // tri 2
            Vertices.Add(new SimpleVertex(to + normal * LineWidth * 0.5f, color));
            Vertices.Add(new SimpleVertex(to - normal * LineWidth * 0.5f, color));
            Vertices.Add(new SimpleVertex(from - normal * LineWidth * 0.5f, color));

            _numOfVertices += 6;
        }

        
        bool? m_lastIsDrawnValue; // Nullable to ensure we catch the initial change.
        
        protected Material m_cachedMaterial;

        Material getMaterial()
        {
            // Material
            if (m_cachedMaterial == null)
            {
                m_cachedMaterial = new Material(Shader.Find("UI/Default"));
            }

            return m_cachedMaterial;
        }
        
        // Mesh Data

        
        public void GenerateMesh()
        {
            // Debug.Log("Not needed in immediate mode element");
        }

        // Called every frame / draw cycle.
        protected override void ImmediateRepaint()
        {
            var material = getMaterial();

            if (material == null)
                return;
            
            // Opacity in shader
            if (material.HasFloat("_Opacity"))
            {
                material.SetFloat("_Opacity", getEffectiveOpacity());
            }
            
            GL.PushMatrix();

            // The parent is the clipping rect (we only use yMin to avoid drawing on top of the buttons).
            var cr = parent.ChangeCoordinatesTo(this, parent.contentRect);
            
            // If SetPass returns false, you should not render anything.
            // See: https://docs.unity3d.com/6000.1/Documentation/ScriptReference/Material.SetPass.html
            for (int p = 0; p < material.passCount; p++)
            {
                if (!material.SetPass(p))
                    continue;

                if (Vertices == null || Vertices.Count == 0)
                    continue;

                GL.Begin(GL.TRIANGLES);
                
                for (int i = 0; i < Vertices.Count; i += 3)
                {
                    var vertex0 = Vertices[i];
                    var vertex1 = Vertices[i+1];
                    var vertex2 = Vertices[i+2];

                    var tint0 = vertex0.Tint;
                    var tint1 = vertex0.Tint;
                    var tint2 = vertex0.Tint;

                    var pos0 = vertex0.Pos;
                    var pos1 = vertex1.Pos;
                    var pos2 = vertex2.Pos;
                    
                    // Clipping on top (yMin) the rest is luckily clipped by the window itself or the UI is drawn on top.
                    if (pos0.y < cr.yMin &&
                        pos1.y < cr.yMin &&
                        pos2.y < cr.yMin
                       )
                    {
                        // All outside, hide completely
                        tint0 = new Color(1f, 1f, 1f, 0f);
                        tint1 = new Color(1f, 1f, 1f, 0f);
                        tint2 = new Color(1f, 1f, 1f, 0f);
                    }
                    else
                    {
                        // 0 and 1 outside
                        if (pos0.y < cr.yMin &&
                            pos1.y < cr.yMin)
                        {
                            pos0 = projectAndLimitTo(pos0, pos2, cr.yMin);
                            pos1 = projectAndLimitTo(pos1, pos2, cr.yMin);
                        }
                        // 1 and 2 outside
                        else if (pos1.y < cr.yMin &&
                                 pos2.y < cr.yMin)
                        {
                            pos1 = projectAndLimitTo(pos1, pos0, cr.yMin);
                            pos2 = projectAndLimitTo(pos2, pos0, cr.yMin);
                        }
                        // 2 and 0 outside
                        else if (pos2.y < cr.yMin &&
                                 pos0.y < cr.yMin)
                        {
                            pos1 = projectAndLimitTo(pos2, pos1, cr.yMin);
                            pos2 = projectAndLimitTo(pos0, pos1, cr.yMin);
                        }
                        else
                        {
                            // Otherwise only one is outside:
                            if (pos0.y < cr.yMin)
                            {
                                pos0 = projectAndLimitTo(pos0, pos1, cr.yMin);
                            }
                            else if (pos1.y < cr.yMin)
                            {
                                pos1 = projectAndLimitTo(pos1, pos2, cr.yMin);
                            }
                            else if (pos2.y < cr.yMin)
                            {
                                pos2 = projectAndLimitTo(pos2, pos0, cr.yMin);
                            }
                        }
                    }

                    GL.Color(tint0);
                    GL.Vertex3(pos0.x, pos0.y, 0f);
                    
                    GL.Color(tint1);
                    GL.Vertex3(pos1.x, pos1.y, 0f);
                    
                    GL.Color(tint2);
                    GL.Vertex3(pos2.x, pos2.y, 0f);
                }

                GL.End();
            }
            
            GL.PopMatrix();
            
            
            // Simple quad for testing
            /*
            {
                GL.PushMatrix();
                
                GL.Begin(GL.TRIANGLES);
                GL.Color(Color.blue);

                Vector2 min = Vector2.zero;
                Vector2 max =
                    new Vector2(contentRect.width + resolvedStyle.borderLeftWidth + resolvedStyle.borderRightWidth,
                        contentRect.height + resolvedStyle.borderTopWidth + resolvedStyle.borderBottomWidth);

                //GL.TexCoord2(m_minUV.x, m_maxUV.y);
                GL.Vertex3(min.x, min.y, 0);

                //GL.TexCoord2(m_maxUV.x, m_maxUV.y);
                GL.Vertex3(max.x, min.y, 0);

                //GL.TexCoord2(m_maxUV.x, m_minUV.y);
                GL.Vertex3(max.x, max.y, 0);

                //GL.TexCoord2(m_minUV.x, m_maxUV.y);
                GL.Vertex3(min.x, min.y, 0);

                //GL.TexCoord2(m_maxUV.x, m_minUV.y);
                GL.Vertex3(max.x, max.y, 0);

                //GL.TexCoord2(m_minUV.x, m_minUV.y);
                GL.Vertex3(min.x, max.y, 0);

                GL.End();
                
                GL.PopMatrix();
            }
            */
        }

        private Vector2 projectAndLimitTo(Vector2 posA, Vector2 posB, float yMin)
        {
            // Calc slope
            float t = (yMin - posA.y) / (posB.y - posA.y);
            // Apply slop the get x pos (we can assume it's always inside)
            float x = posA.x + t * (posB.x - posA.x);

            return new Vector2(x, yMin);
        }

        float getEffectiveOpacity()
        {
            float effectiveOpacity = 1.0f;
            
            VisualElement current = this;
            while (current != null)
            {
                effectiveOpacity *= current.resolvedStyle.opacity;
                current = current.parent;
            }

            return effectiveOpacity;
        }
        
#if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<LineDrawer, UxmlTraits> { }
#endif
    }
}
