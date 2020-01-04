using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SFGenericModel.Materials;
using SmashForge.Filetypes.Models.Nuds;

namespace SmashForge
{
    public partial class Xfbin : FileBase
    {
        // Xfbin files are always big endian
        public override Endianness Endian { get; set; }
        public FileInfo info;
        private FileData fileData;

        public Dictionary<int, object> files = new Dictionary<int, object>(); // int index, object file
        public Dictionary<int, int> offsets = new Dictionary<int, int>(); // int index, int headerOffset
        public Dictionary<string, int> groupNames = new Dictionary<string, int>(); // string group name, int group byte
        public List<string> nuccProperties = new List<string>();
        public List<string> directories = new List<string>();
        public List<string> fileNames = new List<string>();
        public List<string> boneNames = new List<string>();

        public int paddingFlag;
        public int firstFileStart;
        public int directoryStart;
        public int directoryCount;
        public int fileNameStart;
        public int fileNameCount;
        public int firstPaddingStart;

        public enum Header : uint
        {
            NDP3 = 0x4E445033,
            NTP3 = 0x4E545033,
            bin = 0x3E8,
            bin2 = 0x3EB,
            prmBin = 0xE9030000,
            XML = 0xEFBBBF3C,
            XML2 = 0x3C3F786D,

            //section sizes
            prm_loadBin = 0x48,
            gion = 0x298,

        }

        public Xfbin()
        {
            SetupTreeNode();
        }

        public Xfbin(string filename) : this()
        {
            info = new FileInfo(filename);
            Read(filename);
        }

        private void SetupTreeNode()
        {
            if (info != null)
                Text = info.Name;
            else
                Text = "new.xfbin";
            ImageKey = "model";
            SelectedImageKey = "model";
        }

        public override void Read(string filename)
        {
            fileData = new FileData(filename);
            fileData.endian = Endianness.Big;
            fileData.Seek(0);

            // read header
            string magic = fileData.ReadString(0, 4);
            fileData.Seek(4);
            if (magic.Equals("NUCC"))
                Endian = Endianness.Big;
            else
            {
                if (magic.Substring(0, 3).Equals("CPK"))
                    MessageBox.Show($"Correct CPK extraction using CRI Packed File Maker is required.", "Xfbin is not decrypted");
                else
                    MessageBox.Show($"File is not a valid xfbin.");
                return;
            }

            fileData.endian = Endian;
            paddingFlag = fileData.ReadInt(); // Known for now: 79, 63, 7A; should be short not int
            fileData.Skip(8);

            switch (paddingFlag)
            {
                // Start of the first file's header
                // 0x1C is where you start counting
                // 0x18 and 0xC is the padding after the 2nd padding ends
                case 79:
                    firstFileStart = fileData.ReadInt() + 0x1C + 0x18;
                    break;
                case 63:
                    firstFileStart = fileData.ReadInt() + 0x1C + 0xC;
                    break;
                default:
                    firstFileStart = fileData.ReadInt() + 0x1C + 0x18;
                    break;
            }

            fileData.ReadInt(); // Known: 3, 5
            fileData.ReadShort(); // Same as padding flag; should be short not int
            fileData.ReadShort(); // Unknown for now
            int nuccPropsCount = fileData.ReadInt();

            int nuccPropsSize = fileData.ReadInt();
            directoryStart = 0x44 + nuccPropsSize;
            directoryCount = fileData.ReadInt() - 1; // Number of strings in the section
            fileNameStart = directoryStart + fileData.ReadInt();
            fileNameCount = fileData.ReadInt() - 1; // Page0 and index are included in the count, even after -1

            firstPaddingStart = fileNameStart + fileData.ReadInt();
            fileData.ReadInt(); // somehow similar to fileNameCount
            int firstPaddingSize = fileData.ReadInt(); // First padding length, starts after some 00s after file names
            int secondPaddingSize = fileData.ReadInt() * 4; // Second padding count; each section is 4 bytes

            // mostly 0, but is important for mot files
            firstFileStart += fileData.ReadInt() * 8;

            // Nucc properties
            for (int x = 0; x < nuccPropsCount; x++)
            {
                nuccProperties.Add(fileData.ReadString());
                fileData.Skip(1);
            }

            // Directories/Files
            fileData.Skip(1);
            for (int x = 0; x < directoryCount; x++)
            {
                directories.Add(fileData.ReadString());
                fileData.Skip(1);
            }

            // File names/included files
            for (int x = 0; x < fileNameCount; x++)
            {
                fileData.Skip(1);
                string s = fileData.ReadString();
                if (s == "Page0" || s == "index")
                    continue;
                fileNames.Add(s);
            }
            fileData.Skip(1);

            try
            {
                // Read group and bone names
                int state = 0;
                for (int x = 0; x < fileNames.Count; x++)
                {
                    string s = fileNames[x];
                    if (state == 2)
                    {
                        if (s.Contains(" "))
                            boneNames.Add(s);
                    }
                    else if (s.Contains("trall"))
                    {
                        state = 2;
                        continue;
                    }
                    if (state == 1)
                    {
                        if (!s.Contains(" "))
                        {
                            s = s.Substring(s.IndexOf('_') + 1);
                            if (groupNames.ContainsKey(s))
                                s += "2";
                            groupNames.Add(s, 0);
                        }
                    }
                    if (s.Contains("bod") && !s.Contains(" ") && !s.Contains("body"))
                    {
                        if (state == 0)
                        {
                            state = 1;
                            continue;
                        }
                    }
                }
            }
            catch
            {
                groupNames.Clear();
                MessageBox.Show("Group bytes have to be changed manually.", "Warning", MessageBoxButtons.OK);
            }

            switch(fileData.Pos() % 4)
            {
                case 0:
                    fileData.Skip(0xC);
                    break;
                case 1:
                    fileData.Skip(3);
                    goto case 0;
                case 2:
                    fileData.Skip(2);
                    goto case 0;
                case 3:
                    fileData.Skip(1);
                    goto case 0;
            }

            fileData.Read(firstPaddingSize);
            fileData.Read(secondPaddingSize);

            fileData.Seek(firstFileStart);

            while (fileData.Pos() < fileData.Size() - 4)
            {
                ReadFile();
            }

            fileData.Seek(0);

        }

