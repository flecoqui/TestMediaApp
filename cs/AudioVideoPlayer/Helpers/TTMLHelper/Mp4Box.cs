//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// This code is licensed under the MIT License (MIT).
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
namespace AudioVideoPlayer.Helpers.TTMLHelper
{

    public class Mp4Box
    {
        public static Guid kExtPiffTrackEncryptionBoxGuid = new Guid("{8974DBCE-7BE7-4C51-84F9-7148F9882554}");
        public static Guid kExtProtectHeaderBoxGuid = new Guid("{d08a4f18-10f3-4a82-b6c8-32d8aba183d3}");
        public static Guid kExtProtectHeaderMOOFBoxGuid = new Guid("{a2394f52-5a9b-4f14-a244-6c427c648df4}");
        protected Int32 Length;
        protected string Type;
        protected byte[] Data;
        protected List<Mp4Box> Children;
        protected string Path;
        protected Mp4Box Parent;
        protected int ChildrenOffset = 0;
        public Int32 GetBoxLength()
        {
            return Length;
        }
        public void SetBoxLength(int Len)
        {
            Length = Len;
        }
        public string GetBoxType()
        {
            return Type;
        }
        public void SetBoxType(string t)
        {
            Type = t;
        }
        public byte[] GetBoxData()
        {
            return Data;
        }
        public byte[] GetBoxBytes()
        {
            byte[] result = new byte[Length];
            if(WriteMp4BoxInt32(result, 0, Length))
            {
                if(WriteMp4BoxString(result,4,this.Type))
                {
                    if (WriteMp4BoxData(result, 8, this.Data))
                    {
                        return result;
                    }
                }
            }
            return null;
        }
        public Mp4Box GetParent()
        {
            return Parent;
        }
        public void SetParent(Mp4Box box)
        {
            this.Path = box.GetPath() + "/" + this.GetBoxType();
            Parent = box;
        }
        public void  SetPath(string path)
        {
            Path = path;            
        }
        public string GetPath()
        {
            if (string.IsNullOrEmpty(Path))
                Path = "/" + Type;
            return Path;
        }
        public bool RemoveChildBox(string BoxType)
        {
            if (Children != null)
            {
                foreach (var box in Children)
                {
                    if (box.GetBoxType() == BoxType)
                    {
                        Children.Remove(box);
                        return true;
                    }
                }
            }
            return false;
        }
        public Mp4Box FindChildBox(string BoxType)
        {
            if (Children != null)
            {
                foreach (var box in Children)
                {
                    if (box.GetBoxType() == BoxType)
                    {
                        return box;
                    }
                    else
                    {
                        Mp4Box ChildBox = box.FindChildBox(BoxType);
                        if (ChildBox != null)
                            return ChildBox;
                    }
                }
            }
            return null;
        }

        public Mp4Box GetChild(string type)
        {
            if (Children != null)
            {
                foreach (var box in Children)
                {
                    if (box.GetBoxType() == type)
                    {
                        return box;
                    }
                    else
                    {
                        Mp4Box childbox = box.GetChild(type);
                        if (childbox != null)
                            return childbox;
                    }
                }
            }
            return null;
        }


