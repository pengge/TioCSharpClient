using System;
using System.Collections.Generic;
using System.Text;

namespace TioCSharpClient
{
    public class UPacket
    {
        /// <summary>
        /// 封包
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static byte[] Pack(byte[] buffer)
        {
            byte type = new byte();
            byte[] lenbytes = new byte[4];
            //byte[] content;
#if _DEGUG_
           /* Console.WriteLine("发本来的长度=" + buffer.Length.ToString());
             byte[] lastBuffers = new byte[1 + 4 + buffer.Length];
            lastBuffers[0] = (byte)1;//第一部分 类型
            lenbytes = BitConverter.GetBytes(buffer.Length);
            Array.Copy(lenbytes, 0, lastBuffers, 1, lenbytes.Length);//第二部分 长度
            Array.Copy(buffer, 0, lastBuffers, 1 + lenbytes.Length, buffer.Length);//第三部分 内容
            */
#endif
            byte[] lastBuffers = new byte[4 + buffer.Length];
            lenbytes = BitConverter.GetBytes(ReverseBytes((UInt32)buffer.Length));
            
            Array.Copy(lenbytes, 0, lastBuffers, 0, lenbytes.Length);//第一部分 长度
            Array.Copy(buffer, 0, lastBuffers, lenbytes.Length, buffer.Length);//第二部分 内容
            return lastBuffers;
        }

        public static byte[] UnPack(byte[] buffer)
        {
            byte[] revbuffers = buffer;//临时
            int revSize = buffer.Length;
            byte[] tempbytes = new byte[4];//临时大小
            Array.Copy(revbuffers, 0, tempbytes, 0, 4);//准备大小复制好
            int yingShouSize = (int)ReverseBytes((UInt32) BitConverter.ToInt32(tempbytes, 0));//大小取出来了

            byte[] bigBuffers = new byte[yingShouSize];//应该收的大小缓存区


            Array.Copy(revbuffers, 4, bigBuffers, 0, yingShouSize);//准备第一次复制作为缓存区 
            return bigBuffers;
        }
        public static UInt32 ReverseBytes(UInt32 value)
        {
            return (value & 0x000000FFU) << 24 | (value & 0x0000FF00U) << 8 |
                   (value & 0x00FF0000U) >> 8 | (value & 0xFF000000U) >> 24;
        }
    }
}