        public void ReadFile()
        {
            int filesSkipped = 0;
            int lengthSkipped = 0;
            bool skipped = false;

            int headPos = fileData.Pos();
            int headSize = fileData.ReadInt();

            while (headSize == 8 || headSize == 0x2A || headSize == 0)
            {
                skipped = true;
                if (headSize == 0x2A)
                {
                    fileData.Skip(0x32);
                    lengthSkipped += 0x36;
                    filesSkipped++;
                }
                else if (headSize == 8)
                {
                    fileData.Skip(headSize);
                    fileData.Skip(0x14);
                    lengthSkipped += 0x20;
                    filesSkipped++;
                }
                if (fileData.Pos() > fileData.Size() - 4)
                {
                    goto Return;
                }
                headSize = fileData.ReadInt();
            }
            if (skipped) goto Return;

            headPos = fileData.Pos() - 4; // unnecessary
            int index = fileData.ReadInt();
            int padFlag = fileData.ReadInt();
            int temp = fileData.Pos();
            int fileSize = fileData.ReadInt();
            int sizePos = 0;

            //prevent reading miscalculations
            int i = 1;
            if (headSize >= 0x200)
            {
                while (fileSize != headSize - (i * 4) && fileData.Pos() - temp < headSize && i < 10)
                {
                    for (int x = 1; x < 10; x++) //check if group bytes are counted in headSize
                    {
                        if (fileSize == headSize - ((i * 4) + 0x18 + 2 + (x * 4)))
                        {
                            fileData.Skip(0x18);
                            goto Read;
                        }
                        if (fileSize == headSize - ((i * 4) + 2 + (x * 4)))
                        {
                            goto Read;
                        }
                    }
                    sizePos = fileData.Pos();
                    fileSize = fileData.ReadInt();
                    i++;
                }
            }
            if (fileSize != headSize - (i * 4))
            {
                fileSize = headSize;
                fileData.Seek(temp);
            }

        Read:

            int start = fileData.Pos();
            FileData file = new FileData(fileData.Read(fileSize)); // array containing the file

            uint header = file.ReadUInt();
            file.Seek(0);
            switch (header)
            {
                case (uint)Header.NDP3:
                    Nud nud = new Nud(file);
                    nud.filesIndex = files.Count;
                    nud.sizeOffset = sizePos;
                    nud.startOffset = start;
                    offsets.Add(files.Count, headPos);
                    files.Add(files.Count, nud);
                    
                    int groups = fileData.ReadShort();
                    for (int x = 0; x < groups; x++)
                    {
                        int b = fileData.ReadInt();
                        ((Nud.Polygon)nud.FirstNode.Nodes[x]).groupByte = b;
                        if (groupNames.Any() && !groupNames.ContainsValue(b))
                        {
                            KeyValuePair<string, int> p =  groupNames.SkipWhile(pair => pair.Value != 0).First();
                            groupNames[p.Key] = b;
                        }
                    }
                    break;

                case (uint)Header.NTP3:
                    fileData.Seek(start - 0xA);
                    fileData.ReadShort(); //width
                    fileData.ReadShort(); //length
                    fileData.Seek(start + fileSize);

                    try // nsuns texture bug
                    {
                        NUT nut = new NUT(file);
                        nut.filesIndex = files.Count;
                        nut.startOffset = start;
                        nut.headSize = start - 4 - (headPos + 4);
                        offsets.Add(files.Count, headPos);
                        files.Add(files.Count, nut);
                    }
                    catch { }
                    break;

                case (uint)Header.prmBin:
                    break;
                case (uint)Header.bin:
                    break;
                case (uint)Header.bin2:
                    break;
                case (uint)Header.XML:
                    break;
                case (uint)Header.XML2:
                    break;

                default:

                    files.Add(files.Count, new Tuple<int, int>(headPos, headSize + 0xC));

                    /*if ((fileSize - 4) / (header / 0x1000000) == (uint)Header.prm_loadBin)
                    {
                        //Unimplemented
                    }
                    else if ((fileSize - 8) / (header / 0x1000000) == (uint)Header.gion)
                    {
                        //Unimplemented
                    }*/
                    break;
            }
            return;

        Return:
            fileData.Seek(fileData.Pos() - 4);
            files.Add(files.Count, new Tuple<int, int>(headPos, lengthSkipped));
            return;
        }

