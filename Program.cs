using System;
using System.Text;
using System.Threading;
using System.Collections.Generic;

namespace RubiksConsole
{
    class RubiksCube
    {
        // Rubiks cube Singleton Class. Handles its own rotation, Coloring, Drawing etc.

        int size;
        int angleStep;
        char[] colors;
        ShapeManager Renderer;
        public Shape[,,] cubies;
        Matrix[] rotationMatrices;

        // Not implemented in the Matrix class because I think this way is dumb.
        // I'd like it to be able to generate any N-dimentional rot matrix for any plane of rotation,
        // But I don't need to do that and am lazy
        Matrix RotMatrix(int dim1, int dim2, double angle)
        {
            Matrix rot = new Matrix(3, true);
            rot[dim1, dim1] = Math.Cos(angle);
            rot[dim2, dim1] = Math.Sin(angle);
            rot[dim1, dim2] = -Math.Sin(angle);
            rot[dim2, dim2] = Math.Cos(angle);
            return rot;
        }

        // Renders all rotation frames after one another
        public void Rotate(int axis, int[] layers)
        {
            for (int step = 0; step < angleStep / 4; step++)
            {
                foreach (int layer in layers)
                {
                    if (layer < 0) continue;
                    if (layer >= size) continue;
                    double angle = Math.PI * 2 / angleStep;
                    double invertedAngle = Math.PI * 2 - angle;
                    if (layer >= size || layer < 0) return;
                    for (int i = 0; i < size; i++)
                        for (int j = 0; j < size; j++)
                            if (i == 0 || i == size - 1 || j == 0 || j == size - 1 || layer == 0 || layer == size - 1)
                                switch (axis)
                                {
                                    case 0:
                                        cubies[layer, i, j].Transform(rotationMatrices[0]);
                                        break;
                                    case 1:
                                        cubies[i, layer, j].Transform(rotationMatrices[1]);
                                        break;
                                    case 2:
                                        cubies[i, j, layer].Transform(rotationMatrices[2]);
                                        break;
                                    case 3:
                                        cubies[size - layer - 1, i, j].Transform(rotationMatrices[3]);
                                        break;
                                    case 4:
                                        cubies[i, size - layer - 1, j].Transform(rotationMatrices[4]);
                                        break;
                                    case 5:
                                        cubies[i, j, size - layer - 1].Transform(rotationMatrices[5]);
                                        break;
                                }
                }
                Thread.Sleep(10);
                Draw();

            }
            foreach (int layer in layers)
            {
                if (layer >= size || layer < 0) return;
                Shape[,,] snapshot = (Shape[,,])cubies.Clone();
                for (int i = 0; i < size; i++)
                    for (int j = 0; j < size; j++)
                        switch (axis)
                        {
                            case 0:
                                cubies[layer, i, j] = snapshot[layer, size - j - 1, i];
                                break;
                            case 1:
                                cubies[i, layer, j] = snapshot[j, layer, size - i - 1];
                                break;
                            case 2:
                                cubies[i, j, layer] = snapshot[size - j - 1, i, layer];
                                break;
                            case 3:
                                cubies[size - layer - 1, i, j] = snapshot[size - layer - 1, j, size - i - 1];
                                break;
                            case 4:
                                cubies[i, size - layer - 1, j] = snapshot[size - j - 1, size - layer - 1, i];
                                break;
                            case 5:
                                cubies[i, j, size - layer - 1] = snapshot[j, size - i - 1, size - layer - 1];
                                break;
                        }
            }
        }
        // Since we want multiple layers to turn at once, they have to do so on every frame
        // The rotate function therefore needs to work with all layers at once. You cannot rewrite it to work on just one at a time
        // That's why This function seems unintuitive.
        public void Rotate(int axis, int layer)
        {
            Rotate(axis, new int[] { layer });
        }


