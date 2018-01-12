using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace KiraNet.AspectFlare.Utilities
{
    class GT
    {
        public GT(ILGenerator iL)
        {
            generator = iL;
        }

        struct FixupData
        {
            internal Label m_fixupLabel;
            internal int m_fixupPos;
            internal int m_fixupInstSize;
        }

        private ILGenerator generator;
        private Type IL = typeof(ILGenerator);

        private object Get(string str)
        {
            return IL.GetField(str, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(generator);
        }

        public byte[] BakeByteArray()
        {
            // BakeByteArray is an internal function designed to be called by MethodBuilder to do
            // all of the fixups and return a new byte array representing the byte stream with labels resolved, etc.

            int m_currExcStackCount = (int)Get("m_currExcStackCount");
            int m_length = (int)Get("m_length");
            byte[] m_ILStream = (byte[])Get("m_ILStream");
            int m_fixupCount = (int)Get("m_fixupCount");
            FixupData[] m_fixupData;
            Array arr = ((Array)Get("m_fixupData"));
            m_fixupData = new FixupData[m_fixupCount];
            for(var i = 0; i<m_fixupCount;i++)
            {
                var a = arr.GetValue(i);
                var t = a.GetType();
                m_fixupData[i].m_fixupLabel = (Label)t.GetField("m_fixupLabel", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(a);
                m_fixupData[i].m_fixupPos = (int)t.GetField("m_fixupPos", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(a);
                m_fixupData[i].m_fixupInstSize = (int)t.GetField("m_fixupInstSize", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(a);
            }

            int newSize;
            int updateAddr;
            byte[] newBytes;

            if (m_currExcStackCount != 0)
            {
                throw new ArgumentException("UnclosedExceptionBlock");
            }
            if (m_length == 0)
                return null;

            //Calculate the size of the new array.
            newSize = m_length;

            //Allocate space for the new array.
            newBytes = new byte[newSize];

            //Copy the data from the old array
            Buffer.BlockCopy(m_ILStream, 0, newBytes, 0, newSize);

            //Do the fixups.
            //This involves iterating over all of the labels and
            //replacing them with their proper values.
            for (int i = 0; i < m_fixupCount; i++)
            {
                var pos = GetLabelPos(m_fixupData[i].m_fixupLabel);
                updateAddr = pos - (m_fixupData[i].m_fixupPos + m_fixupData[i].m_fixupInstSize);

                //Handle single byte instructions
                //Throw an exception if they're trying to store a jump in a single byte instruction that doesn't fit.
                if (m_fixupData[i].m_fixupInstSize == 1)
                {
                    //Verify that our one-byte arg will fit into a Signed Byte.
                    if (updateAddr < SByte.MinValue || updateAddr > SByte.MaxValue)
                    {
                        throw new NotSupportedException();
                    }

                    //Place the one-byte arg
                    if (updateAddr < 0)
                    {
                        newBytes[m_fixupData[i].m_fixupPos] = (byte)(256 + updateAddr);
                    }
                    else
                    {
                        newBytes[m_fixupData[i].m_fixupPos] = (byte)updateAddr;
                    }
                }
                else
                {
                    //Place the four-byte arg
                    PutInteger4InArray(updateAddr, m_fixupData[i].m_fixupPos, newBytes);
                }
            }
            return newBytes;
        }

        public int GetLabelPos(Label lbl)
        {
            // Gets the position in the stream of a particular label.
            // Verifies that the label exists and that it has been given a value.
            int m_labelCount = (int)Get("m_labelCount");
            int[] m_labelList = (int[])Get("m_labelList");
            var getLabelValue = typeof(Label).GetMethod("GetLabelValue", BindingFlags.Instance | BindingFlags.NonPublic);

            int index = (int)getLabelValue.Invoke(lbl, null);

            if (index < 0 || index >= m_labelCount)
                throw new ArgumentException("_BadLabel");

            if (m_labelList[index] < 0)
                throw new ArgumentException("BadLabelContent");

            return m_labelList[index];
        }


        public int PutInteger4InArray(int value, int startPos, byte[] array)
        {
            // Puts an Int32 onto the stream. This is an internal routine, so it does not do any error checking.

            array[startPos++] = (byte)value;
            array[startPos++] = (byte)(value >> 8);
            array[startPos++] = (byte)(value >> 16);
            array[startPos++] = (byte)(value >> 24);
            return startPos;
        }
    }
}