        public byte[] UpdateBoxBuffer()
        {
            byte[] buffer = new byte[this.Length];

            if  (buffer != null)
            {
                int offset = 0;
                WriteMp4BoxInt32(buffer, offset, this.Length);
                offset += 4;
                WriteMp4BoxString(buffer, offset, this.Type);
                offset += 4;
                if (this.Children != null)
                {
                    if (this.ChildrenOffset > 0)
                    {
                        WriteMp4BoxData(buffer, offset, this.Data, this.ChildrenOffset);
                        offset += this.ChildrenOffset;
                    }

                    foreach (var box in Children)
                    {
                        byte[] childBuffer = box.UpdateBoxBuffer();
                        if (childBuffer != null)
                        {
                            WriteMp4BoxData(buffer, offset, childBuffer);
                            offset += childBuffer.Length;
                        }
                    }
                }
                else
                {
                    WriteMp4BoxData(buffer, offset, this.Data);
                }
            }
            return buffer;
        }
        public void UpdateParentLength(Mp4Box box, int Len)
        {
            Mp4Box pbox = box.GetParent();
            while (pbox != null)
            {
                pbox.SetBoxLength(pbox.GetBoxLength() + Len);
                pbox = pbox.GetParent();
            }
        }
        public bool AddMp4Box(Mp4Box box, bool bAddInData = false)
        {
            if (Children == null)
                Children = new List<Mp4Box>();
            if (Children != null)
            {
                box.SetParent(this);
                box.SetPath(this.GetPath() + "/" + box.GetBoxType());
                Children.Add(box);
                if(bAddInData == true)
                {
                    Append(box.Data);
                }
                return true;
            }
            return false;
        }
        public bool Append(byte[] data)
        {
            if ((data != null)&&(Data != null))
            {
                byte[] newData = new byte[data.Length + Data.Length];
                Buffer.BlockCopy(Data, 0, newData, 0, Data.Length);
                Buffer.BlockCopy(data, 0, newData, Data.Length,data.Length);
                Data = newData;
                return true;
            }
            return false;
        }
        public static Mp4Box CreateMp4BoxFromType(string BoxType)
        {
            switch(BoxType)
            {
                //case "ftyp":
                //    return new Mp4BoxFTYP();
                //case "moov":
                //    return new Mp4BoxMOOV();
                //case "mvhd":
                //    return new Mp4BoxMVHD();
                //case "uuid":
                //    return new Mp4BoxUUID();
                //case "trak":
                //    return new Mp4BoxTRAK();
                //case "tkhd":
                //    return new Mp4BoxTKHD();
                //case "mdia":
                //    return new Mp4BoxMDIA();
                //case "mdhd":
                //    return new Mp4BoxMDHD();
                //case "hdlr":
                //    return new Mp4BoxHDLR();
                //case "enca":
                //    return new Mp4BoxENCA();
                //case "encv":
                //    return new Mp4BoxENCV();
                //case "enct":
                //    return new Mp4BoxENCT();
                //case "moof":
                //    return new Mp4BoxMOOF();
                //case "mfhd":
                //    return new Mp4BoxMFHD();
                //case "traf":
                //    return new Mp4BoxTRAF();
                //case "tfhd":
                //    return new Mp4BoxTFHD();
                //case "trun":
                //    return new Mp4BoxTRUN();
                //case "sdtp":
                //    return new Mp4BoxSDTP();
                //case "mfra":
                //    return new Mp4BoxMFRA();
                //case "tfra":
                //    return new Mp4BoxTFRA();
                //case "mfro":
                //    return new Mp4BoxMFRO();
                default:
                    return new Mp4Box();
            }
        }
        public static Mp4Box CreateMp4Box(byte[] buffer, int offset)
        {
            if((buffer != null)&&
                (offset+8 < buffer.Length))
            {
                Mp4Box box = CreateMp4BoxFromType(ReadMp4BoxType(buffer, offset));
                if (box!=null)
                {
                    box.Length = ReadMp4BoxLength(buffer, offset);
                    if ((offset + box.Length <= buffer.Length)&&(box.Length>8))
                    {
                        box.Type = ReadMp4BoxType(buffer, offset);
                        box.SetPath("/" + box.GetBoxType());
                        box.Data = ReadMp4BoxData(buffer, offset, box.Length);
                        List<Mp4Box> list = box.GetChildren();
                        if((list!=null)&&(list.Count>0))
                        {
                            foreach (var b in list)
                                box.AddMp4Box(b);
                        }
                        return box;
                    }
                }
            }
            return null;
        }
        public static Mp4Box CreateEmptyMp4Box(string Type)
        {
            Mp4Box box = new Mp4Box();
            if (box != null)
            {
                box.Length = 8;
                if (!string.IsNullOrEmpty(Type) &&
                    (Type.Length <= 4))
                {
                    box.Type = Type;
                    return box;
                }
            }
            
            return null;
        }
        static public int ReadMp4BoxLength(byte[] buffer, int offset)
        {
            int mp4BoxLen = 0;
            mp4BoxLen |= (int)(buffer[offset + 0] << 24);
            mp4BoxLen |= (int)(buffer[offset + 1] << 16);
            mp4BoxLen |= (int)(buffer[offset + 2] << 8);
            mp4BoxLen |= (int)(buffer[offset + 3] << 0);
            return mp4BoxLen;
        }
        static public int ReadMp4BoxInt24(byte[] buffer, int offset)
        {
            int Len = 0;
            Len |= (int)(buffer[offset + 0] << 16);
            Len |= (int)(buffer[offset + 1] << 8);
            Len |= (int)(buffer[offset + 2] << 0);
            return Len;
        }
        static public int ReadMp4BoxInt32(byte[] buffer, int offset)
        {
            int Len = 0;
            Len |= (int)(buffer[offset + 0] << 24);
            Len |= (int)(buffer[offset + 1] << 16);
            Len |= (int)(buffer[offset + 2] << 8);
            Len |= (int)(buffer[offset + 3] << 0);
            return Len;
        }
        static public int ReadMp4BoxInt16(byte[] buffer, int offset)
        {
            int Len = 0;
            Len |= (int)(buffer[offset + 0] << 8);
            Len |= (int)(buffer[offset + 1] << 0);
            return Len;
        }
        static public string ReadMp4BoxType(byte[] buffer, int offset)
        {
            string s = string.Empty;
            if ((offset + 8) > buffer.Length)
            {
                return s;
            }
            char[] array = new char[4];
            for (int i = 0; i < 4; i++)
                array[0 + i] = (char)buffer[offset + 4 + i];
            
            s = new string(array);
            return s;
        }
        static public byte[] ReadMp4BoxData(byte[] buffer, int offset, int Length)
        {
            if ((offset + Length) > buffer.Length)
            {
                return null;
            }
            byte[] array = new byte[Length-8];
            for (int i = 0; i < Length - 8; i++)
                array[i] = buffer[offset + 8 + i];
            return array;
        }
        static public byte[] ReadMp4BoxBytes(byte[] buffer, int offset, int Length)
        {
            if ((offset + Length) > buffer.Length)
            {
                return null;
            }
            byte[] array = new byte[Length];
            for (int i = 0; i < Length; i++)
                array[i] = buffer[offset  + i];
            return array;
        }
        static public bool WriteMp4BoxInt64(byte[] buffer, int offset, Int64 value)
        {
            if (buffer != null)
            {
                buffer[offset++] = (byte)(value >> 56);
                buffer[offset++] = (byte)(value >> 48);
                buffer[offset++] = (byte)(value >> 40);
                buffer[offset++] = (byte)(value >> 32);
                buffer[offset++] = (byte)(value >> 24);
                buffer[offset++] = (byte)(value >> 16);
                buffer[offset++] = (byte)(value >> 8);
                buffer[offset++] = (byte)(value >> 0);
                return true;
            }
            return false;
        }
        static public bool WriteMp4BoxInt32(byte[] buffer, int offset, Int32 value)
        {
            if (buffer != null)
            {
                buffer[offset++] = (byte)(value >> 24);
                buffer[offset++] = (byte)(value >> 16);
                buffer[offset++] = (byte)(value >> 8);
                buffer[offset++] = (byte)(value >> 0);
                return true;
            }
            return false;
        }
        static public bool WriteMp4BoxInt24(byte[] buffer, int offset, Int32 value)
        {
            if (buffer != null)
            {
                buffer[offset++] = (byte)(value >> 16);
                buffer[offset++] = (byte)(value >> 8);
                buffer[offset++] = (byte)(value >> 0);
                return true;
            }
            return false;
        }
        static public bool WriteMp4BoxInt16(byte[] buffer, int offset, Int16 value)
        {
            if (buffer != null)
            {
                buffer[offset++] = (byte)(value >> 8);
                buffer[offset++] = (byte)(value >> 0);
                return true;
            }
            return false;
        }
        static public bool WriteMp4BoxInt8(byte[] buffer, int offset, Int16 value)
        {
            if (buffer != null)
            {
                buffer[offset++] = (byte)(value >> 0);
                return true;
            }
            return false;
        }
        static public bool WriteMp4BoxByte(byte[] buffer, int offset, byte value)
        {
            if (buffer != null)
            {
                buffer[offset++] = (byte)(value);
                return true;
            }
            return false;
        }
        static public Int64 GetMp4BoxTime(DateTime time)
        {
            DateTime Begin = new DateTime(1904, 1, 1, 0, 0, 0);
            TimeSpan t = time - Begin;
            Int64 Total = (Int64) t.TotalSeconds;
            return Total;
        }

