﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class LZSSEncoder
{
    public List<byte> LZSSCompress(string inputText, out List<byte> literals, out List<byte> matchLengths, out List<ushort> backwardDistances)
    {
        List<byte> byteStream = ConvertTextToByteStream(inputText);
        return CompressWithLZ77(byteStream, out literals, out matchLengths, out backwardDistances);
    }

    private List<byte> ConvertTextToByteStream(string fileContent)
    {
        Dictionary<char, byte> charToByteMap = CreateCharacterMapping(fileContent);
        List<byte> byteStream = new List<byte>();
        foreach (char C in fileContent)
        {
            byteStream.Add(charToByteMap[C]);
        }
        return byteStream;
    }

    private Dictionary<char, byte> CreateCharacterMapping(string fileContent)
    {
        Dictionary<char, byte> charToByte = new Dictionary<char, byte>();
        byte value = 0;
        foreach (char C in fileContent)
        {
            if (!charToByte.ContainsKey(C))
            {
                charToByte.Add(C, value);
                value++;
            }
        }
        return charToByte;
    }

    private List<byte> CompressWithLZ77(List<byte> input, out List<byte> literals, out List<byte> matchLengths, out List<ushort> backwardDistances)
    {
        const int SearchBufferSize = 512 * 1024;
        const int LookaheadBufferSize = 259;
        const int MinMatchLength = 4;

        List<bool> bitStream = new List<bool>();
        literals = new List<byte>();
        matchLengths = new List<byte>();
        backwardDistances = new List<ushort>();

        int inputLength = input.Count;
        int currentIndex = 0;
        Dictionary<string, List<int>> substringTable = new Dictionary<string, List<int>>();

        while (currentIndex < inputLength)
        {
            int maxMatchLength = 0;
            int bestMatchDistance = 0;

            if (currentIndex >= MinMatchLength)
            {
                string keyString = GetSubstringKey(input, currentIndex, MinMatchLength);
                if (substringTable.ContainsKey(keyString))
                {
                    foreach (int startIndex in substringTable[keyString])
                    {
                        int matchLength = 0;
                        while (currentIndex + matchLength < inputLength &&
                               matchLength < LookaheadBufferSize &&
                               input[startIndex + matchLength] == input[currentIndex + matchLength])
                        {
                            matchLength++;
                        }

                        if (matchLength > maxMatchLength)
                        {
                            maxMatchLength = matchLength;
                            bestMatchDistance = currentIndex - startIndex;
                        }
                    }
                }
            }

            if (maxMatchLength >= MinMatchLength)
            {
                bitStream.Add(true);
                AddBits(bitStream, bestMatchDistance, 19);
                AddBits(bitStream, maxMatchLength - MinMatchLength, 8);

                // Store match details
                backwardDistances.Add((ushort)bestMatchDistance);
                matchLengths.Add((byte)(maxMatchLength - MinMatchLength));

                currentIndex += maxMatchLength;
            }
            else
            {
                bitStream.Add(false);
                AddBits(bitStream, input[currentIndex], 8);

                // Store literal
                literals.Add(input[currentIndex]);

                currentIndex++;
            }

            if (currentIndex >= MinMatchLength)
            {
                string keyString = GetSubstringKey(input, currentIndex - MinMatchLength, MinMatchLength);
                if (!substringTable.ContainsKey(keyString))
                {
                    substringTable[keyString] = new List<int>();
                }
                substringTable[keyString].Add(currentIndex - MinMatchLength);
            }
        }

        return ConvertBitStreamToBytes(bitStream);
    }

private void AddBits(List<bool> bitStream, int value, int numBits)
    {
        for (int i = numBits - 1; i >= 0; i--)
        {
            bitStream.Add(((value >> i) & 1) == 1);
        }
    }

    private string GetSubstringKey(List<byte> data, int startIndex, int length)
    {
        return string.Join("", data.Skip(startIndex).Take(length).Select(b => b.ToString()));
    }

    private List<byte> ConvertBitStreamToBytes(List<bool> bitStream)
    {
        int padding = (8 - (bitStream.Count % 8)) % 8;
        List<byte> output = new List<byte> { (byte)padding };

        for (int i = 0; i < bitStream.Count; i += 8)
        {
            byte b = 0;
            for (int j = 0; j < 8 && i + j < bitStream.Count; j++)
            {
                if (bitStream[i + j])
                {
                    b |= (byte)(1 << (7 - j));
                }
            }
            output.Add(b);
        }

        return output;
    }
}