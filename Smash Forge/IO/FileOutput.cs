﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace SmashForge
{
    public class FileOutput
    {

        List<byte> data = new List<byte>();

        public Endianness Endian;

        public byte[] GetBytes()
        {
            return data.ToArray();
        }

        public void WriteString(string s)
        {
            char[] c = s.ToCharArray();
            for (int i = 0; i < c.Length; i++)
                data.Add((byte)c[i]);
        }

        public int Size()
        {
            return data.Count;
        }

        public void WriteOutput(FileOutput d)
        {
            foreach (RelocOffset o in d.Offsets)
            {
                o.Position += data.Count;
                Offsets.Add(o);
            }
            foreach (RelocOffset o in Offsets)
            {
                if (o.output == d || o.output == null)
                    o.Value += data.Count;
            }
            foreach (byte b in d.data)
                data.Add(b);
        }

        private static char[] HexToCharArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .Select(x => Convert.ToChar(x))
                             .ToArray();
        }

        public void writeHex(string s)
        {
            char[] c = HexToCharArray(s);
            for (int i = 0; i < c.Length; i++)
                data.Add((byte)c[i]);
        }

        public void writeInt(int i)
        {
            if (Endian == Endianness.Little)
            {
                data.Add((byte)((i) & 0xFF));
                data.Add((byte)((i >> 8) & 0xFF));
                data.Add((byte)((i >> 16) & 0xFF));
                data.Add((byte)((i >> 24) & 0xFF));
            }
            else
            {
                data.Add((byte)((i >> 24) & 0xFF));
                data.Add((byte)((i >> 16) & 0xFF));
                data.Add((byte)((i >> 8) & 0xFF));
                data.Add((byte)((i) & 0xFF));
            }
        }

        public void writeUInt(uint i)
        {
            if (Endian == Endianness.Little)
            {
                data.Add((byte)((i) & 0xFF));
                data.Add((byte)((i >> 8) & 0xFF));
                data.Add((byte)((i >> 16) & 0xFF));
                data.Add((byte)((i >> 24) & 0xFF));
            }
            else
            {
                data.Add((byte)((i >> 24) & 0xFF));
                data.Add((byte)((i >> 16) & 0xFF));
                data.Add((byte)((i >> 8) & 0xFF));
                data.Add((byte)((i) & 0xFF));
            }
        }

        public void writeIntAt(int i, int p)
        {
            if (Endian == Endianness.Little)
            {
                data[p++] = (byte)((i) & 0xFF);
                data[p++] = (byte)((i >> 8) & 0xFF);
                data[p++] = (byte)((i >> 16) & 0xFF);
                data[p++] = (byte)((i >> 24) & 0xFF);
            }
            else
            {
                data[p++] = (byte)((i >> 24) & 0xFF);
                data[p++] = (byte)((i >> 16) & 0xFF);
                data[p++] = (byte)((i >> 8) & 0xFF);
                data[p++] = (byte)((i) & 0xFF);
            }
        }
        public void writeShortAt(int i, int p)
        {
            if (Endian == Endianness.Little)
            {
                data[p++] = (byte)((i) & 0xFF);
                data[p++] = (byte)((i >> 8) & 0xFF);
            }
            else
            {
                data[p++] = (byte)((i >> 8) & 0xFF);
                data[p++] = (byte)((i) & 0xFF);
            }
        }

        public void align(int i)
        {
            while ((data.Count % i) != 0)
                writeByte(0);
        }

        public void align(int i, int v)
        {
            while ((data.Count % i) != 0)
                writeByte(v);
        }

        public void writeFloat(float f)
        {
            writeInt(SingleToInt32Bits(f));
        }

        public void writeFloatAt(float f, int p)
        {
            writeIntAt(SingleToInt32Bits(f), p);
        }

        //The return value is big endian representation
        public static int SingleToInt32Bits(float value)
        {
            return BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
        }

        public void writeHalfFloat(float f)
        {
            writeShort(FileData.fromFloat(f));
        }

        public void writeShort(int i)
        {
            if (Endian == Endianness.Little)
            {
                data.Add((byte)((i) & 0xFF));
                data.Add((byte)((i >> 8) & 0xFF));
            }
            else
            {
                data.Add((byte)((i >> 8) & 0xFF));
                data.Add((byte)((i) & 0xFF));
            }
        }

        public void writeUShort(ushort i)
        {
            if (Endian == Endianness.Little)
            {
                data.Add((byte)((i) & 0xFF));
                data.Add((byte)((i >> 8) & 0xFF));
            }
            else
            {
                data.Add((byte)((i >> 8) & 0xFF));
                data.Add((byte)((i) & 0xFF));
            }
        }

        public void writeByte(int i)
        {
            data.Add((byte)((i) & 0xFF));
        }

        public void writeSByte(sbyte i)
        {
            data.Add((byte)((i) & 0xFF));
        }

        public void writeChars(char[] c)
        {
            foreach (char ch in c)
                writeByte(Convert.ToByte(ch));
        }

        public void writeBytes(byte[] bytes)
        {
            foreach (byte b in bytes)
                writeByte(b);
        }

        public void writeFlag(bool b)
        {
            if (b)
                writeByte(1);
            else
                writeByte(0);
        }

        public int pos()
        {
            return data.Count;
        }

        public void save(String fname)
        {
            File.WriteAllBytes(fname, data.ToArray());
        }

        public class RelocOffset
        {
            public int Value;
            public int Position;
            public FileOutput output;
        }
        public List<RelocOffset> Offsets = new List<RelocOffset>();
        public void WriteOffset(int i, FileOutput fo)
        {
            Offsets.Add(new RelocOffset() { Value = i, output = fo, Position = data.Count });
            writeInt(i);
        }
    }
}