        static public bool WriteMp4BoxString(byte[] buffer, int offset, string Message)
        {
            if ((buffer != null)&&(!string.IsNullOrEmpty(Message)))
            {
                char[] array = Message.ToCharArray();
                if(offset + array.Length <= buffer.Length)
                {
                    for(int i = 0; i < array.Length; i++)
                    {
                        buffer[offset+i] = (byte)array[i];
                    }
                    return true;
                }
            }
            return false;
        }
        static public bool WriteMp4BoxString(byte[] buffer, int offset, string Message, int MessageLength)
        {
            if ((buffer != null) && (!string.IsNullOrEmpty(Message)))
            {
                char[] array = Message.ToCharArray();
                if ((array.Length >= MessageLength) && (offset + MessageLength <= buffer.Length))
                {
                    for (int i = 0; i < MessageLength; i++)
                    {
                        buffer[offset + i] = (byte)array[i];
                    }
                    return true;
                }
            }
            return false;
        }
        static public bool WriteMp4BoxData(byte[] buffer, int offset, byte[] data)
        {
            if ((buffer != null) && (data != null))
            {
                if (offset + data.Length <= buffer.Length)
                {
                    for (int i = 0; i < data.Length; i++)
                    {
                        buffer[offset + i] = (byte)data[i];
                    }
                    return true;
                }
            }
            return false;
        }
        static public bool WriteMp4BoxData(byte[] buffer, int offset, byte[] data, int Length)
        {
            if ((buffer != null) && (data != null))
            {
                if (offset + Length <= buffer.Length)
                {
                    for (int i = 0; i < Length; i++)
                    {
                        buffer[offset + i] = (byte)data[i];
                    }
                    return true;
                }
            }
            return false;
        }
        public override string ToString()
        {
            return "Box: " + Type + "\tLength: " + Length.ToString();
        }
        public static Mp4Box ReadMp4Box(FileStream fs)
        {
            Mp4Box box = null;
            if (fs != null)
            {
                byte[] buffer = new byte[4];
                if (buffer!=null)
                {
                    if (fs.Read(buffer, 0, 4) == 4)
                    {
                        int mp4BoxLen = 0;
                        mp4BoxLen |= (int)(buffer[0] << 24);
                        mp4BoxLen |= (int)(buffer[1] << 16);
                        mp4BoxLen |= (int)(buffer[2] << 8);
                        mp4BoxLen |= (int)(buffer[3] << 0);
                        if(mp4BoxLen >= 8)
                        {
                            buffer = new byte[mp4BoxLen];
                            if(buffer!=null)
                            {
                                WriteMp4BoxInt32(buffer, 0, mp4BoxLen);
                                if (fs.Read(buffer, 4, mp4BoxLen-4) == (mp4BoxLen - 4))
                                {
                                    return CreateMp4Box(buffer, 0);
                                }
                            }
                        }
                    }
                }
            }
            return box;
        }
        public static bool WriteMp4Box(Mp4Box box, FileStream fs)
        {
            bool result = false;
            if ((box != null)&&(fs != null))
            {
                try
                {
                    byte[] header = new byte[8];
                    Mp4Box.WriteMp4BoxInt32(header, 0, box.Length);
                    Mp4Box.WriteMp4BoxString(header, 4, box.GetBoxType());
                    fs.Write(header, 0, 8);
                    fs.Write(box.Data, 0, box.Data.Length);
                    result = true;
                }
                catch(Exception ex)
                {
                    System.Diagnostics.Debug.Write("Exception while writing box in file: " + ex.Message);
                }
            }
            return result;
        }
        public static bool WriteMp4BoxBuffer(byte[] buffer, FileStream fs)
        {
            bool result = false;
            if ((buffer != null) && (fs != null))
            {
                try
                {
                    fs.Write(buffer, 0, buffer.Length);
                    result = true;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Write("Exception while writing buffer box in file: " + ex.Message);
                }
            }
            return result;
        }
        public List<Mp4Box> GetChildren()
        {
            List<Mp4Box> list = new List<Mp4Box>();
            if ((list!=null)&&(Data!=null))
            {
                ChildrenOffset = 0;
                if (this.GetBoxType() == "stsd")
                    ChildrenOffset = 8;
                else if (this.GetBoxType() == "dref")
                    ChildrenOffset = 8;
                else if (this.GetBoxType() == "encv")
                    ChildrenOffset = 78;
                else if (this.GetBoxType() == "enca")
                    ChildrenOffset = 28;
                int Offset = ChildrenOffset;
                while (Offset < Data.Length)
                {
                    Mp4Box box = CreateMp4Box(Data, Offset);
                    if (box != null)
                    {
                        list.Add(box);
                        Offset += box.Length;
                    }
                    else
                        break;
                }
            }
            return list;
        }
        public static string GetBoxChildrenString(int level, Mp4Box box)
        {
            string result = string.Empty;
            int locallevel = level + 1;
            if(box!=null)
            {
                List<Mp4Box> list = box.GetChildren();
                if((list!=null)&&(list.Count>0))
                {
                    foreach(var m in list)
                    {
                        string prefix = string.Empty;
                        for (int i = 0; i < locallevel; i++) prefix += "\t\t";
                        result += prefix + m.ToString() + "\r\n";
                        result += GetBoxChildrenString(locallevel, m);
                    }
                }
            }
            return result;
        }

 
        public static byte[] HexStringToByteArray(string str)
        {
            if (string.IsNullOrEmpty(str))
                return null;
            Dictionary<string, byte> hexindex = new Dictionary<string, byte>();
            for (int i = 0; i <= 255; i++)
                hexindex.Add(i.ToString("X2"), (byte)i);

            List<byte> hexres = new List<byte>();
            for (int i = 0; i < str.Length; i += 2)
                hexres.Add(hexindex[str.Substring(i, 2).ToUpper()]);

            return hexres.ToArray();
        }
        public static byte[] GetSPSNALUContent(string HexString)
        {
            // Hack to be confirmed
            if(!string.IsNullOrEmpty(HexString))
            {
                string[] value = HexString.Split(new[] { "00000001" }, StringSplitOptions.None);
                if((value!=null)&&(value.Length == 3))
                {
                    return HexStringToByteArray(value[1]);
                }
            }
            return null;
        }
        public static byte[] GetPPSNALUContent(string HexString)
        {
            // Hack to be confirmed
            if (!string.IsNullOrEmpty(HexString))
            {
                string[] value = HexString.Split(new[] { "00000001" }, StringSplitOptions.None);
                if ((value != null) && (value.Length == 3))
                {
                    return HexStringToByteArray(value[2]);
                }
            }
            return null;
        }
        static Guid GetKIDFromProtectionData(string data)
        {
            Guid result = Guid.Empty;
            var base64EncodedBytes = System.Convert.FromBase64String(data);
            if (base64EncodedBytes != null)
            {
                int Len = ReadMp4BoxInt16(base64EncodedBytes, 0);
                if (Len == base64EncodedBytes.Length)
                {
                    string s = System.Text.Encoding.Unicode.GetString(base64EncodedBytes,10,Len-10);
                    if (!string.IsNullOrEmpty(s))
                    {
                        int start = s.IndexOf("<KID>");
                        int end = s.IndexOf("</KID>");
                        if ((start > 0) && (end > 0) && (start < end))
                        {
                            byte[] KIDBytes = System.Convert.FromBase64String(s.Substring(start + 5, end - start - 5));
                            if (KIDBytes != null)
                            {
                                result = new Guid(KIDBytes);
                            }
                        }
                    }
                }
            }
            return result;
        }

    }
}