        public override byte[] Rebuild()
        {
            FileOutput d = new FileOutput();
            d.endian = Endianness.Big;
            fileData.Seek(0);
            d.WriteBytes(fileData.Read(firstFileStart));
            foreach (var f in files)
            {
                if (f.Value is Tuple<int, int>)
                {
                    Tuple<int, int> t = f.Value as Tuple<int, int>;
                    fileData.Seek(t.Item1);
                    if (f.Key == files.Count - 1)
                    {
                        d.WriteBytes(fileData.Read(t.Item2 - 0xC));
                    }
                    else d.WriteBytes(fileData.Read(t.Item2));
                }
                else if (f.Value is Nud)
                {
                    WriteNud(d, (Nud)f.Value);
                }
                else if (f.Value is NUT)
                {
                    WriteNut(d, (NUT)f.Value);
                }
            }

            return d.GetBytes();
        }

        private void WriteNud(FileOutput d, Nud nud)
        {
            byte[] file = nud.Rebuild();

            if (nud.sizeOffset != 0)
            {
                fileData.Seek(nud.sizeOffset);
            }
            else fileData.Seek(nud.startOffset - 4);

            int size = fileData.ReadInt();

            fileData.Seek(nud.startOffset);
            fileData.Skip(size);
            int groups = fileData.ReadShort();

            // Get difference in size
            int diff = file.Length - size;
            diff += (nud.FirstNode.Nodes.Count - groups) * 4;

            fileData.Seek(offsets[nud.filesIndex]);
            int oldHead = fileData.ReadInt();
            d.WriteInt(oldHead + diff);
            d.WriteBytes(fileData.GetSection(fileData.Pos(), nud.sizeOffset - fileData.Pos()));
            d.WriteUInt(nud.fileSize);
            if (nud.sizeOffset != 0)
            {
                d.WriteBytes(fileData.GetSection(nud.sizeOffset + 4, nud.startOffset - (nud.sizeOffset + 4)));
            }
            d.WriteBytes(file);

            d.WriteShort(nud.FirstNode.Nodes.Count);
            foreach (Nud.Polygon p in  nud.FirstNode.Nodes)
            {
                d.WriteInt(p.groupByte);
            }
        }

        private void WriteNut(FileOutput d, NUT nut)
        {
            byte[] file = nut.Rebuild();

            // Get difference in size
            fileData.Seek(nut.startOffset - 4);
            int size = fileData.ReadInt();
            int diff = file.Length - size;

            fileData.Seek(offsets[nut.filesIndex]);
            int oldHead = fileData.ReadInt();
            d.WriteInt(oldHead + diff);
            d.WriteBytes(fileData.GetSection(fileData.Pos(), nut.headSize));
            d.WriteUInt(nut.fileSize);
            d.WriteBytes(file);
        }
    }
}

