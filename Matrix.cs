using System;
namespace RubiksConsole
{
    public class Vector : Matrix
    {

        public Vector(int n) : base(n, 1) { }
        public Vector(double[] arr) : base(arr.Length, 1)
        {
            for (int i = 0; i < arr.Length; i++)
                grid[i, 0] = arr[i];
        }
        public new double this[int i]
        {
           get => grid[i, 0];
           set => grid[i, 0] = value;
        }
        public static Vector operator +(Vector a, Vector b)
        {
            if (a.N != b.N) return null;
            Vector c = new Vector(a.N);
            for (int i = 0; i < a.N; i++)
                c[i, 0] = a[i, 0] + b[i,0];
            return c;
        }
        public static Vector operator *(double scalar, Vector a)
        {
            Vector b = new Vector(a.N);
            for (int i = 0; i < a.N; i++)
                    b[i, 0] = a[i,0] * scalar;
            return b;
        }
        public static Vector operator *(Vector a, double scalar)
        {
            Vector b = new Vector(a.N);
            for (int i = 0; i < a.N; i++)
                b[i, 0] = a[i, 0] * scalar;
            return b;
        }
        public static Vector operator *(Matrix m, Vector v)
        {
            return (m * (Matrix)v)[0];
        }
        public static Vector Cross(Vector a, Vector b)
        {
            // Standard 3D cross product
            if (a.N != 3 || b.N != 3) return null;
            Vector c = new Vector(3);
            c[0] = a[1] * b[2] - a[2] * b[1];
            c[1] = a[2] * b[0] - a[0] * b[2];
            c[2] = a[0] * b[1] - a[1] * b[0];
            return c;
        }
        public double Length()
        {
            double sum = 0;
            for (int i = 0; i < N; i++)
                sum += this[i]*this[i];
            return Math.Sqrt(sum);
        }

        // Normalize vector
        public Vector Normal() => 1 / Length() * this;
    }
    public class Matrix
    {
        // Implements general matrix operations
        // Destructive: Any operation returns a new object
        private const double EPSILON = 0.00001; // Arithmetic precision
        public double[,] grid;
        public int N => grid.GetUpperBound(0) + 1;
        public int M => grid.GetUpperBound(1) + 1;
        public Matrix(double[,] src)
        {
            grid = (double[,])src.Clone();
        }
        public Matrix(int n, int m)
        {
            grid = new double[n, m];
        }
        public Matrix(int n, bool identity = false)
        {
            grid = new double[n, n];
            if (!identity) return;
            for (int i = 0; i < n; i++)
            {
                grid[i, i] = 1;
            }
        }
        public Vector this[int j]
        {
            get
            {
                Vector c = new Vector(N);
                for (int i = 0; i < N; i++)
                    c[i] = grid[i, j];
                return c;
            }
            set
            {
                if (value.N != N) return;
                for (int i = 0; i < N; i++)
                    grid[i, j] = value[i];
            }
        }
        public double this[int i, int j]
        {
            get => grid[i, j];
            set => grid[i, j] = value;
        }
        public static Matrix operator +(Matrix a, Matrix b)
        {
            if (a.M != b.M || a.N != b.N) return null;
            Matrix c = new Matrix(a.N, a.M);
            for (int i = 0; i < a.N; i++)
                for (int j = 0; j < a.M; j++)
                    c[i, j] = a[i, j] + b[i, j];
            return c;
        }
        public static Matrix operator *(Matrix a, Matrix b)
        {
            if (a.M != b.N) return null;
            Matrix c = new Matrix(a.N, b.M);
            for (int i = 0; i < c.N; i++)
                for (int j = 0; j < c.M; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < a.M; k++)
                        sum += a[i, k] * b[k, j];
                    c[i, j] = sum;
                }
            return c;
        }
        public static Matrix operator *(double scalar, Matrix a)
        {
            Matrix b = new Matrix(a.N, a.M);
            for (int i = 0; i < a.N; i++)
                for (int j = 0; j < a.M; j++)
                {
                    b[i, j] = a[i,j] * scalar;
                }
            return b;
        }
        public Matrix Clone() => new Matrix((double[,])grid.Clone());
        public Matrix Transpose()
        {

            Matrix b = new Matrix(M, N);
            for (int i = 0; i < M; i++)
                for (int j = 0; j < N; j++)
                    b[i, j] = this[j, i];
            return b;

        }
        public Matrix Inverse()
        {
            // Weird-ass algorithm. Similar to Gauss-Jordan, but instead reduces column-wise. pseudocode:
            // for every column x:
            //     multiply row x so that element [x,x] = 1
            //     from every row y (y!=x) subtract row x multiplied by [y,x]
            //     # After every iteration column x = e_x. i.e. 1 on diagonal, 0 everywhere else.
            // Preform same operations to an Identity matrix, at the end return it.
            if (N != M) return null;
            Matrix right = new Matrix(N, true);
            Matrix left = Clone();

            for (int j = 0; j < M; j++)
            {
                // If the diagonal element contains zero, find a row with a non-zero
                // Element in that column and add it.
                if (Math.Abs(left[j, j]) < EPSILON)
                    for (int i = 0; i < N; i++)
                    {
                        if (Math.Abs(left[i, j]) >= EPSILON)
                        {
                            // EPSILON accounts for imprecision inherent to floats, but adds a different problem
                            // If the main row has 0.0015 and this new one is -0.0014, the result would be 0.0001.
                            // Make sure the ABS of the element always increases, check the signs of the numbers.
                            int sign = Math.Sign(left[j, j] * left[i, j]);
                            sign = (sign != 0) ? sign : 1;
                            left.AddRows(j, i, sign);
                            right.AddRows(j, i, sign);

                            break;
                        }
                    }
                // Check if the row addin worked. if not, the entire column is zero and inverse doesn't exist.
                if (Math.Abs(left[j, j]) < EPSILON)
                    return null;

                double diagonal = 1 / left[j, j];
                // multiply row so that the element on the diagonal = 1
                left.MultiplyRow(j, diagonal);
                right.MultiplyRow(j, diagonal);

                // For every row except the one that shares a diagonal element with current collumn...
                for (int i = 0; i < N; i++)
                {
                    if (i == j) continue;

                    // Find its element in the current column...
                    double scalar = left[i, j];

                    // Subtract the row that shared diagonal element with the current for from the current row
                    // As a result the element in the current row and diagonal = 0.
                    left.AddRows(i, j, -scalar);
                    right.AddRows(i, j, -scalar);
                }

            }
            return right;
        }
        // These three are kind of dumb, ideally would implement diferently
        public Matrix MultiplyRow(int row, double scalar)
        {
            for (int j = 0; j < N; j++)
                this[row, j] *= scalar;

            return this;
        }
        public Matrix AddRows(int row, int row2)
        {
            for (int j = 0; j < N; j++)
                this[row, j] += this[row2, j];

            return this;
        }
        public Matrix AddRows(int row, int row2, double scalar)
        {
            for (int j = 0; j < N; j++)
                this[row, j] += this[row2, j] * scalar;

            return this;
        }
    }
}
