using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Yvira.Vm;

public class YviraMatrix
{
    public int Rows { get; }
    public int Cols { get; }
    private readonly double[,] _data;

    public int Length => Rows * Cols;
    public bool IsVector => Rows == 1 || Cols == 1;
    public bool IsSquare => Rows == Cols;

    public YviraMatrix(int rows, int cols)
    {
        if (rows <= 0 || cols <= 0)
            throw new Exception("Erro de Matriz: Dimensões devem ser maiores que zero.");

        Rows = rows;
        Cols = cols;
        _data = new double[rows, cols];
    }

    public YviraMatrix(double[,] data)
    {
        Rows = data.GetLength(0);
        Cols = data.GetLength(1);
        _data = (double[,])data.Clone();
    }

    public static YviraMatrix FromElements(IReadOnlyList<object?> elements)
    {
        if (elements.Count == 0)
        {
            return new YviraMatrix(0, 0);
        }

        // Caso 1: Matriz 2D de Matrizes/Linhas -> [[1, 2], [3, 4]]
        if (elements[0] is YviraMatrix firstRow && firstRow.Rows == 1)
        {
            int rows = elements.Count;
            int cols = firstRow.Cols;
            var mat = new YviraMatrix(rows, cols);

            for (int r = 0; r < rows; r++)
            {
                if (elements[r] is not YviraMatrix rowMat || rowMat.Cols != cols)
                {
                    throw new Exception($"Erro de Matriz: Todas as linhas devem ter o mesmo número de colunas ({cols}).");
                }
                for (int c = 0; c < cols; c++)
                {
                    mat._data[r, c] = rowMat._data[0, c];
                }
            }
            return mat;
        }

        // Caso 2: Vetor 1D -> [1, 2, 3] (armazenado como 1 linha x N colunas)
        var vector = new YviraMatrix(1, elements.Count);
        for (int i = 0; i < elements.Count; i++)
        {
            vector._data[0, i] = Convert.ToDouble(elements[i]);
        }
        return vector;
    }

    public double Get(int row, int col)
    {
        ValidateIndices(row, col);
        return _data[row, col];
    }

    public void Set(int row, int col, double value)
    {
        ValidateIndices(row, col);
        _data[row, col] = value;
    }

    private void ValidateIndices(int row, int col)
    {
        if (row < 0 || row >= Rows || col < 0 || col >= Cols)
            throw new Exception($"Erro de Matriz: Índice [{row}, {col}] fora dos limites da matriz ({Rows}x{Cols}).");
    }

    // Retorna uma linha como novo vetor YviraMatrix (1 x Cols)
    public YviraMatrix GetRow(int row)
    {
        if (row < 0 || row >= Rows)
            throw new Exception($"Erro de Matriz: Linha {row} fora dos limites (total: {Rows}).");

        var result = new YviraMatrix(1, Cols);
        for (int c = 0; c < Cols; c++)
        {
            result._data[0, c] = _data[row, c];
        }
        return result;
    }

    // Retorna uma coluna como novo vetor YviraMatrix (Rows x 1)
    public YviraMatrix GetCol(int col)
    {
        if (col < 0 || col >= Cols)
            throw new Exception($"Erro de Matriz: Coluna {col} fora dos limites (total: {Cols}).");

        var result = new YviraMatrix(Rows, 1);
        for (int r = 0; r < Rows; r++)
        {
            result._data[r, 0] = _data[r, col];
        }
        return result;
    }

    // Transposição de Matriz (Inverte Linhas por Colunas)
    public YviraMatrix Transpose()
    {
        var result = new YviraMatrix(Cols, Rows);
        for (int r = 0; r < Rows; r++)
        {
            for (int c = 0; c < Cols; c++)
            {
                result._data[c, r] = _data[r, c];
            }
        }
        return result;
    }