        public void ResetCube()
        {
            // regenerate all the cubies
            foreach(Shape cubie in cubies)
            {
                Renderer.DeleteShape(cubie);
            }

            cubies = new Shape[size, size, size];

            // Numbers correspond to corner vertices of the cubie, every subarray is one face.
            int[,] faceCodes = { { 0, 1, 2, 3 }, { 0, 1, 5, 4 }, { 1, 2, 6, 5 }, { 2, 3, 7, 6 }, { 3, 0, 4, 7 }, { 4, 5, 6, 7 } };


            // This could probably be made better using 6 double for-loops
            for (int i = 0; i < size; i++)
                for (int j = 0; j < size; j++)
                    for (int k = 0; k < size; k++)
                    {
                        // Do not generate cubies inside the cube
                        if (!(i == 0 || i == size - 1 || j == 0 || j == size - 1 || k == 0 || k == size - 1)) continue;
                        Vector offset = new Vector(new double[] { i - (double)size / 2, j - (double)size / 2, k - (double)size / 2 });

                        Shape cubie = new Shape();
                        List<Vertex> vertices = new List<Vertex>{ new Vertex(0,0,0), new Vertex(size, 0, 0),
                                new Vertex(size, size, 0), new Vertex(0, size, 0), new Vertex(0, 0, size),
                                new Vertex(size, 0, size), new Vertex(size, size, size), new Vertex(0, size, size)};

                        cubie.vertices = vertices;

                        // Only generate the faces visible to the outside
                        if (i == 0)
                            Renderer.AddFace(cubie, new Face(colors[0], new Vertex[] { vertices[3], vertices[0], vertices[4], vertices[7] }));
                        if (j == 0)
                            Renderer.AddFace(cubie, new Face(colors[1], new Vertex[] { vertices[0], vertices[1], vertices[5], vertices[4] }));
                        if (k == 0)
                            Renderer.AddFace(cubie, new Face(colors[2], new Vertex[] { vertices[0], vertices[1], vertices[2], vertices[3] }));
                        if (i == size - 1)
                            Renderer.AddFace(cubie, new Face(colors[3], new Vertex[] { vertices[1], vertices[2], vertices[6], vertices[5] }));
                        if (j == size - 1)
                            Renderer.AddFace(cubie, new Face(colors[4], new Vertex[] { vertices[2], vertices[3], vertices[7], vertices[6] }));
                        if (k == size - 1)
                            Renderer.AddFace(cubie, new Face(colors[5], new Vertex[] { vertices[4], vertices[5], vertices[6], vertices[7] }));

                        cubie.Translate(offset * size);

                        cubies[i, j, k] = cubie;
                    }

        }
        public void Draw()
        {
            // Draw a single frame of yourself to the screen.
            Renderer.Clear();
            Renderer.ProjectVertices();
            foreach (Face face in Renderer.faces)
            {

                Renderer.Draw(face);

            }
            // Includes UI elements because am lazy.
            Renderer.DrawText("wasdqe - rotate faces", 0, 0);
            Renderer.DrawText("zxc - rotate the entire cube", 0, 1);
            Renderer.DrawText("Hold SHIFT - rotate anti-clockwise", 0, 2);
            Renderer.DrawText("r - reset cube", 0, 3);
            Renderer.DrawText("! - 30 move scramble", 0, 4);

            Console.SetCursorPosition(0, 0);
            Console.Write(Renderer.Update());
        }


        public RubiksCube(ShapeManager Renderer, int size)
        {
            this.Renderer = Renderer;
            this.size = size;
            this.angleStep = 32;
            // Should work in most terminals
            colors = new char[]{ '░', '▒', '|', '▓', '█', '-' };

            double angle = Math.PI * 2 / angleStep;

            rotationMatrices = new Matrix[]{ RotMatrix(2,1, angle), RotMatrix(0, 2, angle), RotMatrix(1,0, angle), RotMatrix(1,2, angle), RotMatrix(2,0, angle), RotMatrix(0,1, angle)};

            cubies = new Shape[size, size, size];
            ResetCube();
        }
    }

    class MainClass
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Please resize your terminal window.");
            Console.ReadKey();

            // Number of layers, currently controls work only for 3 and 2.
            int cubeSize = 3;

            // directionthe camera faces. Currently not modified.
            Vector cameraV = new Vector(new double[] { 1, 0.5, 0.5 });
            double zoomY = (double)Math.Min(Console.WindowWidth, Console.WindowHeight) / (cubeSize * (cubeSize + 2));
            double zoomX = zoomY * 2;

            ShapeManager Renderer = new ShapeManager(Console.WindowWidth, Console.WindowHeight-1, cameraV, zoomX, zoomY);

            RubiksCube R = new RubiksCube(Renderer, cubeSize);
            R.Draw();

            // queue of inputs. 
            Queue<char> keys = new Queue<char>();


            while (true)
            {
                if (keys.Count == 0)
                    keys.Enqueue(Console.ReadKey(true).KeyChar);

                // Input processing. I don't know how to make this less ugly.
                switch (keys.Dequeue())
                {
                    case 'w':
                        R.Rotate(3, 0);
                        break;
                    case 'W':
                        R.Rotate(0, cubeSize-1);
                        break;
                    case 'a':
                        R.Rotate(4, 0);
                        break;
                    case 'A':
                        R.Rotate(1, cubeSize - 1);
                        break;
                    case 's':
                        R.Rotate(0, 0);
                        break;
                    case 'S':
                        R.Rotate(3, cubeSize - 1);
                        break;
                    case 'd':
                        R.Rotate(1, 0);
                        break;
                    case 'D':
                        R.Rotate(4, cubeSize - 1);
                        break;
                    case 'e':
                        R.Rotate(5, 0);
                        break;
                    case 'E':
                        R.Rotate(2, cubeSize - 1);
                        break;
                    case 'q':
                        R.Rotate(2, 0);
                        break;
                    case 'Q':
                        R.Rotate(5, cubeSize - 1);
                        break;
                    case 'z':
                        R.Rotate(2, new int[] { 0, 1, 2 });
                        break;
                    case 'Z':
                        R.Rotate(5, new int[] { 0, 1, 2 });
                        break;
                    case 'c':
                        R.Rotate(1, new int[] { 0, 1, 2 });
                        break;
                    case 'C':
                        R.Rotate(4, new int[] { 0, 1, 2 });
                        break;
                    case 'x':
                        R.Rotate(0, new int[] { 0, 1, 2 });
                        break;
                    case 'X':
                        R.Rotate(3, new int[] { 0, 1, 2 });
                        break;
                    case 'r':

                        R.ResetCube();
                        R.Draw();
                        break;
                    case '!':
                        // Shuffle the cube by entering random rotation inputs.
                        Random random = new Random();
                        char[] moves = { 'w', 'W', 'a', 'A', 's', 'S', 'd', 'D', 'q', 'Q', 'e', 'E' };
                        for (int i = 0; i < 30; i++)
                        {
                            keys.Enqueue(moves[random.Next(moves.Length)]);
                        }
                        break;
                }
            }
        }
    }
}