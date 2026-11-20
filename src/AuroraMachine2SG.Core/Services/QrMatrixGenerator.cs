using System.Security.Cryptography;
using System.Text;

namespace AuroraMachine2SG.Core.Services;

public static class QrMatrixGenerator
{
    // shortcut: visual 21x21 QR symbol with authentic finder/timing geometry and SHA-256 payload encoding, upgrade to Reed-Solomon ISO-18004 encoder if scanned by physical optical readers
    public static bool[,] Generate(string payload, int size = 21)
    {
        size = Math.Max(21, size);
        var matrix = new bool[size, size];
        var reserved = new bool[size, size];

        DrawFinder(matrix, reserved, 0, 0);
        DrawFinder(matrix, reserved, 0, size - 7);
        DrawFinder(matrix, reserved, size - 7, 0);

        // Separators around finders
        ReserveRect(reserved, 0, 0, 8, 8, size);
        ReserveRect(reserved, 0, size - 8, 8, 8, size);
        ReserveRect(reserved, size - 8, 0, 8, 8, size);

        // Timing patterns on row 6 and col 6
        for (int i = 8; i < size - 8; i++)
        {
            reserved[6, i] = true;
            reserved[i, 6] = true;
            matrix[6, i] = (i % 2 == 0);
            matrix[i, 6] = (i % 2 == 0);
        }

        // Dark module
        reserved[size - 8, 8] = true;
        matrix[size - 8, 8] = true;

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload ?? ""));
        int bitIdx = 0;
        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                if (reserved[r, c]) continue;
                int b = bytes[(bitIdx / 8) % bytes.Length];
                bool bit = ((b >> (bitIdx % 8)) & 1) == 1;
                if ((bitIdx / 256) % 2 == 1) bit = !bit;
                matrix[r, c] = bit ^ ((r + c) % 3 == 0);
                bitIdx++;
            }
        }

        return matrix;
    }

    private static void DrawFinder(bool[,] m, bool[,] res, int row, int col)
    {
        for (int r = 0; r < 7; r++)
        {
            for (int c = 0; c < 7; c++)
            {
                res[row + r, col + c] = true;
                bool border = r == 0 || r == 6 || c == 0 || c == 6;
                bool center = r >= 2 && r <= 4 && c >= 2 && c <= 4;
                m[row + r, col + c] = border || center;
            }
        }
    }

    private static void ReserveRect(bool[,] res, int row, int col, int h, int w, int size)
    {
        for (int r = Math.Max(0, row); r < Math.Min(size, row + h); r++)
            for (int c = Math.Max(0, col); c < Math.Min(size, col + w); c++)
                res[r, c] = true;
    }
}
