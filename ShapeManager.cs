using System;
using System.Collections.Generic;
namespace RubiksConsole
{
    public class Shape
    {
        // Has its own Vertices and faces

        public List<Vertex> vertices;
        public List<Face> faces;

        // Move entire shape linearly
        public void Translate(Vector translation)
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i].Position3D += translation;
            }
        }

        // Transform entire shape.
        // TODO: Add ability to shift center of transformation
        public void Transform(Matrix transform)
        {
            if (transform.M != 3 || transform.N != 3)
                return;

            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i].Position3D = transform * vertices[i].Position3D;
            }
        }

        public Shape()
        {
            vertices = new List<Vertex>();
            faces = new List<Face>();
        }
    }

    public class Face
    {
        // General class for Edges and convex Faces
        public Vertex[] vertices;
        public char texture;
        public double screenDistance; // Cached distance from screen since last projection.

        public Face(char texture, Vertex[] vertices)
        {
            this.vertices = vertices;
            this.texture = texture;
        }
    }

    public class Vertex
    {
        // Wrapper for 3D vectors
        // Operations never generate a new object, can be used by reference
        public Vector Position3D;
        public Vector Projection; // Cached 2D coordinates on the screen from last projection

        public Vertex(Vector position)
        {
            Position3D = position;
        }
        public Vertex(double x, double y, double z)
        {
            double[] pos = { x, y, z };
            Position3D = new Vector(pos);
        }
    }

    public class ShapeManager : CharacterCanvas
    {
        // 3D object abstraction for the CharacterCanvas class
        // Manages positions of 3D coordinates, Vertices, Faces etc.
        // Manages projection onto the 2D screen /w dimension scaling to corect characer shape.

        Vector cameraV; // Direction of camera, interpreted such that +z always points up
        Vector screenOffset; // 2D Linear offset of the screen center
        double zoomX;
        double zoomY;
        public List<Vertex> vertices = new List<Vertex>(); // All currently active vertices
        public List<Face> faces = new List<Face>(); // All currently active faces

        public void AddFace(Shape shape, Face face)
        {
            // BUG, if face contains vertices from multipe shapes can result in funky behaviour
            faces.Add(face);
            shape.faces.Add(face);
            foreach (Vertex vertex in face.vertices)
                if (!vertices.Contains(vertex))
                    vertices.Add(vertex);
        }

        public void DeleteShape(Shape shape)
        {
            if (shape is null) return;
            foreach (Vertex vertex in shape.vertices)
            {
                vertices.Remove(vertex);
            }
            shape.vertices = new List<Vertex>();
            foreach (Face face in shape.faces)
            {
                faces.Remove(face);
            }
            shape.faces = new List<Face>();
        }

        public void ProjectVertices()
        {
            // Generate projection coordinates for every active vertex
            // Projection matrix is precalculated and then applied to every vertex at once
            Vector e3 = new Vector(new double[] { 0, 0, 1 });
            Matrix pInv = new Matrix(3, 3);
            pInv[0] = 1 / zoomX * Vector.Cross(cameraV, e3).Normal();
            pInv[1] = 1 / zoomY * Vector.Cross(pInv[0], cameraV).Normal();
            pInv[2] = cameraV.Normal();
            Matrix projection = pInv.Inverse();

            for (int i = 0; i < vertices.Count; i++)
                vertices[i].Projection = projection * vertices[i].Position3D;

            SortFaces();
        }

        /* Figure out which stuff is in front, draw everything from back to front.
         * Most face movement will be subtle => the order of faces will change very little.
         * Insertion sort on nearly sorted input is nearly linear.
        */
        public void SortFaces()
        {
            foreach (Face face in faces)
            {
                double total = 0;
                foreach (Vertex vertex in face.vertices)
                    total += vertex.Projection[2];

                face.screenDistance = total / face.vertices.Length;
            }

            for (int i = 0; i < faces.Count; i++)
            {
                Face face = faces[i];
                int j = i;
                while (j > 0 && faces[j - 1].screenDistance < face.screenDistance)
                {
                    faces[j] = faces[j - 1];
                    j--;
                }
                faces[j] = face;
            }
        }

        public void Draw(Face face)
        {
            Vector P1 = screenOffset + face.vertices[0].Projection;
            switch (face.vertices.Length)
            {
                case 1:
                    {
                        DrawText("" + face.texture, (int)P1[0], (int)(P1[1] * 1));
                        break;
                    }
                case 2:
                    {
                        Vector P2 = screenOffset + face.vertices[1].Projection;
                        DrawLine(face.texture, (int)P1[0], (int)(P1[1] * 1), (int)P2[0], (int)(P2[1] * 1));
                        break;
                    }
                default:
                    {
                        Vector P2temp = screenOffset + face.vertices[1].Projection;
                        DrawLine(face.texture, (int)P1[0], (int)(P1[1] * 1), (int)P2temp[0], (int)(P2temp[1] * 1));
                        for (int i = 1; i < face.vertices.Length - 1; i++)
                        {
                            Vector P2 = screenOffset + face.vertices[i].Projection;
                            Vector P3 = screenOffset + face.vertices[i + 1].Projection;
                            DrawTriangle(face.texture, (int)P1[0], (int)(P1[1] * 1), (int)P2[0], (int)(P2[1] * 1), (int)P3[0], (int)(P3[1] * 1));
                            DrawLine(face.texture, (int)P1[0], (int)(P1[1] * 1), (int)P3[0], (int)(P3[1] * 1));
                            DrawLine(face.texture, (int)P2[0], (int)(P2[1] * 1), (int)P3[0], (int)(P3[1] * 1));
                        }
                        break;
                    }
            }

        }
        public ShapeManager(int width, int height, Vector cameraV, double zoomX, double zoomY) : base(width, height)
        {
            this.cameraV = cameraV;
            this.zoomX = zoomX;
            this.zoomY = zoomY;

            screenOffset = new Vector(new double[] { width / 2, height / 2, 0 });
        }
    }
}
