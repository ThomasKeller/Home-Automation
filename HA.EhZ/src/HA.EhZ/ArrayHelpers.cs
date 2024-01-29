using System;
using System.Text;

namespace HA.EhZ;

public static class ArrayHelpers
{
    public static bool CheckDataForSequence(byte[] data, ref int currentPosition, byte[] sequence)
    {
        foreach (var t in sequence)
        {
            var value = data[currentPosition++];
            if (t != value)
                return false;
        }
        return true;
    }

    public static byte[] ReadAndCreateArrayFrom(byte[] source, ref int currentPosition, int numberOfBytes)
    {
        if (numberOfBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(numberOfBytes));
        if (currentPosition < 0)
            throw new ArgumentOutOfRangeException(nameof(currentPosition));
        if (numberOfBytes + currentPosition >= source.Length)
            throw new ArgumentOutOfRangeException(nameof(source));
        var result = new byte[numberOfBytes];
        for (var x = 0; x < numberOfBytes; x++)
            result[x] = source[currentPosition++];
        return result;
    }

    public static string ConvertByteArrayToString(byte[] array)
    {
        return Encoding.Default.GetString(array);
    }

    public static byte[] ConvertStringToByteArray(string data)
    {
        return Encoding.Default.GetBytes(data);
    }

    public static int ConvertTo(byte[] values)
    {
        var result = 0;
        var shiftFactor = 0;
        for (var x = values.Length - 1; x >= 0; x--)
        {
            var resultInt = values[x] << shiftFactor;
            shiftFactor += 8;
            result += resultInt;
        }
        return result;
    }
}