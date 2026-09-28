using System;
using System.Text;

namespace RubiksConsole
{
    public class CharacterCanvas
    {
        /* 2D character canvas-like object 
        */
        
        string Background; // What to blank the screen into
        StringBuilder FrameBuffer; // Actual canvas memory
        public int Width { get; protected set; }
        public int Height { get; protected set; }

        public CharacterCanvas(int width, int height)
        {
            Width = width;
            Height = height;

            Background = new string(' ', width * height);
            FrameBuffer = new StringBuilder();
            FrameBuffer.Append(Background);
        }

        // Replace the appropriate characters in FrameBuffer with string
        void Write(int position, string S)
        {

            FrameBuffer.Remove(position, S.Length);
            FrameBuffer.Insert(position, S);
        }

        // Clear any currently drawn characters
        public void Clear()
        {
            FrameBuffer.Clear();
            FrameBuffer.Append(Background);
        }

        // Sets the default background to the current screen content
        public void SetBackground()
        {
            Background = FrameBuffer.ToString();
        }

        // 
        public StringBuilder Update() => FrameBuffer;

        // Draws a horizontal line of text
        public void DrawText(string S, int x, int y)
        {
            //TODO FIX: if x >= Width text overflows.
            int absolutePosition = y * Width + x;
            if (absolutePosition > Width * Height) return; // Stop if line is partly outside the console
            Write(absolutePosition, S);
        }

        // Draws a horizontal line
        public void DrawLineH(char C, int x, int y, int length)
        {
            // Find endpoints.
            // +1s are added to make sure the x y point is always on the line
            // which otherwise would not be true for lines extending to the left (length < 0)
            int start = Math.Min(x, x + length + 1);
            int end = Math.Max(x+1, x + length);

            // Stop if line is fully outside the console
            if (y < 0 || y >= Height) return;
            if (start >= Width || end < 0) return;

            // If line partly outside console set it fully inside.
            start = Math.Max(start, 0);
            end = Math.Min(end, Width - 1);

            string S = new string(C, end - start);
            int absolutePosition = y * Width + start;
            Write(absolutePosition, S);
        }

        // Draws a vertical line
        public void DrawLineV(char C, int x, int y, int length)
        {
            // Find endpoints
            int start = Math.Min(y, y + length);
            int end = Math.Max(y, y + length);

            // Stop if line is fully outside the console
            if (x < 0 || x >= Width) return;
            if (start >= Height || end < 0) return;

            // If line partly outside console set it fully inside.
            start = Math.Max(start, 0);
            end = Math.Min(end, Height - 1);

            string S = new string(C, 1);
            for (int i = start; i < end; i++)
            {
                int AbsolutePosition = i * Width + x;
                Write(AbsolutePosition, S);
            }
        }

        // Generates the x positions of points on a line
        // index in output array = y coordinate
        int[] BresenhamGenerator(int x1, int y1, int x2, int y2)
        {
            int sizeX = Math.Abs(x1 - x2);
            int sizeY = Math.Abs(y1 - y2);
            int[] positions = new int[sizeY + 1];
            int ptr = 0;
            int stepX = (x1 < x2) ? 1 : -1;
            int stepY = (y1 < y2) ? 1 : -1;
            int err = sizeX - sizeY;

            while (x1 != x2 || y1 != y2)
            {
                int err2 = 2 * err;
                if (err2 > -sizeY)
                {
                    err -= sizeY;
                    x1 += stepX;
                }
                if (err2 < sizeX)
                {
                    err += sizeX;
                    y1 += stepY;
                    positions[ptr++] = x1;
                }
            }

            if (err * 2 > -sizeY) positions[ptr] = x1 + stepX;
            else positions[ptr] = x1;

            return positions;
        }


        public void DrawLine(char C, int x1, int y1, int x2, int y2)
        {
            // Make sure the line slopes downward for consistency
            if (y1 > y2)
            {
                SwapInts(ref y1, ref y2);
                SwapInts(ref x1, ref x2);
            }
            int[] lengths = BresenhamGenerator(x1, y1, x2, y2);
            int stepY = (y1 < y2) ? 1 : -1; // Check if the line slopes up or down
            int sizeY = Math.Abs(y1 - y2) + 1;

            for (int i = 0; i < sizeY; i++)
            {
                // if horizontal distance doesn't change (slope is > 45 deg), ensures the line still has thickness
                int length = (lengths[i] - x1 != 0) ? lengths[i] - x1 : 1;
                DrawLineH(C, x1, y1, length);
                x1 = lengths[i];
                y1 += stepY;
            }
        }

        // Draws an arbitrary triangle
        public void DrawTriangle(char C, int x1, int y1, int x2, int y2, int x3, int y3)
        {
            //Order the points by height
            if (y1 > y2)
            {
                SwapInts(ref y1, ref y2);
                SwapInts(ref x1, ref x2);
            }
            if (y1 > y3)
            {
                SwapInts(ref y1, ref y3);
                SwapInts(ref x1, ref x3);
            }
            if (y2 > y3)
            {
                SwapInts(ref y2, ref y3);
                SwapInts(ref x2, ref x3);
            }

            // Generate lines for the two smaller sides
            int[] lineC = BresenhamGenerator(x1, y1, x2, y2);
            int[] lineB = BresenhamGenerator(x1, y1, x3, y3);
            int[] lineA = BresenhamGenerator(x2, y2, x3, y3);
            // bottom of lineC is the same height as top of lineA. duplicate is removed.
            int[] lineCA = new int[lineB.Length];
            lineC.CopyTo(lineCA, 0);
            lineA.CopyTo(lineCA, lineC.Length - 1);

            // Two points trace along CA and B, drawing horizontal lines between each other.
            int tracingCA = x1;
            int tracingB = x1;

            // Find out whether the triangle "points" left or right, and potentionaly flip it.
            // Used to reduce the draw distance to not draw over the triangle's boundary.
            if (lineB[lineB.Length / 2] - lineCA[lineB.Length / 2] == -1)
            {
                int[] temp = new int[lineB.Length];
                lineCA.CopyTo(temp, 0);
                lineB.CopyTo(lineCA, 0);
                temp.CopyTo(lineB, 0);
            }

            /* Actual drawing phase - draws a horizontal line for every y coordinate between tracing points.
             * Tracing point movement:
             *    To make sure a line always gets drawn whole, we move the tracing point (x coordinate) 
             *    more to the left or right, depending on which direction the edge slopes.
            */ 
            for (int i = 0; i < lineB.Length;i++)
            {
                int outerAC = Math.Min(tracingCA, lineCA[i]); // Gets direction of the slope
                if (outerAC != tracingCA) outerAC++;
                int outerB = Math.Max(tracingB, lineB[i]); // Gets direction of the slope
                if (outerB != tracingB) outerB--; 

                tracingCA = lineCA[i]; // Copy values for next layer
                tracingB = lineB[i];

                DrawLineH(C, outerAC, y1 + i, outerB - outerAC);
            }
        }
        void SwapInts(ref int a, ref int b)
        {
            int temp = a;
            a = b;
            b = temp;
        }
    }
}