    // Inversão de Matriz Quadrada usando Eliminação de Gauss-Jordan
    public YviraMatrix Inverse()
    {
        if (!IsSquare)
            throw new Exception($"Erro de Matriz: Somente matrizes quadradas podem ser invertidas. Esta matriz é ({Rows}x{Cols}).");

        int n = Rows;
        double[,] a = (double[,])_data.Clone();
        double[,] inv = new double[n, n];

        // Inicializa matriz identidade
        for (int i = 0; i < n; i++) inv[i, i] = 1.0;

        for (int i = 0; i < n; i++)
        {
            // Pivoteamento parcial
            int pivot = i;
            for (int j = i + 1; j < n; j++)
            {
                if (Math.Abs(a[j, i]) > Math.Abs(a[pivot, i]))
                    pivot = j;
            }

            if (Math.Abs(a[pivot, i]) < 1e-12)
                throw new Exception("Erro de Matriz: Matriz singular/não inversível (determinante zero).");

            // Troca linhas
            for (int k = 0; k < n; k++)
            {
                (a[i, k], a[pivot, k]) = (a[pivot, k], a[i, k]);
                (inv[i, k], inv[pivot, k]) = (inv[pivot, k], inv[i, k]);
            }

            double div = a[i, i];
            for (int k = 0; k < n; k++)
            {
                a[i, k] /= div;
                inv[i, k] /= div;
            }

            for (int j = 0; j < n; j++)
            {
                if (j != i)
                {
                    double factor = a[j, i];
                    for (int k = 0; k < n; k++)
                    {
                        a[j, k] -= factor * a[i, k];
                        inv[j, k] -= factor * inv[i, k];
                    }
                }
            }
        }

        return new YviraMatrix(inv);
    }

    // Cálculo do Determinante usando Decomposição LU / Eliminação Gaussiana
    public double Determinant()
    {
        if (!IsSquare)
            throw new Exception($"Erro de Matriz: Determinante só existe para matrizes quadradas ({Rows}x{Cols}).");

        int n = Rows;
        double[,] a = (double[,])_data.Clone();
        double det = 1.0;

        for (int i = 0; i < n; i++)
        {
            int pivot = i;
            for (int j = i + 1; j < n; j++)
            {
                if (Math.Abs(a[j, i]) > Math.Abs(a[pivot, i]))
                    pivot = j;
            }

            if (Math.Abs(a[pivot, i]) < 1e-12)
                return 0.0;

            if (pivot != i)
            {
                for (int k = 0; k < n; k++)
                {
                    (a[i, k], a[pivot, k]) = (a[pivot, k], a[i, k]);
                }
                det = -det; // Troca de linha inverte o sinal do determinante
            }

            det *= a[i, i];

            for (int j = i + 1; j < n; j++)
            {
                double factor = a[j, i] / a[i, i];
                for (int k = i + 1; k < n; k++)
                {
                    a[j, k] -= factor * a[i, k];
                }
            }
        }

        return Math.Round(det, 8);
    }

    // Traço da Matriz (Soma da diagonal principal)
    public double Trace()
    {
        if (!IsSquare)
            throw new Exception($"Erro de Matriz: Traço só é aplicável a matrizes quadradas ({Rows}x{Cols}).");

        double sum = 0;
        for (int i = 0; i < Rows; i++)
        {
            sum += _data[i, i];
        }
        return sum;
    }

    // Soma de todos os elementos da matriz ou vetor
    public double Sum()
    {
        double sum = 0;
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                sum += _data[r, c];
        return sum;
    }

    // Média de todos os elementos
    public double Mean()
    {
        if (Length == 0) return 0;
        return Sum() / Length;
    }

    // Valor máximo
    public double Max()
    {
        if (Length == 0) return 0;
        double max = _data[0, 0];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (_data[r, c] > max) max = _data[r, c];
        return max;
    }

    // Valor mínimo
    public double Min()
    {
        if (Length == 0) return 0;
        double min = _data[0, 0];
        for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
                if (_data[r, c] < min) min = _data[r, c];
        return min;
    }

    // Resolução de Sistema Linear A * x = b (retorna o vetor x)
    public YviraMatrix Solve(YviraMatrix b)
    {
        // Se b for vetor linha (1 x N), converte para coluna (N x 1) se necessário
        var bCol = (b.Rows == 1 && b.Cols == Rows) ? b.Transpose() : b;

        if (bCol.Rows != Rows)
            throw new Exception($"Erro de Solução: Vetor b possui {bCol.Rows} linhas, mas a matriz A tem {Rows} linhas.");

        var aInv = Inverse();
        return aInv * bCol;
    }

    // Gerador: Matriz de Zeros
    public static YviraMatrix Zeros(int rows, int cols)
    {
        return new YviraMatrix(rows, cols);
    }

    // Gerador: Matriz de Uns (Ones)
    public static YviraMatrix Ones(int rows, int cols)
    {
        var mat = new YviraMatrix(rows, cols);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                mat._data[r, c] = 1.0;
        return mat;
    }

    // Gerador: Matriz Identidade
    public static YviraMatrix Eye(int size)
    {
        var mat = new YviraMatrix(size, size);
        for (int i = 0; i < size; i++)
            mat._data[i, i] = 1.0;
        return mat;
    }

    // Gerador: Matriz Aleatória
    public static YviraMatrix Random(int rows, int cols, double min = 0.0, double max = 1.0)
    {
        var rnd = new Random();
        var mat = new YviraMatrix(rows, cols);
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                mat._data[r, c] = Math.Round(min + (rnd.NextDouble() * (max - min)), 2);
        return mat;
    }

    // Operações Matemáticas: Soma de Matrizes ou Matriz + Escalar
    // Subtração de Matrizes ou Matriz - Escalar
    public static YviraMatrix operator -(YviraMatrix a, object b)
    {
        if (b is YviraMatrix bm)
        {
            if (a.Rows != bm.Rows || a.Cols != bm.Cols)
                throw new Exception($"Erro de Matriz: Dimensões incompatíveis para subtração ({a.Rows}x{a.Cols} vs {bm.Rows}x{bm.Cols}).");

            var res = new YviraMatrix(a.Rows, a.Cols);
            for (int r = 0; r < a.Rows; r++)
                for (int c = 0; c < a.Cols; c++)
                    res._data[r, c] = a._data[r, c] - bm._data[r, c];
            return res;
        }

        double scalar = Convert.ToDouble(b);
        var scalarRes = new YviraMatrix(a.Rows, a.Cols);
        for (int r = 0; r < a.Rows; r++)
            for (int c = 0; c < a.Cols; c++)
                scalarRes._data[r, c] = a._data[r, c] - scalar;
        return scalarRes;
    }

    public static YviraMatrix operator +(YviraMatrix a, object b)
    {
        if (b is YviraMatrix bm)
        {
            if (a.Rows != bm.Rows || a.Cols != bm.Cols)
                throw new Exception($"Erro de Matriz: Dimensões incompatíveis para soma ({a.Rows}x{a.Cols} vs {bm.Rows}x{bm.Cols}).");

            var res = new YviraMatrix(a.Rows, a.Cols);
            for (int r = 0; r < a.Rows; r++)
                for (int c = 0; c < a.Cols; c++)
                    res._data[r, c] = a._data[r, c] + bm._data[r, c];
            return res;
        }

        double scalar = Convert.ToDouble(b);
        var scalarRes = new YviraMatrix(a.Rows, a.Cols);
        for (int r = 0; r < a.Rows; r++)
            for (int c = 0; c < a.Cols; c++)
                scalarRes._data[r, c] = a._data[r, c] + scalar;
        return scalarRes;
    }

    // Operações Matemáticas: Multiplicação de Matriz por Escalar ou Multiplicação Matricial (Dot Product)
    public static YviraMatrix operator *(YviraMatrix a, object b)
    {
        if (b is YviraMatrix bm)
        {
            // Multiplicação Matricial (A x B)
            if (a.Cols != bm.Rows)
                throw new Exception($"Erro de Matriz: Dimensões incompatíveis para multiplicação matricial ({a.Rows}x{a.Cols} * {bm.Rows}x{bm.Cols}). As colunas de A devem ser iguais às linhas de B.");

            var res = new YviraMatrix(a.Rows, bm.Cols);
            for (int i = 0; i < a.Rows; i++)
            {
                for (int j = 0; j < bm.Cols; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < a.Cols; k++)
                    {
                        sum += a._data[i, k] * bm._data[k, j];
                    }
                    res._data[i, j] = sum;
                }
            }
            return res;
        }

        double scalar = Convert.ToDouble(b);
        var scalarRes = new YviraMatrix(a.Rows, a.Cols);
        for (int r = 0; r < a.Rows; r++)
            for (int c = 0; c < a.Cols; c++)
                scalarRes._data[r, c] = a._data[r, c] * scalar;
        return scalarRes;
    }

    public override string ToString()
    {
        if (Rows == 1)
        {
            // Vetor 1D: [1, 2, 3]
            var sbVec = new StringBuilder("[");
            for (int c = 0; c < Cols; c++)
            {
                sbVec.Append(_data[0, c].ToString(CultureInfo.InvariantCulture));
                if (c < Cols - 1) sbVec.Append(", ");
            }
            sbVec.Append("]");
            return sbVec.ToString();
        }

        var sb = new StringBuilder("\n");
        for (int r = 0; r < Rows; r++)
        {
            sb.Append("  [ ");
            for (int c = 0; c < Cols; c++)
            {
                sb.Append(string.Format(CultureInfo.InvariantCulture, "{0,6:0.##}", _data[r, c]));
                if (c < Cols - 1) sb.Append(", ");
            }
            sb.Append(" ]\n");
        }
        return sb.ToString();
    }
}
